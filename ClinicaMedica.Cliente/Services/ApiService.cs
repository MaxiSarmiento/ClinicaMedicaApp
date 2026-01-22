using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Http.Headers;

namespace ClinicaMedica.Cliente.Services
{
    public class ApiService
    {
        private readonly HttpClient _http;

        public ApiService(HttpClient http)
        {
            _http = http;

        }

        public HttpClient Client => _http;

        public Task<HttpResponseMessage> GetAsync(string path)
            => _http.GetAsync(path);

        public Task<T?> GetJsonAsync<T>(string path)
            => _http.GetFromJsonAsync<T>(path);

        public Task<HttpResponseMessage> PostAsync<T>(string path, T body)
            => _http.PostAsJsonAsync(path, body);

        public Task<HttpResponseMessage> PostMultipartAsync(string path, MultipartFormDataContent content)
            => _http.PostAsync(path, content);


        public Task<HttpResponseMessage> PutAsync<T>(string path, T body)
            => _http.PutAsJsonAsync(path, body);

        public Task<HttpResponseMessage> DeleteAsync(string path)
            => _http.DeleteAsync(path);

    }
}
