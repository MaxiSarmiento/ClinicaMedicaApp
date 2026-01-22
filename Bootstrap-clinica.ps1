<# 
    bootstrap-clinica.ps1
    Crea la solución ClinicaMedica con:
      - ClinicaMedica.Backend (Web API .NET 8, EF Core MySQL, JWT, BCrypt, Swagger)
      - ClinicaMedica.Cliente (MAUI .NET 8 con Shell, páginas y servicios)
#>

$ErrorActionPreference = "Stop"

# ------------------ CONFIGURACIÓN EDITABLE ------------------
$SolutionName = "ClinicaMedica"
$BackendName  = "ClinicaMedica.Backend"
$ClientName   = "ClinicaMedica.Cliente"

# Conexión a MySQL (ajustar password)
$MySqlConn = "server=localhost;port=3306;database=clinica;user=root;password=TU_PASSWORD;"

# Base URL del backend para el cliente
$ApiBaseUrl = "http://10.0.2.2:5000/"

# JWT config
$JwtKey      = "clave-super-secreta-1234567890"
$JwtIssuer   = "ClinicaMedica"
$JwtAudience = "ClinicaUsuarios"
$JwtExpire   = "7"

# ------------------------------------------------------------

function Write-File($Path, [string]$Content) {
    $dir = Split-Path $Path -Parent
    if (!(Test-Path $dir)) { New-Item -Force -ItemType Directory -Path $dir | Out-Null }
    Set-Content -Encoding UTF8 -Path $Path -Value $Content
    Write-Host "  + $Path"
}

function Replace-File($Path, [string]$Content) {
    Write-File -Path $Path -Content $Content
}

function Add-Package($ProjectPath, $Package, $Version = "") {
    if ($Version) {
        dotnet add $ProjectPath package $Package --version $Version
    } else {
        dotnet add $ProjectPath package $Package
    }
}

# 1) Crear solución vacía
Write-Host "==> Creando solución $SolutionName.sln"
dotnet new sln -n $SolutionName | Out-Null

# 2) Backend
Write-Host "`n==> Creando backend $BackendName"
dotnet new webapi -n $BackendName | Out-Null

# Paquetes backend
Write-Host "==> Paquetes backend"
Add-Package "$BackendName" "Microsoft.AspNetCore.Authentication.JwtBearer" "8.0.8"
Add-Package "$BackendName" "Pomelo.EntityFrameworkCore.MySql" "8.0.2"
Add-Package "$BackendName" "Microsoft.EntityFrameworkCore.Design" "8.0.8"
Add-Package "$BackendName" "Microsoft.EntityFrameworkCore.Tools" "8.0.8"
Add-Package "$BackendName" "BCrypt.Net-Next" "4.0.3"
Add-Package "$BackendName" "Swashbuckle.AspNetCore" "6.6.2"

# Archivos backend
Write-Host "==> Escribiendo archivos backend"
Replace-File "$BackendName\appsettings.json" @"
{
  "ConnectionStrings": {
    "DefaultConnection": "$MySqlConn"
  },
  "Jwt": {
    "Key": "$JwtKey",
    "Issuer": "$JwtIssuer",
    "Audience": "$JwtAudience",
    "ExpireDays": $JwtExpire
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
"@

Replace-File "$BackendName\Program.cs" @"
using ClinicaMedica.Backend.Data;
using ClinicaMedica.Backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))));

builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddCors(opt =>
{
    opt.AddDefaultPolicy(p => p
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowAnyOrigin());
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseStaticFiles(); // sirve PDFs desde wwwroot/estudios
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
"@

Replace-File "$BackendName\Data\AppDbContext.cs" @"
using ClinicaMedica.Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace ClinicaMedica.Backend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Usuario> Usuarios => Set<Usuario>();
        public DbSet<Turno> Turnos => Set<Turno>();
        public DbSet<Estudio> Estudios => Set<Estudio>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Usuario>().ToTable("Usuarios");
            modelBuilder.Entity<Turno>().ToTable("Turnos");
            modelBuilder.Entity<Estudio>().ToTable("Estudios");

            modelBuilder.Entity<Turno>()
                .HasOne(t => t.Doctor)
                .WithMany()
                .HasForeignKey(t => t.IdDoctor)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Turno>()
                .HasOne(t => t.Paciente)
                .WithMany()
                .HasForeignKey(t => t.IdPaciente)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Estudio>()
                .HasOne(e => e.Paciente)
                .WithMany()
                .HasForeignKey(e => e.IdPaciente)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
"@

Replace-File "$BackendName\Models\Usuario.cs" @"
using System.ComponentModel.DataAnnotations;

namespace ClinicaMedica.Backend.Models
{
    public class Usuario
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Apellido { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        public string Rol { get; set; } = string.Empty; // Paciente, Doctor, Admin

        public string DNI { get; set; } = string.Empty;
    }
}
"@

Replace-File "$BackendName\Models\Turno.cs" @"
namespace ClinicaMedica.Backend.Models
{
    public class Turno
    {
        public int Id { get; set; }
        public int? IdPaciente { get; set; }
        public int IdDoctor { get; set; }
        public DateTime FechaHora { get; set; }
        public int DuracionMinutos { get; set; }

        public Usuario? Doctor { get; set; }
        public Usuario? Paciente { get; set; }
    }
}
"@

Replace-File "$BackendName\Models\Estudio.cs" @"
namespace ClinicaMedica.Backend.Models
{
    public class Estudio
    {
        public int Id { get; set; }
        public int IdPaciente { get; set; }
        public string NombreArchivo { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public string RutaArchivo { get; set; } = string.Empty;

        public string Dni { get; set; } = string.Empty;
        public string NombrePaciente { get; set; } = string.Empty;
        public string ApellidoPaciente { get; set; } = string.Empty;

        public Usuario? Paciente { get; set; }
    }
}
"@

Replace-File "$BackendName\Services\AuthService.cs" @"
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ClinicaMedica.Backend.Data;
using ClinicaMedica.Backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ClinicaMedica.Backend.Services
{
    public interface IAuthService
    {
        Task<(bool ok, string? token, Usuario? user, string? error)> LoginAsync(string email, string password);
        Task<(bool ok, string? error)> RegisterAsync(Usuario usuario, string password);
    }

    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public AuthService(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        public async Task<(bool ok, string? token, Usuario? user, string? error)> LoginAsync(string email, string password)
        {
            var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                return (false, null, null, "Credenciales inválidas");

            var token = GenerateJwtToken(user);
            return (true, token, user, null);
        }

        public async Task<(bool ok, string? error)> RegisterAsync(Usuario usuario, string password)
        {
            if (await _context.Usuarios.AnyAsync(u => u.Email == usuario.Email))
                return (false, "Email ya registrado");

            usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();
            return (true, null);
        }

        private string GenerateJwtToken(Usuario usuario)
        {
            var key = _config["Jwt:Key"] ?? throw new ArgumentNullException("Jwt:Key");
            var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Email, usuario.Email),
                new Claim(ClaimTypes.Role, usuario.Rol)
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(int.Parse(_config["Jwt:ExpireDays"] ?? "7")),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
"@

Replace-File "$BackendName\Controllers\UsuariosController.cs" @"
using ClinicaMedica.Backend.Models;
using ClinicaMedica.Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaMedica.Backend.Controllers
{
    [ApiController]
    [Route(""api/[controller]"")]
    public class UsuariosController : ControllerBase
    {
        private readonly IAuthService _auth;

        public UsuariosController(IAuthService auth)
        {
            _auth = auth;
        }

        public class LoginDto
        {
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        public class RegisterDto
        {
            public string Nombre { get; set; } = string.Empty;
            public string Apellido { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public string Rol { get; set; } = ""Paciente"";
            public string DNI { get; set; } = string.Empty;
        }

        [HttpPost(""login"")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var result = await _auth.LoginAsync(dto.Email, dto.Password);
            if (!result.ok) return Unauthorized(result.error);

            var user = result.user!;
            return Ok(new
            {
                token = result.token,
                id = user.Id,
                rol = user.Rol,
                nombre = user.Nombre,
                apellido = user.Apellido,
                email = user.Email
            });
        }

        [HttpPost(""register"")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var usuario = new Usuario
            {
                Nombre = dto.Nombre,
                Apellido = dto.Apellido,
                Email = dto.Email,
                Rol = dto.Rol,
                DNI = dto.DNI
            };
            var result = await _auth.RegisterAsync(usuario, dto.Password);
            if (!result.ok) return BadRequest(result.error);
            return Ok(new { message = ""Usuario registrado"" });
        }
    }
}
"@

Replace-File "$BackendName\Controllers\TurnosController.cs" @"
using ClinicaMedica.Backend.Data;
using ClinicaMedica.Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicaMedica.Backend.Controllers
{
    [ApiController]
    [Route(""api/[controller]"")]
    public class TurnosController : ControllerBase
    {
        private readonly AppDbContext _context;
        public TurnosController(AppDbContext context) => _context = context;

        [HttpPost(""crear"")]
        [Authorize(Roles = ""Doctor,Admin"")]
        public async Task<IActionResult> Crear([FromBody] Turno turno)
        {
            _context.Turnos.Add(turno);
            await _context.SaveChangesAsync();
            return Ok(turno);
        }

        [HttpGet(""disponibles"")]
        public async Task<IActionResult> Disponibles([FromQuery] DateTime? fecha = null, [FromQuery] int? idDoctor = null)
        {
            var q = _context.Turnos.AsQueryable();

            if (fecha.HasValue)
            {
                var day = fecha.Value.Date;
                q = q.Where(t => t.FechaHora.Date == day);
            }
            if (idDoctor.HasValue)
            {
                q = q.Where(t => t.IdDoctor == idDoctor.Value);
            }

            var list = await q.Where(t => t.IdPaciente == null && t.FechaHora > DateTime.Now)
                              .OrderBy(t => t.FechaHora)
                              .ToListAsync();
            return Ok(list);
        }

        [HttpPost(""reservar"")]
        [Authorize(Roles = ""Paciente,Admin"")]
        public async Task<IActionResult> Reservar([FromBody] ReservarTurnoDto dto)
        {
            var turno = await _context.Turnos.FindAsync(dto.IdTurno);
            if (turno == null || turno.IdPaciente != null)
                return BadRequest(""Turno inválido o ya reservado"");

            turno.IdPaciente = dto.IdPaciente;
            await _context.SaveChangesAsync();
            return Ok(turno);
        }

        [HttpGet(""mis-turnos/{idPaciente:int}"")]
        [Authorize(Roles = ""Paciente,Admin"")]
        public async Task<IActionResult> MisTurnos(int idPaciente)
        {
            var turnos = await _context.Turnos
                .Include(t => t.Doctor)
                .Where(t => t.IdPaciente == idPaciente)
                .OrderBy(t => t.FechaHora)
                .Select(t => new
                {
                    t.Id,
                    t.FechaHora,
                    t.DuracionMinutos,
                    Doctor = t.Doctor != null ? (t.Doctor.Nombre + "" "" + t.Doctor.Apellido) : """"
                })
                .ToListAsync();

            return Ok(turnos);
        }

        public class ReservarTurnoDto
        {
            public int IdTurno { get; set; }
            public int IdPaciente { get; set; }
        }
    }
}
"@

Replace-File "$BackendName\Controllers\EstudiosController.cs" @"
using ClinicaMedica.Backend.Data;
using ClinicaMedica.Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicaMedica.Backend.Controllers
{
    [ApiController]
    [Route(""api/[controller]"")]
    public class EstudiosController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        public EstudiosController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpPost(""subir"")]
        [Authorize(Roles = ""Admin"")]
        public async Task<IActionResult> Subir([FromForm] int IdPaciente, [FromForm] IFormFile archivo)
        {
            if (archivo == null || archivo.Length == 0)
                return BadRequest(""Archivo inválido"");

            var folder = Path.Combine(_env.WebRootPath ?? ""wwwroot"", ""estudios"");
            Directory.CreateDirectory(folder);

            var fileName = $""{Guid.NewGuid()}{Path.GetExtension(archivo.FileName)}"";
            var path = Path.Combine(folder, fileName);
            using (var stream = new FileStream(path, FileMode.Create))
            {
                await archivo.CopyToAsync(stream);
            }

            var paciente = await _context.Usuarios.FindAsync(IdPaciente);
            var estudio = new Estudio
            {
                IdPaciente = IdPaciente,
                NombreArchivo = archivo.FileName,
                Fecha = DateTime.Now,
                RutaArchivo = $""estudios/{fileName}"",
                Dni = paciente?.DNI ?? """",
                NombrePaciente = paciente?.Nombre ?? """",
                ApellidoPaciente = paciente?.Apellido ?? """"
            };
            _context.Estudios.Add(estudio);
            await _context.SaveChangesAsync();

            return Ok(estudio);
        }

        [HttpGet(""paciente/{idPaciente:int}"")]
        [Authorize(Roles = ""Paciente,Doctor,Admin"")]
        public async Task<IActionResult> PorPaciente(int idPaciente)
        {
            var estudios = await _context.Estudios
                .Where(e => e.IdPaciente == idPaciente)
                .OrderByDescending(e => e.Fecha)
                .ToListAsync();
            return Ok(estudios);
        }

        [HttpGet(""buscar"")]
        [Authorize(Roles = ""Doctor,Admin"")]
        public async Task<IActionResult> Buscar([FromQuery] string? dni, [FromQuery] string? nombre, [FromQuery] string? apellido)
        {
            var q = _context.Estudios.Include(e => e.Paciente).AsQueryable();
            if (!string.IsNullOrWhiteSpace(dni)) q = q.Where(e => e.Dni.Contains(dni));
            if (!string.IsNullOrWhiteSpace(nombre)) q = q.Where(e => e.NombrePaciente.Contains(nombre));
            if (!string.IsNullOrWhiteSpace(apellido)) q = q.Where(e => e.ApellidoPaciente.Contains(apellido));

            var list = await q.OrderByDescending(e => e.Fecha).ToListAsync();
            return Ok(list);
        }

        [HttpGet(""pdf/{id:int}"")]
        public async Task<IActionResult> Pdf(int id)
        {
            var est = await _context.Estudios.FindAsync(id);
            if (est == null) return NotFound();
            var filePath = Path.Combine(_env.WebRootPath ?? ""wwwroot"", est.RutaArchivo.Replace(""/"", Path.DirectorySeparatorChar.ToString()));
            if (!System.IO.File.Exists(filePath)) return NotFound();
            var stream = System.IO.File.OpenRead(filePath);
            return File(stream, ""application/pdf"", enableRangeProcessing: true);
        }
    }
}
"@

# wwwroot para PDFs
New-Item -Force -ItemType Directory -Path "$BackendName\wwwroot\estudios" | Out-Null

# 3) Cliente MAUI
Write-Host "`n==> Creando cliente $ClientName"
dotnet new maui -n $ClientName | Out-Null

# Paquete útil (MVVM Toolkit) y fix Windows SDK
Write-Host "==> Ajustando .csproj del cliente"
$csprojPath = "$ClientName\$ClientName.csproj"
$csproj = Get-Content $csprojPath -Raw
if ($csproj -notmatch "<WindowsSdkPackageVersion>") {
    $csproj = $csproj -replace "</PropertyGroup>", "  <WindowsSdkPackageVersion>10.0.19041.53</WindowsSdkPackageVersion>`n  </PropertyGroup>"
}
Set-Content -Encoding UTF8 -Path $csprojPath -Value $csproj

dotnet add "$ClientName" package CommunityToolkit.Mvvm --version 8.4.0 | Out-Null

# Archivos cliente
Write-Host "==> Escribiendo archivos cliente"
Replace-File "$ClientName\Config\ApiConfig.cs" @"
namespace $ClientName.Config
{
    public static class ApiConfig
    {
        public const string BaseUrl = ""$ApiBaseUrl"";
    }
}
"@

Replace-File "$ClientName\MauiProgram.cs" @"
using $ClientName.Config;
using $ClientName.Services;

namespace $ClientName;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont(""OpenSans-Regular.ttf"", ""OpenSansRegular"");
                fonts.AddFont(""OpenSans-Semibold.ttf"", ""OpenSansSemibold"");
            });

        builder.Services.AddTransient<AuthHttpMessageHandler>();
        builder.Services.AddHttpClient<ApiService>(client =>
        {
            client.BaseAddress = new Uri(ApiConfig.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        }).AddHttpMessageHandler<AuthHttpMessageHandler>();

        builder.Services.AddSingleton<AuthService>();

        builder.Services.AddTransient<Pages.LoginPage>();
        builder.Services.AddTransient<Pages.Paciente.HomePacientePage>();
        builder.Services.AddTransient<Pages.Paciente.MisTurnosPage>();
        builder.Services.AddTransient<Pages.Paciente.VerEstudiosPage>();
        builder.Services.AddTransient<Pages.Paciente.ReservarTurnoPage>();
        builder.Services.AddTransient<Pages.Doctor.HomeDoctorPage>();
        builder.Services.AddTransient<Pages.Doctor.CrearTurnoPage>();
        builder.Services.AddTransient<Pages.Doctor.BuscarEstudiosPage>();
        builder.Services.AddTransient<Pages.Admin.HomeAdminPage>();
        builder.Services.AddTransient<Pages.Admin.SubirEstudioPage>();

        return builder.Build();
    }
}
"@

Replace-File "$ClientName\AppShell.xaml" @"
<?xml version=""1.0"" encoding=""UTF-8""?>
<Shell
    xmlns=""http://schemas.microsoft.com/dotnet/2021/maui""
    xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml""
    xmlns:local=""clr-namespace:$ClientName.Pages""
    xmlns:paciente=""clr-namespace:$ClientName.Pages.Paciente""
    xmlns:doctor=""clr-namespace:$ClientName.Pages.Doctor""
    xmlns:admin=""clr-namespace:$ClientName.Pages.Admin""
    x:Class=""$ClientName.AppShell"">

    <ShellContent Route=""LoginPage"" ContentTemplate=""{DataTemplate local:LoginPage}"" />
    <ShellContent Route=""HomePaciente"" ContentTemplate=""{DataTemplate paciente:HomePacientePage}"" />
    <ShellContent Route=""MisTurnos"" ContentTemplate=""{DataTemplate paciente:MisTurnosPage}"" />
    <ShellContent Route=""VerEstudios"" ContentTemplate=""{DataTemplate paciente:VerEstudiosPage}"" />
    <ShellContent Route=""ReservarTurno"" ContentTemplate=""{DataTemplate paciente:ReservarTurnoPage}"" />

    <ShellContent Route=""HomeDoctor"" ContentTemplate=""{DataTemplate doctor:HomeDoctorPage}"" />
    <ShellContent Route=""CrearTurno"" ContentTemplate=""{DataTemplate doctor:CrearTurnoPage}"" />
    <ShellContent Route=""BuscarEstudios"" ContentTemplate=""{DataTemplate doctor:BuscarEstudiosPage}"" />

    <ShellContent Route=""HomeAdmin"" ContentTemplate=""{DataTemplate admin:HomeAdminPage}"" />
    <ShellContent Route=""SubirEstudio"" ContentTemplate=""{DataTemplate admin:SubirEstudioPage}"" />
</Shell>
"@

Replace-File "$ClientName\AppShell.xaml.cs" @"
namespace $ClientName;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        _ = Shell.Current.GoToAsync(""//LoginPage"");
    }
}
"@

Replace-File "$ClientName\App.xaml" @"
<?xml version=""1.0"" encoding=""utf-8"" ?>
<Application
    xmlns=""http://schemas.microsoft.com/dotnet/2021/maui""
    xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml""
    x:Class=""$ClientName.App"">
    <Application.Resources>
        <ResourceDictionary>
            <Style TargetType=""Label"">
                <Setter Property=""TextColor"" Value=""Black"" />
            </Style>
        </ResourceDictionary>
    </Application.Resources>
</Application>
"@

Replace-File "$ClientName\App.xaml.cs" @"
namespace $ClientName;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        MainPage = new AppShell();
    }
}
"@

Replace-File "$ClientName\Models\Usuario.cs" @"
namespace $ClientName.Models
{
    public class Usuario
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = """";
        public string Apellido { get; set; } = """";
        public string Email { get; set; } = """";
        public string Rol { get; set; } = """";
    }
}
"@

Replace-File "$ClientName\Models\Turno.cs" @"
namespace $ClientName.Models
{
    public class Turno
    {
        public int Id { get; set; }
        public int IdDoctor { get; set; }
        public int? IdPaciente { get; set; }
        public DateTime FechaHora { get; set; }
        public int DuracionMinutos { get; set; }
        public string Doctor { get; set; } = """";
    }
}
"@

Replace-File "$ClientName\Models\Estudio.cs" @"
namespace $ClientName.Models
{
    public class Estudio
    {
        public int Id { get; set; }
        public int IdPaciente { get; set; }
        public string NombreArchivo { get; set; } = """";
        public DateTime Fecha { get; set; }
        public string RutaArchivo { get; set; } = """";
        public string NombrePaciente { get; set; } = """";
        public string ApellidoPaciente { get; set; } = """";
        public string Dni { get; set; } = """";
    }
}
"@

Replace-File "$ClientName\Services\AuthHttpMessageHandler.cs" @"
using System.Net.Http.Headers;

namespace $ClientName.Services
{
    public class AuthHttpMessageHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = Preferences.Get(""token"", string.Empty);
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue(""Bearer"", token);
            }
            return base.SendAsync(request, cancellationToken);
        }
    }
}
"@

