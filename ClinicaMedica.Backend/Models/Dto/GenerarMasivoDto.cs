namespace ClinicaMedica.Backend.Models.Dto
{
    public class GenerarMasivoDto
    {
        public DateTime FechaInicio { get; set; }  
        public DateTime FechaFin { get; set; }     

        public TimeSpan HoraInicio { get; set; }   
        public TimeSpan HoraFin { get; set; }       
        public int DuracionMinutos { get; set; }    

        
        public List<int> ExcluirDiasSemana { get; set; } = new();

       
        public int? MaxTurnosPorDia { get; set; }
    }
}
