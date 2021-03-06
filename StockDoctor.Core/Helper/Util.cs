﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using StockDoctor.Core.Attributes;

namespace StockDoctor.Core.Helper
{
    public static class Util
    {
        private static List<PlainOrderIntervalInfo> _plainInfo;

        public static Dictionary<DateTime, Tuple<int,int>> UpDownIndexMap;

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

        public static string CSVFileName => $"{Settings.InstrumentSymbol}_{PlainInfo.First().Start.ToString("yyyy-MM-dd")}_{PlainInfo.Last().Start.ToString("yyyy-MM-dd")}.csv";

        public static DateTime CurrentDate { get; set; }

        private static Dictionary<int, char> MonthCodeMapper
        {
            get
            {
                return new Dictionary<int, char> () {
                    {1, 'G'},
                    {2, 'H'},
                    {3, 'J'},
                    {4, 'K'},
                    {5, 'M'},
                    {6, 'N'},
                    {7, 'Q'},
                    {8, 'U'},
                    {9, 'V'},
                    {10, 'X'},
                    {11, 'Z'},
                    {12, 'F'},
                };
            }
        }

        public static string StockCode
        {
            get
            {
                if (Settings.IndexStockCodeVariation)
                {
                    return Settings.InstrumentSymbol.ToUpper() + MonthCodeMapper[CurrentDate.Month] + (CurrentDate.Month == 12? CurrentDate.Year + 1 : CurrentDate.Year).ToString().Substring(2);
                }
                return Settings.InstrumentSymbol.ToUpper();
            }
        }

        public static Dictionary<DateTime, double> ClosePricesMap { get; set; }
        public static Dictionary<DateTime, double> HighPricesMap { get; set; }
        public static Dictionary<DateTime, double> LowPricesMap { get; set; }
        public static Dictionary<DateTime, double> CloseAskPricesMap { get; set; }
        public static Dictionary<DateTime, double> HighAskPricesMap { get; set; }
        public static Dictionary<DateTime, Tuple<int,int>> UpDownIndexAskMap;

        public static void ParseLineValues<T>(string fileRelativePath, Action<string[], List<T>> textValuesHandler, Action<List<T>, List<PlainOrderIntervalInfo>> resultHandler = null)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            string lineText;
            var fileName = fileRelativePath.Split("\\").Last();
            var tempList = new List<T>();

            using (FileStream fileStream = new FileStream(fileRelativePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (BufferedStream bs = new BufferedStream(fileStream))
                using (StreamReader reader = new StreamReader(bs))
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
                        if (lineText.Contains(StockCode))
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
                                MinSellOffer = sellBetweenInterval.Any() ? sellBetweenInterval.Where(b => b.OrderPrice > 0).Select(x => x.OrderPrice).Min() : 0

                            };

                            plainInfos.Add(newPlainInfo);
                        }
                        else
                        {
                            plainInfo.SellOffersAmount = sellBetweenInterval.Count;
                            plainInfo.MaxSellOffer = sellBetweenInterval.Any() ? sellBetweenInterval.Select(x => x.OrderPrice).Max() : 0;
                            plainInfo.MinSellOffer = sellBetweenInterval.Any() ? sellBetweenInterval.Where(b => b.OrderPrice > 0).Select(x => x.OrderPrice).Min() : 0;
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
                AddMediumPrice(day);
                AddRSIIndicator(day);
                AddSMA(x => x.ClosePrice, day, Settings.SMAPeriods, SetSMAIndicator);
                AddEMA(x => x.SMAIndicator, x=> x.ClosePrice, x => x.EMAIndicator, day, Settings.SMAPeriods, SetEMAIndicator); // Needs to come after SMA calculation
                AddBollingerBandsIndicators(day);
                AddMACDIndicator(day);
                NormalizingPriceValues(day);
                AddAroonIndicator(day);
                AddATR(day);
                AddDM(day);
                AddSMA(x => x.PlusDM, day, Settings.ATRPeriods, SetSMAPlusDM);
                AddSMA(x => x.MinusDM, day, Settings.ATRPeriods, SetSMAMinusDM);
                AddEMA(x => x.SMAPlusDM, x => x.PlusDM, x => x.EMAPlusDMIndicator, day, Settings.ATRPeriods, SetEMAPlusDM);
                AddEMA(x => x.SMAMinusDM, x => x.MinusDM, x => x.EMAMinusDMIndicator, day, Settings.ATRPeriods, SetEMAMinusDM);
                AddDI(day);
                AddSMA(x => x.AbsoluteDiffDI, day, Settings.ATRPeriods, SetSMADiffDI, startingPoint: Settings.ATRPeriods);
                AddEMA(x => x.SMADiffDI, x=> x.AbsoluteDiffDI, x => x.EMADiffDIIndicator, day, Settings.ATRPeriods, SetEMADiffDI, startingPoint: Settings.ATRPeriods);
                AddADX(day);
                AddCCI(day);
                AddCMO(day);
                AddROC(day);
            }

            SkippingUncalculatedIndicators();
            SetBuySignal(PlainInfo);

        }