Replace-File "$ClientName\Services\ApiService.cs" @"
using System.Net.Http.Json;

namespace $ClientName.Services
{
    public class ApiService
    {
        private readonly HttpClient _http;
        public ApiService(HttpClient http) => _http = http;

        public Task<HttpResponseMessage> GetAsync(string path) => _http.GetAsync(path);
        public Task<T?> GetJsonAsync<T>(string path) => _http.GetFromJsonAsync<T>(path);
        public Task<HttpResponseMessage> PostAsync<T>(string path, T body) => _http.PostAsJsonAsync(path, body);
        public Task<HttpResponseMessage> PostMultipartAsync(string path, MultipartFormDataContent content) => _http.PostAsync(path, content);
    }
}
"@

Replace-File "$ClientName\Services\AuthService.cs" @"
using System.Net.Http.Json;

namespace $ClientName.Services
{
    public class AuthService
    {
        private readonly ApiService _api;
        public AuthService(ApiService api) => _api = api;

        public record LoginDto(string Email, string Password);
        public class LoginResponse
        {
            public string Token { get; set; } = """";
            public int Id { get; set; }
            public string Rol { get; set; } = """";
            public string Nombre { get; set; } = """";
            public string Apellido { get; set; } = """";
            public string Email { get; set; } = """";
        }

        public async Task<LoginResponse?> LoginAsync(string email, string password)
        {
            var resp = await _api.PostAsync(""api/usuarios/login"", new LoginDto(email, password));
            if (!resp.IsSuccessStatusCode) return null;

            var data = await resp.Content.ReadFromJsonAsync<LoginResponse>();
            if (data != null)
            {
                Preferences.Set(""token"", data.Token);
                Preferences.Set(""userId"", data.Id);
                Preferences.Set(""rol"", data.Rol);
                Preferences.Set(""nombre"", data.Nombre);
                Preferences.Set(""apellido"", data.Apellido);
                Preferences.Set(""email"", data.Email);
            }
            return data;
        }

        public void Logout()
        {
            Preferences.Remove(""token"");
            Preferences.Remove(""userId"");
            Preferences.Remove(""rol"");
            Preferences.Remove(""nombre"");
            Preferences.Remove(""apellido"");
            Preferences.Remove(""email"");
        }

        public int UserId => Preferences.Get(""userId"", 0);
        public string Rol => Preferences.Get(""rol"", """");
        public string Token => Preferences.Get(""token"", """");
    }
}
"@

Replace-File "$ClientName\Pages\LoginPage.xaml" @"
<?xml version=""1.0"" encoding=""utf-8"" ?>
<ContentPage
    xmlns=""http://schemas.microsoft.com/dotnet/2021/maui""
    xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml""
    x:Class=""$ClientName.Pages.LoginPage""
    Title=""Iniciar sesión"">

    <VerticalStackLayout Padding=""24"" Spacing=""16"" VerticalOptions=""Center"">
        <Label Text=""Clínica Médica"" FontSize=""28"" HorizontalOptions=""Center"" />
        <Entry x:Name=""EmailEntry"" Placeholder=""Email"" Keyboard=""Email"" />
        <Entry x:Name=""PasswordEntry"" Placeholder=""Contraseña"" IsPassword=""True"" />
        <Button Text=""Ingresar"" Clicked=""OnLoginClicked"" />
    </VerticalStackLayout>
</ContentPage>
"@

Replace-File "$ClientName\Pages\LoginPage.xaml.cs" @"
using $ClientName.Services;

namespace $ClientName.Pages;

public partial class LoginPage : ContentPage
{
    private readonly AuthService _auth;

    public LoginPage(AuthService auth)
    {
        InitializeComponent();
        _auth = auth;
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        var email = EmailEntry.Text?.Trim() ?? """";
        var pass = PasswordEntry.Text ?? """";
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(pass))
        {
            await DisplayAlert(""Error"", ""Ingresá email y contraseña"", ""OK"");
            return;
        }

        var result = await _auth.LoginAsync(email, pass);
        if (result == null)
        {
            await DisplayAlert(""Error"", ""Credenciales inválidas"", ""OK"");
            return;
        }

        switch (result.Rol)
        {
            case ""Paciente"": await Shell.Current.GoToAsync(""//HomePaciente""); break;
            case ""Doctor"":   await Shell.Current.GoToAsync(""//HomeDoctor"");   break;
            case ""Admin"":    await Shell.Current.GoToAsync(""//HomeAdmin"");    break;
            default:           await DisplayAlert(""Error"", ""Rol desconocido"", ""OK""); break;
        }
    }
}
"@

Replace-File "$ClientName\Pages\Paciente\HomePacientePage.xaml" @"
<?xml version=""1.0"" encoding=""utf-8"" ?>
<ContentPage
    xmlns=""http://schemas.microsoft.com/dotnet/2021/maui""
    xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml""
    x:Class=""$ClientName.Pages.Paciente.HomePacientePage""
    Title=""Paciente"">
    <VerticalStackLayout Padding=""24"" Spacing=""12"">
        <Label Text=""Menú Paciente"" FontSize=""22""/>
        <Button Text=""Mis Turnos"" Clicked=""OnMisTurnos""/>
        <Button Text=""Reservar Turno"" Clicked=""OnReservarTurno""/>
        <Button Text=""Ver mis Estudios"" Clicked=""OnEstudios""/>
    </VerticalStackLayout>
</ContentPage>
"@

Replace-File "$ClientName\Pages\Paciente\HomePacientePage.xaml.cs" @"
namespace $ClientName.Pages.Paciente;

public partial class HomePacientePage : ContentPage
{
    public HomePacientePage()
    {
        InitializeComponent();
    }

    private async void OnMisTurnos(object sender, EventArgs e)
        => await Shell.Current.GoToAsync(""//MisTurnos"");

    private async void OnReservarTurno(object sender, EventArgs e)
        => await Shell.Current.GoToAsync(""//ReservarTurno"");

    private async void OnEstudios(object sender, EventArgs e)
        => await Shell.Current.GoToAsync(""//VerEstudios"");
}
"@

Replace-File "$ClientName\Pages\Paciente\MisTurnosPage.xaml" @"
<?xml version=""1.0"" encoding=""utf-8"" ?>
<ContentPage
    xmlns=""http://schemas.microsoft.com/dotnet/2021/maui""
    xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml""
    x:Class=""$ClientName.Pages.Paciente.MisTurnosPage""
    Title=""Mis Turnos"">
    <CollectionView x:Name=""TurnosList"" Margin=""16"">
        <CollectionView.ItemTemplate>
            <DataTemplate>
                <Frame Padding=""12"" Margin=""6"" BorderColor=""LightGray"">
                    <VerticalStackLayout>
                        <Label Text=""{Binding FechaHora, StringFormat='Fecha: {0:dd/MM/yyyy HH:mm}'}"" FontAttributes=""Bold""/>
                        <Label Text=""{Binding DuracionMinutos, StringFormat='Duración: {0} min'}""/>
                        <Label Text=""{Binding Doctor, StringFormat='Doctor: {0}'}""/>
                    </VerticalStackLayout>
                </Frame>
            </DataTemplate>
        </CollectionView.ItemTemplate>
    </CollectionView>
</ContentPage>
"@

Replace-File "$ClientName\Pages\Paciente\MisTurnosPage.xaml.cs" @"
using $ClientName.Models;
using $ClientName.Services;
using System.Net.Http.Json;

namespace $ClientName.Pages.Paciente;

public partial class MisTurnosPage : ContentPage
{
    private readonly ApiService _api;
    private readonly AuthService _auth;

    public MisTurnosPage(ApiService api, AuthService auth)
    {
        InitializeComponent();
        _api = api;
        _auth = auth;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var id = _auth.UserId;
        var resp = await _api.GetAsync($""api/turnos/mis-turnos/{id}"");
        if (!resp.IsSuccessStatusCode)
        {
            await DisplayAlert(""Error"", ""No se pudieron cargar los turnos"", ""OK"");
            return;
        }
        var lista = await resp.Content.ReadFromJsonAsync<List<Turno>>();
        TurnosList.ItemsSource = lista ?? new();
    }
}
"@

Replace-File "$ClientName\Pages\Paciente\VerEstudiosPage.xaml" @"
<?xml version=""1.0"" encoding=""utf-8"" ?>
<ContentPage
    xmlns=""http://schemas.microsoft.com/dotnet/2021/maui""
    xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml""
    x:Class=""$ClientName.Pages.Paciente.VerEstudiosPage""
    Title=""Mis Estudios"">
    <CollectionView x:Name=""EstudiosList"" Margin=""16"">
        <CollectionView.ItemTemplate>
            <DataTemplate>
                <Frame Padding=""12"" Margin=""6"" BorderColor=""LightGray"">
                    <VerticalStackLayout>
                        <Label Text=""{Binding NombreArchivo}"" FontAttributes=""Bold""/>
                        <Label Text=""{Binding Fecha, StringFormat='Fecha: {0:dd/MM/yyyy}'}""/>
                        <Button Text=""Ver PDF"" Clicked=""OnVerPdf""/>
                    </VerticalStackLayout>
                </Frame>
            </DataTemplate>
        </CollectionView.ItemTemplate>
    </CollectionView>
</ContentPage>
"@

Replace-File "$ClientName\Pages\Paciente\VerEstudiosPage.xaml.cs" @"
using $ClientName.Models;
using $ClientName.Services;

namespace $ClientName.Pages.Paciente;

public partial class VerEstudiosPage : ContentPage
{
    private readonly ApiService _api;
    private readonly AuthService _auth;

    public VerEstudiosPage(ApiService api, AuthService auth)
    {
        InitializeComponent();
        _api = api;
        _auth = auth;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var id = _auth.UserId;
        var estudios = await _api.GetJsonAsync<List<Estudio>>($""api/estudios/paciente/{id}"");
        EstudiosList.ItemsSource = estudios ?? new();
    }

    private async void OnVerPdf(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.BindingContext is Estudio est)
        {
            var baseUrl = Config.ApiConfig.BaseUrl.TrimEnd('/');
            var path = est.RutaArchivo?.TrimStart('/') ?? """";
            var url = $""{baseUrl}/{path}"";
            await Launcher.OpenAsync(new Uri(url));
        }
    }
}
"@

Replace-File "$ClientName\Pages\Paciente\ReservarTurnoPage.xaml" @"
<?xml version=""1.0"" encoding=""utf-8"" ?>
<ContentPage
    xmlns=""http://schemas.microsoft.com/dotnet/2021/maui""
    xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml""
    x:Class=""$ClientName.Pages.Paciente.ReservarTurnoPage""
    Title=""Reservar Turno"">
    <VerticalStackLayout Padding=""16"" Spacing=""8"">
        <DatePicker x:Name=""FechaFiltro""/>
        <Button Text=""Buscar Disponibles"" Clicked=""OnBuscar""/>
        <CollectionView x:Name=""TurnosList"">
            <CollectionView.ItemTemplate>
                <DataTemplate>
                    <Frame Padding=""12"" Margin=""6"" BorderColor=""LightGray"">
                        <VerticalStackLayout>
                            <Label Text=""{Binding FechaHora, StringFormat='{0:dd/MM/yyyy HH:mm}'}"" FontAttributes=""Bold""/>
                            <Label Text=""{Binding DuracionMinutos, StringFormat='Duración: {0} min'}""/>
                            <Button Text=""Reservar"" Clicked=""OnReservar""/>
                        </VerticalStackLayout>
                    </Frame>
                </DataTemplate>
            </CollectionView.ItemTemplate>
        </CollectionView>
    </VerticalStackLayout>
</ContentPage>
"@

Replace-File "$ClientName\Pages\Paciente\ReservarTurnoPage.xaml.cs" @"
using $ClientName.Models;
using $ClientName.Services;

namespace $ClientName.Pages.Paciente;

public partial class ReservarTurnoPage : ContentPage
{
    private readonly ApiService _api;
    private readonly AuthService _auth;

