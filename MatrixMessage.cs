namespace Breeze.ChatSummary
{
        public class MatrixMessage
        {
            public string Sender { get; set; }
            public string TimeStamp { get; set; }
            public string Content { get; set; }

            public string value => $"{Sender} - {TimeStamp} : {Content}";
        }
}