using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Windows.Input;
using ProteoformExplorer.GuiFunctions;

namespace ProteoformExplorer.Wpf
{
    public class SettingsViewModel : BaseViewModel
    {
        public ObservableCollection<NameMappingViewModel> PlottableSpecies { get; set; } = [];

        public int TicRollingAverage
        {
            get => GuiSettings.TicRollingAverage;
            set
            {
                GuiSettings.TicRollingAverage = value;
                OnPropertyChanged(nameof(TicRollingAverage));
            }
        }

        public Color TicColor
        {
            get => GuiSettings.TicColor;
            set
            {
                GuiSettings.TicColor = value;
                OnPropertyChanged(nameof(TicColor));
            }
        }

        public Color DeconvolutedColor
        {
            get => GuiSettings.DeconvolutedColor;
            set
            {
                GuiSettings.DeconvolutedColor = value;
                OnPropertyChanged(nameof(DeconvolutedColor));
            }
        }

        public Color IdentifiedColor
        {
            get => GuiSettings.IdentifiedColor;
            set
            {
                GuiSettings.IdentifiedColor = value;
                OnPropertyChanged(nameof(IdentifiedColor));
            }
        }

        public Color RtIndicatorColor
        {
            get => GuiSettings.RtIndicatorColor;
            set
            {
                GuiSettings.RtIndicatorColor = value;
                OnPropertyChanged(nameof(RtIndicatorColor));
            }
        }
        public ICommand AddSpeciesCommand { get; }
        public SettingsViewModel()
        {
            LoadSettingsIntoViewModel();
            AddSpeciesCommand = new DelegateCommand(AddSpeciesFromUI);
        }

        public void UpdateStaticSettings()
        {
            // Update GUI Settings with any new values
            if (PlottableSpecies.Any())
            {
                var nameMappings = new List<NameMapping>();
                foreach (var plottableSpecies in PlottableSpecies)
                {
                    nameMappings.Add(new NameMapping
                    {
                        ShortName = plottableSpecies.ShortName,
                        LongNames = plottableSpecies.LongNames,
                        Color = plottableSpecies.Color
                    });
                }

                // Update the static settings with the new name mappings if they are not already present
                GuiSettings.NameMappings = GuiSettings.NameMappings.Concat(nameMappings).Distinct().ToList();
            }
        }

        public void LoadSettingsIntoViewModel()
        {
            // Clear existing species
            PlottableSpecies.Clear();

            // Add new species from settings
            var nameMappings = GuiSettings.NameMappings;
            foreach (var mapping in nameMappings)
            {
                PlottableSpecies.Add(new(mapping));
            }
        }

        private void AddSpeciesFromUI(object parameter)
        {
            var temp = (Tuple<string, string, Color>)parameter;
            if (parameter is Tuple<string, string, Color> speciesData)
            {
                var (shortName, longNames, color) = speciesData;
                AddSpecies(shortName, longNames, color);
            }
        }

        public void AddSpecies(string shortName, string longNames, Color color)
        {
            if (string.IsNullOrWhiteSpace(shortName))
            {
                throw new ArgumentException("Short name cannot be empty.");
            }

            if (string.IsNullOrWhiteSpace(longNames))
            {
                throw new ArgumentException("Long names cannot be empty.");
            }

            var longNamesList = longNames.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(name => name.Trim())
                .ToList();

            var nameMapping = new NameMapping
            {
                ShortName = shortName,
                LongNames = longNamesList,
                Color = color
            };

            // Update the static settings and those displayed with the new species
            PlottableSpecies.Add(new(nameMapping));
            GuiSettings.NameMappings.Add(nameMapping);
        }
    }
}
