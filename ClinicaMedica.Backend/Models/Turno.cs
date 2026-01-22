using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ClinicaMedica.Backend.Models
{
    public class Turno
    {
        public int Id { get; set; }
        public int IdDoctor { get; set; }
        public int? IdPaciente { get; set; }
        public DateTime FechaHora { get; set; }
        public int DuracionMinutos { get; set; }
        public string Estado { get; set; } = "Libre";

        [JsonIgnore] public Usuario? Doctor { get; set; }
        [JsonIgnore] public Usuario? Paciente { get; set; }
    }
}