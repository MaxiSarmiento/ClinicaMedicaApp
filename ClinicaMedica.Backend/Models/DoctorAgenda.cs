using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicaMedica.Backend.Models
{
    public class DoctorAgenda
    {
        public int Id { get; set; }

        public int DoctorID { get; set; }

        [ForeignKey(nameof(DoctorID))]
        public Usuario Doctor { get; set; } = null!;  

        public int DiaSemana { get; set; }
        public TimeSpan HoraInicio { get; set; }
        public TimeSpan HoraFin { get; set; }
        public int DuracionMinutos { get; set; }
    }
}
