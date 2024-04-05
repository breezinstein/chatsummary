using System.Text;
namespace Breeze.ChatSummary
{
    internal class Program : MatrixMessageExtractor
    {
        //this is a program to retrieve messages from a matrix room
        private static async Task Main(string[] args)
        {
            MatrixMessageExtractor extractor = new MatrixMessageExtractor();
            AzureLanguage azureLanguage = new AzureLanguage();

            DateTime yesterday = DateTime.Now.AddDays(-1);

            Console.WriteLine("Getting Yesterday's Report...");
            var hourlyMessages = await extractor.GetMessagesByHour();
            Console.WriteLine("Analyzing Text...");
            string output = $"Summary of {yesterday.Date.ToString("d")}\n";
            foreach (var message in hourlyMessages)
            {
                if (message.Key.Date == yesterday.Date)
                {
                    Console.WriteLine($"{message.Value.Count} Messages between {message.Key.TimeOfDay} and {message.Key.TimeOfDay + new TimeSpan(6,0,0)}");
                    output += $"{message.Value.Count} Messages between {message.Key.TimeOfDay} and {message.Key.TimeOfDay + new TimeSpan(6, 0, 0)}";
                    output += "\n";
                    output += await azureLanguage.AnalyzeTextAsync(GetConsolidatedMessages(message.Value));
                    output += "\n\n";
                }
            }

            Console.WriteLine("Posting to Matrix...");
            MatrixMessagePoster poster = new MatrixMessagePoster();
            await poster.PostMessage(output);
        }

        static string GetConsolidatedMessages(List<MatrixMessage> messages)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var message in messages)
            {
                sb.Append($"{message.value}");
                sb.Append(" ");
            }
            return sb.ToString();
        }
    }
}