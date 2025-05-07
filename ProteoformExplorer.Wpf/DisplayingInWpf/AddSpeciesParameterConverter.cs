using Easy.Common.Extensions;
using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Data;

namespace ProteoformExplorer.Wpf;

public class AddSpeciesParameterConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length == 3 &&
            values[0] is string shortName && shortName.IsNotNullOrEmpty() &&
            values[1] is string longNames && longNames.IsNotNullOrEmpty())
        {
            if (values[2] is System.Windows.Media.Color mediaColor)
            {
                var drawingColor = System.Drawing.Color.FromArgb(mediaColor.A, mediaColor.R, mediaColor.G, mediaColor.B);
                return new Tuple<string, string, System.Drawing.Color>(shortName, longNames, drawingColor);
            }
            else if (values[2] is System.Drawing.Color drawingColor)
            {
                return new Tuple<string, string, System.Drawing.Color>(shortName, longNames, drawingColor);
            }
        }

        return null;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}