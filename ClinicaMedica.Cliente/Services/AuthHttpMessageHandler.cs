using System.Net.Http.Headers;

namespace ClinicaMedica.Cliente.Services;

public class AuthHttpMessageHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Tu app guarda el token en "token"
        var token = await SecureStorage.GetAsync("token");

        // Compat opcional (por si quedó algo viejo)
        if (string.IsNullOrWhiteSpace(token))
            token = await SecureStorage.GetAsync("jwt");

        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
