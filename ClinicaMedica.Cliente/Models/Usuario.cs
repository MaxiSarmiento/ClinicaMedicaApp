using System.ComponentModel.DataAnnotations;

namespace ClinicaMedica.Cliente.Models
{
    public class Usuario
    {
        [Key] public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public string Apellido { get; set; } = "";
        public string Email { get; set; } = "";
        public string Rol { get; set; } = "";

        public string DNI { get; set; } = "";
        public string? ObraSocial { get; set; }
        public DateOnly? FechaNacimiento { get; set; }
        public string NombreCompleto => $"{Nombre} {Apellido}";
    }

}
