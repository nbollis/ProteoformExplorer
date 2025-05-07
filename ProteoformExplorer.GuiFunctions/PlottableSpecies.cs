using System.Collections.Generic;
using System.Drawing;

namespace ProteoformExplorer.GuiFunctions;

public class PlottableSpecies
{
    public List<string> LongNames { get; set; }
    public string ShortName { get; set; }
    public Color Color { get; set; }
}