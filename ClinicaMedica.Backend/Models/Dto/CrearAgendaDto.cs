namespace ClinicaMedica.Backend.Models.Dto
{
    public class CrearAgendaDto
    {
        public int DoctorID { get; set; }
        public int DiaSemana { get; set; }  
        public TimeSpan HoraInicio { get; set; }
        public TimeSpan HoraFin { get; set; }
        public int DuracionMinutos { get; set; }
    }
}
