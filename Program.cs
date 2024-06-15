using System.Text;
namespace Breeze.ChatSummary
{
    // This class represents the main program for retrieving and summarizing messages from a matrix room
    internal class Program
    {
        private static ProgramSettings settings;

        // Entry point of the program
        private static async Task Main(string[] args)
        {
            //Getting settings from jsonfile
            settings = new ProgramSettings().LoadFromJSON("settings.json").Validate();
            if (settings == null)
            {
                Console.WriteLine("Settings file not found or improperly configured");
                return;
            }

            int roomNumber = GetRoomsPrompt(settings.MatrixConfig.ROOMS);
            if (roomNumber == settings.MatrixConfig.ROOMS.Count)
            {
                return;
            }

            MatrixMessageExtractor extractor = new MatrixMessageExtractor(settings.MatrixConfig, settings.MatrixConfig.ROOMS[roomNumber]);
            IMessageAnalyzer messageAnalyzer = new OllamaAnalyzer(settings.OLLAMAAPI);

            Console.WriteLine("Getting Messages...");
            Dictionary<DateTime, MatrixMessageGroup> hourlyMessages = await extractor.GetMessagesByHour();
            List<MatrixMessageGroup> groupedMessages;
            DateTime date = DateTime.Now.AddDays(-1);

            //List available options


            int option = GetOptionsPrompt();

            switch (option)
            {
                case 1:
                    date = DateTime.Now.AddDays(-1);
                    Console.WriteLine("Grouping Messages...");
                    groupedMessages = GroupMessagesByAmount(hourlyMessages, 500, date);
                    break;
                case 2:
                    date = DateTime.Now;
                    Console.WriteLine("Grouping Messages...");
                    groupedMessages = GroupMessagesByAmount(hourlyMessages, 500, date);
                    break;
                //case 3:
                //    Console.WriteLine("Enter the number of hours you want to analyze (1 - 24):");
                //    date = DateTime.Now.AddHours(-Convert.ToInt32(Console.ReadLine()));
                //    Console.WriteLine("Grouping Messages...");
                //    groupedMessages = GroupMessagesByAmount(hourlyMessages, 500, date);
                //    break;
                //case 3:
                //    Console.WriteLine("Enter the number of messages you want to analyze (10 - 1000):");
                //    int amount = Convert.ToInt32(Console.ReadLine());
                //    Console.WriteLine("Grouping Messages...");
                //    List<MatrixMessage> messages = await extractor.GetLastMessages(amount);
                //    groupedMessages = new List<MatrixMessageGroup>();
                //    MatrixMessageGroup group = new MatrixMessageGroup();
                //    group.AddMessages(messages);
                //    groupedMessages.Add(group); 
                //    break;
                default:
                    Console.WriteLine("Invalid Option!");
                    return;
            }

            Console.WriteLine("Analyzing Text...");
            string output = await AnalyzeMessages(messageAnalyzer, groupedMessages);
            Console.WriteLine(output);

            await PostMessage(groupedMessages, output, settings.OLLAMAAPI);

        }
        private static int GetOptionsPrompt()
        {
            Console.WriteLine("Enter the number of the option you want to use:");
            Console.WriteLine("1: Summarize Yesterday's Messages");
            Console.WriteLine("2: Summarize Today's Messages");
            //Console.WriteLine("3: Summarize Last ### Number of Messages (10 - 1000 messages)");

            int option;
            if (!int.TryParse(Console.ReadLine(), out option))
            {
                Console.WriteLine("Invalid option selected");
                return GetOptionsPrompt();
            }
            if (!(option > 0 && option < 5))
            {
                Console.WriteLine("Invalid option selected");
                return GetOptionsPrompt();
            }
            return option;
        }

        private static int GetRoomsPrompt(List<Room> ROOMS)
        {
            Console.WriteLine("Select Room number:");
            //List configured Rooms
            for (int i = 0; i < ROOMS.Count; i++)
            {
                Room room = ROOMS[i];
                Console.WriteLine($"{i + 1}: {room.Name}");
            }
            Console.WriteLine($"{ROOMS.Count + 1}: Cancel");
            int roomNumber;
            if (!int.TryParse(Console.ReadLine(), out roomNumber))
            {
                Console.WriteLine("Invalid Input");
                return GetRoomsPrompt(ROOMS);
            }
            if (!(roomNumber > 0 && roomNumber <= ROOMS.Count + 1))
            {
                Console.WriteLine("Invalid Room number");
                return GetRoomsPrompt(ROOMS);
            }
            return roomNumber - 1;
        }

        private static List<MatrixMessageGroup> GetAmountOfMessages(Dictionary<DateTime, MatrixMessageGroup> hourlyMessages, int amount)
        {
            return hourlyMessages.Values.ToList().GetRange(0, amount);
        }

        private static async Task PostMessage(List<MatrixMessageGroup> groupedMessages, string output, OLLAMAAPI ollamaAPI)
        {
            Console.WriteLine("Select Room to post to:");
            int roomNumber = GetRoomsPrompt(settings.MatrixConfig.ROOMS);

            if (roomNumber != settings.MatrixConfig.ROOMS.Count)
            {
                Console.WriteLine($"Posting to {settings.MatrixConfig.ROOMS[roomNumber].Name}");
                IMessagePoster poster = new MatrixMessagePoster(settings.MatrixConfig);
                output += "\nDISCLAIMER:Please be aware that text generated by AI can be inaccurate!";
                await poster.PostMessageAsync(output, settings.MatrixConfig.ROOMS[roomNumber].ID);
            }

            else
            {
                Console.WriteLine("Retry Analysis? (Y/N)");
                string response = Console.ReadLine();
                if (response.ToLower() == "y")
                {
                    IMessageAnalyzer messageAnalyzer = new OllamaAnalyzer(ollamaAPI);

                    output = await AnalyzeMessages(messageAnalyzer, groupedMessages);
                    Console.WriteLine(output);
                    await PostMessage(groupedMessages, output, ollamaAPI);
                }
            }
        }

        private static async Task<string> AnalyzeMessages(IMessageAnalyzer messageAnalyzer, List<MatrixMessageGroup> groupedMessages)
        {
            if (groupedMessages.Count == 0)
            {
                return string.Empty;
            }
            string output = $"Summary \n";
            output += $"First message at {groupedMessages[0].earliestMessageTimeStamp.ToString("T")}\n";
            output += $"Last message at {groupedMessages[groupedMessages.Count - 1].latestMessageTimeStamp.ToString("T")}\n";
            output += $"Total Messages: {groupedMessages.Sum(x => x.Count)}\n";
            output += "\n";
            foreach (var group in groupedMessages)
            {
                output += $"{group.Count} Messages between {group.duration}\n";
                output += await messageAnalyzer.AnalyzeTextAsync(group.value);
                output += "\n\n";
            }

            return output;
        }

        // Method to combine multiple MatrixMessageGroup objects into a smaller number of MatrixMessageGroup objects by ensuring that each object has a maximum number of messages
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