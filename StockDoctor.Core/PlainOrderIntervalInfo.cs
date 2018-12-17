using System;
using System.Reflection;
using System.Text;

namespace StockDoctor.Core
{
    public class PlainOrderIntervalInfo
    {
        public DateTime Start { get; set; }

        public DateTime End { get; set; }

        public int BuyOffersAmount { get; set; }

        public int SellOffersAmount { get; set; }

        public int NegociatedOffersAmount { get; set; }

        public double MediumPrice { get; set; }

        public double MaxBuyOffer { get; set; }

        public double MinBuyOffer { get; set; }

        public double MaxSellOffer { get; set; }

        public double MinSellOffer { get; set; }

        public int TotalTradedQuantity { get; set; }

        public double RSIIndicator { get; set; }

        public double FirstTradePrice { get; set; }

        public double OpenPrice { get; set; }

        public double ClosePrice { get; set; }
        public double SMAIndicator { get; internal set; }

        public double EMAIndicator { get; internal set; }
        public double UpperBollingerBand { get; internal set; }
        public double LowerBollingerBand { get; internal set; }
        public double MiddleBollingerBand { get; internal set; }

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
