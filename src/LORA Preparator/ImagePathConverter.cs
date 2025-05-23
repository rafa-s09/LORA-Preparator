using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using System;
using System.Globalization;
using System.IO;

namespace LORA_Preparator;

public class ImagePathConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string path && File.Exists(path))
        {
            try
            {
                return new Bitmap(path);
            }
            catch
            {
                // Fallback image or null
                return null;
            }
        }
        return null; // Or a default image
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
