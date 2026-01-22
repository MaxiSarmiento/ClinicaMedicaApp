namespace ClinicaMedica.Backend.Models.Dto
{
    public class GenerarTurnosDto
    {
        public int DoctorID { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
    }
}
