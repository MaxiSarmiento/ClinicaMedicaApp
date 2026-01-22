using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Auth.OAuth2.Requests;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ClinicaMedica.Backend.Services;

public class GoogleDriveOAuthService
{
    private readonly ILogger<GoogleDriveOAuthService> _logger;
    private readonly DriveService _drive;

    public GoogleDriveOAuthService(ILogger<GoogleDriveOAuthService> logger)
    {
        _logger = logger;

        // ✅ Token store persistente
        var credPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ClinicaMedica",
            "token-store"
        );
        Directory.CreateDirectory(credPath);

        // ✅ Secrets deben estar en output: Keys/google-oauth.json
        var secretsPath = Path.Combine(AppContext.BaseDirectory, "Keys", "google-oauth.json");

        _logger.LogInformation("OAuth token-store: {CredPath}", credPath);
        _logger.LogInformation("OAuth secrets: {SecretsPath}", secretsPath);

        if (!File.Exists(secretsPath))
            throw new FileNotFoundException($"No se encontró el archivo OAuth en: {secretsPath}");

        using var stream = new FileStream(secretsPath, FileMode.Open, FileAccess.Read);
        var secrets = GoogleClientSecrets.FromStream(stream).Secrets;

        // ✅ Flow OAuth
        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = secrets,
            Scopes = new[] { DriveService.Scope.Drive },
            DataStore = new FileDataStore(credPath, true)
        });

        // ✅ Receiver loopback para desktop (abre navegador, callback local)
        var receiver = new LoopbackCodeReceiver(_logger);

        // ✅ Si ya hay token guardado, NO abre navegador.
        var app = new AuthorizationCodeInstalledApp(flow, receiver);
        var credential = app.AuthorizeAsync("clinica-medica-user", CancellationToken.None)
                            .GetAwaiter()
                            .GetResult();

        _drive = new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "ClinicaMedica"
        });
    }

    // =======================
    // ✅ PING / HEALTHCHECK
    // =======================
    public async Task<bool> PingAsync()
    {
        try
        {
            var req = _drive.About.Get();
            req.Fields = "user";
            var about = await req.ExecuteAsync();
            return about?.User != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PingAsync falló (Drive no autorizado / token inválido).");
            return false;
        }
    }

    // =======================
    // ✅ SUBIR
    // =======================
    public async Task<(string fileId, string webViewLink, string downloadLink)> UploadAsync(
        Stream file,
        string fileName,
        string contentType,
        string folderId)
    {
        if (string.IsNullOrWhiteSpace(folderId))
            throw new ArgumentException("folderId vacío.");

        var meta = new Google.Apis.Drive.v3.Data.File
        {
            Name = fileName,
            Parents = new[] { folderId }
        };

        var request = _drive.Files.Create(meta, file, contentType);
        request.Fields = "id, webViewLink, webContentLink";

        var progress = await request.UploadAsync();

        if (progress.Status != Google.Apis.Upload.UploadStatus.Completed)
        {
            _logger.LogError("UploadAsync incompleto. Status={Status} Exception={Ex}",
                progress.Status, progress.Exception?.Message);

            throw new Exception($"Error subiendo a Drive. Status: {progress.Status}");
        }

        var uploaded = request.ResponseBody;
        if (uploaded == null || string.IsNullOrWhiteSpace(uploaded.Id))
            throw new Exception("Drive no devolvió Id del archivo.");

        return (uploaded.Id, uploaded.WebViewLink ?? "", uploaded.WebContentLink ?? "");
    }

    // =======================
    // ✅ DESCARGAR
    // =======================
    public async Task<(byte[] bytes, string contentType, string fileName)> DownloadAsync(string fileId)
    {
        if (string.IsNullOrWhiteSpace(fileId))
            throw new ArgumentException("fileId vacío.");

        // Metadata (mime y nombre)
        var meta = await _drive.Files.Get(fileId).ExecuteAsync();

        using var ms = new MemoryStream();

        // Descarga binaria
        var get = _drive.Files.Get(fileId);
        var progress = await get.DownloadAsync(ms);

        if (progress.Status != Google.Apis.Download.DownloadStatus.Completed)
        {
            _logger.LogError("DownloadAsync incompleto. Status={Status} Exception={Ex}",
                progress.Status, progress.Exception?.Message);

            throw new Exception($"Error descargando de Drive. Status: {progress.Status}");
        }

        return (ms.ToArray(), meta.MimeType ?? "application/octet-stream", meta.Name ?? "archivo");
    }

    // =======================
    // ✅ ELIMINAR
    // =======================
    public async Task DeleteAsync(string fileId)
    {
        if (string.IsNullOrWhiteSpace(fileId))
            return;

        await _drive.Files.Delete(fileId).ExecuteAsync();
    }

    // ==========================================================
    // LoopbackCodeReceiver: abre navegador y captura el "code"
    // ==========================================================
    private sealed class LoopbackCodeReceiver : ICodeReceiver
    {
        private readonly ILogger _logger;
        private HttpListener? _listener;
        private string? _redirectUri;

        public LoopbackCodeReceiver(ILogger logger) => _logger = logger;

        public string RedirectUri
        {
            get
            {
                if (_redirectUri is null)
                {
                    var port = GetRandomUnusedPort();
                    _redirectUri = $"http://127.0.0.1:{port}/";
                }
                return _redirectUri;
            }
        }

        public async Task<AuthorizationCodeResponseUrl> ReceiveCodeAsync(
            AuthorizationCodeRequestUrl url,
            CancellationToken taskCancellationToken)
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add(RedirectUri);
            _listener.Start();

            url.RedirectUri = RedirectUri;
            var authUrl = url.Build().ToString();

            _logger.LogWarning("OAuth URL (si no se abre, copiá y pegá): {Url}", authUrl);

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = authUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo abrir el navegador automáticamente. Abrí la URL del log.");
            }

            // Espera callback
            var context = await _listener.GetContextAsync().WaitAsync(taskCancellationToken);

            var query = context.Request.QueryString;
            var code = query["code"];
            var error = query["error"];

            // Respuesta al navegador
            var html = """
                <html><body>
                <h2>Listo ✅</h2>
                <p>Ya podés volver a la app.</p>
                </body></html>
                """;

            var buffer = Encoding.UTF8.GetBytes(html);
            context.Response.ContentType = "text/html";
            context.Response.ContentLength64 = buffer.Length;
            await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length, taskCancellationToken);
            context.Response.OutputStream.Close();

            try
            {
                _listener.Stop();
                _listener.Close();
            }
            catch { /* ignore */ }

            return new AuthorizationCodeResponseUrl
            {
                Code = code,
                Error = error
            };
        }

        private static int GetRandomUnusedPort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }
    }
}
