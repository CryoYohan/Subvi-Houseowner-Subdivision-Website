using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class PayMongoService
{
    private readonly string _secretKey;
    private readonly HttpClient _httpClient;

    public PayMongoService(string secretKey)
    {
        _secretKey = secretKey ?? throw new ArgumentNullException(nameof(secretKey));
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(secretKey)));
    }

    public async Task<string> CreatePaymentIntent(decimal amount, string gcashNumber, string currency = "PHP")
    {
        if (amount <= 0)
        {
            throw new ArgumentException("Amount must be greater than zero.", nameof(amount));
        }

        if (string.IsNullOrEmpty(gcashNumber))
        {
            throw new ArgumentException("GCash number is required for GCash payments.", nameof(gcashNumber));
        }

        var requestBody = new
        {
            data = new
            {
                attributes = new
                {
                    amount = (int)(amount * 100), // Convert to cents
                    currency = currency,
                    payment_method_allowed = new[] { "gcash" },
                    description = "Payment for Bill"
                }
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("https://api.paymongo.com/v1/payment_intents", content);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to create payment intent: {response.StatusCode}\n{errorContent}");
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);

        // Extract checkout URL from response
        var clientKey = responseData.GetProperty("data").GetProperty("attributes").GetProperty("client_key").GetString();
        var paymentIntentId = responseData.GetProperty("data").GetProperty("id").GetString();
        var checkoutUrl = $"https://paymongo.page/checkout/{paymentIntentId}/{clientKey}";

        return checkoutUrl;
    }
}
