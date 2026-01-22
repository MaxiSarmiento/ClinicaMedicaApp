using ClinicaMedica.Cliente.Services;

namespace ClinicaMedica.Cliente.Pages;

public partial class RegisterPage : ContentPage
{
    private readonly AuthService _authService;

    public RegisterPage(AuthService auth)
    {
        InitializeComponent();
        _authService = auth;
    }

    private async void OnRegistrarClicked(object sender, EventArgs e)
    {
        var nombre = NombreEntry.Text?.Trim() ?? "";
        var apellido = ApellidoEntry.Text?.Trim() ?? "";
        var dni = DniEntry.Text?.Trim() ?? "";
        var email = EmailEntry.Text?.Trim() ?? "";
        var fechaNacimiento = DateOnly.FromDateTime(FechaNacimientoPicker.Date);
        var pass = PasswordEntry.Text ?? "";
        var rol = RolPicker.SelectedItem?.ToString() ?? "Paciente";  // Por defecto establezco usuario Paciente

        if (string.IsNullOrWhiteSpace(nombre) ||
            string.IsNullOrWhiteSpace(apellido) ||
            string.IsNullOrWhiteSpace(dni) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(pass))
        {
            await DisplayAlert("Error", "Completá todos los campos", "OK");
            return;
        }

        // Registrar con rol
        var ok = await _authService.RegisterAsync(email, pass, nombre, apellido, fechaNacimiento, dni, rol);

        if (!ok)
        {
            await DisplayAlert("Error", "No se pudo registrar. Verificá los datos.", "OK");
            return;
        }

        await DisplayAlert("Éxito", "Cuenta creada correctamente", "Iniciar sesión");

        // Volver al Login
        await Shell.Current.GoToAsync("//LoginPage");
    }
    private void OnFechaNacimientoChanged(object sender, DateChangedEventArgs e)
    {
        var fecha = FechaNacimientoPicker.Date;
        var hoy = DateTime.Today;

        int edad = hoy.Year - fecha.Year;

        if (fecha > hoy.AddYears(-edad))
            edad--;

        EdadLabel.Text = $"({edad} años)";
    }
    private async void OnVolverClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//LoginPage");
    }
}
