using Chemistry;
using ProteoformExplorer.Deconvoluter;
using MzLibUtil;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ProteoformExplorer.Core
{
    public class DeconvolutionFeature
    {
        public double MonoisotopicMass { get; private set; }
        public double ApexRt { get; private set; }
        public DoubleRange RtElutionRange { get; private set; }
        public List<int> Charges { get; private set; }
        public string SpectraFileNameWithoutExtension { get; private set; }
        public List<AnnotatedEnvelope> AnnotatedEnvelopes { get; private set; }

        public DeconvolutionFeature(double monoMass, double apexRt, double rtStart, double rtEnd, List<int> charges, string spectraFileName,
            List<AnnotatedEnvelope> annotatedEnvelopes = null)
        {
            this.MonoisotopicMass = monoMass;
            this.ApexRt = apexRt;
            this.RtElutionRange = new DoubleRange(rtStart, rtEnd);
            this.Charges = charges;
            this.SpectraFileNameWithoutExtension = Path.GetFileNameWithoutExtension(spectraFileName);
            this.AnnotatedEnvelopes = annotatedEnvelopes;
        }

        public void FindAnnotatedEnvelopesInData(KeyValuePair<string, CachedSpectraFileData> data)
        {
            if (AnnotatedEnvelopes != null)
            {
                return;
            }

            AnnotatedEnvelopes = new List<AnnotatedEnvelope>();
            List<DeconvolutedPeak> peaksBuffer = new List<DeconvolutedPeak>();
            HashSet<double> alreadyClaimedMzs = new HashSet<double>();
            List<(double, double)> intensitiesBuffer = new List<(double, double)>();

            var start = PfmXplorerUtil.GetClosestScanToRtFromDynamicConnection(data, RtElutionRange.Minimum);
            var end = PfmXplorerUtil.GetClosestScanToRtFromDynamicConnection(data, RtElutionRange.Maximum);

            for (int i = start.OneBasedScanNumber; i <= end.OneBasedScanNumber + 1; i++)
            {
                var scan = data.Value.GetOneBasedScan(i);

                if (scan == null || scan.MsnOrder != 1)
                {
                    continue;
                }

                foreach (var charge in Charges)
                {
                    //PfmXplorerUtil.GetAnnotatedEnvelope(MonoisotopicMass, scan, charge);
                    double modeMass = PfmXplorerUtil.DeconvolutionEngine.GetModeMassFromMonoisotopicMass(MonoisotopicMass);
                    int index = scan.MassSpectrum.GetClosestPeakIndex(modeMass.ToMz(charge));
                    double expMz = scan.MassSpectrum.XArray[index];

                    if (PfmXplorerUtil.DeconvolutionEngine.PpmTolerance.Within(expMz.ToMass(charge), modeMass))
                    {
                        var envelope = PfmXplorerUtil.DeconvolutionEngine.GetIsotopicEnvelope(scan.MassSpectrum, index, charge, peaksBuffer,
                            alreadyClaimedMzs, intensitiesBuffer);

                        if (envelope != null)
                        {
                            AnnotatedEnvelopes.Add(new AnnotatedEnvelope(scan.OneBasedScanNumber, scan.RetentionTime, 
                                charge, envelope.Peaks.Select(p => p.ExperimentalMz).ToList()));
                        }
                    }
                }
            }
        }
    }
}
