using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Breeze.ChatSummary
{
    public class ClaudeMessageAnalyzer : IMessageAnalyzer
    {
        private readonly string _apiKey;
        private readonly string _apiUrl = "https://api.anthropic.com/v1/messages";
        private readonly string _apiVersion = "2023-06-01";
        private string prompt = "You are an expert content summarizer. You take content in and output a Markdown formatted summary, focus on the main points and takeaways. \n";
        public ClaudeMessageAnalyzer(string apiKey)
        {
            _apiKey = apiKey;
        }

        public async Task<string> AnalyzeTextAsync(string textToAnalyze)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
            httpClient.DefaultRequestHeaders.Add("anthropic-version", _apiVersion);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var requestBody = new
            {
                model = "claude-3-5-sonnet-20240620",
                max_tokens = 1024,
                messages = new[]
                {
                    new { role = "user", content = prompt=textToAnalyze }
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            try
            {
                var response = await httpClient.PostAsync(_apiUrl, jsonContent);
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<AnthropicResponse>(jsonResponse);

                return result?.Content[0]?.Text ?? string.Empty;
            }
            catch (HttpRequestException ex)
            {
                // Handle API errors based on the status code
                if (ex.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    // Handle invalid request error
                    Console.WriteLine("Invalid request");                    
                }
                else if (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    // Handle authentication error
                    Console.WriteLine("Unauthorized");
                }
                // Handle other error codes as needed
                else
                {
                    // Handle other errors
                    Console.WriteLine("An error occurred: " + ex.Message);
                }
                throw;
            }
        }
    }

    public class AnthropicResponse
    {
        public AnthropicContent[] Content { get; set; }
        public string Id { get; set; }
        public string Model { get; set; }
        public string Role { get; set; }
        public string StopReason { get; set; }
        public string StopSequence { get; set; }
        public string Type { get; set; }
        public AnthropicUsage Usage { get; set; }
    }

    public class AnthropicContent
    {
        public string Text { get; set; }
        public string Type { get; set; }
    }

    public class AnthropicUsage
    {
        public int InputTokens { get; set; }
        public int OutputTokens { get; set; }
    }
}