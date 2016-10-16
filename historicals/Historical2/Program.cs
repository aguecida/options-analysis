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
            var spx = ReadFile<Index>(SpxPriceDaily).OrderBy(x => x.Date);

            int? expirationDay = null;
            Index expiration = null;
            Index expirationThursday = null;
            Index start = null;
            double periodHigh = 0;
            double periodLow = 0;
            bool foundFirstExpiration = false;
            bool foundExpiration = false;

            foreach (var day in spx)
            {
                if (day.Date.Day >= 1 && day.Date.Day <= 7)
                {
                    // Reset foundExpiration flag during first week of the month
                    foundExpiration = false;
                }

                if (expirationDay != null)
                {
                    start = day;
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

                // Capture the Thursday before the third Friday of the month (in case of holiday)
                if (day.Date.Day >= 14 && day.Date.Day <= 20)
                {
                    if (day.Date.DayOfWeek == DayOfWeek.Thursday)
                    {
                        expirationThursday = day;
                    }
                }

                if (day.Date.Day >= 15 && day.Date.Day <= 21)
                {
                    if (day.Date.DayOfWeek == DayOfWeek.Friday)
                    {
                        foundExpiration = true;
                        expiration = day;
                        expirationDay = day.Date.Day;

                        if (foundFirstExpiration)
                        {
                            Console.WriteLine("Expiration Date: {0}, Start Date: {1}, Opening Price: {2}, High: {3}, Low: {4}, Expiry Price: {5}, Spread: {6}",
                                expiration.Date.ToString("dd/MM/yyyy"), start.Date.ToString("dd/MM/yyyy"), Math.Round(start.Open, 2), Math.Round(periodHigh, 2), Math.Round(periodLow, 2), Math.Round(expiration.Open, 2), Math.Round(Math.Abs(expiration.Open - start.Open), 2));
                        }
                        else
                        {
                            foundFirstExpiration = true;
                        }
                    }
                }

                // If we are passed the third week and still have not found the Friday expiration (it was a holiday), expiration was on the preceeding Thursday
                if (day.Date.Day > 21 && !foundExpiration)
                {
                    foundExpiration = true;
                    expirationDay = day.Date.Day;

                    if (foundFirstExpiration)
                    {
                        Console.WriteLine("Expiration Date: {0}, Start Date: {1}, Opening Price: {2}, High: {3}, Low: {4}, Expiry Price: {5}, Spread: {6}",
                            expirationThursday.Date.ToString("dd/MM/yyyy"), start.Date.ToString("dd/MM/yyyy"), Math.Round(start.Open, 2), Math.Round(periodHigh, 2), Math.Round(periodLow, 2), Math.Round(expirationThursday.Open, 2), Math.Round(Math.Abs(expirationThursday.Open - start.Open), 2));
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
            Console.ReadLine();
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
