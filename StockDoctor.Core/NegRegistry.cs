using System;
using System.Collections.Generic;
using System.Text;

namespace StockDoctor.Core
{
    enum eNegColumnIndex
    {
        SessionDate,
        InstrumentSymbol,
        TradeNumber,
        TradePrice,
        TradedQuantity,
        TradeTime,
        TradeIndicator,
        BuyOrderDate,
        SequentialBuyOrderNumber,
        SecondaryOrderIDBuyOrder,
        AggressorBuyOrderIndicator,
        SellOrderDate,
        SequentialSellOrderNumber,
        SecondaryOrderIDSellOrder,
        AggressorSellOrderIndicator,
        CrossTradeIndicator,
        BuyMember,
        SellMember
    }

    public class NegRegistry: IStockParseable
    {
        private string[] _splitValues;

        public NegRegistry()
        {

        }

        public NegRegistry(string[] splitvalues)
        {
            if (splitvalues.Length != 18)
            {
                throw new FormatException("Wrong number of columns in Neg Constructor.");
            }

            _splitValues = splitvalues;
        }

        public string[] SplitValues {
            get {
                return _splitValues;
            }
            set {
                if (value.Length != 18)
                {
                    throw new FormatException("Wrong number of columns in Neg Constructor.");
                }
                _splitValues = value;
            }
        }

        public DateTime SessionDate => DateTime.Parse(_splitValues[(int)eNegColumnIndex.SessionDate].Trim());

        public string InstrumentSymbol => _splitValues[(int)eNegColumnIndex.InstrumentSymbol].Trim();

        public int TradeNumber => int.Parse(_splitValues[(int)eNegColumnIndex.TradeNumber].Trim());

        public double TradePrice => double.Parse(_splitValues[(int)eNegColumnIndex.TradePrice].Trim());

        public int TradedQuantity => int.Parse(_splitValues[(int)eNegColumnIndex.TradedQuantity].Trim());

        public DateTime TradeTime => DateTime.Parse(_splitValues[(int)eNegColumnIndex.BuyOrderDate] + " " +_splitValues[(int)eNegColumnIndex.TradeTime].Trim());

        public string TradeIndicator => _splitValues[(int)eNegColumnIndex.TradeIndicator].Trim();

        public DateTime BuyOrderDate => DateTime.Parse(_splitValues[(int)eNegColumnIndex.BuyOrderDate].Trim());

        public long SequentialBuyOrderNumber => long.Parse(_splitValues[(int)eNegColumnIndex.SequentialBuyOrderNumber].Trim());

        public long SecundaryBuyOrderId => long.Parse(_splitValues[(int)eNegColumnIndex.SecondaryOrderIDBuyOrder].Trim());

        public string AgressorBuyOrderIndicator => _splitValues[(int)eNegColumnIndex.AggressorBuyOrderIndicator].Trim();

        public DateTime SellOrderDate => DateTime.Parse(_splitValues[(int)eNegColumnIndex.SellOrderDate].Trim());

        public long SequentialSellOrderNumber => long.Parse(_splitValues[(int)eNegColumnIndex.SequentialSellOrderNumber].Trim());

        public long SecundarySellOrderId => long.Parse(_splitValues[(int)eNegColumnIndex.SecondaryOrderIDSellOrder].Trim());

        public string AgressorSellOrderIndicator => _splitValues[(int)eNegColumnIndex.AggressorSellOrderIndicator].Trim();

        public string CrossTradeIndicator => _splitValues[(int)eNegColumnIndex.CrossTradeIndicator].Trim();

        public int BuyMember => int.Parse(_splitValues[(int)eNegColumnIndex.BuyMember].Trim());

        public int SellMember => int.Parse(_splitValues[(int)eNegColumnIndex.SellMember].Trim());
    }
}