        private static void AddROC(DateTime day)
        {
            var plainInfoForDay = PlainInfo.Where(d => d.Start.ToString("yyyyMMdd").Equals(day.ToString("yyyyMMdd"))).ToList();

            for (int i = Settings.ROCPeriods; i < plainInfoForDay.Count; i++)
            {
                var previousCloseTime = plainInfoForDay[i].End.Subtract(new TimeSpan(0, Settings.ROCPeriods, 0));
                double pastClosePrice = GetClosePrice(previousCloseTime);
                plainInfoForDay[i].ROC = (plainInfoForDay[i].ClosePrice - pastClosePrice) / pastClosePrice * 100.0;
            }
        }

        private static double GetClosePrice(DateTime closeTime)
        {
            if (ClosePricesMap.ContainsKey(closeTime))
            {
                return ClosePricesMap[closeTime];
            }
            return GetClosePrice(closeTime.Subtract(new TimeSpan(0, 1, 0)));
        }

        private static double GetCloseAskPrice(DateTime closeTime)
        {
            if (CloseAskPricesMap.ContainsKey(closeTime))
            {
                return CloseAskPricesMap[closeTime];
            }
            return GetCloseAskPrice(closeTime.Subtract(new TimeSpan(0, 1, 0)));
        }

        private static double GetMaxPrice(DateTime closeTime)
        {
            if (HighPricesMap.ContainsKey(closeTime))
            {
                return HighPricesMap[closeTime];
            }
            return 0;
        }

        private static double GetMaxAskPrice(DateTime closeTime)
        {
            if (HighAskPricesMap.ContainsKey(closeTime))
            {
                return HighAskPricesMap[closeTime];
            }
            return 0;
        }

        private static double GetMinPrice(DateTime closeTime)
        {
            if (LowPricesMap.ContainsKey(closeTime))
            {
                return LowPricesMap[closeTime];
            }
            return 0;
        }

        private static Tuple<int, int> GetUpDownIndex(DateTime closeTime)
        {
            if (UpDownIndexMap.ContainsKey(closeTime))
            {
                return UpDownIndexMap[closeTime];
            }
            return new Tuple<int, int>(int.MaxValue, int.MaxValue);
        }

        private static Tuple<int, int> GetUpDownIndexAsk(DateTime closeTime)
        {
            if (UpDownIndexAskMap.ContainsKey(closeTime))
            {
                return UpDownIndexAskMap[closeTime];
            }
            return new Tuple<int, int>(int.MaxValue, int.MaxValue);
        }

        private static void AddCMO(DateTime day)
        {
            var plainInfoForDay = PlainInfo.Where(d => d.Start.ToString("yyyyMMdd").Equals(day.ToString("yyyyMMdd"))).ToList();

            for (int i = Settings.CMOPeriods; i < plainInfoForDay.Count; i++)
            {
                var periodOfInterest = new List<PlainOrderIntervalInfo>();
                for (int j = i - Settings.CMOPeriods; j < i; j++)
                {
                    periodOfInterest.Add(plainInfoForDay[j]);
                }

                var upSum = periodOfInterest.Select(p => plainInfoForDay[i].ClosePrice - p.ClosePrice).Where(x => x > 0).Sum();
                var downSum = periodOfInterest.Select(p => p.ClosePrice - plainInfoForDay[i].ClosePrice).Where(x => x > 0).Sum();

                plainInfoForDay[i].CMO = 100.0 * ((upSum - downSum)/(upSum + downSum));

            }
        }

        private static void AddCCI(DateTime day)
        {
            var plainInfoForDay = PlainInfo.Where(d => d.Start.ToString("yyyyMMdd").Equals(day.ToString("yyyyMMdd"))).ToList();

            for (int i = Settings.SMAPeriods; i < plainInfoForDay.Count; i++)
            {
                double Sum = 0;
                var periodOfInterest = new List<PlainOrderIntervalInfo>();
                for (int j = i - Settings.SMAPeriods; j < i; j++)
                {
                    periodOfInterest.Add(plainInfoForDay[j]);
                }
                Sum = periodOfInterest.Select(x => x.ClosePrice).Sum();
                var SMA = Sum / Settings.SMAPeriods;

                //double squaredDiff = 0;
                //for (int j = i - Settings.SMAPeriods; j < i; j++)
                //{
                //    squaredDiff += Math.Pow(plainInfoForDay[j].ClosePrice - SMA, 2);
                //}
                double squaredDiff = periodOfInterest.Select(x => Math.Pow(x.ClosePrice - SMA, 2)).Sum();
                var stardardDeviation = Math.Sqrt(squaredDiff / (Settings.SMAPeriods - 1));

                var high = periodOfInterest.Select(x => x.High).Max();
                var low = periodOfInterest.Select(x => x.Low).Min();
                var close = periodOfInterest.Select(x => x.ClosePrice).Last();

                var TP = new List<double> { high, low, close }.Sum() / 3.0;

                plainInfoForDay[i].CCI = (TP - SMA) / (0.015 * stardardDeviation);

            }
        }

