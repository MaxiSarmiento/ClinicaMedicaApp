using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClinicaMedica.Cliente.Models
{
    public class DoctorPerfilDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string DNI { get; set; }
        public string Email { get; set; }

        public List<ObraSocialDto> ObrasSociales { get; set; } = new();
        public List<EspecialidadDto> Especialidades { get; set; } = new();
    }

    public class ObraSocialDto
    {
        public int OSID { get; set; }
        public string Nombre { get; set; }
    }

    public class EspecialidadDto
    {
        public int EspID { get; set; }
        public string Nombre { get; set; }
    }

}
