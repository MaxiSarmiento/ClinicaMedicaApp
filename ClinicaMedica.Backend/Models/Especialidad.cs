using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicaMedica.Backend.Models
{
    [Table("Especialidades")]
    public class Especialidad
    {
        [Key] public int EspID { get; set; }
        public string? Nombre { get; set; } 
    }
}
