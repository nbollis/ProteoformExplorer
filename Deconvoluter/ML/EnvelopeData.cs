using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Deconvoluter.ML
{
    public class EnvelopeData
    {
        [LoadColumn(0)]
        public float PearsonCorrelation;

        [LoadColumn(1)]
        public float NormalizedSpectralAngle;

        [LoadColumn(2)]
        public float NumberOfPeaks;

        [LoadColumn(3)]
        public float SignalToNoise;

        [LoadColumn(4)]
        public float FractionIntensityObserved;

        [LoadColumn(5)]
        public float FractionIntensityMissing;

        [LoadColumn(6)]
        public float InterstitialNormalizedSpectralAngle;

        [LoadColumn(7)]
        public float NeighboringChargeObserved;

        [LoadColumn(8)]
        public float NeighboringScanObserved;

        public EnvelopeData(DeconvolutedEnvelope envelope)
        {
            this.PearsonCorrelation = (float)envelope.PearsonCorrelation;
            this.NormalizedSpectralAngle = (float)envelope.NormalizedSpectralAngle;
            this.NumberOfPeaks = envelope.Peaks.Count;
            this.SignalToNoise = Math.Min(20, (float)envelope.SignalToNoise);
            this.FractionIntensityObserved = (float)envelope.FractionIntensityObserved;
            this.FractionIntensityMissing = (float)envelope.FractionIntensityMissing;

            this.InterstitialNormalizedSpectralAngle = (float)envelope.InterstitialSpectralAngle;
            this.NeighboringChargeObserved = 0; //envelope.NormalizedSpectralAngle;
            this.NeighboringScanObserved = 0; // envelope.NormalizedSpectralAngle;
        }

        public string ToOutputString()
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
            sb.Append(NeighboringChargeObserved);
            sb.Append(',');
            sb.Append(NeighboringScanObserved);

            return sb.ToString();
        }
    }

    public class ClusterPrediction
    {
        [ColumnName("PredictedLabel")]
        public uint PredictedClusterId;

        [ColumnName("Score")]
        public float[] Distances;
    }
}
