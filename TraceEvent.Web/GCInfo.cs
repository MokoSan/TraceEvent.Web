namespace TraceEvent.Web
{
    public class GCInfo
    {
        public string Summary { get; set; }
        public string ProcessName { get; set; }
        public double TotalAllocationsMB { get; set; }
        public double TotalGCPauseTimeMSec { get; set; }
        public double GCPauseTimePercentage { get; set; }
    }
}
