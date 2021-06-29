using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProteoformExplorer.Core
{
    public static class DataManagement
    {
        public static Dictionary<string, CachedSpectraFileData> SpectraFiles;
        public static KeyValuePair<string, CachedSpectraFileData> CurrentlySelectedFile;
        public static ObservableCollection<AnnotatedSpecies> AllLoadedAnnotatedSpecies;
    }
}
