using System.ComponentModel.DataAnnotations;

namespace ClinicaMedica.Cliente.Models
{
    public class Turno
    {
        [Key] public int Id { get; set; }
        public int IdDoctor { get; set; }
        public int? IdPaciente { get; set; }
        public DateTime FechaHora { get; set; }
        public int DuracionMinutos { get; set; }
        public string Doctor { get; set; } = "";
    }
}
