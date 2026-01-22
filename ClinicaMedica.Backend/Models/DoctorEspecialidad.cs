using System.ComponentModel.DataAnnotations;

namespace ClinicaMedica.Backend.Models
{
    public class DoctorEspecialidad
    {
        public int ID { get; set; }
        public int DoctorID { get; set; }
        public int EspID { get; set; }

        public Especialidad Especialidad { get; set; }
        public Usuario Doctor { get; set; }
    }
}
