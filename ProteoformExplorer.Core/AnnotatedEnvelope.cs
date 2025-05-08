using System.Collections.Generic;

namespace ProteoformExplorer.Core
{
    /// <summary>
    /// Represents an envelope of peaks with annotations.
    /// </summary>
    public class AnnotatedEnvelope
    {
        public int OneBasedScanNumber { get; private set; }
        public double RetentionTime { get; private set; }
        public int Charge { get; private set; }
        public List<double> PeakMzs { get; private set; }
        public AnnotatedSpecies Species { get; set; }

        public AnnotatedEnvelope(int oneBasedScan, double rt, int charge, List<double> peakMzs)
        {
            OneBasedScanNumber = oneBasedScan;
            RetentionTime = rt;
            Charge = charge;
            PeakMzs = peakMzs;
        }
    }
}
