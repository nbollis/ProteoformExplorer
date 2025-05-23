using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Windows.Input;
using ProteoformExplorer.GuiFunctions;
using ScottPlot;

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

        public double ChartTickFontSize
        {
            get => GuiSettings.ChartTickFontSize;
            set
            {
                GuiSettings.ChartTickFontSize = value;
                OnPropertyChanged(nameof(ChartTickFontSize));
            }
        }

        public double ChartHeaderFontSize
        {
            get => GuiSettings.ChartHeaderFontSize;
            set
            {
                GuiSettings.ChartHeaderFontSize = value;
                OnPropertyChanged(nameof(ChartHeaderFontSize));
            }
        }

        public double ChartLegendFontSize
        {
            get => GuiSettings.ChartLegendFontSize;
            set
            {
                GuiSettings.ChartLegendFontSize = value;
                OnPropertyChanged(nameof(ChartLegendFontSize));
            }
        }

        public double ChartAxisLabelFontSize
        {
            get => GuiSettings.ChartAxisLabelFontSize;
            set
            {
                GuiSettings.ChartAxisLabelFontSize = value;
                OnPropertyChanged(nameof(ChartAxisLabelFontSize));
            }
        }

        public double ChartLineWidth
        {
            get => GuiSettings.ChartLineWidth;
            set
            {
                GuiSettings.ChartLineWidth = value;
                OnPropertyChanged(nameof(ChartLineWidth));
            }
        }

        public ObservableCollection<Alignment> LegendLocations { get; } = [..Enum.GetValues<Alignment>()];

        public Alignment LegendLocation
        {
            get => GuiSettings.LegendLocation;
            set
            {
                GuiSettings.LegendLocation = value;
                OnPropertyChanged(nameof(LegendLocation));
            }
        }

        public double AnnotatedEnvelopeLineWidth
        {
            get => GuiSettings.AnnotatedEnvelopeLineWidth;
            set
            {
                GuiSettings.AnnotatedEnvelopeLineWidth = value;
                OnPropertyChanged(nameof(AnnotatedEnvelopeLineWidth));
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
                GuiSettings.NameMappings = nameMappings;
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
