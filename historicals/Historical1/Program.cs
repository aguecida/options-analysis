using CsvHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Configuration;
using Historical1.Models;

namespace Historical1
{
    class Program
    {
        private static string spxPriceDaily = ConfigurationManager.AppSettings["spxDataFile"];
        private static string vixDaily = ConfigurationManager.AppSettings["vixDataFile"];
        private static string volumeDaily = ConfigurationManager.AppSettings["volumeDataFile"];

        static void Main(string[] args)
        {
            var spx = ReadFile<Index>(spxPriceDaily);
            var vix = ReadFile<Vix>(vixDaily);
            var vol = ReadFile<Volume>(volumeDaily);
        }

        /// <summary>
        /// Reads contents of a file into a List
        /// </summary>
        /// <typeparam name="T">Type of data file</typeparam>
        /// <param name="path">Path to a data file</param>
        /// <returns>A list of daily records</returns>
        public static IEnumerable<T> ReadFile<T>(string path)
        {
            using (var sr = new StreamReader(path))
            {
                var reader = new CsvReader(sr);
                IEnumerable<T> records = reader.GetRecords<T>().ToList();
                return records;
            }
        }
    }
}
