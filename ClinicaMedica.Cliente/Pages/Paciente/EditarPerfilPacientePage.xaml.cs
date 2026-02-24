using ClinicaMedica.Cliente.Models;
using ClinicaMedica.Cliente.Services;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace ClinicaMedica.Cliente.Pages.Paciente;

public partial class EditarPerfilPacientePage : ContentPage
{
    private readonly ApiService _api;
    private int _userId;

    private int? _osidActual = null;
    private int? _osidSeleccionada = null;
    private string? _obraSocialNombre = null;

    public EditarPerfilPacientePage(ApiService api)
    {
        InitializeComponent();
        _api = api;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarPerfilAsync();
    }

    private async Task CargarPerfilAsync()
    {
        try
        {
            Loading.IsVisible = Loading.IsRunning = true;

            var token = await SecureStorage.GetAsync("token");
            var idStr = await SecureStorage.GetAsync("userId");

            if (string.IsNullOrWhiteSpace(token) || !int.TryParse(idStr, out _userId) || _userId <= 0)
            {
                await DisplayAlert("Sesión", "Sesión inválida. Volvé a iniciar sesión.", "OK");
                await Shell.Current.GoToAsync("Login");
                return;
            }

            _api.Client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var datos = await _api.GetJsonAsync<PerfilPacienteDto>($"api/usuarios/paciente/{_userId}");

            if (datos == null)
            {
                await DisplayAlert("Perfil", "No se encontraron datos del paciente.", "OK");
                return;
            }

            TxtNombre.Text = datos.Nombre ?? "";
            TxtApellido.Text = datos.Apellido ?? "";
            TxtDni.Text = datos.DNI ?? "";
            TxtEmail.Text = datos.Email ?? "";

            if (!string.IsNullOrWhiteSpace(datos.FechaNacimiento) &&
                DateTime.TryParse(datos.FechaNacimiento, out var fn))
            {
                FechaNacimientoPicker.Date = fn.Date;
            }

            ActualizarEdad();

            _osidActual = datos.OSID;
            _osidSeleccionada = null;

            LblObraSocial.Text = string.IsNullOrWhiteSpace(datos.ObraSocial) ? "Sin Obra Social" : datos.ObraSocial;
            TxtNroSocio.Text = datos.NroSocio ?? "";
        }
        catch (UnauthorizedAccessException ex)
        {
            await DisplayAlert("Sesión", ex.Message, "OK");
            await Shell.Current.GoToAsync("Login");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo cargar el perfil: {ex.Message}", "OK");
        }
        finally
        {
            Loading.IsVisible = Loading.IsRunning = false;
        }
    }

    private void ActualizarEdad()
    {
        var hoy = DateTime.Today;
        var fn = FechaNacimientoPicker.Date;

        var edad = hoy.Year - fn.Year;
        if (fn.Date > hoy.AddYears(-edad)) edad--;

        EdadLabel.Text = $"({Math.Max(0, edad)} años)";
    }

    private void OnFechaNacimientoChanged(object sender, DateChangedEventArgs e)
        => ActualizarEdad();

