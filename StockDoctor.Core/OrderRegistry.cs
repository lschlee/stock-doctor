using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace StockDoctor.Core
{

    enum eOrderColumnIndex
    {
        SessionDate,
        InstrumentSymbol,
        OrderSide,
        SequentialOrderNumber,
        SecondaryOrderID,
        ExecutionType,
        PriorityTime,
        PriorityIndicator,
        OrderPrice,
        TotalQuantityofOrder,
        TradedQuantityofOrder,
        OrderDate,
        OrderDatetimeentry,
        OrderStatus,
        AggressorIndicator,
        Member
    }

    public abstract class OrderRegistry: IStockParseable
    {

        private string[] _splitValues;

        public OrderRegistry()
        {

        }

        public OrderRegistry(string[] splitvalues)
        {
            if (splitvalues.Length != 16)
            {
                throw new FormatException("Wrong number of columns in Order Constructor.");
            }

            _splitValues = splitvalues;
        }

        public string[] SplitValues
        {
            get
            {
                return _splitValues;
            }
            set
            {
                if (value.Length != 16)
                {
                    throw new FormatException("Wrong number of columns in Order Constructor.");
                }
                _splitValues = value;
            }
        }

        public DateTime SessionDate => DateTime.Parse(_splitValues[(int)eOrderColumnIndex.SessionDate].Trim());

        public string InstrumentSymbol => _splitValues[(int)eOrderColumnIndex.InstrumentSymbol].Trim();

        public int OrderSide => int.Parse(_splitValues[(int)eOrderColumnIndex.OrderSide].Trim());

        public long SequentialOrderNumber => long.Parse(_splitValues[(int)eOrderColumnIndex.SequentialOrderNumber].Trim());

        public long SecondaryOrderID => long.Parse(_splitValues[(int)eOrderColumnIndex.SecondaryOrderID].Trim());

        public string ExecutionType => _splitValues[(int)eOrderColumnIndex.ExecutionType].Trim();

        public DateTime PriorityTime => DateTime.ParseExact($"{_splitValues[(int)eOrderColumnIndex.OrderDate]} {_splitValues[(int)eOrderColumnIndex.PriorityTime].Trim()}", "yyyy-MM-dd HH:mm:ss.ffffff", CultureInfo.InvariantCulture);

        public string PriorityIndicator => _splitValues[(int)eOrderColumnIndex.PriorityIndicator].Trim();

        public double OrderPrice => double.Parse(_splitValues[(int)eOrderColumnIndex.OrderPrice].Trim());


        public int TotalQuantityofOrder => int.Parse(_splitValues[(int)eOrderColumnIndex.TotalQuantityofOrder].Trim());

        public int TradedQuantityofOrder => int.Parse(_splitValues[(int)eOrderColumnIndex.TradedQuantityofOrder].Trim());

        public DateTime OrderDate => DateTime.Parse(_splitValues[(int)eOrderColumnIndex.OrderDate].Trim());

        public DateTime OrderDatetimeentry => DateTime.Parse(_splitValues[(int)eOrderColumnIndex.OrderDatetimeentry].Trim());

        public string OrderStatus => _splitValues[(int)eOrderColumnIndex.OrderStatus].Trim();

        public string AggressorIndicator => _splitValues[(int)eOrderColumnIndex.AggressorIndicator].Trim();

        public string Member => _splitValues[(int)eOrderColumnIndex.Member].Trim();

    }
}
