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
                            Console.Write($"\rParsed {(dividedBy20.IndexOf(currentLine) + 1) * 5}% of {fileName}");
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
            var newRegistry = new T
            {
                SplitValues = textValues
            };
            tempList.Add(newRegistry);
        }

        public static void PlanifySellOrderRegistry(List<SellOrderRegistry> sellOrderRegistries, List<PlainOrderIntervalInfo> plainInfos)
        {
            sellOrderRegistries = sellOrderRegistries.OrderBy(x => x.PriorityTime).ToList();
            var minSellEntrytime = sellOrderRegistries.First().PriorityTime;

            var minDatetime = minSellEntrytime;

            DateTime startTime = new DateTime(minDatetime.Year, minDatetime.Month, minDatetime.Day, minDatetime.Hour, minDatetime.Minute, 0);

            var endIndexIntervals = new List<int>() { 0 };
            var nextTimeTest = startTime.AddMinutes(1);
            for (int i = 0; i < sellOrderRegistries.Count; i++)
            {
                if (sellOrderRegistries[i].PriorityTime.Ticks >= nextTimeTest.Ticks)
                {
                    endIndexIntervals.Add(i);
                    nextTimeTest = nextTimeTest.AddMinutes(1);
                }
            }

            for (int i = 0; i < endIndexIntervals.Count; i++)
            {
                if (i + Settings.SlidingWindowMinutes < endIndexIntervals.Count)
                {
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    var sellBetweenInterval = sellOrderRegistries.GetRange(endIndexIntervals[i], endIndexIntervals[i + Settings.SlidingWindowMinutes] - endIndexIntervals[i] - 1);
                    watch.Stop();
                    var elapsedTimeSpan = TimeSpan.FromMilliseconds(watch.ElapsedMilliseconds);

                    if (sellBetweenInterval.Any())
                    {
                        var startTimeInterval = new DateTime(sellBetweenInterval.First().PriorityTime.Year, sellBetweenInterval.First().PriorityTime.Month, sellBetweenInterval.First().PriorityTime.Day, sellBetweenInterval.First().PriorityTime.Hour, sellBetweenInterval.First().PriorityTime.Minute, 0);
                        var endTimeInterval = new DateTime(sellBetweenInterval.Last().PriorityTime.Year, sellBetweenInterval.Last().PriorityTime.Month, sellBetweenInterval.Last().PriorityTime.Day, sellBetweenInterval.Last().PriorityTime.Hour, sellBetweenInterval.Last().PriorityTime.Minute, 0).AddMinutes(1);
                        var plainInfo = plainInfos.FirstOrDefault(p => p.Start == startTimeInterval && p.End == endTimeInterval);

                        if (plainInfo == null)
                        {
                            var newPlainInfo = new PlainOrderIntervalInfo()
                            {
                                Start = startTimeInterval,
                                End = endTimeInterval,
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

                        Console.Write($"\rPlanified Sell data between {startTimeInterval.ToString("dd/MM/yyyy HH:mm:ss")} and {endTimeInterval.ToString("dd/MM/yyyy HH:mm:ss")}.");
                    }

                }

            }
        }

        public static void TreatPlainData(IEnumerable<DateTime> availableDays)
        {
            EnsureNegotiationsPerInterval(PlainInfo);

            var orderedDays = availableDays.OrderBy(d => d);
            Console.WriteLine("Adding indicators...");

            foreach (var day in orderedDays)
            {
                Util.AddMediumPrice(day);
                Util.AddRSIIndicator(day);
                Util.AddSMAIndicator(day);
                Util.AddEMAIndicator(day); // Needs to come after SMA calculation
                Util.AddBollingerBandsIndicator(day);
            }


        }

        private static void EnsureNegotiationsPerInterval(List<PlainOrderIntervalInfo> plainInfo)
        {
            PlainInfo = PlainInfo.Where(p => p.NegociatedOffersAmount > 0).OrderBy(p => p.Start).ToList();
        }

        public static void AddMediumPrice(DateTime day)
        {

            var plainInfoForDay = PlainInfo.Where(d => d.Start.ToString("yyyyMMdd").Equals(day.ToString("yyyyMMdd"))).ToList();

            for (int i = 0; i < plainInfoForDay.Count; i++)
            {
                SetMediumPrice(plainInfoForDay[i]);

                if (i == 0)
                {
                    plainInfoForDay[i].OpenPrice = plainInfoForDay[i].FirstTradePrice;
                }
                else
                {
                    plainInfoForDay[i].OpenPrice = plainInfoForDay[i - 1].ClosePrice;
                }
            }
        }

        public static void AddRSIIndicator(DateTime day)
        {

            var plainInfoForDay = PlainInfo.Where(d => d.Start.ToString("yyyyMMdd").Equals(day.ToString("yyyyMMdd"))).ToList();

            for (int i = Settings.RSIPeriods; i < plainInfoForDay.Count; i++)
            {
                var gainsDiffs = new List<double>();
                var lossesDiffs = new List<double>();
                for (int j = i - Settings.RSIPeriods; j < i ; j++)
                {
                    var actualPlainInfo = plainInfoForDay[j];
                    if (actualPlainInfo.ClosePrice > actualPlainInfo.OpenPrice)
                    {
                        gainsDiffs.Add(actualPlainInfo.ClosePrice - actualPlainInfo.OpenPrice);
                    }
                    else
                    {
                        lossesDiffs.Add(actualPlainInfo.OpenPrice - actualPlainInfo.ClosePrice);
                    }
                }
                plainInfoForDay[i].RSIIndicator = (100 - 100/(1 + (gainsDiffs.Sum()/lossesDiffs.Sum())))/100;
            }
        }

        public static void AddSMAIndicator(DateTime day)
        {
            var plainInfoForDay = PlainInfo.Where(d => d.Start.ToString("yyyyMMdd").Equals(day.ToString("yyyyMMdd"))).ToList();

            for (int i = Settings.SMAPeriods; i < plainInfoForDay.Count; i++)
            {
                double SmaSUM = 0;
                for (int j = i - Settings.RSIPeriods; j < i; j++)
                {
                    SmaSUM += plainInfoForDay[j].ClosePrice;
                }
                plainInfoForDay[i].SMAIndicator = SmaSUM/ Settings.SMAPeriods;
            }
        }

        public static void AddEMAIndicator(DateTime day)
        {

            var plainInfoForDay = PlainInfo.Where(d => d.Start.ToString("yyyyMMdd").Equals(day.ToString("yyyyMMdd"))).ToList();

            for (int i = Settings.SMAPeriods; i < plainInfoForDay.Count; i++)
            {
                double K = 2.0 / (Settings.SMAPeriods + 1.0);

                var lastPeriod = plainInfoForDay[i - 1];

                plainInfoForDay[i].EMAIndicator = i == Settings.SMAPeriods? plainInfoForDay[i].SMAIndicator : (K * (plainInfoForDay[i].ClosePrice - lastPeriod.EMAIndicator)) + lastPeriod.EMAIndicator;

            }
        }

        public static void AddBollingerBandsIndicator(DateTime day)
        {

            var plainInfoForDay = PlainInfo.Where(d => d.Start.ToString("yyyyMMdd").Equals(day.ToString("yyyyMMdd"))).ToList();

            for (int i = Settings.BollingerBandsPeriods; i < plainInfoForDay.Count; i++)
            {
                double Sum = 0;
                for (int j = i - Settings.BollingerBandsPeriods; j < i; j++)
                {
                    Sum += plainInfoForDay[j].ClosePrice;
                }
                var SMA = Sum / Settings.BollingerBandsPeriods;

                double squaredDiff = 0;
                for (int j = i - Settings.BollingerBandsPeriods; j < i; j++)
                {
                    squaredDiff += Math.Pow(plainInfoForDay[j].ClosePrice - SMA, 2);
                }
                var stardardDeviation = Math.Sqrt(squaredDiff / (Settings.BollingerBandsPeriods - 1));

                plainInfoForDay[i].UpperBollingerBand = SMA + (2 * stardardDeviation);
                plainInfoForDay[i].LowerBollingerBand = SMA - (2 * stardardDeviation);
                plainInfoForDay[i].MiddleBollingerBand = SMA;


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

        public static void PlanifyBuyOrderRegistry(List<BuyOrderRegistry> buyOrderRegistries, List<PlainOrderIntervalInfo> plainInfos)
        {
            buyOrderRegistries = buyOrderRegistries.OrderBy(x => x.PriorityTime).ToList();
            var minBuyEntrytime = buyOrderRegistries.First().PriorityTime;

            var minDatetime = minBuyEntrytime;

            DateTime startTime = new DateTime(minDatetime.Year, minDatetime.Month, minDatetime.Day, minDatetime.Hour, minDatetime.Minute, 0);

            var endIndexIntervals = new List<int>() { 0 };
            var nextTimeTest = startTime.AddMinutes(1);
            for (int i = 0; i < buyOrderRegistries.Count; i++)
            {
                if (buyOrderRegistries[i].PriorityTime.Ticks >= nextTimeTest.Ticks)
                {
                    endIndexIntervals.Add(i);
                    nextTimeTest = nextTimeTest.AddMinutes(1);
                }
            }

            for (int i = 0; i < endIndexIntervals.Count; i++)
            {
                if (i + Settings.SlidingWindowMinutes < endIndexIntervals.Count)
                {
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    var buyBetweenInterval = buyOrderRegistries.GetRange(endIndexIntervals[i], endIndexIntervals[i + Settings.SlidingWindowMinutes] - endIndexIntervals[i] - 1);
                    watch.Stop();
                    var elapsedTimeSpan = TimeSpan.FromMilliseconds(watch.ElapsedMilliseconds);

                    if (buyBetweenInterval.Any())
                    {
                        var startTimeInterval = new DateTime(buyBetweenInterval.First().PriorityTime.Year, buyBetweenInterval.First().PriorityTime.Month, buyBetweenInterval.First().PriorityTime.Day, buyBetweenInterval.First().PriorityTime.Hour, buyBetweenInterval.First().PriorityTime.Minute, 0);
                        var endTimeInterval = new DateTime(buyBetweenInterval.Last().PriorityTime.Year, buyBetweenInterval.Last().PriorityTime.Month, buyBetweenInterval.Last().PriorityTime.Day, buyBetweenInterval.Last().PriorityTime.Hour, buyBetweenInterval.Last().PriorityTime.Minute, 0).AddMinutes(1);
                        var plainInfo = plainInfos.FirstOrDefault(p => p.Start == startTimeInterval && p.End == endTimeInterval);

                        if (plainInfo == null)
                        {
                            var newPlainInfo = new PlainOrderIntervalInfo()
                            {
                                Start = startTimeInterval,
                                End = endTimeInterval,
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

                        Console.Write($"\rPlanified Buy data between {startTimeInterval.ToString("dd/MM/yyyy HH:mm:ss")} and {endTimeInterval.ToString("dd/MM/yyyy HH:mm:ss")}.");
                    }

                }

            }
        }

        public static void PlanifyNegRegistry(List<NegRegistry> negRegistries, List<PlainOrderIntervalInfo> plainInfos)
        {

            negRegistries = negRegistries.OrderBy(x => x.TradeTime).ToList();
            var minNegEntrytime = negRegistries.First().TradeTime;

            var minDatetime = minNegEntrytime;

            DateTime startTime = new DateTime(minDatetime.Year, minDatetime.Month, minDatetime.Day, minDatetime.Hour, minDatetime.Minute, 0);

            var endIndexIntervals = new List<int>() { 0 };
            var nextTimeTest = startTime.AddMinutes(1);
            for (int i = 0; i < negRegistries.Count; i++)
            {
                if (negRegistries[i].TradeTime.Ticks >= nextTimeTest.Ticks)
                {
                    endIndexIntervals.Add(i);
                    nextTimeTest = nextTimeTest.AddMinutes(1);
                }
            }

            for (int i = 0; i < endIndexIntervals.Count; i++)
            {
                if (i + Settings.SlidingWindowMinutes < endIndexIntervals.Count)
                {
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    var negBetweenInterval = negRegistries.GetRange(endIndexIntervals[i], endIndexIntervals[i + Settings.SlidingWindowMinutes] - endIndexIntervals[i] - 1);
                    watch.Stop();
                    var elapsedTimeSpan = TimeSpan.FromMilliseconds(watch.ElapsedMilliseconds);

                    if (negBetweenInterval.Any())
                    {
                        var startTimeInterval = new DateTime(negBetweenInterval.First().TradeTime.Year, negBetweenInterval.First().TradeTime.Month, negBetweenInterval.First().TradeTime.Day, negBetweenInterval.First().TradeTime.Hour, negBetweenInterval.First().TradeTime.Minute, 0);
                        var endTimeInterval = new DateTime(negBetweenInterval.Last().TradeTime.Year, negBetweenInterval.Last().TradeTime.Month, negBetweenInterval.Last().TradeTime.Day, negBetweenInterval.Last().TradeTime.Hour, negBetweenInterval.Last().TradeTime.Minute, 0).AddMinutes(1);
                        var plainInfo = plainInfos.FirstOrDefault(p => p.Start == startTimeInterval && p.End == endTimeInterval);

                        if (plainInfo == null)
                        {
                            var newPlainInfo = new PlainOrderIntervalInfo()
                            {
                                Start = startTimeInterval,
                                End = endTimeInterval,
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

                        Console.Write($"\rPlanified Neg data between {startTimeInterval.ToString("dd/MM/yyyy HH:mm:ss")} and {endTimeInterval.ToString("dd/MM/yyyy HH:mm:ss")}.");
                    }

                }

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
