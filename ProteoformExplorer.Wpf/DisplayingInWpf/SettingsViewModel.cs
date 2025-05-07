using ProteoformExplorer.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Input;
using ProteoformExplorer.GuiFunctions;
using Nett;

namespace ProteoformExplorer.Wpf
{
    public class SettingsViewModel : BaseViewModel
    {
        private static readonly string SettingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ProteoformExplorer"
        );

        private static readonly string SettingsFilePath = Path.Combine(SettingsDirectory, "ProteoformExplorerSettings.toml");

        public ObservableCollection<PlottableSpeciesViewModel> PlottableSpecies { get; set; } = [];

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
            LoadSettings();
            AddSpeciesCommand = new DelegateCommand((obj) => AddSpeciesFromUI(obj));
        }

        public void SaveSettings()
        {
            // Ensure the settings directory exists
            if (!Directory.Exists(SettingsDirectory))
            {
                Directory.CreateDirectory(SettingsDirectory);
            }

            // Update GUI Settings with any new values
            if (PlottableSpecies.Any())
            {
                var nameConversions = new Dictionary<string, string>();
                var nameToColors = new Dictionary<string, Color>();
                foreach (var plottableSpecies in PlottableSpecies)
                {
                    foreach (var longName in plottableSpecies.LongNames)
                    {
                        nameConversions[longName] = plottableSpecies.ShortName;
                        nameToColors[plottableSpecies.ShortName] = plottableSpecies.Color;
                    }
                }
                GuiSettings.NameConversionDictionary = nameConversions;
                GuiSettings.ColorDict = nameToColors;
            }

            var table = GuiSettings.ToTomlTable();
            Toml.WriteFile(table, SettingsFilePath);
        }

        public void LoadSettings()
        {
            if (File.Exists(SettingsFilePath))
            {
                var table = Toml.ReadFile(SettingsFilePath).ToDictionary();
                GuiSettings.FromTomlTable(table);

                var nameConversions = GuiSettings.NameConversionDictionary;
                var nameToColors = GuiSettings.ColorDict;
                var plottableSpecies = new List<PlottableSpecies>();

                foreach (var kvp in nameConversions.GroupBy(p => p.Value))
                {
                    var name = kvp.Key;
                    var color = nameToColors.TryGetValue(name, out var toColor) ? toColor : Color.Black;
                    plottableSpecies.Add(new PlottableSpecies
                    {
                        ShortName = name,
                        LongNames = kvp.Select(p => p.Key).ToList(),
                        Color = color
                    });
                }

                foreach (var spec in plottableSpecies)
                    PlottableSpecies.Add(new PlottableSpeciesViewModel(spec));
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

            var newSpecies = new PlottableSpeciesViewModel(new PlottableSpecies
            {
                ShortName = shortName,
                LongNames = longNamesList,
                Color = color
            });

            PlottableSpecies.Add(newSpecies);
            SaveSettings();
        }
    }
}
