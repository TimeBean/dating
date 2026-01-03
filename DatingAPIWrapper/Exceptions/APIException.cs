using System.Net;

namespace DatingAPIWrapper.Exceptions;

public class ApiException : Exception
{
    public HttpStatusCode StatusCode { get; }

    private ApiException(string message, HttpStatusCode statusCode) : base(message)
    {
        StatusCode = statusCode;
    }

    public static async Task<ApiException> FromResponse(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();

        return new ApiException($"API error {(int)response.StatusCode}: {content}", response.StatusCode);
    }
}