using CsvHelper;
using Historical3.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Historical3
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

            Index expirationFriday;
            Index expirationThursday = null;
            Index start = null;
            Index periodHigh = null;
            Index periodLow = null;
            bool foundFirstExpiration = false;
            bool foundExpiration = false;
            Index biggestMove = null;
            double biggestMoveAmount = 0;

            // Monday open stats
            Index mondayOpen = null;
            Index m_periodHigh = null;
            Index m_periodLow = null;
            Index m_biggestMove = null;
            double m_biggestMoveAmount = 0;


            // Tuesday open stats
            Index tuesdayOpen = null;
            Index t_periodHigh = null;
            Index t_periodLow = null;
            Index t_biggestMove = null;
            double t_biggestMoveAmount = 0;

            foreach (var day in spx)
            {
                if (DateTime.Compare(day.Date, parameters.StartDate) < 0)
                {
                    continue;
                }

                // Reset some flags during first week of the month
                if (day.Date.Day >= 1 && day.Date.Day <= 7)
                {
                    foundExpiration = false;
                    expirationThursday = null;
                    mondayOpen = null;
                    tuesdayOpen = null;
                }

                if (mondayOpen != null)
                {
                    if (day.High > m_periodHigh.High)
                    {
                        m_periodHigh = day;
                    }

                    if (day.Low < m_periodLow.Low)
                    {
                        m_periodLow = day;
                    }

                    if (Math.Abs(day.Close - day.Open) > Math.Abs(m_biggestMoveAmount))
                    {
                        m_biggestMove = day;
                        m_biggestMoveAmount = day.Close - day.Open;
                    }
                }

                if (day.Date.Day >= 11 && day.Date.Day <= 17)
                {
                    if (day.Date.DayOfWeek == DayOfWeek.Monday)
                    {
                        mondayOpen = day;
                        m_periodHigh = day;
                        m_periodLow = day;
                        m_biggestMove = day;
                        m_biggestMoveAmount = day.Close - day.Open;
                    }
                }

                if (tuesdayOpen != null)
                {
                    if (day.High > t_periodHigh.High)
                    {
                        t_periodHigh = day;
                    }

                    if (day.Low < t_periodLow.Low)
                    {
                        t_periodLow = day;
                    }

                    if (Math.Abs(day.Close - day.Open) > Math.Abs(t_biggestMoveAmount))
                    {
                        t_biggestMove = day;
                        t_biggestMoveAmount = day.Close - day.Open;
                    }
                }

                if (day.Date.Day >= 12 && day.Date.Day <= 18)
                {
                    if (day.Date.DayOfWeek == DayOfWeek.Tuesday)
                    {
                        tuesdayOpen = day;
                        t_periodHigh = day;
                        t_periodLow = day;
                        t_biggestMove = day;
                        t_biggestMoveAmount = day.Close - day.Open;
                    }
                }

                // Try to find third Friday of the month (expiration day)
                if (day.Date.Day >= 15 && day.Date.Day <= 21 && !foundExpiration)
                {
                    if (expirationThursday != null && day.Date.DayOfWeek != DayOfWeek.Friday)
                    {
                        foundExpiration = true;
                        
                        if (foundFirstExpiration)
                        {
                            double expirePrice = settlePrices.Single(x => x.Date == expirationThursday.Date).SettlePrice;

                            if (mondayOpen == null && tuesdayOpen == null)
                            {
                                throw new Exception("Error in data: Could not find Monday or Tuesday open");
                            }

                            double spread = mondayOpen != null ? Math.Round(expirePrice - mondayOpen.Open, 2) : Math.Round(expirePrice - tuesdayOpen.Open, 2);
                            
                            UpdateStats(spread);
                            PrintIntervalStats(expirationThursday, mondayOpen ?? tuesdayOpen, mondayOpen != null ? m_periodHigh : t_periodHigh, mondayOpen != null ? m_periodLow : t_periodLow, mondayOpen != null ? m_biggestMove : t_biggestMove, expirePrice, mondayOpen != null ? m_biggestMoveAmount : t_biggestMoveAmount);
                        }
                        else
                        {
                            foundFirstExpiration = true;
                        }

                        mondayOpen = null;
                        tuesdayOpen = null;
                    }

                    if (day.Date.DayOfWeek == DayOfWeek.Friday)
                    {
                        foundExpiration = true;
                        
                        expirationFriday = day;

                        if (foundFirstExpiration)
                        {
                            double expirePrice = settlePrices.Single(x => x.Date == expirationFriday.Date).SettlePrice;

                            if (mondayOpen == null && tuesdayOpen == null)
                            {
                                throw new Exception("Error in data: Could not find Monday or Tuesday open");
                            }

                            double spread = mondayOpen != null ? Math.Round(expirePrice - mondayOpen.Open, 2) : Math.Round(expirePrice - tuesdayOpen.Open, 2);
                            UpdateStats(spread);
                            PrintIntervalStats(expirationFriday, mondayOpen ?? tuesdayOpen, mondayOpen != null ? m_periodHigh : t_periodHigh, mondayOpen != null ? m_periodLow : t_periodLow, mondayOpen != null ? m_biggestMove : t_biggestMove, expirePrice, mondayOpen != null ? m_biggestMoveAmount : t_biggestMoveAmount);
                        }
                        else
                        {
                            foundFirstExpiration = true;
                        }

                        mondayOpen = null;
                        tuesdayOpen = null;
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
        /// <param name="biggestMoveAmount">Amount of the biggest move (Close - Open) in a day during the interval</param>
        private static void PrintIntervalStats(Index expiration, Index intervalStart, Index intervalHigh, Index intervalLow, Index biggestMove, double expirePrice, double biggestMoveAmount)
        {
            Console.WriteLine("Expiration Date: {0}, Start Date: {1}, Opening Price: {2}, High: {3} on {4}, Low: {5} on {6}, Expiry Price: {7}, Spread: {8}, Biggest Move (Close - Open): {9} on {10}",
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
