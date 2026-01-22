using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClinicaMedica.Cliente.Models
{

        public class UploadEstudioDto
        {
            public int PacienteId { get; set; }
            public string Descripcion { get; set; } = string.Empty;
            public DateTime? Fecha { get; set; }
            public FileResult? Archivo { get; set; }
        }
    
}
