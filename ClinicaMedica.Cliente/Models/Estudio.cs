using System.ComponentModel.DataAnnotations;

namespace ClinicaMedica.Cliente.Models
{
    public class Estudio
    {
        [Key] public int Id { get; set; }
        public int IdPaciente { get; set; }
        public string NombreArchivo { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public string RutaArchivo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string Dni { get; set; } = string.Empty;
        public string NombrePaciente { get; set; } = string.Empty;
        public string ApellidoPaciente { get; set; } = string.Empty;

        public Usuario? Paciente { get; set; }
    }
}
