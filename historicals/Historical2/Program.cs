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
        private static readonly string SpxPriceDaily = ConfigurationManager.AppSettings["spxDataFile"];

        static void Main(string[] args)
        {
            Console.WriteLine("Starting analysis");

            var watch = Stopwatch.StartNew();

            // Read data files into Lists
            var spx = ReadFile<Index>(SpxPriceDaily);

            int? expirationDay = null;
            Index expiration = null;
            Index open = null;
            double periodHigh = 0;
            double periodLow = 0;
            bool foundFirstExpiration = false;

            foreach (var day in spx)
            {
                if (expirationDay != null)
                {
                    open = day;
                    expirationDay = null;
                    periodHigh = day.High;
                    periodLow = day.Low;
                }
                else
                {
                    if (day.High > periodHigh)
                    {
                        periodHigh = day.High;
                    }

                    if (day.Low < periodLow)
                    {
                        periodLow = day.Low;
                    }
                }

                if (day.Date.Day >= 15 && day.Date.Day <= 21 && day.Date.DayOfWeek == DayOfWeek.Friday)
                {
                    expiration = day;
                    expirationDay = day.Date.Day;

                    if (foundFirstExpiration)
                    {
                        Console.WriteLine("Expiration: {0}, High: {1}, Low: {2}, Spread: {3}",
                            expiration.Date.ToString("dd/MM/yyyy"), Math.Round(periodHigh, 2), Math.Round(periodLow, 2), Math.Round(periodHigh - periodLow, 2));
                    }
                    else
                    {
                        foundFirstExpiration = true;
                    }
                }
            }

            watch.Stop();

            Console.WriteLine("Analysis complete. Total execution time: {0}ms", watch.ElapsedMilliseconds);
            Console.WriteLine("\nPress any key to exit");
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
    }
}
