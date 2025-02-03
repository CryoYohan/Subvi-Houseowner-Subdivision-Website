using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace YourApp.Services
{
    public class ChatbotService
    {
        private readonly HttpClient _httpClient;
        private readonly string _pythonApiUrl = "http://localhost:5000/predict"; // URL of your Python Flask API

        public ChatbotService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> GetBotResponseAsync(string userMessage, string sessionId)
        {
            // Prepare the message to send to Python API
            var requestBody = new
            {
                message = userMessage
            };

            var jsonContent = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // Call the Python API
            var response = await _httpClient.PostAsync(_pythonApiUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                return "Error: Could not process the message.";
            }

            // Parse the response from the Python API
            var responseData = await response.Content.ReadAsStringAsync();
            dynamic jsonResponse = JsonConvert.DeserializeObject(responseData);

            // Use the response from the bot API (response is based on the intent)
            return jsonResponse?.response ?? "I didn't understand that.";
        }
    }
}
