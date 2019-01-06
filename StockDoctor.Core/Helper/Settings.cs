using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace StockDoctor.Core.Helper
{
    public static class Settings
    {

        private static IConfiguration _configuration;

        private static IConfiguration Configuration { get {
                if (_configuration != null)
                {
                    return _configuration;
                }

                var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

                _configuration = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", true, true)
                    .Build();

                return _configuration;
            } }

        public static int SlidingWindowMinutes => int.Parse(Configuration["slidingWindowMinutes"]);

        public static string NegFolderPath => Configuration["negFolderPath"];

        public static string BuyFolderPath => Configuration["buyFolderPath"];

        public static string SellFolderPath => Configuration["sellFolderPath"];

        public static string InstrumentSymbol => Configuration["instrumentSymbol"];

        public static int RSIPeriods => int.Parse(Configuration["RSIPeriods"]);

        public static int SMAPeriods => int.Parse(Configuration["SMAPeriods"]);

        public static string NegFilePrefix => Configuration["negFilePrefix"];

        public static string BuyFilePrefix => Configuration["buyFilePrefix"];

        public static string SellFilePrefix => Configuration["sellFilePrefix"];

        public static string DateSuffixFormat => Configuration["dateSuffixFormat"];

        public static int BollingerBandsPeriods => int.Parse(Configuration["BollingerBandsPeriods"]);

        public static int BuyTimeHold => int.Parse(Configuration["BuyTimeHold"]);
    }
}