        private static void AddADX(DateTime day)
        {
            var plainInfoForDay = PlainInfo.Where(d => d.Start.ToString("yyyyMMdd").Equals(day.ToString("yyyyMMdd"))).ToList();
            var ATRPeriods = Settings.ATRPeriods;

            for (int i = 2 * ATRPeriods; i < plainInfoForDay.Count; i++)
            {
                plainInfoForDay[i].ADXIndicator = (100 * plainInfoForDay[i].EMADiffDIIndicator) / (plainInfoForDay[i].PlusDirectionalIndicator + plainInfoForDay[i].MinusDirectionalIndicator);
            }
        }

        private static void SetEMADiffDI(PlainOrderIntervalInfo arg1, double arg2)
        {
            arg1.EMADiffDIIndicator = arg2;
        }

        private static void SetSMADiffDI(PlainOrderIntervalInfo arg1, double arg2)
        {
            arg1.SMADiffDI = arg2;
        }

        private static void SetEMAMinusDM(PlainOrderIntervalInfo arg1, double arg2)
        {
            arg1.EMAMinusDMIndicator = arg2;
        }

        private static void SetEMAPlusDM(PlainOrderIntervalInfo arg1, double arg2)
        {
            arg1.EMAPlusDMIndicator = arg2;
        }

        private static void SetSMAMinusDM(PlainOrderIntervalInfo arg1, double arg2)
        {
            arg1.SMAMinusDM = arg2;
        }

        private static void SetSMAPlusDM(PlainOrderIntervalInfo arg1, double arg2)
        {
            arg1.SMAPlusDM = arg2;
        }

        private static void AddDI(DateTime day)
        {
            var plainInfoForDay = PlainInfo.Where(d => d.Start.ToString("yyyyMMdd").Equals(day.ToString("yyyyMMdd"))).ToList();
            var ATRPeriods = Settings.ATRPeriods;

            for (int i = ATRPeriods; i < plainInfoForDay.Count; i++)
            {
                plainInfoForDay[i].PlusDirectionalIndicator = (plainInfoForDay[i].EMAPlusDMIndicator * 100)/(plainInfoForDay[i].ATRIndicator);
                plainInfoForDay[i].MinusDirectionalIndicator = (plainInfoForDay[i].EMAMinusDMIndicator * 100)/(plainInfoForDay[i].ATRIndicator);
                plainInfoForDay[i].AbsoluteDiffDI = Math.Abs(plainInfoForDay[i].PlusDirectionalIndicator - plainInfoForDay[i].MinusDirectionalIndicator);
            }
        }

        private static void SetDMEMA(PlainOrderIntervalInfo arg1, double arg2)
        {
            throw new NotImplementedException();
        }

        private static void AddDM(DateTime day)
        {
            var plainInfoForDay = PlainInfo.Where(d => d.Start.ToString("yyyyMMdd").Equals(day.ToString("yyyyMMdd"))).ToList();

            for (int i = 1; i < plainInfoForDay.Count; i++)
            {
                var upMove = plainInfoForDay[i].High - plainInfoForDay[i -1].High;
                var downMove = plainInfoForDay[i - 1].Low - plainInfoForDay[i].Low;

                if (upMove > downMove && upMove > 0)
                {
                    plainInfoForDay[i].PlusDM = upMove;
                }

                if (downMove > upMove && downMove > 0)
                {
                    plainInfoForDay[i].MinusDM = downMove;
                }
            }         
        }

        private static void AddATR(DateTime day)
        {
            var plainInfoForDay = PlainInfo.Where(d => d.Start.ToString("yyyyMMdd").Equals(day.ToString("yyyyMMdd"))).ToList();

            var ATRPeriods = Settings.ATRPeriods;

            if (plainInfoForDay.Count >= ATRPeriods)
            {
                var firstTRInterval = plainInfoForDay.GetRange(0, ATRPeriods);
                var TRlist = new List<double>();
                for (int i = 0; i < firstTRInterval.Count; i++)
                {
                    double max = 0;
                    if (i == 0)
                    {
                        max = firstTRInterval[i].High - firstTRInterval[i].Low;
                    } else
                    {
                        max = GetTR(firstTRInterval, i);
                    }
                    TRlist.Add(max);
                }
                double lastATR = TRlist.Sum()/(double)TRlist.Count;

                for (int i = ATRPeriods; i < plainInfoForDay.Count; i++)
                {
                    plainInfoForDay[i].ATRIndicator = ( (lastATR * (double)(ATRPeriods - 1)) + GetTR(plainInfoForDay, i))/((double)ATRPeriods);
                    lastATR = plainInfoForDay[i].ATRIndicator;
                }
            }


        }

        private static double GetTR(List<PlainOrderIntervalInfo> interval, int i)
        {
            double max;
            var highLowDiff = interval[i].High - interval[i].Low;
            var highPrevCloseDiff = Math.Abs(interval[i].High - interval[i - 1].ClosePrice);
            var lowPrevCloseDiff = Math.Abs(interval[i].Low - interval[i - 1].ClosePrice);
            max = new double[] { highLowDiff, highPrevCloseDiff, lowPrevCloseDiff }.Max();
            return max;
        }

