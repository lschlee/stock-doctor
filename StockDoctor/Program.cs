﻿using StockDoctor.Core;
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
                var availableNegDays = Directory.GetFiles(Settings.NegFolderPath).Where(f => f.Contains(Settings.NegFilePrefix) && f.ToUpper().EndsWith(".ZIP")).Select(d => DateTime.ParseExact(d.Split(Settings.NegFilePrefix)[1].Split(".zip")[0], Settings.DateSuffixFormat, null));
                var availableCPADays = Directory.GetFiles(Settings.BuyFolderPath).Where(f => f.Contains(Settings.BuyFilePrefix) && f.ToUpper().EndsWith(".ZIP")).Select(d => DateTime.ParseExact(d.Split(Settings.BuyFilePrefix)[1].Split(".zip")[0], Settings.DateSuffixFormat, null));
                var availableVDADays = Directory.GetFiles(Settings.SellFolderPath).Where(f => f.Contains(Settings.SellFilePrefix) && f.ToUpper().EndsWith(".ZIP")).Select(d => DateTime.ParseExact(d.Split(Settings.SellFilePrefix)[1].Split(".zip")[0], Settings.DateSuffixFormat, null));

                var availableDays = availableNegDays;
                
                if (Settings.ConsiderOrderFiles)
                {
                    availableDays = availableDays.Intersect(availableVDADays).Intersect(availableCPADays);
                }
                availableDays = availableDays.OrderBy(d => d); 

                Console.WriteLine("The current available days are:");
                foreach (var day in availableDays)
                {
                    Console.WriteLine(day.ToString("dd/MM/yyyy"));
                }
                if (false)
                {
                    Console.WriteLine($"Do you want to procceed and proccess these days for the stock {Settings.InstrumentSymbol}? (y/n)");
                    var yn = Console.ReadLine();
                    if (!yn.ToUpper().Trim().Equals("Y"))
                    {
                        return;
                    }
                }

                Util.ClosePricesMap = new Dictionary<DateTime, double>();
                Util.HighPricesMap = new Dictionary<DateTime, double>();
                Util.LowPricesMap = new Dictionary<DateTime, double>();
                Util.CloseAskPricesMap = new Dictionary<DateTime, double>();
                Util.HighAskPricesMap = new Dictionary<DateTime, double>();
                Util.UpDownIndexMap = new Dictionary<DateTime, Tuple<int, int>>();
                Util.UpDownIndexAskMap = new Dictionary<DateTime, Tuple<int, int>>();
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

                    var zipsToExtract = new List<string>() { negZipPath};
                    if (Settings.ConsiderOrderFiles)
                    {
                        zipsToExtract.AddRange(new string[] {buyZipPath, sellZipPath});
                    }
                    foreach (var zipFile in zipsToExtract)
                    {
                        Console.WriteLine($"Extracting {zipFile}...");
                        var resultFolderPath = new FileInfo(zipFile).Directory.FullName;
                        ZipFile.ExtractToDirectory(zipFile, resultFolderPath);

                    }
                    var filesToDelete = new List<string>() { negTxtPath };

                    
                    Util.CurrentDate = day;
                    Util.ParseLineValues<NegRegistry>(negTxtPath, Util.GenericParserHandler<NegRegistry>, Util.PlanifyNegRegistry);
                    GC.Collect();

                    if (Settings.ConsiderOrderFiles)
                    {
                        filesToDelete.AddRange(new string[] { sellTXTPath, buyTXTPath });
                        Util.ParseLineValues<BuyOrderRegistry>(buyTXTPath, Util.GenericParserHandler<BuyOrderRegistry>, Util.PlanifyBuyOrderRegistry);
                        GC.Collect();

                        Util.ParseLineValues<SellOrderRegistry>(sellTXTPath, Util.GenericParserHandler<SellOrderRegistry>, Util.PlanifySellOrderRegistry);
                        GC.Collect();
                    }

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
                Util.TreatPlainData(availableDays);

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
