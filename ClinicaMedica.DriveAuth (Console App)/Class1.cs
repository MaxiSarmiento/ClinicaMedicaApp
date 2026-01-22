using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;

Console.WriteLine("Autorizando Google Drive...");

using var stream = new FileStream("google-oauth.json", FileMode.Open, FileAccess.Read);

var credPath = "token-store";

var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
    GoogleClientSecrets.FromStream(stream).Secrets,
    new[] { DriveService.Scope.Drive },
    "clinica-medica-user",
    CancellationToken.None,
    new FileDataStore(credPath, true)
);

Console.WriteLine("AUTORIZADO CORRECTAMENTE");
Console.ReadLine();