        private static void AddAroonIndicator(DateTime day)
        {
            var plainInfoForDay = PlainInfo.Where(d => d.Start.ToString("yyyyMMdd").Equals(day.ToString("yyyyMMdd"))).ToList();
            
            double aroonPeriods = Settings.AroonIndicatorPeriods;

            for (int i = (int)aroonPeriods; i < plainInfoForDay.Count; i++)
            {
                var currentPlainInfo = plainInfoForDay[i];
                var intervalOfInterest = new List<PlainOrderIntervalInfo>();
                for (int j = i - (int)aroonPeriods; j < i; j++)
                {
                    intervalOfInterest.Add(plainInfoForDay[j]);
                }
                List<double> closePrices = intervalOfInterest.Select(x => (double)x.ClosePrice).ToList();
                double maxIndex = closePrices.IndexOf(closePrices.Max());
                double minIndex = closePrices.IndexOf(closePrices.Min());

                var aroonUp = ((aroonPeriods - (aroonPeriods - maxIndex) + 1)/aroonPeriods)*100;
                var aroonDown = ((aroonPeriods - (aroonPeriods - minIndex) + 1)/aroonPeriods)*100;

                currentPlainInfo.AroonUpIndicator = aroonUp;
                currentPlainInfo.AroonDownIndicator = aroonDown;
                currentPlainInfo.AroonIndicator = aroonUp - aroonDown;

            }
        }

        private static void AddMACDIndicator(DateTime day)
        {
            AddSMA(x=> x.ClosePrice,day, Settings.ShortMACDPeriods, SetShortSMAMACD);
            AddSMA(x => x.ClosePrice,day, Settings.LongMACDPeriods, SetLongSMAMACD);
            AddEMA(x => x.ShortSMAMACD, x=> x.ClosePrice, x=> x.ShortEMAMACD, day, Settings.ShortMACDPeriods, SetShortEMAMACD);
            AddEMA(x => x.LongSMAMACD, x=> x.ClosePrice, x=> x.LongEMAMACD, day, Settings.LongMACDPeriods, SetLongEMAMACD);

            SetMACD(day);
        }

        private static void SetMACD(DateTime day)
        {
            var plainInfoForDay = PlainInfo.Where(d => d.Start.ToString("yyyyMMdd").Equals(day.ToString("yyyyMMdd"))).ToList();
            foreach (var plainInfo in plainInfoForDay)
            {
                plainInfo.MACD = plainInfo.ShortEMAMACD - plainInfo.LongEMAMACD;
            }
        }

        private static void SetLongSMAMACD(PlainOrderIntervalInfo plainInfo, double value)
        {
            plainInfo.LongSMAMACD = value;
        }

        private static void SetShortSMAMACD(PlainOrderIntervalInfo plainInfo, double value)
        {
            plainInfo.ShortSMAMACD = value;
        }

        private static void SetLongEMAMACD(PlainOrderIntervalInfo plainInfo, double value)
        {
            plainInfo.LongEMAMACD = value;
        }

        private static void SetShortEMAMACD(PlainOrderIntervalInfo plainInfo, double value)
        {
            plainInfo.ShortEMAMACD = value;
        }

        private static void NormalizingPriceValues(DateTime day)
        {
            var plainInfoForDay = PlainInfo.Where(d => d.Start.ToString("yyyyMMdd").Equals(day.ToString("yyyyMMdd"))).ToList();

            int periodsToNormalize = Settings.PeriodsToNormalize;

            for (int i = periodsToNormalize; i < plainInfoForDay.Count; i++)
            {
                var currentPlainInfo = plainInfoForDay[i];
                var intervalOfInterest = new List<PlainOrderIntervalInfo>();
                for (int j = i - periodsToNormalize; j < i; j++)
                {
                    intervalOfInterest.Add(plainInfoForDay[j]);
                }

                currentPlainInfo.NormalizedClosePrice = NormalizeValue(currentPlainInfo.ClosePrice, intervalOfInterest, (PlainOrderIntervalInfo x) => x.ClosePrice);
                currentPlainInfo.NormalizedMediumPrice = NormalizeValue(currentPlainInfo.MediumPrice, intervalOfInterest, (PlainOrderIntervalInfo x) => x.MediumPrice);
                currentPlainInfo.NormalizedOpenPrice = NormalizeValue(currentPlainInfo.OpenPrice, intervalOfInterest, (PlainOrderIntervalInfo x) => x.OpenPrice);
                currentPlainInfo.NormalizedSMAIndicator = NormalizeValue(currentPlainInfo.SMAIndicator, intervalOfInterest, (PlainOrderIntervalInfo x) => x.SMAIndicator);
                currentPlainInfo.NormalizedEMAIndicator = NormalizeValue(currentPlainInfo.EMAIndicator, intervalOfInterest, (PlainOrderIntervalInfo x) => x.EMAIndicator);
                currentPlainInfo.NormalizedUpperBollingerBand = NormalizeValue(currentPlainInfo.UpperBollingerBand, intervalOfInterest, (PlainOrderIntervalInfo x) => x.UpperBollingerBand);
                currentPlainInfo.NormalizedLowerBollingerBand = NormalizeValue(currentPlainInfo.LowerBollingerBand, intervalOfInterest, (PlainOrderIntervalInfo x) => x.LowerBollingerBand);
                currentPlainInfo.NormalizedMiddleBollingerBand = NormalizeValue(currentPlainInfo.MiddleBollingerBand, intervalOfInterest, (PlainOrderIntervalInfo x) => x.MiddleBollingerBand);
                currentPlainInfo.NormalizedMaxBuyOffer = NormalizeValue(currentPlainInfo.MaxBuyOffer, intervalOfInterest, (PlainOrderIntervalInfo x) => x.MaxBuyOffer);
                currentPlainInfo.NormalizedMinSellOffer = NormalizeValue(currentPlainInfo.MinSellOffer, intervalOfInterest, (PlainOrderIntervalInfo x) => x.MinSellOffer);
                currentPlainInfo.NormalizedFirstTradePrice = NormalizeValue(currentPlainInfo.FirstTradePrice, intervalOfInterest, (PlainOrderIntervalInfo x) => x.FirstTradePrice);
            }
        }

