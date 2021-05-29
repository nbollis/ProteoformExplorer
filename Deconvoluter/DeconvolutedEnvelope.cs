using Chemistry;
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
        public double SignalToNoise { get; set; }
        public double PearsonCorrelation { get; private set; }
        public double FractionIntensityObserved { get; private set; }
        public double NoiseFwhm { get; set; }
        public double Baseline { get; set; }
        public double TotalScanDeconvolutedIntensity { get; set; }

        public DeconvolutedEnvelope(List<DeconvolutedPeak> peaks, double monoMass, int charge, double pearsonCorr, double fractionIntensityObserved)
        {
            Peaks = peaks;
            MonoisotopicMass = monoMass;
            Charge = charge;
            PearsonCorrelation = pearsonCorr;
            FractionIntensityObserved = fractionIntensityObserved;
            Score = Math.Log(Peaks.Count, 2) * pearsonCorr;
            DeltaScore = Score;
        }

        public override string ToString()
        {
            return MonoisotopicMass.ToString("F2") + "; z=" + Charge;
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
            sb.Append(Math.Log(Peaks.Sum(p => p.ExperimentalIntensity)));
            sb.Append('\t');
            sb.Append(TotalScanDeconvolutedIntensity);
            sb.Append('\t');
            sb.Append(string.Join(",", Peaks.Select(p => 
                (Math.Log(p.ExperimentalIntensity, 2) - Math.Log(p.TheoreticalIntensity, 2)) + 
                ";" + 
                (p.ExperimentalIntensity - this.Baseline) / this.NoiseFwhm)));

            return sb.ToString();
        }
    }
}
