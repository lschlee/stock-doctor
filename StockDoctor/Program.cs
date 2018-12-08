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
            
            var watch = System.Diagnostics.Stopwatch.StartNew();
           
            Util.ParseLineValues<NegRegistry>(Settings.NegFilePath, Util.GenericParserHandler<NegRegistry>, Util.PlanifyNegRegistry);
            GC.Collect();

            Util.ParseLineValues<BuyOrderRegistry>(Settings.BuyFilePath, Util.GenericParserHandler<BuyOrderRegistry>, Util.PlanifyBuyOrderRegistry);
            GC.Collect();

            Util.ParseLineValues<SellOrderRegistry>(Settings.SellFilePath, Util.GenericParserHandler<SellOrderRegistry>, Util.PlanifySellOrderRegistry);
            GC.Collect();

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            Console.WriteLine($"Elaspsed time: {elapsedMs} ms");

            Console.WriteLine("Treating plain data...");

            Util.TreatPlainData();
            Console.WriteLine("Adding indicators...");
            Util.AddRSIIndicator();

            Console.WriteLine("Writting to .csv");
            Util.WriteCsv();
            Console.WriteLine($"Wrote data in {Util.CSVFileName}.");
            Console.Read();

        }

        
    }
}