        private static double NormalizeValue(double currentValue, List<PlainOrderIntervalInfo> intervalOfInterest, Func<PlainOrderIntervalInfo, double> p)
        {
            var values = intervalOfInterest.Select(p).ToList();
            values.Add(currentValue);
            var maxValue = values.Max();
            var minValue = values.Min();
            var diffMaxMin = maxValue - minValue;
            var diffCurrentMin = currentValue - minValue;

            if (diffMaxMin == 0)
            {
                return 0;
            }
            return diffCurrentMin/diffMaxMin;
        }

        private static void SkippingUncalculatedIndicators()
        {
            PlainInfo = PlainInfo.Where(p => p.RSIIndicator > 0 && p.MiddleBollingerBand != 0 && p.SMAIndicator != 0 && p.LongEMAMACD != 0 && p.AroonIndicator != 0).ToList();
        }

        private static void RemovingImpredictableData()
        {
            PlainInfo = PlainInfo.GetRange(0, PlainInfo.Count - Settings.SlidingWindowMinutes);
        }


        private static double GetMaxIntervalPrice(DateTime intervalStart, DateTime intervalEnd)
        {
            var diffMin = (intervalEnd - intervalStart).TotalMinutes;
            var maxPrices = new List<double>();

            for (int min = 0; min < diffMin; min++)
            {
                maxPrices.Add(GetMaxPrice(intervalStart.AddMinutes(min)));
            }

            return maxPrices.Max();
        }


        private static double GetMinIntervalPrice(DateTime intervalStart, DateTime intervalEnd)
        {
            var diffMin = (intervalEnd - intervalStart).TotalMinutes;
            var minPrices = new List<double>();

            for (int min = 0; min < diffMin; min++)
            {
                minPrices.Add(GetMinPrice(intervalStart.AddMinutes(min)));
            }

            return minPrices.Min();
        }
        

        private static double GetMaxIntervalAskPrice(DateTime intervalStart, DateTime intervalEnd)
        {
            var diffMin = (intervalEnd - intervalStart).TotalMinutes;
            var maxPrices = new List<double>();

            for (int min = 0; min < diffMin; min++)
            {
                maxPrices.Add(GetMaxAskPrice(intervalStart.AddMinutes(min)));
            }

            return maxPrices.Max();
        }

        private static Tuple<int, int> GetUpDownIndexInterval(DateTime intervalStart, DateTime intervalEnd)
        {
            var diffMin = (intervalEnd - intervalStart).TotalMinutes;
            var allUpDown = new List<Tuple<int, int>>();

            for (int min = 0; min < diffMin; min++)
            {
                allUpDown.Add(GetUpDownIndex(intervalStart.AddMinutes(min)));
            }

            return new Tuple<int, int>(allUpDown.Select(x => x.Item1).Min(), allUpDown.Select(x => x.Item2).Min());
        }

        private static Tuple<int, int> GetUpDownIndexAskInterval(DateTime intervalStart, DateTime intervalEnd)
        {
            var diffMin = (intervalEnd - intervalStart).TotalMinutes;
            var allUpDown = new List<Tuple<int, int>>();

            for (int min = 0; min < diffMin; min++)
            {
                allUpDown.Add(GetUpDownIndexAsk(intervalStart.AddMinutes(min)));
            }

            return new Tuple<int, int>(allUpDown.Select(x => x.Item1).Min(), allUpDown.Select(x => x.Item2).Min());
        }

        private static double GetCloseAskIntervalPrice(DateTime intervalStart, DateTime intervalEnd)
        {
            var diffMin = (intervalEnd - intervalStart).TotalMinutes;
            var closePrices = new List<double>();

            for (int min = 0; min < diffMin; min++)
            {
                closePrices.Add(GetCloseAskPrice(intervalStart.AddMinutes(min)));
            }

            return closePrices.Last();
        }

