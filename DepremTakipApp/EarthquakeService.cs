using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

public class EarthquakeService
{
    // HttpClient'ı tek bir örnek (instance) olarak yönetmek bağlantı performansını artırır
    private static readonly HttpClient client = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(10) // 10 saniye içinde cevap gelmezse zaman aşımı uygula
    };

    private const string ApiUrl = "https://api.orhanaydogdu.com.tr/deprem/kandilli/live";

    public async Task<List<Earthquake>> GetEarthquakesAsync()
    {
        try
        {
            if (!client.DefaultRequestHeaders.Contains("User-Agent"))
            {
                client.DefaultRequestHeaders.Add("User-Agent", "DepremTakipApp/1.0");
            }

            // Asenkron olarak veriyi çekiyoruz
            HttpResponseMessage response = await client.GetAsync(ApiUrl);
            response.EnsureSuccessStatusCode();

            string jsonString = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var apiResponse = JsonSerializer.Deserialize<ApiResponse>(jsonString, options);

            return apiResponse?.result ?? new List<Earthquake>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"API Hatası: {ex.Message}");
            return null;
        }
    }
}