    public ReservarTurnoPage(ApiService api, AuthService auth)
    {
        InitializeComponent();
        _api = api;
        _auth = auth;
        FechaFiltro.Date = DateTime.Today;
    }

    private async void OnBuscar(object sender, EventArgs e)
    {
        var fecha = FechaFiltro.Date.ToString(""yyyy-MM-dd"");
        var turnos = await _api.GetJsonAsync<List<Turno>>($""api/turnos/disponibles?fecha={fecha}"");
        TurnosList.ItemsSource = turnos ?? new();
    }

    private async void OnReservar(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.BindingContext is Turno turno)
        {
            var dto = new { IdTurno = turno.Id, IdPaciente = _auth.UserId };
            var resp = await _api.PostAsync(""api/turnos/reservar"", dto);
            if (resp.IsSuccessStatusCode)
                await DisplayAlert(""OK"", ""Turno reservado"", ""Cerrar"");
            else
                await DisplayAlert(""Error"", ""No se pudo reservar"", ""Cerrar"");
        }
    }
}
"@

Replace-File "$ClientName\Pages\Doctor\HomeDoctorPage.xaml" @"
<?xml version=""1.0"" encoding=""utf-8"" ?>
<ContentPage
    xmlns=""http://schemas.microsoft.com/dotnet/2021/maui""
    xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml""
    x:Class=""$ClientName.Pages.Doctor.HomeDoctorPage""
    Title=""Doctor"">
    <VerticalStackLayout Padding=""24"" Spacing=""12"">
        <Label Text=""Menú Doctor"" FontSize=""22""/>
        <Button Text=""Crear Turno"" Clicked=""OnCrearTurno""/>
        <Button Text=""Buscar Estudios"" Clicked=""OnBuscarEstudios""/>
    </VerticalStackLayout>
