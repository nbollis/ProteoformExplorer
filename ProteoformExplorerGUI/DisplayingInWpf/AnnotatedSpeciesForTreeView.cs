using ProteoformExplorer.Objects;
using System.Collections.ObjectModel;

namespace ProteoformExplorer.ProteoformExplorerGUI
{
    public interface INode
    {
        string Name { get; }
    }

    public class AnnotatedSpeciesNode : INode
    {
        public AnnotatedSpecies AnnotatedSpecies;
        public ObservableCollection<INode> Charges { get; set; }
        public string Name { get; }

        public AnnotatedSpeciesNode(AnnotatedSpecies species)
        {
            Charges = new ObservableCollection<INode>();
            Name = species.SpeciesLabel;
            AnnotatedSpecies = species;
        }
    }

    public class AnnotatedSpeciesNodeSpecificCharge : INode
    {
        public AnnotatedSpecies AnnotatedSpecies { get; set; }
        public int Charge { get; set; }
        public string Name { get; set; }

        public AnnotatedSpeciesNodeSpecificCharge(AnnotatedSpecies species, int charge, string name = null)
        {
            AnnotatedSpecies = species;
            Charge = charge;
            Name = "z=" + charge.ToString();
        }
    }
}
