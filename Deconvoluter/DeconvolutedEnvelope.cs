using Chemistry;
using MassSpectrometry;
using MzLibUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Deconvoluter
{
    public class DeconvolutedEnvelope
    {
        public string SpectraFileName { get; set; }
        public double RetentionTime { get; set; }
        public int OneBasedScan { get; set; }
        public List<DeconvolutedPeak> Peaks { get; private set; }
        public double MonoisotopicMass { get; private set; }
        public readonly int Charge;
        public double Score { get; set; }
        public double DeltaScore { get; set; }
        public double SignalToNoise
        {
            get
            {
                if (double.IsNaN(_signalToNoise))
                {
                    CalculateSignalToNoise();
                }

                return _signalToNoise;
            }
        }
        public double PearsonCorrelation { get; private set; }
        public double FractionIntensityObserved { get; private set; }
        public double NoiseFwhm { get; set; }
        public double Baseline { get; set; }
        public double TotalScanDeconvolutedIntensity { get; set; }
        public double NormalizedSpectralAngle { get; set; }
        public double FractionIntensityMissing { get; set; }

        private double _signalToNoise = double.NaN;
        public string MachineLearningClassification;
        public float[] MachineLearningClassificationScores;
        public double InterstitialSpectralAngle { get; set; }
        public int NeighboringCharges { get; set; }
        public IEnumerable<DeconvolutedPeak> PeaksOrderedByMass { get { return Peaks.OrderBy(p => p.ExperimentalMz.ToMass(Charge)); } }

        public DeconvolutedEnvelope(List<DeconvolutedPeak> peaks, double monoMass, int charge, double pearsonCorr, double fractionIntensityObserved)
        {
            Peaks = peaks;
            MonoisotopicMass = monoMass;
            Charge = charge;
            PearsonCorrelation = pearsonCorr;
            FractionIntensityObserved = fractionIntensityObserved;
            Score = Math.Log(Peaks.Count, 2) * pearsonCorr;
            DeltaScore = Score;
            FractionIntensityMissing = double.NaN;

            CalculateIntensityErrors();
        }

        public override string ToString()
        {
            return MonoisotopicMass.ToString("F2") + "; z=" + Charge + "; FM=" + FractionIntensityMissing.ToString("F2") + "; ISA=" + InterstitialSpectralAngle.ToString("F2");
        }

        public static string TabDelimitedHeader
        {
            get
            {
                StringBuilder sb = new StringBuilder();

                sb.Append("File Name");
                sb.Append('\t');
                sb.Append("Scan Number");
                sb.Append('\t');
                sb.Append("Retention Time");
                sb.Append('\t');
                sb.Append("Species");
                sb.Append('\t');
                sb.Append("Monoisotopic Mass");
                sb.Append('\t');
                sb.Append("Charge");
                sb.Append('\t');
                sb.Append("Peaks List");
                sb.Append('\t');
                sb.Append("Primary Mass");
                sb.Append('\t');
                sb.Append("S/N");
                sb.Append('\t');
                sb.Append("Delta Score");
                sb.Append('\t');
                sb.Append("Log Intensity");
                sb.Append('\t');
                sb.Append("Log Deconvoluted Scan Intensity");
                sb.Append('\t');
                sb.Append("Normalized Spectral Angle");
                sb.Append('\t');
                sb.Append("Fraction Intensity Expected But Missing");
                sb.Append('\t');
                sb.Append("Interstitial Spectral Angle");
                sb.Append('\t');
                sb.Append("Machine Learning Classifier");
                sb.Append('\t');
                sb.Append("Machine Learning Classifier Scores");

                return sb.ToString();
            }
        }

        public string ToOutputString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(SpectraFileName);
            sb.Append('\t');
            sb.Append(OneBasedScan);
            sb.Append('\t');
            sb.Append(RetentionTime);
            sb.Append('\t');
            sb.Append(MonoisotopicMass);
            sb.Append('\t');
            sb.Append(MonoisotopicMass);
            sb.Append('\t');
            sb.Append(Charge);
            sb.Append('\t');
            sb.Append(string.Join(",", Peaks.Select(p => p.ExperimentalMz.ToString("F4"))));
            sb.Append('\t');
            sb.Append(Peaks.First().ExperimentalMz.ToMass(Charge));
            sb.Append('\t');
            sb.Append(SignalToNoise);
            sb.Append('\t');
            sb.Append(DeltaScore);
            sb.Append('\t');
            sb.Append(Math.Log(Peaks.Sum(p => p.ExperimentalIntensity), 2));
            sb.Append('\t');
            sb.Append(Math.Log(TotalScanDeconvolutedIntensity, 2));
            sb.Append('\t');
            sb.Append(NormalizedSpectralAngle);
            sb.Append('\t');
            sb.Append(FractionIntensityMissing);
            sb.Append('\t');
            sb.Append(InterstitialSpectralAngle);
            sb.Append('\t');
            sb.Append(MachineLearningClassification);
            sb.Append('\t');
            sb.Append(string.Join("\t", MachineLearningClassificationScores.Select(p => p.ToString("F4"))));

            return sb.ToString();
        }

        private void CalculateIntensityErrors()
        {
            double sumNormalizedAbundance = Peaks.Sum(p => p.TheoreticalNormalizedAbundance);
            double sumExperimentalIntensity = Peaks.Sum(p => p.ExperimentalIntensity);

            foreach (var peak in Peaks)
            {
                peak.Envelope = this;

                double fractionOfTotalEnvelopeAbundance = peak.TheoreticalNormalizedAbundance / sumNormalizedAbundance;
                double theoreticalIntensity = fractionOfTotalEnvelopeAbundance * sumExperimentalIntensity;
                peak.TheoreticalIntensity = theoreticalIntensity;
            }
        }

        public double GetNormalizedSpectralAngle(MzSpectrum spectrum, double[] averagineMasses, double[] averagineIntensities, Tolerance tol, HashSet<double> mzsToExcludeFromCalculation,
            bool calculatingInterstitial = false)
        {
            List<(double theor, double actual)> intensities = new List<(double theor, double actual)>();

            double summedAbundance = this.Peaks.Sum(p => p.TheoreticalNormalizedAbundance);
            double summedExperimentalIntensity = this.Peaks.Sum(p => p.ExperimentalIntensity);

            double intensityMissing = 0;
            double summedIntensityThatShouldHaveBeenObserved = 0;

            for (int i = 0; i < averagineMasses.Length; i++)
            {
                var observedPeak = Peaks.FirstOrDefault(p => p.IsotopeNumber == i);
                double theoreticalIntensity = (averagineIntensities[i] / summedAbundance) * summedExperimentalIntensity;
                double theoreticalSn = (theoreticalIntensity - this.Baseline) / this.NoiseFwhm;

                if (theoreticalSn > 0)
                {
                    summedIntensityThatShouldHaveBeenObserved += (theoreticalIntensity - this.Baseline);
                }

                if (observedPeak == null)
                {
                    if (theoreticalSn >= 0)
                    {
                        intensities.Add((theoreticalIntensity - Baseline, 0));

                        double theorIsotopeMz = (MonoisotopicMass + i * Constants.C13MinusC12).ToMz(this.Charge);
                        int pkIndex = spectrum.GetClosestPeakIndex(theorIsotopeMz);
                        double expMz = spectrum.XArray[pkIndex];

                        if (!tol.Within(expMz.ToMass(this.Charge), theorIsotopeMz.ToMass(this.Charge)))
                        {
                            intensityMissing += (theoreticalIntensity - this.Baseline);
                        }
                    }
                }
                else
                {
                    if (mzsToExcludeFromCalculation != null && mzsToExcludeFromCalculation.Contains(observedPeak.ExperimentalMz))
                    {
                        continue;
                    }

                    intensities.Add((theoreticalIntensity - Baseline, observedPeak.ExperimentalIntensity - Baseline));
                }
            }

            if (!calculatingInterstitial)
            {
                FractionIntensityMissing = intensityMissing / summedIntensityThatShouldHaveBeenObserved;
            }

            // L2 norm
            double expNormalizer = Math.Sqrt(intensities.Sum(p => Math.Pow(p.actual, 2)));
            double theorNormalizer = Math.Sqrt(intensities.Sum(p => Math.Pow(p.theor, 2)));

            // interstitial spectral angle has a different normalization
            if (calculatingInterstitial)
            {
                expNormalizer = (expNormalizer + theorNormalizer) / 2;
                theorNormalizer = expNormalizer;
            }

            double dotProduct = 0;

            foreach (var ion in intensities)
            {
                dotProduct += (ion.theor / theorNormalizer) * (ion.actual / expNormalizer);
            }

            double normalizedSpectralAngle = 1 - (2 * Math.Acos(dotProduct) / Math.PI);

            return normalizedSpectralAngle;
        }

        private void CalculateSignalToNoise()
        {
            //double excessIntensity = Peaks.Sum(p => Math.Max(0, p.ExperimentalIntensity - Baseline));
            //double noiseWidth = Peaks.Count(p => Math.Max(0, p.ExperimentalIntensity - Baseline) > 0) * NoiseFwhm;
            //_signalToNoise = excessIntensity / noiseWidth;
            _signalToNoise = (Peaks.Max(p => p.ExperimentalIntensity) - this.Baseline) / NoiseFwhm;
        }
    }
}
