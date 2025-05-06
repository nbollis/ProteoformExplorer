using Nett;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace ProteoformExplorer.GuiFunctions
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

        public static Color UnannotatedSpectrumColor = Color.Gray;
        public static Color TicColor = Color.FromArgb(53, 59, 72); // dark gray
        public static Color DeconvolutedColor = Color.FromArgb(0, 255, 255); // blue
        public static Color IdentifiedColor = Color.FromArgb(102, 0, 204); // purple
        public static Color RtIndicatorColor = Color.Red;
        public static int IntegrationFillAlpha = 50;
        public static int FillAlpha = 190;
        public static double ChartTickFontSize = 10;
        public static double ChartHeaderFontSize = 14;
        public static double ChartLegendFontSize = 10;
        public static double ChartAxisLabelFontSize = 13;
        public static double ChartLineWidth = 1;
        public static double AnnotatedEnvelopeLineWidth = 2;
        public static ScottPlot.Alignment LegendLocation = ScottPlot.Alignment.UpperRight;
        public static bool ShowChartGrid = false;
        public static double XLabelRotation = 30;
        public static bool FillWaterfall = true;
        public static bool FillSideView = false;
        public static bool WaterfallXics = false;
        public static double RtExtractionWindow = 10.0; // in minutes
        public static double WaterfallSpacing = 10; // in millimeters
        public static double MassHistogramBinWidth = 500; // in daltons
        public static bool DpiScaling = true;
        public static double DpiScalingX = 1;
        public static double DpiScalingY = 1;

        #region Coloring identified Tics

        private static Dictionary<string, Color> ColorDict;

        private static Queue<Color> ColorQueue = new Queue<Color>(new[]
        {
            Color.Purple, 
            Color.Green,
            Color.OrangeRed,
            Color.Yellow, 
            Color.Pink,
            Color.DarkMagenta,
            Color.DarkCyan,
            Color.DarkGoldenrod,
            Color.DarkOliveGreen,
            Color.DarkRed,
            Color.DarkSlateBlue,
            Color.DarkTurquoise,
            Color.DarkViolet,
            Color.DeepPink,
            Color.DeepSkyBlue,
        });

        public static Color ConvertStringToColor(this string input)
        {
            ColorDict ??= new Dictionary<string, Color>();

            if (ColorDict.TryGetValue(input, out var color))
            {
                return color;
            }

            if (ColorQueue.Count == 0)
            {
                throw new Exception("Ran out of colors for identified TICs");
            }

            color = ColorQueue.Dequeue();
            ColorDict.Add(input, color);

            return color;
        }
        

        #endregion

        public static TomlTable ToTomlTable()
        {
            TomlTable table = Toml.Create();

            Type type = typeof(GuiSettings);

            var fields = type.GetFields();

            foreach (var field in fields)
            {
                var name = field.Name;

                if (name.ToLower() == "dpiscalingx" || name.ToLower() == "dpiscalingy")
                {
                    continue;
                }

                var v = field.GetValue(null);

                if (v is int val)
                {
                    table.Add(name, val);
                }
                else if (v is double dval)
                {
                    table.Add(name, dval);
                }
                else if (v is bool b)
                {
                    table.Add(name, b);
                }
                else if (v is ScottPlot.Alignment alignment)
                {
                    table.Add(name, alignment);
                }
                else if (v is Color color)
                {
                    table.Add(name, color);
                }
                else if (v is string[] strarr)
                {
                    table.Add(name, strarr);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            return table;
        }

        public static void FromTomlTable(Dictionary<string, object> tomlDictionary)
        {
            Type type = typeof(GuiSettings);
            var fields = type.GetFields();

            foreach (var field in fields)
            {
                var name = field.Name;

                if (tomlDictionary.TryGetValue(name, out var v))
                {
                    if (v is int val)
                    {
                        field.SetValue(null, val);
                    }
                    else if (v is double dval)
                    {
                        field.SetValue(null, dval);
                    }
                    else if (v is bool b)
                    {
                        field.SetValue(null, b);
                    }
                    else if (v is ScottPlot.Alignment alignment)
                    {
                        field.SetValue(null, alignment);
                    }
                    else if (v is Color color)
                    {
                        field.SetValue(null, color);
                    }
                    else if (v is object[] objarr)
                    {
                        if (objarr[1] is string str)
                        {
                            field.SetValue(null, objarr.Select(p => (string)p).ToArray());
                        }
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
            }
        }
    }
}
