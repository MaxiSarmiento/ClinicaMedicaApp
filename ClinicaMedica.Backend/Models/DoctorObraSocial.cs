using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicaMedica.Backend.Models
{
    public class DoctorObraSocial
    {
        public int DoctorID { get; set; }
        public int OSID { get; set; }

        [ForeignKey(nameof(DoctorID))]
        public Usuario Doctor { get; set; } = null!;

        [ForeignKey(nameof(OSID))]
        public ObraSocial ObraSocial { get; set; } = null!;
    }

}
