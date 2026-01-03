using System.Net.Http.Json;
using DatingAPIWrapper.Exceptions;
using DatingAPIWrapper.Options;
using DatingContracts;

namespace DatingAPIWrapper;

public class Wrapper
{
    private readonly HttpClient _httpClient;
    
    public Wrapper(HttpClient http, WrapperOption options)
    {
        _httpClient = http;
        
        _httpClient.BaseAddress = new Uri(options.BaseUrl);
        _httpClient.Timeout = options.Timeout;
    }
    
    protected async Task<T> GetAsync<T>(string url)
    {
        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            throw await ApiException.FromResponse(response);

        return await response.Content.ReadFromJsonAsync<T>()
               ?? throw new InvalidOperationException("Empty response");
    }

    protected async Task<TResponse> PostAsync<TRequest, TResponse>(string url, TRequest body)
    {
        var response = await _httpClient.PostAsJsonAsync(url, body);

        if (!response.IsSuccessStatusCode)
            throw await ApiException.FromResponse(response);

        return await response.Content.ReadFromJsonAsync<TResponse>()
               ?? throw new InvalidOperationException("Empty response");
    }

    public Task<List<UserDto>> GetUsersAsync()
        => GetAsync<List<UserDto>>("/api/users");
}