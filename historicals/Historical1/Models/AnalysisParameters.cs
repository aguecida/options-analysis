using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Historical1.Models
{
    public class AnalysisParameters
    {
        public int VrocPeriod { get; set; }
        public double VrocThreshold { get; set; }

        public AnalysisParameters()
        {
            // Set defaults
            VrocPeriod = 15;
            VrocThreshold = 100;
        }
    }
}
