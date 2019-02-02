using System;
using System.Reflection;
using System.Text;
using StockDoctor.Core.Attributes;

namespace StockDoctor.Core
{
    public class PlainOrderIntervalInfo
    {
        public DateTime Start { get; set; }

        public DateTime End { get; set; }

        [NotConsumed]
        public int BuyOffersAmount { get; set; }

        [NotConsumed]
        public int SellOffersAmount { get; set; }

        public int NegociatedOffersAmount { get; set; }

        [NotConsumed]
        public double MediumPrice { get; set; }

        [NotConsumed]
        public double MaxBuyOffer { get; set; }

        [NotConsumed]
        public double MinBuyOffer { get; set; }

        [NotConsumed]
        public double MaxSellOffer { get; set; }

        [NotConsumed]
        public double MinSellOffer { get; set; }

        public int TotalTradedQuantity { get; set; }

        public double RSIIndicator { get; set; }

        [NotConsumed]
        public double FirstTradePrice { get; set; }

        [NotConsumed]
        public double OpenPrice { get; set; }

        [NotConsumed]
        public double ClosePrice { get; set; }

        [NotConsumed]
        public double SMAIndicator { get; internal set; }

        [NotConsumed]
        public double EMAIndicator { get; internal set; }

        public double SMAIndicatorDiff { get; internal set; }

        public double EMAIndicatorDiff { get; internal set; }

        public double UpperBollingerBand { get; internal set; }

        public double LowerBollingerBand { get; internal set; }

        public double MiddleBollingerBand { get; internal set; }

        [NotConsumed]
        public double ShortSMAMACD { get; set; }

        [NotConsumed]
        public double LongSMAMACD { get; set; }

        [NotConsumed]
        public double ShortEMAMACD { get; set; }

        [NotConsumed]
        public double LongEMAMACD { get; set; }

        public double MACD { get; set; }

        public double AroonUpIndicator { get; internal set; }

        public double AroonDownIndicator { get; internal set; }

        public double AroonIndicator { get; internal set; }

        [NotConsumed]
        public double NormalizedClosePrice { get; set; }

        [NotConsumed]
        public double NormalizedMediumPrice { get; internal set; }

        [NotConsumed]
        public double NormalizedOpenPrice { get; internal set; }

        [NotConsumed]
        public double NormalizedSMAIndicator { get; internal set; }

        [NotConsumed]
        public double NormalizedEMAIndicator { get; internal set; }

        [NotConsumed]
        public double NormalizedUpperBollingerBand { get; internal set; }

        [NotConsumed]
        public double NormalizedLowerBollingerBand { get; internal set; }

        [NotConsumed]
        public double NormalizedMiddleBollingerBand { get; internal set; }

        [NotConsumed]
        public double NormalizedMaxBuyOffer { get; internal set; }

        [NotConsumed]
        public double NormalizedMinSellOffer { get; internal set; }

        [NotConsumed]
        public double NormalizedFirstTradePrice { get; internal set; }


        [NotConsumed]
        public double High { get; internal set; }

        [NotConsumed]
        public double Low { get; internal set; }

        [NotConsumed]
        public double ATRIndicator { get; internal set; }

        [NotConsumed]
        public double PlusDM { get; internal set; }

        [NotConsumed]
        public double MinusDM { get; internal set; }

        [NotConsumed]
        public double SMAPlusDM { get; internal set; }

        [NotConsumed]
        public double SMAMinusDM { get; internal set; }

        [NotConsumed]
        public double EMAPlusDMIndicator { get; internal set; }

        [NotConsumed]
        public double EMAMinusDMIndicator { get; internal set; }

        public double PlusDirectionalIndicator { get; internal set; }

        public double MinusDirectionalIndicator { get; internal set; }

        [NotConsumed]
        public double AbsoluteDiffDI { get; internal set; }

        [NotConsumed]
        public double SMADiffDI { get; internal set; }

        [NotConsumed]
        public double EMADiffDIIndicator { get; set; }

        public double ADXIndicator { get; set; }

        public double CCI { get; internal set; }

        public double CMO { get; internal set; }

        public double ROC { get; internal set; }

        public int BuySignal { get; set; }

        private PropertyInfo[] _PropertyInfos = null;

        public override string ToString()
        {
            if (_PropertyInfos == null)
                _PropertyInfos = this.GetType().GetProperties();

            var sb = new StringBuilder();

            foreach (var info in _PropertyInfos)
            {
                var value = info.GetValue(this, null) ?? "(null)";
                sb.AppendLine(info.Name + ": " + value.ToString());
            }

            return sb.ToString();
        }

    }
}
