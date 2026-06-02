using System;

namespace ConsumeControlTFG.Models
{
    public class CarbonIntensityResponse
    {
        public string Zone { get; set; }
        public double CarbonIntensity { get; set; } // gCO2eq/kWh
        public DateTime DateTime { get; set; }
    }
}