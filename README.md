# ClinicaMedicaApp
 App MAUI para rendir Final de Programacion movil

# Clínica Médica — App (MAUI + ASP.NET Core + SQL Server)

Proyecto completo tipo Clínica / Instituto Médico desarrollado con:

- **Frontend:** .NET MAUI (Android + Desktop)
- **Backend:** ASP.NET Core Web API
- **Base de Datos:** SQL Server
- **ORM:** Entity Framework Core
- **Autenticación:** JWT (JSON Web Tokens)
- **Roles:** Paciente / Doctor / Admin

---

##  Objetivo del sistema

Crear una aplicación donde los usuarios puedan:

### Paciente
- Registrarse e iniciar sesión
- Editar su perfil (incluye fecha de nacimiento → muestra edad)
- Seleccionar Obra Social y número de socio
- Reservar turnos médicos
- Ver sus turnos
- Ver estudios clínicos

### Doctor
- Configurar agenda semanal (reglas)
- Generar turnos libres automáticamente (según reglas o masivo)
- Ver agenda con turnos generados/reservados
- Buscar pacientes
- Ver estudios del paciente
- Editar sus datos y asignarse Obras Sociales / Especialidades

### Admin
- Gestionar usuarios:
  - Cambiar rol
  - Bloquear / desbloquear
  - Eliminar usuario
- Gestionar Obras Sociales
- Gestionar Especialidades
- (Opcional) Gestión de estudios clínicos

---

## Arquitectura del proyecto

📌 Solución dividida en 2 partes principales:

- `ClinicaMedica.Cliente` → **Aplicación .NET MAUI**
- `ClinicaMedica.Backend` → **ASP.NET Core Web API**
- SQL Server → Base de datos local

---

##  Requisitos para ejecutar

###  Backend
- .NET SDK **8.0**
- SQL Server (2017+ / recomendado 2022)
- Visual Studio 2022 o VS Code

###  Cliente MAUI
- .NET MAUI workload instalado
- Android SDK (si se ejecuta en Android)
- Emulador o celular real habilitado para debug USB

---

##  Configuración rápida

### 1) Configurar el backend

1. Abrir la carpeta `ClinicaMedica.Backend`
2. Configurar el `appsettings.json` con tu SQL Server:

Ejemplo (Windows Auth):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ClinicaMedicaDB;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}