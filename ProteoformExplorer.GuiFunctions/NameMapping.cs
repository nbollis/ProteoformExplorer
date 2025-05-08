using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace ProteoformExplorer.GuiFunctions;

public class NameMapping
{
    public string ShortName { get; set; }
    public List<string> LongNames { get; set; }
    public Color Color { get; set; }

    public override bool Equals(object obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(obj, this)) return true;
        return obj is NameMapping mapping &&
               ShortName == mapping.ShortName &&
               LongNames.SequenceEqual(mapping.LongNames) &&
               Color.ToArgb() == mapping.Color.ToArgb();
    }
}