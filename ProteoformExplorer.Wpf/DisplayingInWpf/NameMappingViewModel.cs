using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using ProteoformExplorer.GuiFunctions;

namespace ProteoformExplorer.Wpf;

public class NameMappingViewModel(NameMapping plottableSpecies) : BaseViewModel
{
    private NameMapping _plottableSpecies = plottableSpecies;

    public NameMapping PlottableSpecies
    {
        get => _plottableSpecies;
        set
        {
            _plottableSpecies = value;
            OnPropertyChanged(nameof(PlottableSpecies));
        }
    }

    public string ShortName
    {
        get => _plottableSpecies.ShortName;
        set
        {
            _plottableSpecies.ShortName = value;
            OnPropertyChanged(nameof(ShortName));
        }
    }

    public List<string> LongNames
    {
        get => _plottableSpecies.LongNames;
        set
        {
            _plottableSpecies.LongNames = value;
            OnPropertyChanged(nameof(LongNames));
        }
    }

    public string LongNamesDisplay
    {
        get => string.Join(", ", _plottableSpecies.LongNames);
        set
        {
            _plottableSpecies.LongNames = value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(name => name.Trim())
                .ToList();
            OnPropertyChanged(nameof(LongNames));
            OnPropertyChanged(nameof(LongNamesDisplay));
        }
    }

    public Color Color
    {
        get => _plottableSpecies.Color;
        set
        {
            _plottableSpecies.Color = value;
            OnPropertyChanged(nameof(Color));
        }
    }
}