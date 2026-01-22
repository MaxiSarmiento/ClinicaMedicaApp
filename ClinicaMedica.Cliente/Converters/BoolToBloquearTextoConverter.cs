using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace ClinicaMedica.Cliente.Converters
{
    public class BoolToBloquearTextoConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var bloqueado = value is bool b && b;
            return bloqueado ? "Desbloquear" : "Bloquear";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
