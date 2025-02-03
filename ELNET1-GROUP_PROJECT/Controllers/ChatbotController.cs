using Microsoft.AspNetCore.Mvc;
using YourApp.Services;  // Make sure the correct namespace is used for your services

namespace YourApp.Controllers
{
    [Route("api/chatbot")]
    [ApiController]
    public class ChatbotController : ControllerBase
    {
        private readonly ChatbotService _chatbotService;

        // Constructor to inject ChatbotService
        public ChatbotController(ChatbotService chatbotService)
        {
            _chatbotService = chatbotService;
        }

        // POST endpoint to handle the conversation
        [HttpPost]
        public async Task<IActionResult> GetResponse([FromBody] ChatRequest request)
        {
            // Retrieve the sessionId to track the conversation
            string sessionId = request.SessionId;

            // Call the chatbot service to get the bot's response based on the user's message
            string botResponse = await _chatbotService.GetBotResponseAsync(request.Message, sessionId);

            // Return the bot's response to the frontend
            return Ok(new { response = botResponse });
        }
    }

    // ChatRequest model to accept the incoming request with user message and session ID
    public class ChatRequest
    {
        public string Message { get; set; }
        public string SessionId { get; set; } // Session ID to track the conversation
    }
}
