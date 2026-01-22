using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClinicaMedica.Cliente.Models
{
    internal class CalendarDayVm
    {
        public DateTime Date { get; set; }
        public bool IsCurrentMonth { get; set; }
        public bool IsOccupied { get; set; }     
        public bool IsSelected { get; set; }
        public string DayText => Date.Day.ToString();
    }
}

