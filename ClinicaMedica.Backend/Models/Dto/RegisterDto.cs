namespace ClinicaMedica.Backend.Models.Dto
{
    public class RegisterDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Rol { get; set; } = "Paciente";
        public string DNI { get; set; } = string.Empty;
    }
}
