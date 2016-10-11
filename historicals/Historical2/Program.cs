using CsvHelper;
using Historical2.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Historical2
{
    class Program
    {
        private static string spxPriceDaily = ConfigurationManager.AppSettings["spxDataFile"];

        static void Main(string[] args)
        {
            Console.WriteLine("Starting analysis.");

            var watch = Stopwatch.StartNew();

            // Parse analysis arguments
            AnalysisParameters param = ParseInputArguments(args);

            // Read data files into Lists
            var spx = ReadFile<Index>(spxPriceDaily).OrderBy(x => x.Date);

            // Make sure data files are starting at a common date
            //if (spx.First().Date != vix.First().Date && spx.First().Date != vol.First().Date && spx.First().Date != vix.First().Date)
            //{
            //    Console.WriteLine("ERROR: Start date of records do not match.");
            //}
            int count = 0;

            for (var i = 0; i < spx.Count(); i++)
            {
                if (DateTime.Compare(spx.ElementAt(i).Date, param.StartDate) < 0)
                    continue;

                count++;
            }

            Console.WriteLine("{0}", count);

            watch.Stop();

            Console.WriteLine("Analysis complete. Total execution time: {0}ms", watch.ElapsedMilliseconds);
            Console.WriteLine("\nPress any key to exit.");
            Console.ReadKey();
        }

        /// <summary>
        /// Reads contents of a csv file into a List
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

        public static AnalysisParameters ParseInputArguments(string[] args)
        {
            AnalysisParameters param = new AnalysisParameters();

            // Get Start date of analysis
            if (args.Length > 0)
            {
                param.StartDate = Convert.ToDateTime(args[0]);
            }


            Console.WriteLine("\nAnalysis parameters:");
            Console.WriteLine("Start date = {0}", param.StartDate);

            return param;
        }

    }
}
