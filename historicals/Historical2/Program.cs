using CsvHelper;
using Historical2.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Historical2
{
    class Program
    {
        private static readonly string SpxPriceDaily = ConfigurationManager.AppSettings["spxDataFile"];
        private static readonly string SpxSettlePrices = ConfigurationManager.AppSettings["spxSettleFile"];

        // Analysis stats
        private static int _totalExpirations;
        private static int _totalPositiveSpreads;
        private static int _spreadsUnder100;
        private static int _spreadsUnder80;
        private static int _spreadsUnder60;
        private static int _spreadsUnder40;
        private static int _spreadsUnder20;

        static void Main(string[] args)
        {
            Console.WriteLine("Starting analysis");

            var watch = Stopwatch.StartNew();

            AnalysisParameters parameters = new AnalysisParameters();

            // Read data files into Lists
            var spx = ReadFile<Index>(SpxPriceDaily).OrderBy(x => x.Date);
            var settlePrices = ReadFile<SettlePrices>(SpxSettlePrices).OrderBy(x => x.Date);

            int? expirationDay = null;
            Index expiration;
            Index expirationThursday = null;
            Index start = null;
            Index periodHigh = null;
            Index periodLow = null;
            bool foundFirstExpiration = false;
            bool foundExpiration = false;
            Index biggestMove = null;
            double biggestMoveAmount = 0;

            foreach (var day in spx)
            {
                if (DateTime.Compare(day.Date, parameters.StartDate) < 0)
                {
                    continue;
                }

                if (day.Date.Day >= 1 && day.Date.Day <= 7)
                {
                    // Reset foundExpiration flag during first week of the month
                    foundExpiration = false;
                }

                if (expirationDay != null)
                {
                    // Symbolizes first day after expiration
                    start = day;
                    expirationDay = null;
                    periodHigh = day;
                    periodLow = day;
                    biggestMove = day;
                    biggestMoveAmount = Math.Abs(day.Open - day.Close);
                }
                else
                {
                    if (periodHigh == null || day.High > periodHigh.High)
                    {
                        periodHigh = day;
                    }

                    if (periodLow == null || day.Low < periodLow.Low)
                    {
                        periodLow = day;
                    }

                    if (Math.Abs(day.Open - day.Close) > biggestMoveAmount)
                    {
                        biggestMove = day;
                        biggestMoveAmount = Math.Abs(day.Open - day.Close);
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

                // Try to find third Friday of the week (expiration day)
                if (day.Date.Day >= 15 && day.Date.Day <= 21)
                {
                    if (day.Date.DayOfWeek == DayOfWeek.Friday)
                    {
                        foundExpiration = true;
                        expiration = day;
                        expirationDay = day.Date.Day;

                        if (foundFirstExpiration)
                        {
                            double expirePrice = settlePrices.Single(x => x.Date == expiration.Date).SettlePrice;
                            double spread = Math.Round(expirePrice - start.Open, 2);
                            UpdateStats(spread);
                            PrintIntervalStats(expiration, start, periodHigh, periodLow, biggestMove, expirePrice, biggestMoveAmount);
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
                    if (expirationThursday == null)
                    {
                        Console.WriteLine("Error in data: Could not find Thursday or Friday expiration");
                        throw new Exception("Error in data: Could not find Thursday or Friday expiration");
                    }

                    foundExpiration = true;
                    expirationDay = day.Date.Day;

                    if (foundFirstExpiration)
                    {
                        double expirePrice = settlePrices.Single(x => x.Date == expirationThursday.Date).SettlePrice;
                        double spread = Math.Round(expirePrice - start.Open, 2);
                        UpdateStats(spread);
                        PrintIntervalStats(expirationThursday, start, periodHigh, periodLow, biggestMove, expirePrice, biggestMoveAmount);
                    }
                    else
                    {
                        foundFirstExpiration = true;
                    }
                }
            }

            watch.Stop();

            PrintFinalStats();

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
        private static IEnumerable<T> ReadFile<T>(string path)
        {
            using (var sr = new StreamReader(path))
            {
                var reader = new CsvReader(sr);
                IEnumerable<T> records = reader.GetRecords<T>().ToList();
                return records;
            }
        }

        /// <summary>
        /// Update the running total of analysis stats
        /// </summary>
        /// <param name="spread">Price spread for the current interval</param>
        private static void UpdateStats(double spread)
        {
            _totalExpirations++;

            double absoluteSpread = Math.Abs(spread);

            if (spread > 0)
            {
                _totalPositiveSpreads++;
            }

            if (absoluteSpread < 100)
            {
                _spreadsUnder100++;
            }

            if (absoluteSpread < 80)
            {
                _spreadsUnder80++;
            }

            if (absoluteSpread < 60)
            {
                _spreadsUnder60++;
            }

            if (absoluteSpread < 40)
            {
                _spreadsUnder40++;
            }

            if (absoluteSpread < 20)
            {
                _spreadsUnder20++;
            }
        }

        /// <summary>
        /// Print results for the current interval
        /// </summary>
        /// <param name="expiration">Expiration day</param>
        /// <param name="intervalStart">Start day</param>
        /// <param name="intervalHigh">Day when the interval high was recorded</param>
        /// <param name="intervalLow">Day when the interval low was recorded</param>
        /// <param name="biggestMove">Day when the biggest move in the interval was recorded</param>
        /// <param name="expirePrice">Expire/settle price</param>
        /// <param name="biggestMoveAmount">Amount of the biggest move (Open to Close) in a day during the interval</param>
        private static void PrintIntervalStats(Index expiration, Index intervalStart, Index intervalHigh, Index intervalLow, Index biggestMove, double expirePrice, double biggestMoveAmount)
        {
            Console.WriteLine("Expiration Date: {0}, Start Date: {1}, Opening Price: {2}, High: {3} on {4}, Low: {5} on {6}, Expiry Price: {7}, Spread: {8}, Biggest Move (Open to Close): {9} on {10}",
                expiration.Date.ToString("dd/MM/yyyy"), intervalStart.Date.ToString("dd/MM/yyyy"), Math.Round(intervalStart.Open, 2), Math.Round(intervalHigh.High, 2), intervalHigh.Date.ToString("dd/MM/yyyy"), Math.Round(intervalLow.Low, 2), intervalLow.Date.ToString("dd/MM/yyyy"), Math.Round(expirePrice, 2), Math.Round(expirePrice - intervalStart.Open, 2), Math.Round(biggestMoveAmount, 2), biggestMove.Date.ToString("dd/MM/yyyy"));
        }

        /// <summary>
        /// Print the final stats of the analysis
        /// </summary>
        private static void PrintFinalStats()
        {
            Console.WriteLine("\nTotal expirations: {0}", _totalExpirations);
            Console.WriteLine("\nPercentage of positive spreads: {0}%", Decimal.Divide(_totalPositiveSpreads, _totalExpirations) * 100);
            Console.WriteLine("\nPercentage of spreads under 100: {0}%", Decimal.Divide(_spreadsUnder100, _totalExpirations) * 100);
            Console.WriteLine("\nPercentage of spreads under 80: {0}%", Decimal.Divide(_spreadsUnder80, _totalExpirations) * 100);
            Console.WriteLine("\nPercentage of spreads under 60: {0}%", Decimal.Divide(_spreadsUnder60, _totalExpirations) * 100);
            Console.WriteLine("\nPercentage of spreads under 40: {0}%", Decimal.Divide(_spreadsUnder40, _totalExpirations) * 100);
            Console.WriteLine("\nPercentage of spreads under 20: {0}%", Decimal.Divide(_spreadsUnder20, _totalExpirations) * 100);
        }
    }
}
