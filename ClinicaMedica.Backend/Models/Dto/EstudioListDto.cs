namespace ClinicaMedica.Backend.Models.Dto
{
    public class EstudioListDto
    {
        public int Id { get; set; }
        public string NombreArchivo { get; set; } = "";
        public DateTime Fecha { get; set; }
        public string Descripcion { get; set; } = "";
    }

}
