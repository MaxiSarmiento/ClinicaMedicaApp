using ClinicaMedica.Cliente.Services;

namespace ClinicaMedica.Cliente.Pages;

public partial class RegisterPage : ContentPage
{
    private readonly AuthService _authService;
    private int _debugTapCount = 0;

    public RegisterPage(AuthService auth)
    {
        InitializeComponent();
        _authService = auth;
        RolDebugFrame.IsVisible = false;
    }

    private async void OnRegistrarClicked(object sender, EventArgs e)
    {
        var nombre = NombreEntry.Text?.Trim() ?? "";
        var apellido = ApellidoEntry.Text?.Trim() ?? "";
        var dni = DniEntry.Text?.Trim() ?? "";
        var email = EmailEntry.Text?.Trim() ?? "";
        var fechaNacimiento = DateOnly.FromDateTime(FechaNacimientoPicker.Date);
        var pass = PasswordEntry.Text?.Trim() ?? "";
        var passRepeat = PasswordRepeatEntry.Text?.Trim() ?? "";

        var rol = RolDebugFrame.IsVisible
            ? (RolPicker.SelectedItem?.ToString() ?? "Paciente")
            : "Paciente";

        if (string.IsNullOrWhiteSpace(nombre) ||
            string.IsNullOrWhiteSpace(apellido) ||
            string.IsNullOrWhiteSpace(dni) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(pass) ||
            string.IsNullOrWhiteSpace(passRepeat))
        {
            await DisplayAlert("Error", "Completá todos los campos", "OK");
            return;
        }

        if (pass.Length < 6)
        {
            await DisplayAlert("Error", "La contraseña debe tener al menos 6 caracteres.", "OK");
            return;
        }

        if (pass != passRepeat)
        {
            await DisplayAlert("Error", "Las contraseñas no coinciden.", "OK");
            return;
        }

        var ok = await _authService.RegisterAsync(email, pass, nombre, apellido, fechaNacimiento, dni, rol);

        if (!ok)
        {
            await DisplayAlert("Error", "No se pudo registrar. Verificá los datos.", "OK");
            return;
        }

        await DisplayAlert("Éxito", "Cuenta creada correctamente", "Iniciar sesión");
        await Shell.Current.GoToAsync("//LoginPage");
    }

    private void OnFechaNacimientoChanged(object sender, DateChangedEventArgs e)
    {
        var fecha = FechaNacimientoPicker.Date;
        var hoy = DateTime.Today;

        int edad = hoy.Year - fecha.Year;
        if (fecha > hoy.AddYears(-edad)) edad--;

        EdadLabel.Text = $"({edad} años)";
    }

    private async void OnVolverClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//LoginPage");
    }

    private void OnToggleDebugRol(object sender, EventArgs e)
    {
        _debugTapCount++;

        if (_debugTapCount >= 5)
        {
            RolDebugFrame.IsVisible = !RolDebugFrame.IsVisible;
            _debugTapCount = 0;
        }
    }
}