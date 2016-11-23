using System;

namespace Historical3.Models
{
    public class AnalysisParameters
    {
        public DateTime StartDate { get; set; }

        public AnalysisParameters()
        {
            StartDate = new DateTime(2000, 1, 1);
        }
    }
}
