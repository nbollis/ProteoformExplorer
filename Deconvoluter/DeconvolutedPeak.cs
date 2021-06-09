using Chemistry;
using System;
using System.Collections.Generic;
using System.Text;

namespace Deconvoluter
{
    public class DeconvolutedPeak
    {
        public double ExperimentalMz { get; private set; }
        public double TheoreticalMz { get; private set; }
        public int Charge { get; private set; }
        public double ExperimentalIntensity { get; private set; }
        public double TheoreticalIntensity { get; set; }
        public double TheoreticalNormalizedAbundance { get; private set; }
        public int IsotopeNumber { get; private set; }
        public double PpmError { get { return ((ExperimentalMz.ToMass(Charge) - TheoreticalMz.ToMass(Charge)) / TheoreticalMz.ToMass(Charge)) * 1e6; } }
        public double IntensityError { get { return ExperimentalIntensity - TheoreticalIntensity; } }
        public double SignalToNoise { get { return (ExperimentalIntensity - Envelope.Baseline) / Envelope.NoiseFwhm; } }
        public DeconvolutedEnvelope Envelope { get; set; }

        public DeconvolutedPeak(double experimentalMz, double theorMz, int z, double expIntensity, double theorIntensity, int isotopeNumber, double theoreticalNormalizedAbundance)
        {
            this.ExperimentalMz = experimentalMz;
            this.TheoreticalMz = theorMz;
            this.Charge = z;
            this.ExperimentalIntensity = expIntensity;
            this.TheoreticalIntensity = theorIntensity;
            this.IsotopeNumber = isotopeNumber;
            this.TheoreticalNormalizedAbundance = theoreticalNormalizedAbundance;
        }

        public override string ToString()
        {
            return TheoreticalIntensity.ToString("F1") + " : " + ExperimentalIntensity.ToString("F1");
        }
    }
}
