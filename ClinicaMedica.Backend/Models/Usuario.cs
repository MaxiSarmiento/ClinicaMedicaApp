using System.ComponentModel.DataAnnotations;

namespace ClinicaMedica.Backend.Models
{
    public class Usuario
    {
        [Key] public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Apellido { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        public string Rol { get; set; } = string.Empty; // Paciente, Doctor, Admin

        public string DNI { get; set; } = string.Empty;
        public DateOnly? FechaNacimiento { get; set; }

        public DateTime? FechaRegistro { get; set; }
        public int? OSID { get; set; }
        public string? NroSocio { get; set; }
        public bool Bloqueado { get; set; }



        public ICollection<DoctorEspecialidad> Especialidades { get; set; } = new List<DoctorEspecialidad>();
        public ICollection<DoctorObraSocial> ObrasSociales { get; set; } = new List<DoctorObraSocial>();
        public ICollection<Turno> TurnosComoDoctor { get; set; } = new List<Turno>();
        public ICollection<Turno> TurnosComoPaciente { get; set; } = new List<Turno>();
        public ICollection<DoctorAgenda> Agendas { get; set; } = new List<DoctorAgenda>();


    }
}
