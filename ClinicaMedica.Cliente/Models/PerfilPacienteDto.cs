namespace ClinicaMedica.Cliente.Models
{
    public class PerfilPacienteDto
    {
        public int Id { get; set; }

        public string? Nombre { get; set; }
        public string? Apellido { get; set; }
        public string? DNI { get; set; }
        public string? Email { get; set; }
        public string? FechaNacimiento { get; set; }
        public string? ObraSocial { get; set; }
        public int? OSID { get; set; }
       public string? NroSocio { get; set; }
    }
}