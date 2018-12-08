using System;
using System.Collections.Generic;
using System.Text;

namespace StockDoctor.Core
{
    public class BuyOrderRegistry : OrderRegistry
    {
        private string[] _splitValues;

        public BuyOrderRegistry()
        {

        }

        public BuyOrderRegistry(string[] splitvalues) : base(splitvalues)
        {
            if (splitvalues.Length != 16)
            {
                throw new FormatException("Wrong number of columns in BuyOrderRegistry Constructor.");
            }

            _splitValues = splitvalues;
        }
    }
}

