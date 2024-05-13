using Newtonsoft.Json;
using OllamaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Breeze.ChatSummary
{
    public class OllamaAnalyzer : IMessageAnalyzer
    {
        private IProgramSettings matrixSettings { get; set; }
        public OllamaAnalyzer()
        {
            matrixSettings = new MatrixSettings();
        }

        private ConversationContext _context;
        public void resetContext()
        {
            _context = new ConversationContext(new long[] { });
        }

        public async Task<string> AnalyzeTextAsync(string textToAnalyze)
        {
            var ollama = new OllamaApiClient(matrixSettings.OLLAMA_API_ENDPOINT);
            string heading = "You are an professional executive assistant. Generate an abstractive summary of the given conversation, Relying strictly on the provided text, without including external information\n";
            string grounding = "\nConstraints: Please start the summary with the delimiter “Summary” and limit the number of sentences in the abstractive summary to a maximum of one.";
            string text = heading + textToAnalyze + grounding;
            ollama.SelectedModel = "mistral";
            var result = await ollama.GetCompletion(text, _context);
            if (result != null)
            {
                _context = new ConversationContext(result.Context);
            }
            return result.Response;
        }

    }
}
