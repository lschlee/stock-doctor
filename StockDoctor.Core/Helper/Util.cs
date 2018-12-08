using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace StockDoctor.Core.Helper
{
    public static class Util
    {
        private static List<PlainOrderIntervalInfo> _plainInfo;
        public static List<PlainOrderIntervalInfo> PlainInfo
        {
            get
            {
                if (_plainInfo == null)
                {
                    _plainInfo = new List<PlainOrderIntervalInfo>();
                }
                return _plainInfo;
            }
            set
            {
                _plainInfo = value;
            }
        }

        public static string CSVFileName => $"{Settings.InstrumentSymbol}_{PlainInfo.First().Start.ToString("yyyy-MM-dd")}.csv";

        public static void ParseLineValues<T>(string fileRelativePath, Action<string[], List<T>> textValuesHandler, Action<List<T>, List<PlainOrderIntervalInfo>> resultHandler = null)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            string lineText;
            var fileName = fileRelativePath.Split("\\").Last();
            var tempList = new List<T>();

            using (FileStream fileStream = new FileStream(fileRelativePath, FileMode.Open))
            {
                using (StreamReader reader = new StreamReader(fileStream))
                {
                    var currentLine = 0;
                    var totalLines = int.Parse(reader.ReadLine().Split(" ").Last());
                    var dividedBy20 = new List<int>();
                    for (int i = 1; i <= 20; i++)
                    {
                        dividedBy20.Add(totalLines/20 * i);
                    }

                    while ((lineText = reader.ReadLine()) != null)
                    {
                        if (lineText.Contains(Settings.InstrumentSymbol.ToUpper()))
                        {

                            var textValues = lineText.Split(";");


                            textValuesHandler(textValues, tempList);
                        }

                        currentLine++;
                        if (dividedBy20.Contains(currentLine))
                        {
                            //Console.WriteLine($"Parsed {(dividedBy20.IndexOf(currentLine) + 1) * 5}% of {fileName}");
                            Console.Write($"\r  Parsed {(dividedBy20.IndexOf(currentLine) + 1) * 5}% of {fileName}");
                        }
                    }
                }
            }
            Console.WriteLine("");

            resultHandler?.Invoke(tempList, PlainInfo);

            watch.Stop();
            var elapsedTimeSpan = TimeSpan.FromMilliseconds(watch.ElapsedMilliseconds);
            Console.WriteLine($"Last {elapsedTimeSpan.Hours} hours, {elapsedTimeSpan.Minutes} minutes and {elapsedTimeSpan.Seconds} seconds plaining {fileRelativePath}.");


        }

        public static void GenericParserHandler<T>(string[] textValues, List<T> tempList) where T : IStockParseable, new()
        {
            var newRegistry = new T();
            newRegistry.SplitValues = textValues;
            tempList.Add(newRegistry);
        }

        public static void PlanifySellOrderRegistry(List<SellOrderRegistry> sellOrderRegistries, List<PlainOrderIntervalInfo> plainInfos)
        {
            var minSellEntrytime = sellOrderRegistries.OrderBy(x => x.PriorityTime).First().PriorityTime;

            var maxSellEntrytime = sellOrderRegistries.OrderByDescending(x => x.PriorityTime).First().PriorityTime;

            var minDatetime = minSellEntrytime;
            var maxDatetime = maxSellEntrytime;

            DateTime startTime = new DateTime(minDatetime.Year, minDatetime.Month, minDatetime.Day, minDatetime.Hour, minDatetime.Minute, 0);
            DateTime endTime = new DateTime(maxDatetime.Year, maxDatetime.Month, maxDatetime.Day, maxDatetime.Hour, maxDatetime.Minute, 0);
            endTime = endTime.AddMinutes(1);

            var currentTime = startTime;
            var plainInfoList = new List<PlainOrderIntervalInfo>();

            while (currentTime < endTime)
            {
                var slidingWindowEnd = currentTime.AddMinutes(Settings.SlidingWindowMinutes);

                var sellBetweenInterval = sellOrderRegistries.Where(x => x.PriorityTime >= currentTime && x.PriorityTime <= slidingWindowEnd).ToList();


                if (sellBetweenInterval.Any())
                {

                    var plainInfo = plainInfos.FirstOrDefault(p => p.Start == currentTime && p.End == slidingWindowEnd);

                    if (plainInfo == null)
                    {
                        var newPlainInfo = new PlainOrderIntervalInfo()
                        {
                            Start = currentTime,
                            End = slidingWindowEnd,
                            SellOffersAmount = sellBetweenInterval.Count,
                            MaxSellOffer = sellBetweenInterval.Any() ? sellBetweenInterval.Select(x => x.OrderPrice).Max() : 0,
                            MinSellOffer = sellBetweenInterval.Any() ? sellBetweenInterval.Select(x => x.OrderPrice).Min() : 0

                        };

                        plainInfos.Add(newPlainInfo);
                    }
                    else
                    {
                        plainInfo.SellOffersAmount = sellBetweenInterval.Count;
                        plainInfo.MaxSellOffer = sellBetweenInterval.Any() ? sellBetweenInterval.Select(x => x.OrderPrice).Max() : 0;
                        plainInfo.MinSellOffer = sellBetweenInterval.Any() ? sellBetweenInterval.Select(x => x.OrderPrice).Min() : 0;
                    }

                }

                Console.Write($"\r  Planified Sell data between {currentTime.ToString("dd/MM/yyyy HH:mm:ss")} and {slidingWindowEnd.ToString("dd/MM/yyyy HH:mm:ss")}.");

                var nextTime = currentTime.AddMinutes(1);
                var intervalCount = sellBetweenInterval.Count;
                var remainingCount = sellOrderRegistries.Count;
                if (remainingCount > intervalCount)
                {
                    sellOrderRegistries = sellOrderRegistries.GetRange(intervalCount + 1, remainingCount - intervalCount - 1);
                }

                currentTime = nextTime;
            }
            Console.WriteLine("");
        }

        public static void TreatPlainData()
        {
            for (int i = 0; i < PlainInfo.Count; i++)
            {
                SetMediumPrice(PlainInfo[i]);

                if (i == 0)
                {
                    PlainInfo[i].OpenPrice = PlainInfo[i].FirstTradePrice;
                }
                else
                {
                    PlainInfo[i].OpenPrice = PlainInfo[i - 1].ClosePrice;
                }
            }
        }

        public static void AddRSIIndicator()
        {
            for (int i = Settings.RSIPeriods; i < PlainInfo.Count; i++)
            {
                var gainsDiffs = new List<double>();
                var lossesDiffs = new List<double>();
                for (int j = i - Settings.RSIPeriods; j < i ; j++)
                {
                    var actualPlainInfo = PlainInfo[j];
                    if (actualPlainInfo.ClosePrice > actualPlainInfo.OpenPrice)
                    {
                        gainsDiffs.Add(actualPlainInfo.ClosePrice - actualPlainInfo.OpenPrice);
                    }
                    else
                    {
                        lossesDiffs.Add(actualPlainInfo.OpenPrice - actualPlainInfo.ClosePrice);
                    }
                }
                PlainInfo[i].RSIIndicator = (100 - 100/(1 + (gainsDiffs.Sum()/lossesDiffs.Sum())))/100;
            }
        }

        public static void AddSMAIndicator()
        {
            for (int i = Settings.SMAPeriods; i < PlainInfo.Count; i++)
            {
                double SmaSUM = 0;
                for (int j = i - Settings.RSIPeriods; j < i; j++)
                {
                    SmaSUM += PlainInfo[j].ClosePrice;
                }
                PlainInfo[i].SMAIndicator = SmaSUM/ Settings.SMAPeriods;
            }
        }

        public static void AddEMAIndicator()
        {
            for (int i = Settings.SMAPeriods; i < PlainInfo.Count; i++)
            {
                double K = 2.0 / (Settings.SMAPeriods + 1.0);

                var lastPeriod = PlainInfo[i - 1];

                PlainInfo[i].EMAIndicator = i == Settings.SMAPeriods? PlainInfo[i].SMAIndicator : (K * (PlainInfo[i].ClosePrice - lastPeriod.EMAIndicator)) + lastPeriod.EMAIndicator;

            }
        }

        private static void SetMediumPrice(PlainOrderIntervalInfo plainInfo)
        {
            plainInfo.MediumPrice = (plainInfo.MaxBuyOffer + plainInfo.MinSellOffer) / 2;
        }

        public static void WriteCsv()
        {
            var plainInfoProps = typeof(PlainOrderIntervalInfo).GetProperties();

            var headerLine = string.Join(";", plainInfoProps.Select(pi => pi.Name));
            var lines = new List<string>(new string[] { headerLine });

            foreach (var info in PlainInfo)
            {
                string lineString = "";
                foreach (var propInfo in plainInfoProps)
                {
                    lineString = $"{lineString};{propInfo.GetValue(info)}";
                }
                lines.Add(lineString.Substring(1));
            }

            File.WriteAllLines(CSVFileName, lines.ToArray());
        }

        public static void OrderPlainData()
        {
            PlainInfo = PlainInfo.OrderBy(p => p.Start).ToList();
        }

        public static void PlanifyBuyOrderRegistry(List<BuyOrderRegistry> buyOrderRegistries, List<PlainOrderIntervalInfo> plainInfos)
        {
            var minBuyEntrytime = buyOrderRegistries.OrderBy(x => x.PriorityTime).First().PriorityTime;

            var maxBuyEntrytime = buyOrderRegistries.OrderByDescending(x => x.PriorityTime).First().PriorityTime;

            var minDatetime = minBuyEntrytime;
            var maxDatetime = maxBuyEntrytime;

            DateTime startTime = new DateTime(minDatetime.Year, minDatetime.Month, minDatetime.Day, minDatetime.Hour, minDatetime.Minute, 0);
            DateTime endTime = new DateTime(maxDatetime.Year, maxDatetime.Month, maxDatetime.Day, maxDatetime.Hour, maxDatetime.Minute, 0);
            endTime = endTime.AddMinutes(1);

            var currentTime = startTime;
            var plainInfoList = new List<PlainOrderIntervalInfo>();

            while (currentTime < endTime)
            {
                var slidingWindowEnd = currentTime.AddMinutes(Settings.SlidingWindowMinutes);

                var buyBetweenInterval = buyOrderRegistries.Where(x => x.PriorityTime >= currentTime && x.PriorityTime <= slidingWindowEnd).ToList();


                if (buyBetweenInterval.Any())
                {

                    var plainInfo = plainInfos.FirstOrDefault(p => p.Start == currentTime && p.End == slidingWindowEnd);

                    if (plainInfo == null)
                    {
                        var newPlainInfo = new PlainOrderIntervalInfo()
                        {
                            Start = currentTime,
                            End = slidingWindowEnd,
                            BuyOffersAmount = buyBetweenInterval.Count,
                            MaxBuyOffer = buyBetweenInterval.Any() ? buyBetweenInterval.Select(x => x.OrderPrice).Max() : 0,
                            MinBuyOffer = buyBetweenInterval.Any() ? buyBetweenInterval.Select(x => x.OrderPrice).Min() : 0

                        };

                        plainInfos.Add(newPlainInfo);
                    }
                    else
                    {
                        plainInfo.BuyOffersAmount = buyBetweenInterval.Count;
                        plainInfo.MaxBuyOffer = buyBetweenInterval.Any() ? buyBetweenInterval.Select(x => x.OrderPrice).Max() : 0;
                        plainInfo.MinBuyOffer = buyBetweenInterval.Any() ? buyBetweenInterval.Select(x => x.OrderPrice).Min() : 0;
                    }

                }

                Console.Write($"\r  Planified Buy data between {currentTime.ToString("dd/MM/yyyy HH:mm:ss")} and {slidingWindowEnd.ToString("dd/MM/yyyy HH:mm:ss")}.");
                var nextTime = currentTime.AddMinutes(1);

                var intervalCount = buyBetweenInterval.Count;
                var remainingCount = buyOrderRegistries.Count;
                if (remainingCount > intervalCount)
                {
                    buyOrderRegistries = buyOrderRegistries.GetRange(intervalCount + 1, remainingCount - intervalCount - 1);
                }

                currentTime = nextTime;
            }
            Console.WriteLine("");
        }

        public static void PlanifyNegRegistry(List<NegRegistry> negRegistries, List<PlainOrderIntervalInfo> plainInfos)
        {
            var minNegEntrytime = negRegistries.OrderBy(x => x.TradeTime).First().TradeTime;

            var maxNegEntrytime = negRegistries.OrderByDescending(x => x.TradeTime).First().TradeTime;

            DateTime startTime = new DateTime(minNegEntrytime.Year, minNegEntrytime.Month, minNegEntrytime.Day, minNegEntrytime.Hour, minNegEntrytime.Minute, 0);
            DateTime endTime = new DateTime(maxNegEntrytime.Year, maxNegEntrytime.Month, maxNegEntrytime.Day, maxNegEntrytime.Hour, maxNegEntrytime.Minute, 0);
            endTime = endTime.AddMinutes(1);

            var currentTime = startTime;

            while (currentTime < endTime)
            {
                var slidingWindowEnd = currentTime.AddMinutes(Settings.SlidingWindowMinutes);

                var negBetweenInterval = negRegistries.Where(x => x.TradeTime >= currentTime && x.TradeTime <= slidingWindowEnd).ToList();


                if (negBetweenInterval.Any())
                {

                    var plainInfo = plainInfos.FirstOrDefault(p => p.Start == currentTime && p.End == slidingWindowEnd);

                    if (plainInfo == null)
                    {
                        var newPlainInfo = new PlainOrderIntervalInfo()
                        {
                            Start = currentTime,
                            End = slidingWindowEnd,
                            NegociatedOffersAmount = negBetweenInterval.Count,
                            TotalTradedQuantity = negBetweenInterval.Select(x => x.TradedQuantity).Sum(),
                            ClosePrice = negBetweenInterval.Last().TradePrice,
                            FirstTradePrice = negBetweenInterval.First().TradePrice

                        };

                        plainInfos.Add(newPlainInfo);
                    }
                    else
                    {
                        plainInfo.NegociatedOffersAmount = negBetweenInterval.Count;
                        plainInfo.TotalTradedQuantity += negRegistries.Select(x => x.TradedQuantity).Sum();
                        plainInfo.ClosePrice = negBetweenInterval.Last().TradePrice;
                        plainInfo.FirstTradePrice = negBetweenInterval.First().TradePrice;
                    }

                }

                Console.Write($"\r  Planified Neg data between {currentTime.ToString("dd/MM/yyyy HH:mm:ss")} and {slidingWindowEnd.ToString("dd/MM/yyyy HH:mm:ss")}.");
                var nextTime = currentTime.AddMinutes(1);
                var intervalCount = negBetweenInterval.Count;
                var remainingCount = negRegistries.Count;
                if (remainingCount > intervalCount)
                {
                    negRegistries = negRegistries.GetRange(intervalCount + 1, remainingCount - intervalCount - 1);
                }

                currentTime = nextTime;

            }
            Console.WriteLine("");
        }

        [Obsolete("This method requires over 16GB of memory too run", true)]
        public static IEnumerable<PlainOrderIntervalInfo> MountMassivePlainData(List<NegRegistry> negRegistries, List<BuyOrderRegistry> buyOrderRegistries, List<SellOrderRegistry> sellOrderRegistries)
        {

            var minNegEntrytime = negRegistries.OrderBy(x => x.TradeTime).First().TradeTime;
            var minBuyEntrytime = buyOrderRegistries.OrderBy(x => x.OrderDatetimeentry).First().OrderDatetimeentry;
            var minSellEntrytime = sellOrderRegistries.OrderBy(x => x.OrderDatetimeentry).First().OrderDatetimeentry;

            var maxNegEntrytime = negRegistries.OrderByDescending(x => x.TradeTime).First().TradeTime;
            var maxBuyEntrytime = buyOrderRegistries.OrderByDescending(x => x.OrderDatetimeentry).First().OrderDatetimeentry;
            var maxSellEntrytime = sellOrderRegistries.OrderByDescending(x => x.OrderDatetimeentry).First().OrderDatetimeentry;

            var minDatetime = new DateTime(Math.Min(Math.Min(minNegEntrytime.Ticks, minBuyEntrytime.Ticks), minSellEntrytime.Ticks));
            var maxDatetime = new DateTime(Math.Max(Math.Max(maxNegEntrytime.Ticks, maxBuyEntrytime.Ticks), maxSellEntrytime.Ticks));

            DateTime startTime = new DateTime(minDatetime.Year, minDatetime.Month, minDatetime.Day, minDatetime.Hour, minDatetime.Minute, 0);
            DateTime endTime = new DateTime(maxDatetime.Year, maxDatetime.Month, maxDatetime.Day, maxDatetime.Hour, maxDatetime.Minute, 0);
            endTime = endTime.AddMinutes(1);

            var currentTime = startTime;
            var plainInfoList = new List<PlainOrderIntervalInfo>();

            while (currentTime < endTime)
            {
                var nextTime = currentTime.AddMinutes(10);

                var negBetweenInterval = negRegistries.Where(x => x.TradeTime >= currentTime && x.TradeTime <= nextTime).ToList();
                var buyBetweenInterval = buyOrderRegistries.Where(x => x.OrderDatetimeentry >= currentTime && x.OrderDatetimeentry <= nextTime).ToList();
                var sellBetweenInterval = sellOrderRegistries.Where(x => x.OrderDatetimeentry >= currentTime && x.OrderDatetimeentry <= nextTime).ToList();

                var plainOrderIntervalInfo = new PlainOrderIntervalInfo()
                {
                    Start = currentTime,
                    End = nextTime,
                    BuyOffersAmount = buyBetweenInterval.Count,
                    SellOffersAmount = sellBetweenInterval.Count,
                    NegociatedOffersAmount = negBetweenInterval.Count,
                    MediumPrice = sellBetweenInterval.Any() && buyBetweenInterval.Any() ? (sellBetweenInterval.Select(x => x.OrderPrice).Min() + buyBetweenInterval.Select(x => x.OrderPrice).Max()) / 2 : 0,
                    MaxBuyOffer = buyBetweenInterval.Any() ? buyBetweenInterval.Select(x => x.OrderPrice).Max() : 0,
                    MinBuyOffer = buyBetweenInterval.Any() ? buyBetweenInterval.Select(x => x.OrderPrice).Min() : 0,
                    MaxSellOffer = sellBetweenInterval.Any() ? sellBetweenInterval.Select(x => x.OrderPrice).Max() : 0,
                    MinSellOffer = sellBetweenInterval.Any() ? sellBetweenInterval.Select(x => x.OrderPrice).Min() : 0,
                    TotalTradedQuantity = negRegistries.Any() ? negRegistries.Select(x => x.TradedQuantity).Sum() : 0

                };

                plainInfoList.Add(plainOrderIntervalInfo);
                currentTime = nextTime;

            }


            return plainInfoList.Where(x => x.BuyOffersAmount + x.SellOffersAmount + x.NegociatedOffersAmount > 0);
        }
    }
}
