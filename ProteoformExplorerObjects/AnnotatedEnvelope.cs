using System.Collections.Generic;

namespace ProteoformExplorer
{
    public class AnnotatedEnvelope
    {
        public int OneBasedScanNumber { get; private set; }
        public double RetentionTime { get; private set; }
        public int Charge { get; private set; }
        public List<double> PeakMzs { get; private set; }

        public AnnotatedEnvelope(int oneBasedScan, double rt, int charge, List<double> peakMzs)
        {
            OneBasedScanNumber = oneBasedScan;
            RetentionTime = rt;
            Charge = charge;
            PeakMzs = peakMzs;
        }
    }
}
