using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ProteoformExplorer.Wpf;

public class ColorToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is System.Drawing.Color drawingColor)
        {
            return new SolidColorBrush(Color.FromArgb(drawingColor.A, drawingColor.R, drawingColor.G, drawingColor.B));
        }
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is SolidColorBrush brush)
        {
            var mediaColor = brush.Color;
            return System.Drawing.Color.FromArgb(mediaColor.A, mediaColor.R, mediaColor.G, mediaColor.B);
        }
        return null;
    }
}

public class DrawingColorToWindowsColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is System.Drawing.Color drawingColor)
        {
            return Color.FromArgb(drawingColor.A, drawingColor.R, drawingColor.G, drawingColor.B);
        }
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Color mediaColor)
        {
            return System.Drawing.Color.FromArgb(mediaColor.A, mediaColor.R, mediaColor.G, mediaColor.B);
        }
        return null;
    }
}
