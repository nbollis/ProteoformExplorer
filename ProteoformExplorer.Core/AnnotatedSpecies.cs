namespace ProteoformExplorer.Core
{
    /// <summary>
    /// A class which represents a species in a MS1. This contains a decovoluted feature and sometimes an Identifications. 
    /// </summary>
    public class AnnotatedSpecies
    {
        public string SpeciesLabel { get; private set; }
        public string SpectraFileNameWithoutExtension { get; private set; }
        public Identification Identification { get; private set; }
        public DeconvolutionFeature DeconvolutionFeature { get; set; }

        public AnnotatedSpecies(DeconvolutionFeature deconvolutionFeature, string speciesLabel = null)
        {
            SpeciesLabel = speciesLabel ?? deconvolutionFeature.MonoisotopicMass.ToString("F2");
            DeconvolutionFeature = deconvolutionFeature;
            SpectraFileNameWithoutExtension = deconvolutionFeature.SpectraFileNameWithoutExtension;
        }

        public AnnotatedSpecies(Identification identification)
        {
            SpeciesLabel = identification.FullSequence;
            Identification = identification;
            SpectraFileNameWithoutExtension = identification.SpectraFileNameWithoutExtension;
        }

        public AnnotatedSpecies(DeconvolutionFeature deconvolutionFeature, Identification identification)
        {
            SpeciesLabel = identification.FullSequence;
            Identification = identification;
            DeconvolutionFeature = deconvolutionFeature;
            SpectraFileNameWithoutExtension = deconvolutionFeature.SpectraFileNameWithoutExtension;
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
