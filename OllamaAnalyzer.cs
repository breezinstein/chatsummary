using OllamaSharp;

namespace Breeze.ChatSummary
{
    public class OllamaAnalyzer : IMessageAnalyzer
    {
        private OLLAMAAPI settings { get; set; }
        public OllamaAnalyzer(OLLAMAAPI _settings)
        {
            settings = _settings;
            _context = new ConversationContext(new long[]{});
        }

        private ConversationContext _context;
        public void resetContext()
        {
            _context = new ConversationContext(new long[] { });
        }

        public async Task<string> AnalyzeTextAsync(string textToAnalyze)
        {
            var ollama = new OllamaApiClient(settings.API_ENDPOINT_URL);

            string heading = "# IDENTITY and PURPOSE\r\n\r\nYou are an expert content summarizer. You take content in and output a Markdown formatted summary using the format below.\r\n\r\nTake a deep breath and think step by step about how to best accomplish this goal using the following steps.\r\n\r\n# OUTPUT SECTIONS\r\n\r\n- Combine all of your understanding of the content into a single, 20-word sentence in a section called ONE SENTENCE SUMMARY:.\r\n\r\n- Output the 10 most important points of the content as a list with no more than 15 words per point into a section called MAIN POINTS:.\r\n\r\n- Output a list of the 5 best takeaways from the content in a section called TAKEAWAYS:.\r\n";
            string grounding = "# OUTPUT INSTRUCTIONS\r\n\r\n- Create the output using the formatting above.\r\n- You only output human readable Markdown.\r\n- Output numbered lists, not bullets.\r\n- Do not output warnings or notes—just the requested sections.\r\n- Do not repeat items in the output sections.\r\n- Do not start items with the same opening words.\r\n\r\n# INPUT:";
            string text = heading + grounding + textToAnalyze;
            ollama.SelectedModel = "llama3";
            var result = await ollama.GetCompletion(text, _context);
            if (result != null)
            {
                _context = new ConversationContext(result.Context);
            return result.Response;
            }
            return string.Empty;
        }

    }
}
