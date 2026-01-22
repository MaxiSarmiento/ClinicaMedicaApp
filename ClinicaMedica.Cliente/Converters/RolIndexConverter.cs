using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace ClinicaMedica.Cliente.Converters
{
    public class RolIndexConverter : IValueConverter
    {
        private static readonly string[] Roles = { "Paciente", "Doctor", "Admin" };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var rol = (value as string)?.Trim();

            if (string.IsNullOrWhiteSpace(rol))
                return -1;

            for (int i = 0; i < Roles.Length; i++)
            {
                if (string.Equals(Roles[i], rol, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return -1;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int i && i >= 0 && i < Roles.Length)
                return Roles[i];

            return "Paciente";
        }
    }
}
