using ClinicaMedica.Backend.Data;
using ClinicaMedica.Backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Information);

builder.WebHost.ConfigureKestrel(options =>
{

    options.ListenAnyIP(5293); 
});

// Controllers y Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "ClinicaMedica API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Ingresá el token JWT así: Bearer {tu_token}"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
 

builder.Services.AddSingleton<IGoogleDriveService, GoogleDriveService>();

// AuthService
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<GoogleDriveOAuthService>();


// ⭐ SQL SERVER
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ClinicaDb"))
);

// ⭐ CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMobile", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ⭐ JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
            )
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var drive = scope.ServiceProvider.GetRequiredService<GoogleDriveOAuthService>();

    var ok = await drive.PingAsync();
    if (!ok)
    {
        app.Logger.LogWarning("⚠️ Google Drive NO está autorizado. Probá GET /api/health/drive para confirmar.");
    }
    else
    {
        app.Logger.LogInformation("✅ Google Drive autorizado correctamente.");
    }
}
app.MapGet("/debug/routes", (IEnumerable<EndpointDataSource> sources) =>
{
    var endpoints = sources.SelectMany(s => s.Endpoints)
        .OfType<RouteEndpoint>()
        .Select(e => e.RoutePattern.RawText)
        .Distinct()
        .OrderBy(x => x);

    return Results.Ok(endpoints);
});

// ⭐ Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseCors("AllowMobile");
app.UseAuthentication();
app.UseAuthorization();
app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path.StartsWithSegments("/api/DoctorAgendas", StringComparison.OrdinalIgnoreCase)
        || ctx.Request.Path.StartsWithSegments("/api/doctor-agendas", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine($"[REQ] {ctx.Request.Method} {ctx.Request.Path}{ctx.Request.QueryString}");
    }

    await next();

    if (ctx.Request.Path.StartsWithSegments("/api/DoctorAgendas", StringComparison.OrdinalIgnoreCase)
        || ctx.Request.Path.StartsWithSegments("/api/doctor-agendas", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine($"[RES] {ctx.Response.StatusCode} {ctx.Request.Method} {ctx.Request.Path}{ctx.Request.QueryString}");
    }
});
app.MapControllers();

app.Run();
