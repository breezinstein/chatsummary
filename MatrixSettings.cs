using Newtonsoft.Json;

namespace Breeze.ChatSummary
{
    public class ProgramSettings
    {
        // Use getters and setters to access properties instead of directly accessing fields.
        private MatrixConfig _matrixConfig;
        public MatrixConfig MatrixConfig { get => _matrixConfig; set => _matrixConfig = value; }

        private OLLAMAAPI _ollamaApi;
        public OLLAMAAPI OLLAMAAPI { get => _ollamaApi; set => _ollamaApi = value; }

        private AzureAPI _azureApi;
        public AzureAPI AzureAPI { get => _azureApi; set => _azureApi = value; }

        public ProgramSettings LoadFromJSON(string filePath)
        {
            var configFile = File.ReadAllText(filePath);
            var config = JsonConvert.DeserializeObject<ProgramSettings>(configFile);
            if (config == null)
            {
                throw new Exception("Could not deserialize config file");
            }
            _matrixConfig = config.MatrixConfig;
            _ollamaApi = config.OLLAMAAPI;
            _azureApi = config.AzureAPI;
            return this;
        }

        public ProgramSettings Validate()
        {
            if(_matrixConfig.ACCESS_TOKEN == null || _matrixConfig.HOMESERVER == null)
            {
                Console.WriteLine("No Access Token or Homeserver configured");
                return null;
            }
            if(_matrixConfig.ROOMS == null)
            {
                Console.WriteLine("No Rooms Configured");
                return null;
            }
            if(OLLAMAAPI.API_ENDPOINT_URL == null && (AzureAPI.API_KEY == null || AzureAPI.API_ENDPOINT_URL == null))
            {
                Console.WriteLine("No valid analyzer configured, please configure Azure API or Ollama in settings.json");
                return null;
            }
            return this;
        }
    }

    public struct Room
    {
        public string ID { get; set; }
        public string Name { get; set; }
    }

    public struct AzureAPI
    {
        public string API_KEY { get; set; }
        public string API_ENDPOINT_URL { get; set; }
    }

    public struct OLLAMAAPI
    {
        public string API_ENDPOINT_URL { get; set; }
    }

    public struct MatrixConfig
    {
        public string ACCESS_TOKEN { get; set; }
        public string HOMESERVER { get; set; }
        public string PASSWORD { get; set; }
        public string USERNAME { get; set; }
        public List<Room> ROOMS { get; set; }
    }
}