</ContentPage>
"@

Replace-File "$ClientName\Pages\Doctor\HomeDoctorPage.xaml.cs" @"
namespace $ClientName.Pages.Doctor;

public partial class HomeDoctorPage : ContentPage
{
    public HomeDoctorPage()
    {
        InitializeComponent();
    }

    private async void OnCrearTurno(object sender, EventArgs e)
        => await Shell.Current.GoToAsync(""//CrearTurno"");

    private async void OnBuscarEstudios(object sender, EventArgs e)
        => await Shell.Current.GoToAsync(""//BuscarEstudios"");
}
"@

Replace-File "$ClientName\Pages\Doctor\CrearTurnoPage.xaml" @"
<?xml version=""1.0"" encoding=""utf-8"" ?>
<ContentPage
    xmlns=""http://schemas.microsoft.com/dotnet/2021/maui""
    xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml""
    x:Class=""$ClientName.Pages.Doctor.CrearTurnoPage""
    Title=""Crear Turno"">
    <VerticalStackLayout Padding=""16"" Spacing=""8"">
        <DatePicker x:Name=""FechaPicker""/>
        <TimePicker x:Name=""HoraPicker""/>
        <Entry x:Name=""DuracionEntry"" Placeholder=""Duración (min)"" Keyboard=""Numeric""/>
        <Button Text=""Guardar"" Clicked=""OnGuardar""/>
    </VerticalStackLayout>
</ContentPage>
"@

Replace-File "$ClientName\Pages\Doctor\CrearTurnoPage.xaml.cs" @"
using $ClientName.Services;
using System.Globalization;

namespace $ClientName.Pages.Doctor;

public partial class CrearTurnoPage : ContentPage
{
    private readonly ApiService _api;
    private readonly AuthService _auth;

    public CrearTurnoPage(ApiService api, AuthService auth)
    {
        InitializeComponent();
        _api = api;
        _auth = auth;
        FechaPicker.Date = DateTime.Today;
        HoraPicker.Time = TimeSpan.FromHours(9);
    }

    private async void OnGuardar(object sender, EventArgs e)
    {
        if (!int.TryParse(DuracionEntry.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var dur))
        {
            await DisplayAlert(""Error"", ""Duración inválida"", ""OK"");
            return;
        }

        var fechaHora = FechaPicker.Date + HoraPicker.Time;
        var dto = new { IdDoctor = _auth.UserId, FechaHora = fechaHora, DuracionMinutos = dur };
        var resp = await _api.PostAsync(""api/turnos/crear"", dto);

        if (resp.IsSuccessStatusCode)
            await DisplayAlert(""OK"", ""Turno creado"", ""Cerrar"");
        else
            await DisplayAlert(""Error"", ""No se pudo crear"", ""Cerrar"");
    }
}
"@

Replace-File "$ClientName\Pages\Doctor\BuscarEstudiosPage.xaml" @"
<?xml version=""1.0"" encoding=""utf-8"" ?>
<ContentPage
    xmlns=""http://schemas.microsoft.com/dotnet/2021/maui""
    xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml""
    x:Class=""$ClientName.Pages.Doctor.BuscarEstudiosPage""
    Title=""Buscar Estudios"">

    <ScrollView>
        <VerticalStackLayout Padding=""16"" Spacing=""8"">
            <Entry x:Name=""DniEntry"" Placeholder=""DNI""/>
            <Entry x:Name=""NombreEntry"" Placeholder=""Nombre""/>
            <Entry x:Name=""ApellidoEntry"" Placeholder=""Apellido""/>
            <Button Text=""Buscar"" Clicked=""OnBuscar""/>
            <CollectionView x:Name=""ResultadosList"">
                <CollectionView.ItemTemplate>
                    <DataTemplate>
                        <Frame Padding=""12"" Margin=""6"" BorderColor=""LightGray"">
                            <VerticalStackLayout>
                                <Label Text=""{Binding NombreArchivo}"" FontAttributes=""Bold""/>
                                <Label Text=""{Binding Fecha, StringFormat='Fecha: {0:dd/MM/yyyy}'}""/>
                                <Label Text=""{Binding Dni, StringFormat='DNI: {0}'}""/>
                                <Button Text=""Ver PDF"" Clicked=""OnVerPdf""/>
                            </VerticalStackLayout>
                        </Frame>
                    </DataTemplate>
                </CollectionView.ItemTemplate>
            </CollectionView>
        </VerticalStackLayout>
    </ScrollView>
</ContentPage>
"@

Replace-File "$ClientName\Pages\Doctor\BuscarEstudiosPage.xaml.cs" @"
using $ClientName.Models;
using $ClientName.Services;

namespace $ClientName.Pages.Doctor;

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
        var dni = DniEntry.Text?.Trim();
        var nombre = NombreEntry.Text?.Trim();
        var apellido = ApellidoEntry.Text?.Trim();

        var url = $""api/estudios/buscar?dni={dni}&nombre={nombre}&apellido={apellido}"";
        var lista = await _api.GetJsonAsync<List<Estudio>>(url);
        ResultadosList.ItemsSource = lista ?? new();
    }

