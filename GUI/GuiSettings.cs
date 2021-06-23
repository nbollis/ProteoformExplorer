using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace GUI
{
    public static class GuiSettings
    {
        // from: http://seaborn.pydata.org/tutorial/color_palettes.html (qualitative bright palette)
        public static string[] ColorPalette = new string[]
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

        public static double ChartTickFontSize = 12;
        public static double ChartHeaderFontSize = 14;
        public static double ChartLineWidth = 1;
        public static double AnnotatedEnvelopeLineWidth = 2;
        public static double ExtractionWindow = 10.0;
        public static double WaterfallSpacingInMm = 12;
        public static Color UnannotatedSpectrumColor = Color.Gray;
    }
}
