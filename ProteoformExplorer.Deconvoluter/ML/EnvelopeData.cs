using Microsoft.ML.Data;
using System;
using System.Linq;
using System.Text;

namespace ProteoformExplorer.Deconvoluter.ML
{
    public class EnvelopeData
    {
        [ColumnName("PearsonCorrelation"), LoadColumn(0)]
        public float PearsonCorrelation;

        [ColumnName("NormalizedSpectralAngle"), LoadColumn(1)]
        public float NormalizedSpectralAngle;

        [ColumnName("NumberOfPeaks"), LoadColumn(2)]
        public float NumberOfPeaks;

        [ColumnName("SignalToNoise"), LoadColumn(3)]
        public float SignalToNoise;

        [ColumnName("FractionIntensityObserved"), LoadColumn(4)]
        public float FractionIntensityObserved;

        [ColumnName("FractionIntensityMissing"), LoadColumn(5)]
        public float FractionIntensityMissing;

        [ColumnName("InterstitialNormalizedSpectralAngle"), LoadColumn(6)]
        public float InterstitialNormalizedSpectralAngle;

        [ColumnName("Label"), LoadColumn(7)]
        public string Label { get; set; }

        //[LoadColumn(7)]
        //public float NeighboringChargeObserved;

        //[LoadColumn(8)]
        //public float NeighboringScanObserved;

        public EnvelopeData(DeconvolutedEnvelope envelope)
        {
            this.PearsonCorrelation = (float)envelope.PearsonCorrelation;
            this.NormalizedSpectralAngle = (float)envelope.NormalizedSpectralAngle;
            this.NumberOfPeaks = envelope.Peaks.Count;
            this.SignalToNoise = (float)Math.Log(envelope.SignalToNoise + Math.Sqrt(Math.Pow(envelope.SignalToNoise, 2) + 1));
            this.FractionIntensityObserved = (float)envelope.FractionIntensityObserved;
            this.FractionIntensityMissing = (float)envelope.FractionIntensityMissing;
            this.InterstitialNormalizedSpectralAngle = (float)envelope.InterstitialSpectralAngle;
            //this.NeighboringChargeObserved = 0; //envelope.NormalizedSpectralAngle;
            //this.NeighboringScanObserved = 0; // envelope.NormalizedSpectralAngle;
        }

        public static string TabDelimitedHeader
        {
            get 
            { 
                return string.Join(',', typeof(EnvelopeData).GetFields().Select(p => p.Name)) + ",Label";
            }
        }

        public string ToOutputString(string label)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(PearsonCorrelation);
            sb.Append(',');
            sb.Append(NormalizedSpectralAngle);
            sb.Append(',');
            sb.Append(NumberOfPeaks);
            sb.Append(',');
            sb.Append(SignalToNoise);
            sb.Append(',');
            sb.Append(FractionIntensityObserved);
            sb.Append(',');
            sb.Append(FractionIntensityMissing);
            sb.Append(',');
            sb.Append(InterstitialNormalizedSpectralAngle);
            sb.Append(',');
            sb.Append(label);
            //sb.Append(',');
            //sb.Append(NeighboringChargeObserved);
            //sb.Append(',');
            //sb.Append(NeighboringScanObserved);

            return sb.ToString();
        }
    }

    public class ClassificationPrediction
    {
        [ColumnName("PredictedLabel")]
        public string Prediction { get; set; }
        public float[] Score { get; set; }
    }
}
