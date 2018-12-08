using System;
using System.Collections.Generic;
using System.Text;

namespace StockDoctor.Core
{
    public class SellOrderRegistry : OrderRegistry
    {
        private string[] _splitValues;

        public SellOrderRegistry()
        {

        }

        public SellOrderRegistry(string[] splitvalues) : base(splitvalues)
        {
            if (splitvalues.Length != 16)
            {
                throw new FormatException("Wrong number of columns in SellOrderRegistry Constructor.");
            }

            _splitValues = splitvalues;
        }
    }
}
