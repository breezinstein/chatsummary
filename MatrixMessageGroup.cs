using System.Text;

namespace Breeze.ChatSummary
{
    public class MatrixMessageGroup
    {
        public DateTime earliestMessageTimeStamp;
        public DateTime latestMessageTimeStamp;

        List<MatrixMessage> messages = new List<MatrixMessage>();
        public MatrixMessageGroup() { }

        public void AddMessages(List<MatrixMessage> _messages)
        {
            foreach (MatrixMessage m in messages)
            {
                AddMessage(m);
            }
        }

        public void AddMessage(MatrixMessage message)
        {
            messages.Add(message);

            if (earliestMessageTimeStamp == DateTime.MinValue || message.TimeStamp < earliestMessageTimeStamp)
            {
                earliestMessageTimeStamp = message.TimeStamp;
            }

            if (latestMessageTimeStamp == DateTime.MinValue || message.TimeStamp > latestMessageTimeStamp)
            {
                latestMessageTimeStamp = message.TimeStamp;
            }
        }

        public string duration => $"{earliestMessageTimeStamp.ToString("T")} - {latestMessageTimeStamp.ToString("T")}";

        public string value
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.Append($"{duration}\n");
                foreach (var message in messages)
                {
                    sb.Append($"{message.value}\n");
                }
                return sb.ToString();
            }
        }

        public int Count => messages.Count;

        public static MatrixMessageGroup operator +(MatrixMessageGroup a, MatrixMessageGroup b)
        {
            MatrixMessageGroup result = new MatrixMessageGroup();
            result.messages.AddRange(a.messages);
            result.messages.AddRange(b.messages);

            //update the earliest and latest message timestamps
            if (a.earliestMessageTimeStamp < b.earliestMessageTimeStamp)
            {
                result.earliestMessageTimeStamp = a.earliestMessageTimeStamp;
            }
            else
            {
                result.earliestMessageTimeStamp = b.earliestMessageTimeStamp;
            }

            return result;
        }

        public void UpdateDuration()
        {
            earliestMessageTimeStamp = DateTime.MinValue;
            latestMessageTimeStamp = DateTime.MinValue;

            foreach (var message in messages)
            {
                if (earliestMessageTimeStamp == DateTime.MinValue || message.TimeStamp < earliestMessageTimeStamp)
                {
                    earliestMessageTimeStamp = message.TimeStamp;
                }

                if (latestMessageTimeStamp == DateTime.MinValue || message.TimeStamp > latestMessageTimeStamp)
                {
                    latestMessageTimeStamp = message.TimeStamp;
                }
            }
        }
    }

}