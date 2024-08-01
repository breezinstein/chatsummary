using Newtonsoft.Json;

namespace Breeze.ChatSummary
{
    public class ProgramSettings
    {
        // Use getters and setters to access properties instead of directly accessing fields.
        private MatrixConfig _matrixConfig;
        public MatrixConfig MatrixConfig { get => _matrixConfig; set => _matrixConfig = value; }

        private OLLAMAAPI _ollamaApi;
        public OLLAMAAPI OllamaApi { get => _ollamaApi; set => _ollamaApi = value; }

        private AzureAPI _azureApi;
        public AzureAPI AzureAPI { get => _azureApi; set => _azureApi = value; }

        private CLAUDEAPI _claudeApi;
        private APIType _api;

        public CLAUDEAPI ClaudeApi { get => _claudeApi; set => _claudeApi = value; }

        public APIType API { get => _api; set => _api = value; }


        public ProgramSettings LoadFromJSON(string filePath)
        {
            var configFile = File.ReadAllText(filePath);
            var config = JsonConvert.DeserializeObject<ProgramSettings>(configFile);
            if (config == null)
            {
                throw new Exception("Could not deserialize config file");
            }
            _matrixConfig = config.MatrixConfig;
            _ollamaApi = config.OllamaApi;
            _azureApi = config.AzureAPI;
            _claudeApi = config.ClaudeApi;
            _api = config.API;
            return this;
        }

        public ProgramSettings? Validate()
        {
            if (_matrixConfig.ACCESS_TOKEN == null || _matrixConfig.HOMESERVER == null)
            {
                Console.WriteLine("No Access Token or Homeserver configured");
                return null;
            }
            if (_matrixConfig.DESTINATION_ROOM_ID == null)
            {
                Console.WriteLine("No Destination Room Configured");
                return null;
            }
            if (_matrixConfig.SOURCE_ROOM_ID == null)
            {
                Console.WriteLine("No Source Room Configured");
                return null;
            }
            if (OllamaApi.API_ENDPOINT_URL == null && (AzureAPI.API_KEY == null || AzureAPI.API_ENDPOINT_URL == null) && ClaudeApi.API_KEY == null)
            {
                Console.WriteLine("No valid analyzer configured, please configure Azure API or Ollama in settings.json");
                return null;
            }
            return this;
        }
    }

    public enum APIType { AZURE, OLLAMA, CLAUDE }

    public struct AzureAPI
    {
        public string API_KEY { get; set; }
        public string API_ENDPOINT_URL { get; set; }
    }

    public struct OLLAMAAPI
    {
        public string API_ENDPOINT_URL { get; set; }
        public string MODEL { get; set; }
    }

    public struct MatrixConfig
    {
        public string ACCESS_TOKEN { get; set; }
        public string HOMESERVER { get; set; }
        public string PASSWORD { get; set; }
        public string USERNAME { get; set; }
        public string SOURCE_ROOM_ID { get; set; }
        public string DESTINATION_ROOM_ID { get; set; }
    }

    public struct CLAUDEAPI
    {
        public string API_KEY { get; set; }
    }
}
