using System;
using System.Collections.Generic;
using System.Text;

namespace StockDoctor.Core
{
    public interface IStockParseable
    {
        string[] SplitValues { get; set; }
    }
}
