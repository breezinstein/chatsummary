namespace Breeze.ChatSummary
{
    public class MatrixMessage
    {
        public string Sender { get; set; } = string.Empty;
        public DateTime TimeStamp { get; set; }
        public string Content { get; set; } = string.Empty;

        public string value => $"{Sender} - {TimeStamp.ToString()} : {Content}";

    }

}