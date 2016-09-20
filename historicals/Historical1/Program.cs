using CsvHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Configuration;

namespace Historical1
{
    class Program
    {
        static void Main(string[] args)
        {
            var spx = ReadFile<Index>(ConfigurationManager.AppSettings["spxDataFile"]);
            

            foreach (var record in spx.Take(5))
            {
                Console.WriteLine("{0}, {1}, {2}, {3}", record.Date, record.Open, record.Close, record.Volume);
            }

            Thread.Sleep(5000);
        }

        public static IEnumerable<T> ReadFile<T>(string path)
        {
            using (var sr = new StreamReader(path))
            {
                // Read file to list
                var reader = new CsvReader(sr);
                IEnumerable<T> records = reader.GetRecords<T>().ToList();
                return records;
            }
        }
    }
}
