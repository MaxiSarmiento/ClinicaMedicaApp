using ClinicaMedica.Cliente.Models;
using ClinicaMedica.Cliente.Services;

namespace ClinicaMedica.Cliente.Pages.Admin;

public partial class SubirEstudioPage : ContentPage
{
    private readonly UsuariosService _usuariosService;
    private readonly EstudiosService _estudiosService;

    private List<Usuario> _pacientes = new();
    private List<PacienteItem> _filtrados = new();

    private Usuario? _pacienteSeleccionado;
    private FileResult? _archivo;

    public SubirEstudioPage(UsuariosService usuariosService, EstudiosService estudiosService)
    {
        InitializeComponent();
        _usuariosService = usuariosService;
        _estudiosService = estudiosService;

        FechaPicker.Date = DateTime.Today;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarPacientes();
    }

    private async Task CargarPacientes()
    {
        try
        {
            _pacientes = await _usuariosService.GetPacientes();

           
            AplicarFiltro(Buscador.Text ?? "");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error al cargar pacientes", ex.Message, "OK");
        }
    }

    private void OnBuscarChanged(object sender, TextChangedEventArgs e)
    {
        AplicarFiltro(e.NewTextValue ?? "");
    }

    private void AplicarFiltro(string texto)
    {
        var f = (texto ?? "").Trim().ToLowerInvariant();

        _filtrados = _pacientes
            .Select(p => new PacienteItem
            {
                Id = p.Id,
                Display = $"{p.Id} - {p.Nombre} {p.Apellido} - DNI: {p.DNI}"
            })
            .Where(x => string.IsNullOrWhiteSpace(f) || x.Display.ToLowerInvariant().Contains(f))
            .OrderBy(x => x.Display)
            .ToList();

        PacientesList.ItemsSource = _filtrados;
    }

    private async void OnSeleccionarPaciente(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is PacienteItem item)
        {
            _pacienteSeleccionado = _pacientes.FirstOrDefault(p => p.Id == item.Id);

            if (_pacienteSeleccionado == null)
            {
                await DisplayAlert("Error", "Paciente inválido.", "OK");
                return;
            }

            PacienteSeleccionadoLabel.Text = $"Paciente seleccionado: {item.Display}";
        }
    }

    private async void OnPickFile(object sender, EventArgs e)
    {
        _archivo = await FilePicker.PickAsync();
        ArchivoLabel.Text = _archivo != null ? _archivo.FileName : "Ningún archivo";
    }

    private async void OnUpload(object sender, EventArgs e)
    {
        if (_pacienteSeleccionado == null)
        {
            await DisplayAlert("Error", "Seleccioná un paciente.", "OK");
            return;
        }

        if (_archivo == null)
        {
            await DisplayAlert("Error", "Seleccioná un archivo.", "OK");
            return;
        }

        try
        {
            var dto = new UploadEstudioDto
            {
                PacienteId = _pacienteSeleccionado.Id,
                Descripcion = DescripcionEditor.Text ?? "",
                Fecha = FechaPicker.Date,
                Archivo = _archivo
            };

            await _estudiosService.SubirEstudio(dto);

            await DisplayAlert("Éxito", "Estudio subido correctamente.", "OK");

            DescripcionEditor.Text = "";
            _archivo = null;
            ArchivoLabel.Text = "Ningún archivo";
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error al subir", ex.Message, "OK");
        }
    }

    private async void OnVolver(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("HomeAdmin");
    }

    private class PacienteItem
    {
        public int Id { get; set; }
        public string Display { get; set; } = "";
    }
}
