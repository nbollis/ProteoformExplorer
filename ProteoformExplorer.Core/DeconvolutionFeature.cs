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
            this.SpectraFileNameWithoutExtension = PfmXplorerUtil.GetFileNameWithoutExtension(spectraFileName);
            this.AnnotatedEnvelopes = annotatedEnvelopes;
        }

        public DeconvolutionFeature(Identification id, KeyValuePair<string, CachedSpectraFileData> data)
        {
            this.MonoisotopicMass = id.MonoisotopicMass;
            this.SpectraFileNameWithoutExtension = PfmXplorerUtil.GetFileNameWithoutExtension(id.SpectraFileNameWithoutExtension);

            GenerateDeconvolutionFeatureFromIdentification(id, data);
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
            double modeMass = PfmXplorerUtil.DeconvolutionEngine.GetModeMassFromMonoisotopicMass(MonoisotopicMass);

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

        private void GenerateDeconvolutionFeatureFromIdentification(Identification id, KeyValuePair<string, CachedSpectraFileData> data)
        {
            var deconEngine = PfmXplorerUtil.DeconvolutionEngine;

            double modeMass = deconEngine.GetModeMassFromMonoisotopicMass(MonoisotopicMass);

            int lastScanNum = PfmXplorerUtil.GetLastOneBasedScanNumber(data);
            List<DeconvolutedPeak> peaksBuffer = new List<DeconvolutedPeak>();
            HashSet<double> alreadyClaimedMzs = new HashSet<double>();
            List<(double, double)> intensitiesBuffer = new List<(double, double)>();

            List<AnnotatedEnvelope> envelopes = new List<AnnotatedEnvelope>();

            int direction = 1;
            for (int i = id.OneBasedPrecursorScanNumber; i >= 1 && i <= lastScanNum; i += direction)
            {
                var scan = data.Value.GetOneBasedScan(i);

                if (scan == null || scan.MsnOrder != 1)
                {
                    continue;
                }

                int index = scan.MassSpectrum.GetClosestPeakIndex(modeMass.ToMz(id.PrecursorChargeState));
                double expMz = scan.MassSpectrum.XArray[index];

                bool successfullyFoundEnvelope = false;

                if (PfmXplorerUtil.DeconvolutionEngine.PpmTolerance.Within(expMz.ToMass(id.PrecursorChargeState), modeMass))
                {
                    var envelope = PfmXplorerUtil.DeconvolutionEngine.GetIsotopicEnvelope(scan.MassSpectrum, index, id.PrecursorChargeState, peaksBuffer,
                        alreadyClaimedMzs, intensitiesBuffer);

                    if (envelope != null)
                    {
                        envelopes.Add(new AnnotatedEnvelope(scan.OneBasedScanNumber, scan.RetentionTime,
                            id.PrecursorChargeState, envelope.Peaks.Select(p => p.ExperimentalMz).ToList()));

                        successfullyFoundEnvelope = true;
                    }
                }

                if (!successfullyFoundEnvelope)
                {
                    if (direction == 1)
                    {
                        direction = -1;
                        i = id.OneBasedPrecursorScanNumber - 1;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            if (!envelopes.Any())
            {
                var precursorScan = data.Value.GetOneBasedScan(id.OneBasedPrecursorScanNumber);
                envelopes.Add(new AnnotatedEnvelope(id.OneBasedPrecursorScanNumber, precursorScan.RetentionTime, id.PrecursorChargeState,
                    new List<double> { precursorScan.MassSpectrum.GetClosestPeakXvalue(modeMass.ToMz(id.PrecursorChargeState)).Value }));
            }

            //TODO
            this.ApexRt = envelopes.First().RetentionTime;
            this.RtElutionRange = new DoubleRange(envelopes.Min(p => p.RetentionTime), envelopes.Max(p => p.RetentionTime));
            this.Charges = new List<int> { id.PrecursorChargeState };

            FindAnnotatedEnvelopesInData(data);
        }
    }
}
