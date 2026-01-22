using ClinicaMedica.Cliente.Models;
using ClinicaMedica.Cliente.Services;

namespace ClinicaMedica.Cliente.Pages.Paciente;

public partial class VerEstudiosPacientePage : ContentPage
{
    private readonly EstudiosService _estudiosService;
    private List<EstudioListDto> _estudios = new();

    public VerEstudiosPacientePage(EstudiosService estudiosService)
    {
        InitializeComponent();
        _estudiosService = estudiosService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarEstudios();
    }

    private async Task CargarEstudios()
    {
        try
        {
            Loading.IsVisible = true;
            Loading.IsRunning = true;

            var idStr = await SecureStorage.GetAsync("id_usuario"); 
            if (!int.TryParse(idStr, out var pacienteId))
            {
                await DisplayAlert("Error", "No se pudo identificar al usuario logueado.", "OK");
                return;
            }

            _estudios = await _estudiosService.GetEstudiosPaciente(pacienteId);
            ListadoEstudios.ItemsSource = _estudios;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            Loading.IsRunning = false;
            Loading.IsVisible = false;
        }
    }

    private async void OnVerArchivo(object sender, EventArgs e)
    {
        int estudioId;

        if (sender is Button btn && btn.CommandParameter is int i)
            estudioId = i;
        else if (sender is Button btn2 && btn2.CommandParameter is string s && int.TryParse(s, out var parsed))
            estudioId = parsed;
        else
        {
            await DisplayAlert("Error", "No se pudo determinar el estudio.", "OK");
            return;
        }

        try
        {
            Loading.IsVisible = true;
            Loading.IsRunning = true;

            var bytes = await _estudiosService.Descargar(estudioId);
            if (bytes == null || bytes.Length == 0)
            {
                await DisplayAlert("Error", "No se pudo descargar el archivo.", "OK");
                return;
            }

            var item = _estudios.FirstOrDefault(x => x.Id == estudioId);
            var nombre = item?.NombreArchivo;

            if (string.IsNullOrWhiteSpace(nombre))
                nombre = $"estudio_{estudioId}";

            // Evitar caracteres raros en nombre
            foreach (var c in Path.GetInvalidFileNameChars())
                nombre = nombre.Replace(c, '_');

            var path = Path.Combine(FileSystem.CacheDirectory, nombre);

            File.WriteAllBytes(path, bytes);

            // Abre visor nativo (PDF viewer / Galería / etc)
            await Launcher.Default.OpenAsync(new OpenFileRequest
            {
                File = new ReadOnlyFile(path)
            });
        }
        catch (UnauthorizedAccessException)
        {
            await DisplayAlert("Sesión", "Sesión expirada o token inválido. Volvé a iniciar sesión.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            Loading.IsRunning = false;
            Loading.IsVisible = false;
        }
    }

    private async void OnVolver(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("HomePaciente");
    }
}
