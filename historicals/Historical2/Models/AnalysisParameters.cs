using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Historical2.Models
{
    public class AnalysisParameters
    {
        public DateTime StartDate { get; set; }

        public AnalysisParameters()
        {
            StartDate = new DateTime(2010, 1, 1);
        }
    }
}
