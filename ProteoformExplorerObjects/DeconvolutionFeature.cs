using MassSpectrometry;
using MzLibUtil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ProteoformExplorer
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

        public void PopulateAnnotatedEnvelopes(KeyValuePair<string, DynamicDataConnection> data)
        {
            if (AnnotatedEnvelopes != null)
            {
                return;
            }

            AnnotatedEnvelopes = new List<AnnotatedEnvelope>();
            var start = PfmXplorerUtil.GetClosestScanToRtFromDynamicConnection(data, RtElutionRange.Minimum);
            var end = PfmXplorerUtil.GetClosestScanToRtFromDynamicConnection(data, RtElutionRange.Maximum);

            for (int i = start.OneBasedScanNumber; i <= end.OneBasedScanNumber + 1; i++)
            {
                var scan = data.Value.GetOneBasedScanFromDynamicConnection(i);

                if (scan.MsnOrder != 1)
                {
                    continue;
                }

                foreach (var charge in Charges)
                {
                    var envelope = PfmXplorerUtil.GetAnnotatedEnvelope(MonoisotopicMass, scan, charge);

                    if (envelope != null)
                    {
                        AnnotatedEnvelopes.Add(envelope);
                    }
                }
            }
        }
    }
}