        private static void SetBuySignal(List<PlainOrderIntervalInfo> planInfo)
        {
            for (int i = 0; i < planInfo.Count; i++)
            {
                try
                {

                    if (Math.Round(GetMaxIntervalPrice(planInfo[i].End, planInfo[i].End.AddMinutes(Settings.BuyTimeHold)) - planInfo[i].ClosePrice, 2) >= Settings.UpperBoundary)
                    {
                        var tuple = GetUpDownIndexInterval(planInfo[i].End, planInfo[i].End.AddMinutes(Settings.BuyTimeHold));
                        if (tuple.Item2 < tuple.Item1)
                        {
                            planInfo[i].BuySignal = 1;
                        }
                    }

                    if (Math.Round(GetMaxIntervalAskPrice(planInfo[i].End, planInfo[i].End.AddMinutes(Settings.BuyTimeHold)) - planInfo[i].BidPrice, 2) >= Settings.UpperBoundary)
                    {
                        var tuple = GetUpDownIndexAskInterval(planInfo[i].End, planInfo[i].End.AddMinutes(Settings.BuyTimeHold));
                        if (tuple.Item2 < tuple.Item1)
                        {
                            planInfo[i].Profit = Settings.UpperBoundary;
                        }
                        else
                        {
                            planInfo[i].Profit = -Settings.LowerBoundary;
                        }
                    }
                    else
                    {
                        if (Math.Round(planInfo[i].BidPrice - GetMinIntervalPrice(planInfo[i].End, planInfo[i].End.AddMinutes(Settings.BuyTimeHold)), 2) >= Settings.LowerBoundary)
                        {
                            planInfo[i].Profit = -Settings.LowerBoundary;
                        }
                        else
                        {
                            planInfo[i].Profit = GetCloseAskIntervalPrice(planInfo[i].End, planInfo[i].End.AddMinutes(Settings.BuyTimeHold)) - planInfo[i].BidPrice;
                        }
                    }

                    //if (Math.Round(GetMaxPrice(planInfo[i].End) - planInfo[i].ClosePrice,2) >= Settings.UpperBoundary)
                    //{
                    //    var tuple = GetUpDownIndex(planInfo[i].End);
                    //    if (tuple.Item2 < tuple.Item1)
                    //    {
                    //        planInfo[i].BuySignal = 1;
                    //    }
                    //}

                    //if (Math.Round(GetMaxAskPrice(planInfo[i].End) - planInfo[i].BidPrice, 2) >= Settings.UpperBoundary)
                    //{
                    //    var tuple = GetUpDownIndexAsk(planInfo[i].End);
                    //    if (tuple.Item2 < tuple.Item1)
                    //    {
                    //        planInfo[i].Profit = Settings.UpperBoundary;
                    //    }
                    //    else
                    //    {
                    //        planInfo[i].Profit = -Settings.LowerBoundary;
                    //    }
                    //}
                    //else
                    //{
                    //    if (Math.Round(planInfo[i].BidPrice - GetMinPrice(planInfo[i].End), 2) >= Settings.LowerBoundary)
                    //    {
                    //        planInfo[i].Profit = -Settings.LowerBoundary;
                    //    }
                    //    else
                    //    {
                    //        planInfo[i].Profit = GetCloseAskPrice(planInfo[i].End) - planInfo[i].BidPrice;
                    //    }
                    //}

                }
                catch (SystemException)
                {
                    continue;
                }
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

        public static void AddSMA(Func<PlainOrderIntervalInfo, double> lambda, DateTime day, int periods, Action<PlainOrderIntervalInfo, double> SMAHandler, int startingPoint = 0)
        {
            var plainInfoForDay = PlainInfo.Where(d => d.Start.ToString("yyyyMMdd").Equals(day.ToString("yyyyMMdd"))).ToList();

            for (int i = periods + startingPoint; i < plainInfoForDay.Count; i++)
            {
                double SmaSUM = 0;
                for (int j = i - periods; j < i; j++)
                {
                    SmaSUM += plainInfoForDay.GetRange(j,1).Select(lambda).Sum();
                }
                var value  = SmaSUM/ periods;
                SMAHandler(plainInfoForDay[i], value);
            }
        }

        public static void SetSMAIndicator(PlainOrderIntervalInfo plainInfo, double value) {
            plainInfo.SMAIndicator = value;
            plainInfo.SMAIndicatorDiff = plainInfo.ClosePrice - value;
        }

        public static void AddEMA(Func<PlainOrderIntervalInfo, double> baseSMA, Func<PlainOrderIntervalInfo, double> baseColumn, Func<PlainOrderIntervalInfo, double> resultEMAColumn, DateTime day, int periods, Action<PlainOrderIntervalInfo, double> EMAHandler, int startingPoint = 0)
        {

            var plainInfoForDay = PlainInfo.Where(d => d.Start.ToString("yyyyMMdd").Equals(day.ToString("yyyyMMdd"))).ToList();

            for (int i = periods + startingPoint; i < plainInfoForDay.Count; i++)
            {
                double K = 2.0 / (periods + 1.0);

                var lastPeriod = plainInfoForDay[i - 1];
                var currentInterval = plainInfoForDay.GetRange(i,1);
                var lastEMA = plainInfoForDay.GetRange(i - 1,1).Select(resultEMAColumn).First();
                var value = i == periods + startingPoint ? currentInterval.Select(baseSMA).First() : (K * (currentInterval.Select(baseColumn).First() - lastEMA)) + lastEMA;

                EMAHandler(plainInfoForDay[i], value);

            }
        }

        public static void SetEMAIndicator(PlainOrderIntervalInfo plainInfo, double value) {
            plainInfo.EMAIndicator = value;
            plainInfo.EMAIndicatorDiff = plainInfo.ClosePrice - value;
        } 

        public static void AddBollingerBandsIndicators(DateTime day)
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

                plainInfoForDay[i].UpperBollingerBand = SMA + (2 * stardardDeviation) - plainInfoForDay[i].ClosePrice;
                plainInfoForDay[i].LowerBollingerBand = SMA - (2 * stardardDeviation) - plainInfoForDay[i].ClosePrice;
                plainInfoForDay[i].MiddleBollingerBand = SMA - plainInfoForDay[i].ClosePrice;

            }
        }

