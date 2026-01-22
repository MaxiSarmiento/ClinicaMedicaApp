using ClinicaMedica.Cliente.Services;

namespace ClinicaMedica.Cliente.Pages;

public partial class LoginPage : ContentPage
{
    private readonly AuthService _authService;

    public LoginPage(AuthService authService)
    {
        InitializeComponent();
        _authService = authService;
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        var email = EmailEntry.Text?.Trim() ?? "";
        var pass = PasswordEntry.Text ?? "";

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(pass))
        {
            await DisplayAlert("Error", "Ingresá email y contraseña", "OK");
            return;
        }

        try
        {
            var result = await _authService.LoginAsync(email, pass);
            if (result == null)
            {
                await DisplayAlert("Error", "Credenciales inválidas", "OK");
                return;
            }

            await SecureStorage.SetAsync("token", result.token);
            await SecureStorage.SetAsync("userId", result.id.ToString());
            await SecureStorage.SetAsync("rol", result.rol);

            // opcional: compatibilidad 
            await SecureStorage.SetAsync("jwt", result.token);
            await SecureStorage.SetAsync("id_usuario", result.id.ToString());


            await Shell.Current.GoToAsync(result.rol switch
            {
                "Paciente" => "HomePaciente",
                "Doctor" => "HomeDoctor",
                "Admin" => "HomeAdmin",
                _ => "HomePaciente"
            });
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async void OnCrearUsuarioClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("Register");
}
