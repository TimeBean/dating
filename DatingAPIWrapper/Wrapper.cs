using System.Net;
using System.Net.Http.Json;
using DatingAPIWrapper.Exceptions;
using DatingAPIWrapper.Options;
using DatingContracts;
using DatingContracts.Dtos;
using Microsoft.Extensions.Options;

namespace DatingAPIWrapper;

public class Wrapper
{
    private readonly HttpClient _httpClient;
    private readonly WrapperOption _wrapperOption;

    public Wrapper(HttpClient http, IOptions<WrapperOption> wrapperOption)
    {
        _httpClient = http;
        _wrapperOption = wrapperOption.Value;
        _httpClient.BaseAddress = new Uri(_wrapperOption.BaseUrl);
        _httpClient.Timeout = _wrapperOption.Timeout;
    }

    protected async Task<T> GetAsync<T>(string url)
    {
        var response = await _httpClient.GetAsync(url);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return default;

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

    protected async Task PutAsync<TRequest>(string url, TRequest body)
    {
        var response = await _httpClient.PutAsJsonAsync(url, body);

        if (!response.IsSuccessStatusCode)
            throw await ApiException.FromResponse(response);
    }

    protected async Task DeleteAsync(string url)
    {
        var response = await _httpClient.DeleteAsync(url);

        if (!response.IsSuccessStatusCode)
            throw await ApiException.FromResponse(response);
    }
    
    protected async Task PatchAsync<TRequest>(string url, TRequest body)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, url)
        {
            Content = JsonContent.Create(body)
        };

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            throw await ApiException.FromResponse(response);
    }

    public Task<List<UserDto>> GetUsersAsync()
        => GetAsync<List<UserDto>>("/api/users");

    public Task<UserDto?> GetUserAsync(long id)
        => GetAsync<UserDto?>($"/api/users/{id}");

    public Task<UserDto> CreateUserAsync(UserDto user)
        => PostAsync<UserDto, UserDto>("/api/users", user);

    public Task UpdateUserAsync(long id, UserDto user)
        => PutAsync($"/api/users/{id}", user);

    public Task DeleteUserAsync(long id)
        => DeleteAsync($"/api/users/{id}");
    
    public Task PatchUserAsync(long id, UpdateUser update)
        => PatchAsync($"/api/users/{id}", update);
}