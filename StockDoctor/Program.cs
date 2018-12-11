using StockDoctor.Core;
using StockDoctor.Core.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace StockDoctor
{

    class Program
    {
        


        static void Main(string[] args)
        {

            try
            {
                var availableNegDays = Directory.GetFiles(Settings.NegFolderPath).Where(f => f.ToUpper().EndsWith(".ZIP")).Select(d => DateTime.ParseExact(d.Split(Settings.NegFilePrefix)[1].Split(".zip")[0], Settings.DateSuffixFormat, null));
                var availableCPADays = Directory.GetFiles(Settings.BuyFolderPath).Where(f => f.ToUpper().EndsWith(".ZIP")).Select(d => DateTime.ParseExact(d.Split(Settings.BuyFilePrefix)[1].Split(".zip")[0], Settings.DateSuffixFormat, null));
                var availableVDADays = Directory.GetFiles(Settings.SellFolderPath).Where(f => f.ToUpper().EndsWith(".ZIP")).Select(d => DateTime.ParseExact(d.Split(Settings.SellFilePrefix)[1].Split(".zip")[0], Settings.DateSuffixFormat, null));

                var availableDays = availableNegDays.Intersect(availableVDADays).Intersect(availableCPADays);

                Console.WriteLine("The current available days are:");
                foreach (var day in availableDays)
                {
                    Console.WriteLine(day.ToString("dd/MM/yyyy"));
                }
                Console.WriteLine($"Do you want to procceed and proccess these days for the stock {Settings.InstrumentSymbol}? (y/n)");
                var yn = Console.ReadLine();
                if (!yn.ToUpper().Trim().Equals("Y"))
                {
                    return;
                }

                foreach (var day in availableDays)
                {
                    var dateString = day.ToString(Settings.DateSuffixFormat);
                    var negfileName = Settings.NegFolderPath +  Settings.NegFilePrefix + dateString;
                    var buyfileName = Settings.BuyFolderPath + Settings.BuyFilePrefix + dateString;
                    var sellfileName = Settings.SellFolderPath + Settings.SellFilePrefix + dateString;
                    var negZipPath = negfileName + ".zip";
                    var negTxtPath = negfileName + ".TXT";
                    var buyZipPath = buyfileName + ".zip";
                    var buyTXTPath = buyfileName + ".TXT";
                    var sellZipPath = sellfileName + ".zip";
                    var sellTXTPath = sellfileName + ".TXT";

                    var zipsToExtract = new List<string>() { negZipPath, sellZipPath, buyZipPath };
                    foreach (var zipFile in zipsToExtract)
                    {
                        Console.WriteLine($"Extracting {zipFile}...");
                        var resultFolderPath = new FileInfo(zipFile).Directory.FullName;
                        ZipFile.ExtractToDirectory(zipFile, resultFolderPath);

                    }
                    var filesToDelete = new List<string>() { negTxtPath, sellTXTPath, buyTXTPath };

                    Util.ParseLineValues<NegRegistry>(negTxtPath, Util.GenericParserHandler<NegRegistry>, Util.PlanifyNegRegistry);
                    GC.Collect();

                    Util.ParseLineValues<BuyOrderRegistry>(buyTXTPath, Util.GenericParserHandler<BuyOrderRegistry>, Util.PlanifyBuyOrderRegistry);
                    GC.Collect();

                    Util.ParseLineValues<SellOrderRegistry>(sellTXTPath, Util.GenericParserHandler<SellOrderRegistry>, Util.PlanifySellOrderRegistry);
                    GC.Collect();

                    foreach (var filePath in filesToDelete)
                    {
                        Console.WriteLine($"Deleting {filePath}...");
                        if (File.Exists(filePath))
                        {
                            File.Delete(filePath);
                        }
                    }

                }

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
            catch (Exception)
            {

                throw;
            }



        }

        
    }
}
