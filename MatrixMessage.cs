namespace Breeze.ChatSummary
{
    public class MatrixMessage
    {
        public string Sender { get; set; }
        public DateTime TimeStamp { get; set; }
        public string Content { get; set; }

        public string value => $"{Sender} - {TimeStamp.ToString()} : {Content}";
    }

}