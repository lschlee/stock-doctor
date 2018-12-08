using StockDoctor.Core;
using StockDoctor.Core.Helper;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StockDoctor
{

    class Program
    {
        


        static void Main(string[] args)
        {
            
           
            Util.ParseLineValues<NegRegistry>(Settings.NegFilePath, Util.GenericParserHandler<NegRegistry>, Util.PlanifyNegRegistry);
            GC.Collect();

            Util.ParseLineValues<BuyOrderRegistry>(Settings.BuyFilePath, Util.GenericParserHandler<BuyOrderRegistry>, Util.PlanifyBuyOrderRegistry);
            GC.Collect();

            Util.ParseLineValues<SellOrderRegistry>(Settings.SellFilePath, Util.GenericParserHandler<SellOrderRegistry>, Util.PlanifySellOrderRegistry);
            GC.Collect();

            

            Console.WriteLine("Ordering plain data...");
            Util.OrderPlainData();

            Console.WriteLine("Treating plain data...");
            Util.TreatPlainData();

            Console.WriteLine("Adding indicators...");
            Util.AddRSIIndicator();
            Util.AddSMAIndicator();
            Util.AddEMAIndicator(); // Needs to come after SMA calculation

            Console.WriteLine("Writting to .csv");
            Util.WriteCsv();
            Console.WriteLine($"Wrote data in {Util.CSVFileName}.");

        }

        
    }
}
