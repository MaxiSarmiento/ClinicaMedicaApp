using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ClinicaMedica.Cliente.Models;
using ClinicaMedica.Cliente.Services;

namespace ClinicaMedica.Cliente.Pages.Admin;

public class SubirEstudioViewModel : INotifyPropertyChanged
{
    private readonly UsuariosService _usuariosService;
    private readonly EstudiosService _estudiosService;

    private List<Usuario> _pacientes = new();

    public ObservableCollection<PacienteItem> PacientesFiltrados { get; } = new();

    private string _filtro = "";
    public string Filtro
    {
        get => _filtro;
        set
        {
            if (_filtro == value) return;
            _filtro = value;
            OnPropertyChanged();
            AplicarFiltro();
        }
    }

    public ICommand SeleccionarPacienteCommand { get; }

    public SubirEstudioViewModel(
        UsuariosService usuariosService,
        EstudiosService estudiosService)
    {
        _usuariosService = usuariosService;
        _estudiosService = estudiosService;

        SeleccionarPacienteCommand = new Command<PacienteItem>(p =>
        {
            // solo para confirmar que funciona
            Application.Current.MainPage.DisplayAlert(
                "Paciente seleccionado",
                p.Display,
                "OK");
        });

        _ = InicializarAsync();
    }

    private async Task InicializarAsync()
    {
        _pacientes = await _usuariosService.GetPacientes();
        AplicarFiltro();
    }

    private void AplicarFiltro()
    {
        var f = (Filtro ?? "").Trim().ToLowerInvariant();

        PacientesFiltrados.Clear();

        foreach (var p in _pacientes)
        {
            var display = $"{p.Id} - {p.Nombre} {p.Apellido} - DNI: {p.DNI}";

            if (string.IsNullOrWhiteSpace(f) ||
                display.ToLowerInvariant().Contains(f))
            {
                PacientesFiltrados.Add(new PacienteItem
                {
                    Id = p.Id,
                    Display = display
                });
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class PacienteItem
{
    public int Id { get; set; }
    public string Display { get; set; } = "";
}
