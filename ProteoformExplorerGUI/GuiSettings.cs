using System.Drawing;

namespace ProteoformExplorer.ProteoformExplorerGUI
{
    public static class GuiSettings
    {
        // from: http://seaborn.pydata.org/tutorial/color_palettes.html (qualitative bright palette)
        public static string[] ChartColorPalette = new string[]
        {
            @"#00ca3d",  // green
            @"#e91405",  // red
            @"#9214e1",  // purple
            @"#9f4a00",  // brown
            @"#f44ac1",  // pink
            @"#fdc807",  // gold
            @"#17d4ff",  // teal
            @"#3729fe",  // blue
            @"#ff8000",  // orange
        };

        public static double ChartTickFontSize = 10;
        public static double ChartHeaderFontSize = 14;
        public static double ChartLegendFontSize = 10;
        public static double ChartLabelFontSize = 13;
        public static ScottPlot.Alignment LegendLocation = ScottPlot.Alignment.UpperRight;
        public static double ChartLineWidth = 1;
        public static double AnnotatedEnvelopeLineWidth = 2;
        public static double ExtractionWindow = 10.0;
        public static double WaterfallSpacingInMm = 12;
        public static Color UnannotatedSpectrumColor = Color.Gray;
        public static Color TicColor = Color.FromArgb(53, 59, 72); // dark gray
        public static Color DeconvolutedColor = Color.FromArgb(0, 168, 255); // blue
        public static Color IdentifiedColor = Color.FromArgb(140, 122, 230); // purple
        public static bool ShowChartGrid = false;
        public static double XLabelRotation = 30;
        public static double DpiScalingX = 1;
        public static double DpiScalingY = 1;
    }
}