    private async void OnVerPdf(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.BindingContext is Estudio est)
        {
            var baseUrl = Config.ApiConfig.BaseUrl.TrimEnd('/');
            var path = est.RutaArchivo?.TrimStart('/') ?? """";
            var url = $""{baseUrl}/{path}"";
            await Launcher.OpenAsync(new Uri(url));
        }
    }
}
"@

Replace-File "$ClientName\Pages\Admin\HomeAdminPage.xaml" @"
<?xml version=""1.0"" encoding=""utf-8"" ?>
<ContentPage
    xmlns=""http://schemas.microsoft.com/dotnet/2021/maui""
    xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml""
    x:Class=""$ClientName.Pages.Admin.HomeAdminPage""
    Title=""Admin"">

    <VerticalStackLayout Padding=""24"" Spacing=""12"">
        <Label Text=""Menú Admin"" FontSize=""22""/>
        <Button Text=""Subir Estudio"" Clicked=""OnSubirEstudio""/>
    </VerticalStackLayout>
</ContentPage>
"@

Replace-File "$ClientName\Pages\Admin\HomeAdminPage.xaml.cs" @"
namespace $ClientName.Pages.Admin;

public partial class HomeAdminPage : ContentPage
{
    public HomeAdminPage()
    {
        InitializeComponent();
    }

    private async void OnSubirEstudio(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(""//SubirEstudio"");
    }
}
"@

Replace-File "$ClientName\Pages\Admin\SubirEstudioPage.xaml" @"
<?xml version=""1.0"" encoding=""utf-8"" ?>
<ContentPage
    xmlns=""http://schemas.microsoft.com/dotnet/2021/maui""
    xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml""
    x:Class=""$ClientName.Pages.Admin.SubirEstudioPage""
    Title=""Subir Estudio PDF"">

    <ScrollView>
        <VerticalStackLayout Padding=""16"" Spacing=""8"">
            <Entry x:Name=""PacienteIdEntry"" Placeholder=""Id Paciente"" Keyboard=""Numeric""/>
            <Button Text=""Seleccionar PDF"" Clicked=""OnPickPdf""/>
            <Label x:Name=""ArchivoLabel"" Text=""Ningún archivo"" FontAttributes=""Italic""/>
            <Button Text=""Subir"" Clicked=""OnUpload""/>
        </VerticalStackLayout>
    </ScrollView>
</ContentPage>
"@

Replace-File "$ClientName\Pages\Admin\SubirEstudioPage.xaml.cs" @"
using $ClientName.Services;

namespace $ClientName.Pages.Admin;

public partial class SubirEstudioPage : ContentPage
{
    private readonly ApiService _api;
    private FileResult? _pdf;

    public SubirEstudioPage(ApiService api)
    {
        InitializeComponent();
        _api = api;
    }

    private async void OnPickPdf(object sender, EventArgs e)
    {
        _pdf = await FilePicker.PickAsync(new PickOptions
        {
            PickerTitle = ""Seleccionar PDF"",
            FileTypes = FilePickerFileType.Pdf
        });

        ArchivoLabel.Text = _pdf?.FileName ?? ""Ningún archivo"";
    }

    private async void OnUpload(object sender, EventArgs e)
    {
        if (_pdf == null)
        {
            await DisplayAlert(""Error"", ""Seleccioná un PDF"", ""OK"");
            return;
        }

        if (!int.TryParse(PacienteIdEntry.Text, out var idPaciente))
        {
            await DisplayAlert(""Error"", ""Id Paciente inválido"", ""OK"");
            return;
        }

        using var stream = await _pdf.OpenReadAsync();
        var content = new MultipartFormDataContent();
        content.Add(new StreamContent(stream), ""archivo"", _pdf.FileName);
        content.Add(new StringContent(idPaciente.ToString()), ""IdPaciente"");

        var resp = await _api.PostMultipartAsync(""api/estudios/subir"", content);
        if (resp.IsSuccessStatusCode)
            await DisplayAlert(""OK"", ""Estudio subido"", ""Cerrar"");
        else
            await DisplayAlert(""Error"", ""No se pudo subir"", ""Cerrar"");
    }
}
"@

# Recursos mínimos (evitar warnings)
Write-File "$ClientName\Resources\Splash\splash.svg" "<svg xmlns='http://www.w3.org/2000/svg' width='128' height='128'><rect width='100%' height='100%' fill='#512BD4'/></svg>"
Write-File "$ClientName\Resources\AppIcon\appicon.svg" "<svg xmlns='http://www.w3.org/2000/svg' width='128' height='128'><circle cx='64' cy='64' r='60' fill='#512BD4'/></svg>"
Write-File "$ClientName\Resources\AppIcon\appiconfg.svg" "<svg xmlns='http://www.w3.org/2000/svg' width='128' height='128'><text x='50%' y='50%' dominant-baseline='middle' text-anchor='middle' fill='white' font-size='48'>CM</text></svg>"

# (Opcional) fuentes dummy para evitar errores si no existen
""> "$ClientName\Resources\Fonts\OpenSans-Regular.ttf"
""> "$ClientName\Resources\Fonts\OpenSans-Semibold.ttf"

# 4) Agregar proyectos a la solución
Write-Host "`n==> Agregando proyectos a la solución"
dotnet sln $SolutionName.sln add "$BackendName\$BackendName.csproj" | Out-Null
dotnet sln $SolutionName.sln add "$ClientName\$ClientName.csproj"   | Out-Null

Write-Host "`n✅ Listo!"
Write-Host "Abrí $SolutionName.sln en Visual Studio."
Write-Host "Ajustá la conexión MySQL en $BackendName\appsettings.json y la BaseUrl en $ClientName\Config\ApiConfig.cs"
Write-Host "Para EF (opcional): dotnet tool install --global dotnet-ef; dotnet ef migrations add Initial -p $BackendName -s $BackendName; dotnet ef database update -p $BackendName -s $BackendName"
