using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ClinicaMedica.Backend.Models.Dto
{
    public class UploadEstudioDto
    {
        [Required]
        public int PacienteId { get; set; }

        public string Descripcion { get; set; } = string.Empty;

        public DateTime? Fecha { get; set; }

        [Required]
        public IFormFile? Archivo { get; set; }
    }
}
