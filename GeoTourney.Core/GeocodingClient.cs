using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace GeoTourney.Core
{
    public class GeocodingClient
    {
        static readonly HttpClient Client = new()
        {
            BaseAddress = new Uri("https://api.bigdatacloud.net")
        };

        public static async Task<IReadOnlyCollection<RoundLocation>> Locations(IConfiguration configuration, IEnumerable<GeoTournament.Round> rounds)
        {
            var apiKey = configuration["BigDataCloudApiKey"];
            var tasks = rounds.Select(x => new
            {
                Task = string.IsNullOrEmpty(apiKey) ? Task.FromResult<BigDataCloudResponse?>(null) : GetGeoDataFromBigDataCloud(x.lat, x.lng, apiKey),
                Round = x
            }).ToArray();
            await Task.WhenAll(tasks.Select(x => x.Task));
            return tasks.Select(x =>
            {
                var result = x.Task.Result;
                return new RoundLocation
                {
                    lat = x.Round.lat,
                    lng = x.Round.lng,
                    countryCode = result?.countryCode,
                    countryName = result?.countryName
                };
            }).Where(x => x != null).Select(x => x!).ToArray();
        }

        static async Task<BigDataCloudResponse?> GetGeoDataFromBigDataCloud(decimal lat, decimal lng, string apiKey)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
                var latitude = lat.ToString(CultureInfo.InvariantCulture);
                var longitude = lng.ToString(CultureInfo.InvariantCulture);
                return await Client.GetFromJsonAsync<BigDataCloudResponse>(
                    $"data/reverse-geocode?latitude={latitude}&longitude={longitude}&localityLanguage=en&key={apiKey}", cancellationToken: cts.Token);
            }
            catch (Exception)
            {
                return null;
            }
        }

        record BigDataCloudResponse
        {
            public decimal latitude { get; set; }
            public decimal longitude { get; set; }
            public string countryName { get; set; } = string.Empty;
            public string countryCode { get; set; } = string.Empty;
        }
    }

    public record RoundLocation
    {
        public decimal lat { get; set; }
        public decimal lng { get; set; }
        public string? countryCode { get; set; }
        public string? countryName { get; set; }
    }
}