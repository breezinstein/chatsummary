
using Azure;
using Azure.AI.TextAnalytics;
using Azure.Identity;
using System.Text;

namespace Breeze.ChatSummary
{
    public class AzureLanguage: IMessageAnalyzer
    {
        private IProgramSettings matrixSettings;

        public AzureLanguage()
        {
            matrixSettings = new MatrixSettings();
        }

        public async Task<string> AnalyzeTextAsync(string textToAnalyze)
        {
            AzureKeyCredential credential = new(matrixSettings.AZURE_API_KEY);
            var client = new TextAnalyticsClient(new Uri(matrixSettings.AZURE_API_ENDPOINT), credential);
            var options = new AbstractiveSummarizeOptions
            {
                SentenceCount = 7
            };
            return await AbstractiveSummarize(client, new List<string> { textToAnalyze});
        }

        private async Task<string> AbstractiveSummarize(TextAnalyticsClient client, List<string> document)
        {
            AbstractiveSummarizeOperation abstractiveSummarizeOperation = client.AbstractiveSummarize(Azure.WaitUntil.Completed, document);
            
            string output = "";
            

            // View the operation results.
            await foreach (AbstractiveSummarizeResultCollection documentsInPage in abstractiveSummarizeOperation.Value)
            {
                foreach (AbstractiveSummarizeResult documentResult in documentsInPage)
                {
                    if (documentResult.HasError)
                    {
                        Console.WriteLine($"  Error!");
                        Console.WriteLine($"  Document error code: {documentResult.Error.ErrorCode}");
                        Console.WriteLine($"  Message: {documentResult.Error.Message}");
                        continue;
                    }

                    Console.WriteLine();

                    foreach (AbstractiveSummary summary in documentResult.Summaries)
                    {
                        var tempText = RemoveFirstSentence(summary.Text); 
                        output += $"Summary: {tempText}";
                    }
                }
            }

            return output;
        }

        private string RemoveFirstSentence(string paragraph)
        {
            int firstSentenceEnd = paragraph.IndexOf('.');
            if (firstSentenceEnd >= 0)
            {
                return paragraph.Substring(firstSentenceEnd + 1).TrimStart();
            }
            else
            {
                return paragraph;
            }
        }
    }
}
