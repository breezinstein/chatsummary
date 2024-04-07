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
            var groupedMessages = GroupMessagesByAmount(hourlyMessages, 100, yesterday);

            Console.WriteLine("Analyzing Text...");
            Console.WriteLine("Grouping Messages...");

            string output = $"Summary of {yesterday.Date.ToString("d")}\n";
            output += $"First message at {groupedMessages[0].earliestMessageTimeStamp.ToString("T")}\n";
            output += $"Last message at {groupedMessages[groupedMessages.Count - 1].latestMessageTimeStamp.ToString("T")}\n";
            output += $"Total Messages: {groupedMessages.Sum(x => x.Count)}\n";
            output += "\n";
            foreach (var group in groupedMessages)
            {
                output += $"{group.Count} Messages between {group.duration}\n";
                output += await azureLanguage.AnalyzeTextAsync(group.value);
                output += "\n\n";
            }

            Console.WriteLine(output);
            Console.WriteLine("Posting to Matrix...");
            MatrixMessagePoster poster = new MatrixMessagePoster();
            await poster.PostMessage(output);
        }

        //method to combine multiple MatrixMessageGroup objects into a smaller number of MatrixMessageGroup objects by ensuring that each object has a maxixum number of messages
        private static List<MatrixMessageGroup> GroupMessagesByAmount(Dictionary<DateTime, MatrixMessageGroup> dictionary, int maxAmountPerEntry, DateTime dateToGroup)
        {
            List<MatrixMessageGroup> groupedMessages = new List<MatrixMessageGroup>();
            MatrixMessageGroup currentGroup = new MatrixMessageGroup();

            foreach (var item in dictionary)
            {
                if (item.Key.Date != dateToGroup.Date)
                {
                    continue;
                }
                if (currentGroup.Count + item.Value.Count <= maxAmountPerEntry)
                {
                    currentGroup = currentGroup + item.Value;
                }
                else
                {
                    groupedMessages.Add(currentGroup);
                    currentGroup = new MatrixMessageGroup();
                    currentGroup = item.Value;
                }
            }
            if (currentGroup.Count > 0)
            {
                groupedMessages.Add(currentGroup);
            }

            foreach (var item in groupedMessages)
            {
                item.UpdateDuration();
            }

            return groupedMessages;

        }

    }
}