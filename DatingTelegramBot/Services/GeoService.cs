using Nominatim.API.Geocoders;
using Nominatim.API.Interfaces;
using Nominatim.API.Models;
using Nominatim.API.Web;

namespace DatingTelegramBot.Services;

public class GeoService
{
    private readonly ForwardGeocoder _geocoder;

    public GeoService(IHttpClientFactory httpClientFactory)
    {
        INominatimWebInterface webInterface =
            new NominatimWebInterface(
                httpClientFactory
            );

        _geocoder = new ForwardGeocoder(webInterface);
    }

    public async Task<(double lat, double lon)?> GeocodeAsync(string place, CancellationToken ct)
    {
        var result = (await _geocoder.Geocode(
            new ForwardGeocodeRequest
            {
                queryString = place,
                LimitResults = 1
            }
        )).FirstOrDefault();

        if (result == null)
            return null;

        return (result.Latitude, result.Longitude);
    }
}