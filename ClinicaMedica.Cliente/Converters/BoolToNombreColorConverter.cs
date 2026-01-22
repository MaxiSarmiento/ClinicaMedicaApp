using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace ClinicaMedica.Cliente.Converters
{
    public class BoolToNombreColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var bloqueado = value is bool b && b;

            if (bloqueado)
                return Colors.Orange;

            return Application.Current?.RequestedTheme == AppTheme.Dark
                ? Colors.White
                : Colors.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
