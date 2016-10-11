using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
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
            if (spx.First().Date != vix.First().Date && spx.First().Date != vol.First().Date && spx.First().Date != vix.First().Date)
            {
                Console.WriteLine("ERROR: Start date of records do not match.");
            }

            // Calculate VROC for each data entry
            for (var i = 0; i < vol.Count(); i++)
            {
                if (i < param.VrocPeriod)
                    continue;

                double vroc = GetVROC(vol, i, param.VrocPeriod);

                if (vroc > param.VrocThreshold)
                {
                    Console.WriteLine("VROC at {0} on {1}", vroc, vol.ElementAt(i).Date);
                }
            }

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

        /// <summary>
        /// Calculates the colume rate of change over n periods
        /// </summary>
        /// <param name="vol">Volume data</param>
        /// <param name="index">Current index of volume data</param>
        /// <param name="period">Period, in days, for calculating rate of change</param>
        /// <returns></returns>
        public static double GetVROC(IEnumerable<Volume> vol, int index, int period)
        {
            var currVol = (double)vol.ElementAt(index).TotalVolume;
            var prevVol = (double)vol.ElementAt(index - period).TotalVolume;

            return ((currVol - prevVol) / prevVol) * 100;
        }

        public static AnalysisParameters ParseInputArguments(string[] args)
        {
            AnalysisParameters param = new AnalysisParameters();

            // Get VROC period
            if (args.Length > 0)
            {
                param.VrocPeriod = Convert.ToInt32(args[0]);
            }

            // Get VROC threshold
            if (args.Length > 1)
            {
                param.VrocThreshold = Convert.ToDouble(args[1]);
            }

            Console.WriteLine("\nAnalysis parameters:");
            Console.WriteLine("VROC period (in days) = {0}", param.VrocPeriod);
            Console.WriteLine("VROC threshold (percentage) = {0}\n", param.VrocThreshold);

            return param;
        }
    }
}
