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

        public int BuyOffersAmount { get; set; }

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

        [NotConsumed]
        public double UpperBollingerBand { get; internal set; }

        [NotConsumed]
        public double LowerBollingerBand { get; internal set; }

        [NotConsumed]
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

        public double AroonIndicator { get; internal set; }

        public double NormalizedClosePrice { get; set; }

        public double NormalizedMediumPrice { get; internal set; }

        public double NormalizedOpenPrice { get; internal set; }

        public double NormalizedSMAIndicator { get; internal set; }

        public double NormalizedEMAIndicator { get; internal set; }

        public double NormalizedUpperBollingerBand { get; internal set; }

        public double NormalizedLowerBollingerBand { get; internal set; }
        
        public double NormalizedMiddleBollingerBand { get; internal set; }
        
        public double NormalizedMaxBuyOffer { get; internal set; }

        public double NormalizedMinSellOffer { get; internal set; }

        public double NormalizedFirstTradePrice { get; internal set; }
        

        public int BuySignal { get; internal set; }

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
