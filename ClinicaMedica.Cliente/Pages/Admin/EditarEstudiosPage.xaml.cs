using ClinicaMedica.Cliente.Models;
using ClinicaMedica.Cliente.Services;

namespace ClinicaMedica.Cliente.Pages.Admin;

public partial class EditarEstudiosPage : ContentPage
{
    private readonly UsuariosService _usuariosService;
    private readonly EstudiosService _estudiosService;

    private List<Usuario> _pacientes = new();
    private List<EstudioListDto> _estudios = new();

    public EditarEstudiosPage(UsuariosService usuariosService, EstudiosService estudiosService)
    {
        InitializeComponent();
        _usuariosService = usuariosService;
        _estudiosService = estudiosService;

        _ = CargarPacientes();
    }

    private async Task CargarPacientes()
    {
        try
        {
            _pacientes = await _usuariosService.GetPacientes();

            PacientePicker.ItemsSource = _pacientes;

           
            PacientePicker.ItemDisplayBinding = new Binding("NombreCompleto");
           
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async void OnPacienteChanged(object sender, EventArgs e)
    {
        if (PacientePicker.SelectedItem is not Usuario paciente)
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

    private async void OnVerEstudio(object sender, EventArgs e)
    {
        int id;

        // CommandParameter puede ser int o string
        if (sender is Button btn && btn.CommandParameter is int i)
            id = i;
        else if (sender is Button btn2 && btn2.CommandParameter is string s && int.TryParse(s, out var parsed))
            id = parsed;
        else
        {
            await DisplayAlert("Error", "No se pudo identificar el estudio.", "OK");
            return;
        }

        try
        {
            var bytes = await _estudiosService.Descargar(id);

            if (bytes.Length == 0)
            {
                await DisplayAlert("Error", "No se pudo descargar el archivo.", "OK");
                return;
            }

            
            var item = _estudios.FirstOrDefault(x => x.Id == id);
            var nombre = item?.NombreArchivo;
            if (string.IsNullOrWhiteSpace(nombre))
                nombre = $"estudio_{id}.pdf";

            var tempPath = Path.Combine(FileSystem.CacheDirectory, $"{id}_{nombre}");
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

    private async void OnBorrarEstudio(object sender, EventArgs e)
    {
        int id;

        if (sender is Button btn && btn.CommandParameter is int i)
            id = i;
        else if (sender is Button btn2 && btn2.CommandParameter is string s && int.TryParse(s, out var parsed))
            id = parsed;
        else
        {
            await DisplayAlert("Error", "No se pudo identificar el estudio.", "OK");
            return;
        }

        bool confirm = await DisplayAlert("Confirmar", "¿Eliminar el estudio?", "Sí", "No");
        if (!confirm) return;

        try
        {
            await _estudiosService.EliminarEstudio(id);

            await DisplayAlert("Éxito", "Estudio eliminado.", "OK");

            // Recargar lista del paciente seleccionado
            if (PacientePicker.SelectedItem is Usuario paciente)
            {
                _estudios = await _estudiosService.GetEstudiosPaciente(paciente.Id);
                EstudiosList.ItemsSource = _estudios;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async void OnVolver(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("HomeAdmin");
    }
}
