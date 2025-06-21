using Chemistry;
using ProteoformExplorer.Deconvoluter;
using MzLibUtil;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MassSpectrometry;
using System.Runtime.InteropServices;

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
        public Polarity Polarity { get; private set; }

        public DeconvolutionFeature(double monoMass, double apexRt, double rtStart, double rtEnd, List<int> charges, string spectraFileName,
            List<AnnotatedEnvelope> annotatedEnvelopes = null)
        {
            this.MonoisotopicMass = monoMass;
            this.ApexRt = apexRt;
            this.RtElutionRange = new DoubleRange(rtStart, rtEnd);
            this.Charges = charges;
            this.SpectraFileNameWithoutExtension = PfmXplorerUtil.GetFileNameWithoutExtension(spectraFileName);
            this.AnnotatedEnvelopes = annotatedEnvelopes;
            this.Polarity = charges.Sum() > 0 
                ? Polarity.Positive
                : Polarity.Negative;
        }

        public DeconvolutionFeature(Identification id, KeyValuePair<string, CachedSpectraFileData> data)
        {
            this.MonoisotopicMass = id.MonoisotopicMass;
            this.SpectraFileNameWithoutExtension = PfmXplorerUtil.GetFileNameWithoutExtension(id.SpectraFileNameWithoutExtension);
            this.Polarity = id.PrecursorChargeState > 0
                ? Polarity.Positive
                : Polarity.Negative;
            GenerateDeconvolutionFeatureFromIdentification(id, data);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        public void FindAnnotatedEnvelopesInData(KeyValuePair<string, CachedSpectraFileData> data)
        {
            if (AnnotatedEnvelopes is { Count: > 0 })
            {
                return;
            }

            AnnotatedEnvelopes = new List<AnnotatedEnvelope>();
            List<DeconvolutedPeak> peaksBuffer = new List<DeconvolutedPeak>();
            HashSet<double> alreadyClaimedMzs = new HashSet<double>();
            List<(double, double)> intensitiesBuffer = new List<(double, double)>();

            double modeMass = PfmXplorerUtil.DeconvolutionEngine.GetModeMassFromMonoisotopicMass(MonoisotopicMass);
            foreach (var scan in data.Value.DataFile.Value.GetMsScansInTimeRange(RtElutionRange.Minimum, RtElutionRange.Maximum))
            {
                if (scan is not { MsnOrder: 1 })
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

            if (AnnotatedEnvelopes.Count == 0)
            {
                var scan = PfmXplorerUtil.GetClosestScanToRtFromDynamicConnection(data, RtElutionRange.Minimum);

                if (Charges.Any() && scan != null)
                {
                    var mz = scan.MassSpectrum.GetClosestPeakXvalue(modeMass.ToMz(Charges.First()));

                    AnnotatedEnvelopes.Add(new AnnotatedEnvelope(scan.OneBasedScanNumber, scan.RetentionTime,
                                Charges.First(), new List<double> { mz.Value }));
                }
            }
        }

        private void GenerateDeconvolutionFeatureFromIdentification(Identification id, KeyValuePair<string, CachedSpectraFileData> data)
        {
            id.GetPrecursorInfoForIdentification();

            if (id.OneBasedPrecursorScanNumber <= 0 || id.PrecursorChargeState == 0)
            {
                // TODO: some kind of error here?
                return;
            }

            var deconEngine = PfmXplorerUtil.DeconvolutionEngine;
            double modeMass = deconEngine.GetModeMassFromMonoisotopicMass(MonoisotopicMass);
            int lastScanNum = data.Value.DataFile.Value.Scans[^1].OneBasedScanNumber;

            List<DeconvolutedPeak> peaksBuffer = new List<DeconvolutedPeak>();
            HashSet<double> alreadyClaimedMzs = new HashSet<double>();
            List<(double, double)> intensitiesBuffer = new List<(double, double)>();

            List<AnnotatedEnvelope> envelopes = new List<AnnotatedEnvelope>();

            int direction = 1;
            (int scanNum, double intensity) mostIntenseEnvelope = (id.OneBasedPrecursorScanNumber, 0);

            // Collect envelopes, starting with the precursor scan
            var processedScans = new HashSet<int>();
            int precursorScanNumber = id.OneBasedPrecursorScanNumber;

            // Helper to try adding an envelope at a scan
            bool TryAddEnvelopeAtScan(int scanNum)
            {
                if (processedScans.Contains(scanNum)) return false;
                var scan = data.Value.GetOneBasedScan(scanNum);
                if (scan == null || scan.MsnOrder != 1) return false;

                int index = scan.MassSpectrum.GetClosestPeakIndex(modeMass.ToMz(id.PrecursorChargeState));
                double expMz = scan.MassSpectrum.XArray[index];

                if (PfmXplorerUtil.DeconvolutionEngine.PpmTolerance.Within(expMz.ToMass(id.PrecursorChargeState), modeMass))
                {
                    var envelope = PfmXplorerUtil.DeconvolutionEngine.GetIsotopicEnvelope(
                        scan.MassSpectrum, index, id.PrecursorChargeState, peaksBuffer, alreadyClaimedMzs, intensitiesBuffer);

                    if (envelope != null)
                    {
                        envelopes.Add(new AnnotatedEnvelope(scan.OneBasedScanNumber, scan.RetentionTime,
                            id.PrecursorChargeState, envelope.Peaks.Select(p => p.ExperimentalMz).ToList()));
                        processedScans.Add(scanNum);
                        return true;
                    }
                }
                return false;
            }

            // Always add the precursor scan first
            TryAddEnvelopeAtScan(precursorScanNumber);

            // Expand left (lower scan numbers)
            int gapCount = 0;
            for (int i = precursorScanNumber - 1; i >= 1 && gapCount < 2; i--)
            {
                if (!TryAddEnvelopeAtScan(i))
                    gapCount++;
                else
                    gapCount = 0;
            }

            // Expand right (higher scan numbers)
            gapCount = 0;
            for (int i = precursorScanNumber + 1; i <= lastScanNum && gapCount < 2; i++)
            {
                if (!TryAddEnvelopeAtScan(i))
                    gapCount++;
                else
                    gapCount = 0;
            }


            var mostIntenseScan = data.Value.GetOneBasedScan(mostIntenseEnvelope.scanNum);

            // Find envelopes by moving in both directions from the precursor charge
            var foundCharges = new HashSet<int>();
            int precursorCharge = id.PrecursorChargeState;

            // Always check the precursor charge itself
            TryAddEnvelopeAtCharge(precursorCharge);

            int step = Polarity == Polarity.Positive ? 1 : -1;

            // Search higher charges
            for (int z = precursorCharge + step;
                 Polarity == Polarity.Positive ? z <= deconEngine.MaxCharge : z >= -deconEngine.MaxCharge;
                 z += step)
            {
                if (!TryAddEnvelopeAtCharge(z))
                    break;
            }

            // Search lower charges
            for (int z = precursorCharge - step;
                 Polarity == Polarity.Positive ? z >= deconEngine.MinCharge : z <= -deconEngine.MinCharge;
                 z -= step)
            {
                if (!TryAddEnvelopeAtCharge(z))
                    break;
            }

            bool TryAddEnvelopeAtCharge(int z)
            {
                int index = mostIntenseScan.MassSpectrum.GetClosestPeakIndex(modeMass.ToMz(z));
                double expMz = mostIntenseScan.MassSpectrum.XArray[index];

                if (PfmXplorerUtil.DeconvolutionEngine.PpmTolerance.Within(expMz.ToMass(z), modeMass))
                {
                    var envelope = PfmXplorerUtil.DeconvolutionEngine.GetIsotopicEnvelope(
                        mostIntenseScan.MassSpectrum, index, z, peaksBuffer, alreadyClaimedMzs, intensitiesBuffer);

                    if (envelope != null)
                    {
                        envelopes.Add(new AnnotatedEnvelope(
                            mostIntenseScan.OneBasedScanNumber,
                            mostIntenseScan.RetentionTime,
                            z,
                            envelope.Peaks.Select(p => p.ExperimentalMz).ToList()));
                        foundCharges.Add(z);
                        return true;
                    }
                }
                return false;
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

            int minCharge = envelopes.Min(p => p.Charge);
            int maxCharge = envelopes.Max(p => p.Charge);
            if (maxCharge >= minCharge)
            {
                this.Charges = Enumerable.Range(minCharge, maxCharge - minCharge + 1).ToList();
            }
            else
            {
                // Handle the error case appropriately
            }
        }

        
    }
}
