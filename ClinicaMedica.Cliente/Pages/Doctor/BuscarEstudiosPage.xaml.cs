using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using ClinicaMedica.Cliente.Models;
using ClinicaMedica.Cliente.Services;

namespace ClinicaMedica.Cliente.Pages.Doctor
{
    public partial class BuscarEstudiosPage : ContentPage
    {
        private readonly ApiService _api;

        public BuscarEstudiosPage(ApiService api)
        {
            InitializeComponent();
            _api = api;
        }

        private async void OnBuscar(object sender, EventArgs e)
        {
            // Normalizar inputs y escapar para una query segura
            var dni = (DniEntry.Text ?? string.Empty).Trim();
            var nombre = (NombreEntry.Text ?? string.Empty).Trim();
            var apellido = (ApellidoEntry.Text ?? string.Empty).Trim();

            var url =
                $"api/estudios/buscar?dni={Uri.EscapeDataString(dni)}" +
                $"&nombre={Uri.EscapeDataString(nombre)}" +
                $"&apellido={Uri.EscapeDataString(apellido)}";

            try
            {
                var lista = await _api.GetJsonAsync<List<Estudio>>(url);
                ResultadosList.ItemsSource = lista ?? new();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"No se pudieron cargar los estudios.\n{ex.Message}", "OK");
            }
        }
        private async void OnVolver(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("PerfilDoctor");
        }
        private async void OnVerPdf(object sender, EventArgs e)
        {
            if (sender is not Button btn || btn.BindingContext is not Estudio est)
                return;

            var baseUrl = Config.ApiConfig.BaseUrl?.TrimEnd('/') ?? string.Empty;
            var path = (est.RutaArchivo ?? string.Empty).TrimStart('/');

            if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(path))
            {
                await DisplayAlert("Error", "No se puede abrir el PDF: ruta inválida.", "OK");
                return;
            }

            var full = $"{baseUrl}/{path}";
            if (Uri.TryCreate(full, UriKind.Absolute, out var uri))
            {
                await Launcher.OpenAsync(uri);
            }
            else
            {
                await DisplayAlert("Error", $"URL inválida: {full}", "OK");
            }
        }
    }
}