    private async void OnVolver(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("..");

    private async void OnGuardar(object sender, EventArgs e)
    {
        try
        {
            Loading.IsVisible = Loading.IsRunning = true;

            var token = await SecureStorage.GetAsync("token");
            var idStr = await SecureStorage.GetAsync("userId");

            if (string.IsNullOrWhiteSpace(token) || !int.TryParse(idStr, out var id) || id <= 0)
            {
                await DisplayAlert("Sesión", "Sesión inválida. Volvé a iniciar sesión.", "OK");
                return;
            }

            var payload = new
            {
                Nombre = TxtNombre.Text?.Trim(),
                Apellido = TxtApellido.Text?.Trim(),
                FechaNacimiento = FechaNacimientoPicker.Date.Date
            };

            var req1 = new HttpRequestMessage(HttpMethod.Put, "api/usuarios/actualizar-perfil");
            req1.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req1.Content = JsonContent.Create(payload);

            var res1 = await _api.Client.SendAsync(req1);
            var body1 = await res1.Content.ReadAsStringAsync();
            if (!res1.IsSuccessStatusCode)
            {
                await DisplayAlert("Error", $"Perfil: HTTP {(int)res1.StatusCode}\n{body1}", "OK");
                return;
            }

            var nro = TxtNroSocio.Text?.Trim();
            var osidParaEnviar = _osidSeleccionada ?? _osidActual;

            if (!osidParaEnviar.HasValue && !string.IsNullOrWhiteSpace(nro))
            {
                await DisplayAlert("Error", "Ingresaste número de socio pero no seleccionaste obra social.", "OK");
                return;
            }

            if (osidParaEnviar.HasValue && (_osidSeleccionada.HasValue || !string.IsNullOrWhiteSpace(nro)))
            {
                var osPayload = new
                {
                    OSID = osidParaEnviar.Value,
                    NroSocio = string.IsNullOrWhiteSpace(nro) ? null : nro
                };

                var req2 = new HttpRequestMessage(HttpMethod.Put, $"api/usuarios/paciente/obra-social/{id}");
                req2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                req2.Content = JsonContent.Create(osPayload);

                var res2 = await _api.Client.SendAsync(req2);
                var body2 = await res2.Content.ReadAsStringAsync();
                if (!res2.IsSuccessStatusCode)
                {
                    await DisplayAlert("Error", $"Obra Social: HTTP {(int)res2.StatusCode}\n{body2}", "OK");
                    return;
                }
            }

            await DisplayAlert("Listo", "Cambios guardados.", "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.ToString(), "OK");
        }
        finally
        {
            Loading.IsVisible = Loading.IsRunning = false;
        }
    }

    private async void OnCambiarPassword(object sender, EventArgs e)
    {
        try
        {
            var actual = PasswordActualEntry.Text?.Trim() ?? "";
            var nueva = PasswordNuevaEntry.Text?.Trim() ?? "";
            var repetir = PasswordRepetirEntry.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(actual) || string.IsNullOrWhiteSpace(nueva) || string.IsNullOrWhiteSpace(repetir))
            {
                await DisplayAlert("Error", "Completá todos los campos de contraseña.", "OK");
                return;
            }

            if (!ValidarPasswordsSoloUI() || nueva != repetir)
            {
                await DisplayAlert("Error", "Revisá la nueva contraseña (mínimo y coincidencia).", "OK");
                return;
            }

            Loading.IsVisible = Loading.IsRunning = true;

            var token = await SecureStorage.GetAsync("token");
            if (string.IsNullOrWhiteSpace(token))
            {
                await DisplayAlert("Sesión", "Sesión inválida. Volvé a iniciar sesión.", "OK");
                await Shell.Current.GoToAsync("//Login");
                return;
            }

            var req = new HttpRequestMessage(HttpMethod.Put, "api/usuarios/cambiar-password");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Content = JsonContent.Create(new
            {
                PasswordActual = actual,
                PasswordNueva = nueva
            });

            var res = await _api.Client.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
            {
                await DisplayAlert("Error", $"No se pudo cambiar: HTTP {(int)res.StatusCode}\n{body}", "OK");
                return;
            }

            await DisplayAlert("Listo", "Contraseña actualizada.", "OK");

            PasswordActualEntry.Text = "";
            PasswordNuevaEntry.Text = "";
            PasswordRepetirEntry.Text = "";
            LblPasswordError.IsVisible = false;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            Loading.IsVisible = Loading.IsRunning = false;
        }
    }

    private void OnTogglePasswordActual(object sender, EventArgs e)
        => PasswordActualEntry.IsPassword = !PasswordActualEntry.IsPassword;

    private void OnTogglePasswordNueva(object sender, EventArgs e)
        => PasswordNuevaEntry.IsPassword = !PasswordNuevaEntry.IsPassword;

    private void OnTogglePasswordRepetir(object sender, EventArgs e)
        => PasswordRepetirEntry.IsPassword = !PasswordRepetirEntry.IsPassword;

    private void OnPasswordFieldsChanged(object sender, TextChangedEventArgs e)
    {
        ValidarPasswordsSoloUI();
    }

    private bool ValidarPasswordsSoloUI()
    {
        var nueva = PasswordNuevaEntry.Text?.Trim() ?? "";
        var repetir = PasswordRepetirEntry.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(nueva) && string.IsNullOrWhiteSpace(repetir))
        {
            LblPasswordError.IsVisible = false;
            LblPasswordError.Text = "";
            return false;
        }

        if (nueva.Length < 6)
        {
            LblPasswordError.IsVisible = true;
            LblPasswordError.Text = "La nueva contraseña debe tener al menos 6 caracteres.";
            return false;
        }

        if (!string.IsNullOrEmpty(repetir) && nueva != repetir)
        {
            LblPasswordError.IsVisible = true;
            LblPasswordError.Text = "Las contraseñas no coinciden.";
            return false;
        }

        LblPasswordError.IsVisible = false;
        LblPasswordError.Text = "";
        return true;
    }

    private async void OnCambiarOS(object sender, EventArgs e)
    {
        var page = new SeleccionarObraSocialPacientePage(_api, async (os) =>
        {
            _osidSeleccionada = os.OSID;
            _obraSocialNombre = os.Nombre;

            LblObraSocial.Text = os.Nombre;

            var nro = await DisplayPromptAsync("Número de socio", "Ingresá tu número de socio (opcional):", "OK", "Cancelar");
            if (nro != null)
                TxtNroSocio.Text = nro.Trim();

            await Navigation.PopAsync();
        });

        await Navigation.PushAsync(page);
    }
}