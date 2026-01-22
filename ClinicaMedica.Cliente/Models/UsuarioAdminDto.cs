using System.ComponentModel;

namespace ClinicaMedica.Cliente.Models
{
    public class UsuarioAdminDto : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public string Apellido { get; set; } = "";
        public string Email { get; set; } = "";

        private string _rol = "";
        public string Rol
        {
            get => _rol;
            set
            {
                if (_rol == value) return;
                _rol = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Rol)));
            }
        }

        public string NombreCompleto => $"{Nombre} {Apellido}";

        private bool _bloqueado;
        public bool Bloqueado
        {
            get => _bloqueado;
            set
            {
                if (_bloqueado == value) return;
                _bloqueado = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Bloqueado)));
            }
        }
    }
}
