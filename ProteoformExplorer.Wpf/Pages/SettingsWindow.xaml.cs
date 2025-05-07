using ProteoformExplorer.GuiFunctions;
using System;
using System.Windows;

namespace ProteoformExplorer.Wpf.Pages;

/// <summary>
/// Interaction logic for SettingsWindow.xaml
/// </summary>
public partial class SettingsWindow : Window
{
    SettingsViewModel Data;
    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        Data = viewModel;
        DataContext = Data;
    }

    private void SettingsWindow_OnClosed(object sender, EventArgs e)
    {
        Data.UpdateStaticSettings();
        GuiSettings.SaveSettingsToFile();
    }
}
