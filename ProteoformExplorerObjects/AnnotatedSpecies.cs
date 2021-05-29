using MassSpectrometry;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProteoformExplorer
{
    public class AnnotatedSpecies
    {
        public string SpeciesLabel { get; private set; }
        public string SpectraFileNameWithoutExtension { get; private set; }
        public Identification Identification { get; private set; }
        public DeconvolutionFeature DeconvolutionFeature { get; private set; }

        public AnnotatedSpecies(DeconvolutionFeature deconvolutionFeature)
        {
            SpeciesLabel = deconvolutionFeature.MonoisotopicMass.ToString("F2");
            DeconvolutionFeature = deconvolutionFeature;
        }

        public AnnotatedSpecies(Identification identification)
        {
            SpeciesLabel = identification.FullSequence;
            Identification = identification;
        }

        public AnnotatedSpecies(DeconvolutionFeature deconvolutionFeature, Identification identification)
        {
            SpeciesLabel = identification.FullSequence;
            Identification = identification;
            DeconvolutionFeature = deconvolutionFeature;
        }

        //public override bool Equals(object obj)
        //{
        //    var other = (AnnotatedSpecies)obj;

        //    if (other != null && this.SpeciesLabel == other.SpeciesLabel)
        //    {
        //        return true;
        //    }

        //    return false;
        //}

        //public override int GetHashCode()
        //{
        //    return SpeciesLabel.GetHashCode();
        //}

        public override string ToString()
        {
            return SpeciesLabel;
        }
    }
}
