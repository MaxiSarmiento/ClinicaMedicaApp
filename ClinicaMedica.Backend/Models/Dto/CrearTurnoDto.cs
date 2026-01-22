namespace ClinicaMedica.Backend.Models.Dto
{
    public class CrearTurnoDto
    {
        public DateTime FechaHora { get; set; }
        public int DuracionMinutos { get; set; }
        public int IdDoctor { get; set; } 
    }
}
