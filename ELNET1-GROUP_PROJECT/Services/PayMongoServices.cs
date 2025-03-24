using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class PayMongoServices
{
    private readonly HttpClient _httpClient;
    private readonly string _secretKey;
    private readonly string _baseUrl;

    public PayMongoServices(HttpClient httpClient, string secretKey, bool useSandbox = true)
    {
        _httpClient = httpClient;
        _secretKey = secretKey;
        _baseUrl = useSandbox ? "https://api.paymongo.com/v1" : "https://api.paymongo.com/v1";

        // Set Auth Header
        var encodedKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(_secretKey));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", encodedKey);
    }

    // Create Payment Intent
    public async Task<PayMongoPaymentIntentResponse> CreatePaymentIntent(decimal amount, string description, string[] paymentMethods)
    {
        var requestBody = new
        {
            data = new
            {
                attributes = new
                {
                    amount = (int)(amount * 100),  // Convert PHP to centavos
                    currency = "PHP",
                    description = description,
                    payment_method_allowed = paymentMethods,
                    payment_method_options = new { card = new { request_three_d_secure = "automatic" } }
                }
            }
        };

        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync($"{_baseUrl}/payment_intents", content);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to create payment intent: {response.StatusCode}");
        }

        var jsonResponse = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<PayMongoPaymentIntentResponse>(jsonResponse, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }
}
