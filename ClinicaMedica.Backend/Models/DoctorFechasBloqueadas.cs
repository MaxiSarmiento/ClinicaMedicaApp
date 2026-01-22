using System.ComponentModel.DataAnnotations;

namespace ClinicaMedica.Backend.Models
{
    public class DoctorFechasBloqueadas
    {
        [Key]
        public int Id { get; set; }

        public int DoctorID { get; set; }

        public DateTime Fecha { get; set; }

      
        public string? Motivo { get; set; }

        public Usuario? Doctor { get; set; }
    }
}
