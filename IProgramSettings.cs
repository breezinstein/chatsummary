namespace Breeze.ChatSummary
{
    public interface IProgramSettings
    {
        string ACCESS_TOKEN { get; set; }
        string HOMESERVER { get; set; }
        string PASSWORD { get; set; }
        string ROOM_ID_TO_ANALYZE { get; set; }
        string ROOM_ID_TO_POST { get; set; }
        string USERNAME { get; set; }
        string AZURE_API_KEY { get; set; }
        string AZURE_API_ENDPOINT { get; set; }
    }
}