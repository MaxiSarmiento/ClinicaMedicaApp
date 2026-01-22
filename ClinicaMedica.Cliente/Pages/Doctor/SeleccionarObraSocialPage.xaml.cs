using ClinicaMedica.Cliente.Models;
using ClinicaMedica.Cliente.Services;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace ClinicaMedica.Cliente.Pages.Doctor;

public partial class SeleccionarObraSocialPage : ContentPage
{
    private readonly ApiService _api;
    private readonly AuthService _auth;
    private List<ObraSocial> _obrasDisponibles;

    public SeleccionarObraSocialPage()
    {
        InitializeComponent();

        var services = App.Current.Handler.MauiContext.Services;
        _api = services.GetService<ApiService>();
        _auth = services.GetService<AuthService>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarObrasDisponibles();
    }

    private async Task CargarObrasDisponibles()
    {
        var token = await SecureStorage.GetAsync("jwt");
        _api.Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var resp = await _api.GetAsync("api/ObrasSociales");

        if (!resp.IsSuccessStatusCode)
        {
            await DisplayAlert("Error", "Error al cargar obras sociales.", "OK");
            return;
        }

        _obrasDisponibles = await resp.Content.ReadFromJsonAsync<List<ObraSocial>>();

        PickerObras.ItemsSource = _obrasDisponibles;
        PickerObras.ItemDisplayBinding = new Binding("Nombre");
    }
    private async void OnVolver(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("PerfilDoctor");
    }
    private async void OnAgregar(object sender, EventArgs e)
    {
        if (PickerObras.SelectedItem is not ObraSocial seleccionada)
        {
            await DisplayAlert("Error", "Seleccioná una obra social.", "OK");
            return;
        }

        var doctorId = await SecureStorage.GetAsync("userId");
        var token = await SecureStorage.GetAsync("jwt");

        _api.Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var dto = new
        {
            DoctorID = int.Parse(doctorId),
            OSID = seleccionada.OSID
        };

        var resp = await _api.PostAsync("api/DoctoresObrasSociales/agregar", dto);

        if (resp.IsSuccessStatusCode)
        {
            await DisplayAlert("OK", "Obra social agregada.", "Cerrar");
            await Navigation.PopAsync();
        }
        else
        {
            var error = await resp.Content.ReadAsStringAsync();
            await DisplayAlert("Error", error, "Cerrar");
        }
    }
}