        private static void SetMediumPrice(PlainOrderIntervalInfo plainInfo)
        {
            plainInfo.MediumPrice = (plainInfo.MaxBuyOffer + plainInfo.MinSellOffer) / 2;
        }

        public static void WriteCsv()
        {
            var plainInfoProps = typeof(PlainOrderIntervalInfo).GetProperties();

            var headerLine = string.Join(Settings.CsvCharSeparator, plainInfoProps.Where(p => !System.Attribute.IsDefined(p, typeof(NotConsumed))).Select(pi => pi.Name));
            var lines = new List<string>(new string[] { headerLine });

            foreach (var info in PlainInfo)
            {
                string lineString = "";
                foreach (var propInfo in plainInfoProps)
                {
                    if (!System.Attribute.IsDefined(propInfo, typeof(NotConsumed)))
                    {
                        lineString = $"{lineString}{Settings.CsvCharSeparator}{propInfo.GetValue(info)}";
                    }
                }
                lines.Add(lineString.Substring(1));
            }

            File.WriteAllLines(Settings.OutputCsvPath + CSVFileName, lines.ToArray());
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
                                MinBuyOffer = buyBetweenInterval.Any() ? buyBetweenInterval.Where(b => b.OrderPrice > 0).Select(x => x.OrderPrice).Min() : 0

                            };

