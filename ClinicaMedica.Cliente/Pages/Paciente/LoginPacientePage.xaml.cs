using ClinicaMedica.Cliente.Services;

namespace ClinicaMedica.Cliente.Pages;

public partial class LoginPacientePage : ContentPage
{
    private readonly AuthService _authService;  

    public LoginPacientePage(AuthService auth)
    {
        InitializeComponent();
        _authService = auth;
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

        var result = await _authService.LoginAsync(email, pass); 
        if (result == null)
        {
            await DisplayAlert("Error", "Credenciales inválidas", "OK");
            return;
        }

        switch (result.rol)
        {
            case "Paciente": await Shell.Current.GoToAsync("//HomePaciente"); break;
            case "Doctor": await Shell.Current.GoToAsync("//HomeDoctor"); break;
            case "Admin": await Shell.Current.GoToAsync("//HomeAdmin"); break;
            default: await DisplayAlert("Error", "Rol desconocido", "OK"); break;
        }

    }
    private async void OnCrearUsuarioClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//RegisterPage");
    }

}
