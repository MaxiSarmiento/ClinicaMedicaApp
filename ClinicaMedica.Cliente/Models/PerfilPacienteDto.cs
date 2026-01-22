using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClinicaMedica.Cliente.Models
{
    class PerfilPacienteDto
    {
        public string? Nombre { get; set; }
        public string? Apellido { get; set; }
        public string Dni {  get; set; }    
        public string ObraSocial { get; set; }
        public string Email { get; set; }
        public string? FechaNacimiento { get; set; }
        public string? NroSocio { get; set; }
    }
}
