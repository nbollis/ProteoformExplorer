using ProteoformExplorer.Core;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace ProteoformExplorer.Wpf
{
    public interface INode : INotifyPropertyChanged
    {
        string Name { get; }
        public bool IsSelected { get; set; }
        public bool IsExpanded { get; set; }
    }

    public class AnnotatedSpeciesNode : INode
    {
        public AnnotatedSpecies AnnotatedSpecies;
        public ObservableCollection<INode> Charges { get; set; }
        public string Name { get; }
        public bool IsSelected { get { return _isSelected; } set { _isSelected = value; OnPropertyChanged("IsSelected"); } }
        public bool IsExpanded { get { return _isExpanded; } set { _isExpanded = value; OnPropertyChanged("IsExpanded"); } }
        public event PropertyChangedEventHandler PropertyChanged;
        private bool _isSelected;
        private bool _isExpanded;

        public AnnotatedSpeciesNode(AnnotatedSpecies species)
        {
            Charges = new ObservableCollection<INode>();
            Name = species.SpeciesLabel;
            AnnotatedSpecies = species;
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class AnnotatedSpeciesNodeSpecificCharge : INode
    {
        public AnnotatedSpecies AnnotatedSpecies { get; set; }
        public int Charge { get; set; }
        public string Name { get; set; }
        public bool IsSelected { get { return _isSelected; } set { _isSelected = value; OnPropertyChanged("IsSelected"); } }
        public bool IsExpanded { get { return _isExpanded; } set { _isExpanded = value; OnPropertyChanged("IsExpanded"); } }
        public event PropertyChangedEventHandler PropertyChanged;
        private bool _isSelected;
        private bool _isExpanded;

        public AnnotatedSpeciesNodeSpecificCharge(AnnotatedSpecies species, int charge, string name = null)
        {
            AnnotatedSpecies = species;
            Charge = charge;
            Name = name == null ? "z=" + charge.ToString() : name;
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
