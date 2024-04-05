using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Breeze.ChatSummary
{
    public class MatrixMessageExtractor
    {
        readonly HttpClient client = new HttpClient();
        private string lastMessageToken;
        Dictionary<DateTime, List<MatrixMessage>> dictionary = new Dictionary<DateTime, List<MatrixMessage>>();
        private IProgramSettings matrixSettings { get; set; }
        public MatrixMessageExtractor() {
            matrixSettings = new MatrixSettings();
         }
        private async Task<string?> GetAccessToken()
        {
            if (matrixSettings.ACCESS_TOKEN != "")
            {
                return matrixSettings.ACCESS_TOKEN;
            }
            else
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync($"{matrixSettings.HOMESERVER}/r0/login");
                    string responseBody = await response.Content.ReadAsStringAsync();
                    dynamic json = JsonConvert.DeserializeObject(responseBody);
                    string flowType = json.flows[0].type;

                    if (flowType == "m.login.password")
                    {
                        var payload = new
                        {
                            type = "m.login.password",
                            user = matrixSettings.USERNAME,
                            password = matrixSettings.PASSWORD
                        };

                        response = await client.PostAsync($"{matrixSettings.HOMESERVER}/login", new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json"));
                        responseBody = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Response: {responseBody}");
                        json = JsonConvert.DeserializeObject(responseBody);
                        string accessToken = json.access_token;
                        Console.WriteLine($"Access Token: {accessToken}");
                        return accessToken;
                    }

                    return null;
                }
            }
        }

        public async Task<List<MatrixMessage>> GetLastMessages(int amount)
        {
            string? accessToken = await GetAccessToken();

            string roomId = GetRoomId(accessToken);

            var messages = await GetMessages(accessToken, roomId, amount);
            Console.WriteLine($"Filtered Messages Retrieved: {messages.Count}");

            if (messages.Count < amount)
            {
                Console.WriteLine($"Requesting Additional Messages");

                var remainingMessages = await GetMessagesFromToken(accessToken, roomId, lastMessageToken, amount);
                messages.AddRange(remainingMessages);
                Console.WriteLine($"Filtered Messages Retrieved: {messages.Count}");

            }

            return messages;
        }

        public async Task<Dictionary<DateTime, List<MatrixMessage>>> GetMessagesByHour()
        {
            string? accessToken = await GetAccessToken();

            string roomId = GetRoomId(accessToken);

            var messages = await GetMessages(accessToken, roomId, 1000);

            if (messages.Count < 1500)
            {
                var remainingMessages = await GetMessagesFromToken(accessToken, roomId, lastMessageToken, 1000);
                messages.AddRange(remainingMessages);
            }

            var groupedMessages = GroupMessagesBySixHourChunks(messages);
            return groupedMessages;
        }

        private Dictionary<DateTime, List<MatrixMessage>> GroupMessagesByHour(List<MatrixMessage> messages)
        {
            foreach (var message in messages)
            {
                var date = DateTime.Parse(message.TimeStamp);
                var hour = new DateTime(date.Year, date.Month, date.Day, date.Hour, 0, 0);
                if (dictionary.ContainsKey(hour))
                {
                    dictionary[hour].Add(message);
                }
                else
                {
                    dictionary.Add(hour, new List<MatrixMessage> { message });
                }
            }
            return dictionary;
        }

        public Dictionary<DateTime, List<MatrixMessage>> GroupMessagesBySixHourChunks(List<MatrixMessage> messages)
        {
            foreach (var message in messages)
            {
                var date = DateTime.Parse(message.TimeStamp);
                var sixHourChunk = new DateTime(date.Year, date.Month, date.Day, date.Hour / 6 * 6, 0, 0);
                if (dictionary.ContainsKey(sixHourChunk))
                {
                    dictionary[sixHourChunk].Add(message);
                }
                else
                {
                    dictionary.Add(sixHourChunk, new List<MatrixMessage> { message });
                }
            }
            return dictionary.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
        }

        private async Task<List<MatrixMessage>> GetMessages(string accessToken, string roomId, int amount)
        {
            HttpResponseMessage response = await client.GetAsync($"{matrixSettings.HOMESERVER}/r0/rooms/{roomId}/messages?dir=b&limit={amount}&access_token={accessToken}");
            string responseBody = await response.Content.ReadAsStringAsync();
            dynamic json = JsonConvert.DeserializeObject(responseBody);
            JArray messages = json.chunk;
            var filteredMessages = FilteredMessages(messages);

            lastMessageToken = json.end;
            return filteredMessages;
        }

        private async Task<List<MatrixMessage>> GetMessagesFromToken(string accessToken, string roomId, string start, int amount)
        {
            HttpResponseMessage response = await client.GetAsync($"{matrixSettings.HOMESERVER}/r0/rooms/{roomId}/messages?from={start}&access_token={accessToken}&dir=b&limit={amount}");
            string responseBody = await response.Content.ReadAsStringAsync();
            dynamic json = JsonConvert.DeserializeObject(responseBody);
            JArray messages = json.chunk;
            return FilteredMessages(messages);
        }

        private string GetRoomId(string accessToken)
        {
            return matrixSettings.ROOM_ID_TO_ANALYZE;
        }

        private List<MatrixMessage> FilteredMessages(JArray messages)
        {
            List<MatrixMessage> filteredMessages = new List<MatrixMessage>();
            foreach (var message in messages)
            {
                if (message["type"].ToString() == "m.room.message")
                {
                    filteredMessages.Add(new MatrixMessage
                    {
                        Sender = ConvertSender(message["sender"].ToString()),
                        TimeStamp = DateTimeinWATFromEpoch((long)message["origin_server_ts"]),
                        Content = RemovePattern(message["content"]?["body"]?.ToString())
                    });
                }
            }
            return filteredMessages;
        }


        private string RemovePattern(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return "";
            }
            var pattern = @"@\w+_\d+:chat\.\w+\.\w+";
            var regex = new Regex(pattern);
            var result = regex.Replace(text, "");
            return result;
        }

        private string DateTimeinWATFromEpoch(long epoch)
        {
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddMilliseconds(epoch);
            TimeZoneInfo wAT = TimeZoneInfo.FindSystemTimeZoneById("W. Central Africa Standard Time");
            DateTime wATDateTime = TimeZoneInfo.ConvertTimeFromUtc(dateTime, wAT);
            return wATDateTime.ToString();
        }

        private string ConvertSender(string data)
        {
            var startIndex = data.IndexOf("_") + 1;
            var endIndex = data.IndexOf(":");
            var result = data.Substring(startIndex, endIndex - startIndex);
            return "+" + result;
        }
    }
}