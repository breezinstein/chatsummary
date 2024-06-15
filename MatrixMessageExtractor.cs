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
        Dictionary<DateTime, MatrixMessageGroup> dictionary = new Dictionary<DateTime, MatrixMessageGroup>();
        private MatrixConfig matrixConfig { get; set; }
        private Room roomToAnalyze{get;set;}
        public MatrixMessageExtractor(MatrixConfig _matrixConfig, Room _roomToAnalyze)
        {
            matrixConfig = _matrixConfig;
            roomToAnalyze = _roomToAnalyze;
            lastMessageToken = string.Empty;
        }
        private async Task<string?> GetAccessToken()
        {
            if (matrixConfig.ACCESS_TOKEN != "")
            {
                return matrixConfig.ACCESS_TOKEN;
            }
            else
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync($"{matrixConfig.HOMESERVER}/r0/login");
                    string responseBody = await response.Content.ReadAsStringAsync();
                    dynamic json = JsonConvert.DeserializeObject(responseBody);
                    string flowType = json.flows[0].type;

                    if (flowType == "m.login.password")
                    {
                        var payload = new
                        {
                            type = "m.login.password",
                            user = matrixConfig.USERNAME,
                            password = matrixConfig.PASSWORD
                        };

                        response = await client.PostAsync($"{matrixConfig.HOMESERVER}/login", new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json"));
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

        public async Task<Dictionary<DateTime, MatrixMessageGroup>> GetMessagesByHour()
        {
            string? accessToken = await GetAccessToken();

            string roomId = GetRoomId(accessToken);

            var messages = await GetMessages(accessToken, roomId, 1000);

            if (messages.Count < 1500)
            {
                var remainingMessages = await GetMessagesFromToken(accessToken, roomId, lastMessageToken, 1000);
                messages.AddRange(remainingMessages);
            }

            var groupedMessages = GroupMessagesByHour(messages);
            return groupedMessages;
        }

        private Dictionary<DateTime, MatrixMessageGroup> GroupMessagesByHour(List<MatrixMessage> messages)
        {
            foreach (var message in messages)
            {
                var date = message.TimeStamp;
                var hour = new DateTime(date.Year, date.Month, date.Day, date.Hour, 0, 0);
                if (dictionary.ContainsKey(hour))
                {
                    dictionary[hour].AddMessage(message);
                }
                else
                {
                    dictionary.Add(hour, new MatrixMessageGroup());
                    dictionary[hour].AddMessage(message);
                }
            }
            return dictionary.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
        }

        private async Task<List<MatrixMessage>> GetMessages(string accessToken, string roomId, int amount)
        {
            HttpResponseMessage response = await client.GetAsync($"{matrixConfig.HOMESERVER}/r0/rooms/{roomId}/messages?dir=b&limit={amount}&access_token={accessToken}");
            string responseBody = await response.Content.ReadAsStringAsync();
            dynamic json = JsonConvert.DeserializeObject(responseBody);
            JArray messages = json.chunk;
            var filteredMessages = FilteredMessages(messages);

            lastMessageToken = json.end;
            return filteredMessages;
        }

        private async Task<List<MatrixMessage>> GetMessagesFromToken(string accessToken, string roomId, string start, int amount)
        {
            HttpResponseMessage response = await client.GetAsync($"{matrixConfig.HOMESERVER}/r0/rooms/{roomId}/messages?from={start}&access_token={accessToken}&dir=b&limit={amount}");
            string responseBody = await response.Content.ReadAsStringAsync();
            dynamic json = JsonConvert.DeserializeObject(responseBody);
            JArray messages = json.chunk;
            return FilteredMessages(messages);
        }

        private string GetRoomId(string accessToken)
        {
            return roomToAnalyze.ID;
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
                        Sender = message["sender"].ToString(),
                        TimeStamp = DateTimeinWATFromEpoch((long)message["origin_server_ts"]),
                        Content = message["content"]?["body"]?.ToString()
                    });
                }
            }
            return filteredMessages;
        }

        private DateTime DateTimeinWATFromEpoch(long epoch)
        {
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddMilliseconds(epoch);
            TimeZoneInfo wAT = TimeZoneInfo.FindSystemTimeZoneById("W. Central Africa Standard Time");
            DateTime wATDateTime = TimeZoneInfo.ConvertTimeFromUtc(dateTime, wAT);
            return wATDateTime;
        }
    }
}