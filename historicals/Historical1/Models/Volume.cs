using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Historical1.Models
{
    public class Volume
    {
        public DateTime Date { get; set; }
        public double PCRatio { get; set; }
        public int PVolume { get; set; }
        public int CVolume { get; set; }
        public int TotalVolume { get; set; }
    }
}