                            plainInfos.Add(newPlainInfo);
                        }
                        else
                        {
                            plainInfo.BuyOffersAmount = buyBetweenInterval.Count;
                            plainInfo.MaxBuyOffer = buyBetweenInterval.Any() ? buyBetweenInterval.Select(x => x.OrderPrice).Max() : 0;
                            plainInfo.MinBuyOffer = buyBetweenInterval.Any() ? buyBetweenInterval.Where(b => b.OrderPrice > 0).Select(x => x.OrderPrice).Min() : 0;
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
                    //nextTimeTest = nextTimeTest.AddMinutes(1);
                    nextTimeTest = new DateTime(negRegistries[i].TradeTime.Year, negRegistries[i].TradeTime.Month, negRegistries[i].TradeTime.Day, negRegistries[i].TradeTime.Hour, negRegistries[i].TradeTime.Minute, 0).AddMinutes(1);
                }
            }

            var interpolationPace = 0;
            if (Settings.InterpolateWindows)
            {
                interpolationPace = 1;
            } else
            {
                interpolationPace = Settings.SlidingWindowMinutes;
            }

            for (int i = 0; i < endIndexIntervals.Count - Settings.SlidingWindowMinutes; i++)
            {
                var negBetweenInterval = negRegistries.GetRange(endIndexIntervals[i], endIndexIntervals[i + 1] - endIndexIntervals[i]);
                var lastNeg = negBetweenInterval.Last();
                var firstNeg = negBetweenInterval.First();
                var endTimeInterval = new DateTime(lastNeg.TradeTime.Year, lastNeg.TradeTime.Month, lastNeg.TradeTime.Day, lastNeg.TradeTime.Hour, lastNeg.TradeTime.Minute, 0);
                var startTimeInterval = new DateTime(firstNeg.TradeTime.Year, firstNeg.TradeTime.Month, firstNeg.TradeTime.Day, firstNeg.TradeTime.Hour, firstNeg.TradeTime.Minute, 0);

                if (!ClosePricesMap.ContainsKey(endTimeInterval))
                {
                    ClosePricesMap.Add(endTimeInterval, negBetweenInterval.Last().TradePrice);
                }

                var agressiveBuys = negBetweenInterval.Where(n => n.AgressorBuyOrderIndicator == "1" && n.AgressorSellOrderIndicator == "2");
                if (!CloseAskPricesMap.ContainsKey(endTimeInterval))
                {

                    if (!agressiveBuys.Any())
                    {
                        CloseAskPricesMap.Add(endTimeInterval, ClosePricesMap[ClosePricesMap.Keys.Last()]);
                    }
                    else
                    {
                        CloseAskPricesMap.Add(endTimeInterval, agressiveBuys.Select(n => n.TradePrice).Last());
                    }
                }

                if (!HighPricesMap.ContainsKey(endTimeInterval))
                {
                    HighPricesMap.Add(endTimeInterval, negBetweenInterval.Select(n => n.TradePrice).Max());
                }

                if (!HighAskPricesMap.ContainsKey(endTimeInterval))
                {
                    if (!agressiveBuys.Any())
                    {
                        HighAskPricesMap.Add(endTimeInterval, 0);
                    }
                    else
                    {
                        HighAskPricesMap.Add(endTimeInterval, agressiveBuys.Select(n => n.TradePrice).Max());
                    }
                }

                if (!LowPricesMap.ContainsKey(endTimeInterval))
                {
                    if (!agressiveBuys.Any())
                    {
                        LowPricesMap.Add(endTimeInterval, 0);
                    }
                    else
                    {
                        LowPricesMap.Add(endTimeInterval, agressiveBuys.Select(n => n.TradePrice).Min());
                    }
                }

                if (i!= 0)
                {

                    var firstDown = negBetweenInterval.FirstOrDefault(n => Math.Round(GetClosePrice(endTimeInterval.Subtract(new TimeSpan(0, 1, 0))) - n.TradePrice, 2) >= Settings.LowerBoundary);
                    var firstUp = negBetweenInterval.FirstOrDefault(n => Math.Round(n.TradePrice - GetClosePrice(endTimeInterval.Subtract(new TimeSpan(0, 1, 0))), 2) >= Settings.UpperBoundary);
                    var firstDownIndex = firstDown != null ? negBetweenInterval.IndexOf(firstDown) : int.MaxValue;
                    var firstUpIndex = firstUp != null ? negBetweenInterval.IndexOf(firstUp) : int.MaxValue;

                    if (!UpDownIndexMap.ContainsKey(endTimeInterval))
                    {
                        UpDownIndexMap.Add(endTimeInterval, new Tuple<int, int>(firstDownIndex, firstUpIndex));
                    }

                    firstDown = agressiveBuys.FirstOrDefault(n => Math.Round(GetClosePrice(endTimeInterval.Subtract(new TimeSpan(0, 1, 0))) - n.TradePrice,2) >= Settings.LowerBoundary);
                    firstUp = agressiveBuys.FirstOrDefault(n => Math.Round(n.TradePrice - GetClosePrice(endTimeInterval.Subtract(new TimeSpan(0, 1, 0))),2) >= Settings.UpperBoundary);
                    firstDownIndex = firstDown != null ? negBetweenInterval.IndexOf(firstDown) : int.MaxValue; 
                    firstUpIndex = firstUp != null ? negBetweenInterval.IndexOf(firstUp) : int.MaxValue;

                    if (!UpDownIndexAskMap.ContainsKey(endTimeInterval))
                    {
                        UpDownIndexAskMap.Add(endTimeInterval, new Tuple<int, int>(firstDownIndex, firstUpIndex));
                    }
                }

            }

            for (int i = 0; i < endIndexIntervals.Count; i = i + interpolationPace)
            {
                if (i + Settings.SlidingWindowMinutes < endIndexIntervals.Count)
                {
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    var negBetweenInterval = negRegistries.GetRange(endIndexIntervals[i], endIndexIntervals[i + Settings.SlidingWindowMinutes] - endIndexIntervals[i]);
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
                                FirstTradePrice = negBetweenInterval.First().TradePrice,
                                High = negBetweenInterval.Select(n => n.TradePrice).Max(),
                                Low = negBetweenInterval.Select(n => n.TradePrice).Min(),
                                BidPrice = negBetweenInterval.Where(n => n.AgressorBuyOrderIndicator == "2" && n.AgressorSellOrderIndicator == "1").LastOrDefault()?.TradePrice ?? plainInfos.Last().BidPrice
                            };

                            plainInfos.Add(newPlainInfo);
                        }
                        else
                        {
                            plainInfo.NegociatedOffersAmount = negBetweenInterval.Count;
                            plainInfo.TotalTradedQuantity += negRegistries.Select(x => x.TradedQuantity).Sum();
                            plainInfo.ClosePrice = negBetweenInterval.Last().TradePrice;
                            plainInfo.FirstTradePrice = negBetweenInterval.First().TradePrice;
                            plainInfo.High = negBetweenInterval.Select(n => n.TradePrice).Max();
                            plainInfo.Low = negBetweenInterval.Select(n => n.TradePrice).Min();
                            plainInfo.BidPrice = negBetweenInterval.Where(n => n.AgressorBuyOrderIndicator == "2" && n.AgressorSellOrderIndicator == "1").LastOrDefault()?.TradePrice ?? plainInfos.Last().BidPrice;
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
