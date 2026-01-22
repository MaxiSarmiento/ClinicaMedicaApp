namespace ClinicaMedica.Cliente.Models
{
    public class UsuarioPacienteDto
    {
        public int Id { get; set; }
        public string? Nombre { get; set; }
        public string? Apellido { get; set; }
        public string? DNI { get; set; }
        public string? Email { get; set; }
        public int? OSID { get; set; }

        DateOnly? FechaNacimiento = null;
        public string? ObraSocial { get; set; }
        public string? NroSocio { get; set; }
    }
}
