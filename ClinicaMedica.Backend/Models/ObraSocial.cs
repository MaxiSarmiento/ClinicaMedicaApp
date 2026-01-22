using System.ComponentModel.DataAnnotations;

namespace ClinicaMedica.Backend.Models
{
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("ObraSocial")]
    public class ObraSocial
    {
        [Key]
        public int OSID { get; set; }
        public string Nombre { get; set; } = string.Empty;
    }

}

