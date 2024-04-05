using Newtonsoft.Json;
using System.Text;

namespace Breeze.ChatSummary
{
    /// <summary>
    /// This class is responsible for posting messages to a Matrix room.
    /// </summary>
    public class MatrixMessagePoster
    {
        private IProgramSettings matrixSettings { get; set; }
        public MatrixMessagePoster()
        {
            matrixSettings = new MatrixSettings();
        }

        public async Task PostMessage(string message)
        {
            string? accessToken = await GetAccessToken();
            string roomId = GetRoomId(accessToken);

            await SendMessageToMatrix(accessToken, roomId, "m.room.message", message);
        }

        private async Task<string> SendMessageToMatrix(string accessToken, string roomId, string eventType, string message)
        {
            using (var client = new HttpClient())
            {
                string txnId = Guid.NewGuid().ToString();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
                client.DefaultRequestHeaders.Add("accept", "application/json");

                var messageContent = new StringContent(JsonConvert.SerializeObject(new { msgtype = "m.text", body = message }), Encoding.UTF8, "application/json");

                var response = await client.PutAsync($"{matrixSettings.HOMESERVER}/v3/rooms/{roomId}/send/{eventType}/{txnId}", messageContent);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Message sent successfully!");
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    Console.WriteLine($"Error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                    return $"Error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}";
                }
            }
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
        private string GetRoomId(string accessToken)
        {
            return matrixSettings.ROOM_ID_TO_POST;
        }
    }
}
