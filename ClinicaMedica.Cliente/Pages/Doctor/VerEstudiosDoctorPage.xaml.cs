using ClinicaMedica.Cliente.Models;
using ClinicaMedica.Cliente.Services;

namespace ClinicaMedica.Cliente.Pages.Doctor;

public partial class VerEstudiosDoctorPage : ContentPage
{
    private readonly UsuariosService _usuariosService;
    private readonly EstudiosService _estudiosService;

    private List<PacientePickerItem> _pacientes = new();
    private List<PacientePickerItem> _pacientesFiltrados = new();

    private List<EstudioListDto> _estudios = new();

    public VerEstudiosDoctorPage(UsuariosService usuariosService, EstudiosService estudiosService)
    {
        InitializeComponent();
        _usuariosService = usuariosService;
        _estudiosService = estudiosService;

        _ = CargarPacientes();
    }

    private class PacientePickerItem
    {
        public int Id { get; set; }
        public string DNI { get; set; } = "";
        public string Nombre { get; set; } = "";
        public string Apellido { get; set; } = "";

        public string Display => $"{Id} - {Apellido}, {Nombre} (DNI: {DNI})";
    }

    private async Task CargarPacientes()
    {
        try
        {
            var pacientes = await _usuariosService.GetPacientes();

            _pacientes = pacientes.Select(p => new PacientePickerItem
            {
                Id = p.Id,
                DNI = p.DNI ?? "",
                Nombre = p.Nombre ?? "",
                Apellido = p.Apellido ?? ""
            }).ToList();

            AplicarFiltro();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private void OnFiltroChanged(object sender, TextChangedEventArgs e)
    {
        AplicarFiltro();
    }

    private void AplicarFiltro()
    {
        var q = (FiltroPaciente.Text ?? "").Trim().ToLowerInvariant();

        _pacientesFiltrados = string.IsNullOrWhiteSpace(q)
            ? _pacientes
            : _pacientes.Where(p =>
                (p.DNI ?? "").ToLowerInvariant().Contains(q) ||
                (p.Nombre ?? "").ToLowerInvariant().Contains(q) ||
                (p.Apellido ?? "").ToLowerInvariant().Contains(q))
              .Take(40)
              .ToList();

        PacientePicker.ItemsSource = _pacientesFiltrados;

      
        PacientePicker.SelectedItem = null;
        EstudiosList.ItemsSource = null;
        _estudios.Clear();
    }

    private async void OnPacienteChanged(object sender, EventArgs e)
    {
        if (PacientePicker.SelectedItem is not PacientePickerItem paciente)
            return;

        try
        {
            _estudios = await _estudiosService.GetEstudiosPaciente(paciente.Id);
            EstudiosList.ItemsSource = _estudios;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }
    private async void OnVolver(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("HomeDoctor");
    }
    private async void OnVerEstudio(object sender, EventArgs e)
    {
        int estudioId;

        if (sender is Button btn && btn.CommandParameter is int i)
            estudioId = i;
        else if (sender is Button btn2 && btn2.CommandParameter is string s && int.TryParse(s, out var parsed))
            estudioId = parsed;
        else
        {
            await DisplayAlert("Error", "No se pudo identificar el estudio.", "OK");
            return;
        }

        try
        {
            var bytes = await _estudiosService.Descargar(estudioId);

            if (bytes.Length == 0)
            {
                await DisplayAlert("Error", "No se pudo descargar el estudio.", "OK");
                return;
            }

            var estudio = _estudios.FirstOrDefault(e => e.Id == estudioId);
            var nombre = estudio?.NombreArchivo;

            if (string.IsNullOrWhiteSpace(nombre))
                nombre = $"estudio_{estudioId}.pdf";

   
            var safeName = $"{estudioId}_{nombre}";
            var tempPath = Path.Combine(FileSystem.CacheDirectory, safeName);

            File.WriteAllBytes(tempPath, bytes);

            await Launcher.Default.OpenAsync(new OpenFileRequest
            {
                File = new ReadOnlyFile(tempPath)
            });
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }
}
