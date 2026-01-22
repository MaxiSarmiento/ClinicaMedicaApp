namespace ClinicaMedica.Backend.Models.Dto
{
    public class ActualizarPerfilPacienteDto
    {
        public string? Nombre { get; set; }
        public string? Apellido { get; set; }
        public DateTime? FechaNacimiento { get; set; }
        public string? NroSocio { get; set; }
    }

}
