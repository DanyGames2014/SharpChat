using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatThreadTest
{
    public class ChatManager
    {
        List<ChatMessage> messages;
        public List<List<ChatMessage>> subscribedThreads;

        public ChatManager()
        {
            messages = new List<ChatMessage>();
            subscribedThreads = new List<List<ChatMessage>>();
        }

        /// <summary>
        /// Returns a list containing all the ChatMessage objects that have timestamp equal or higher than the specified one
        /// </summary>
        /// <param name="timestamp">Starting timestamp</param>
        public List<ChatMessage> GetFromTimestamp(long timestamp)
        {
            lock (messages)
            {
                int count = messages.Count;
                if (count > 0)
                {
                    int index = messages.FindIndex(x => x.unixTimestamp == timestamp);

                    if (index < 0)
                    {
                        index = 0;
                    }

                    List<ChatMessage> toreturn = new List<ChatMessage>();

                    for (int i = index; i < messages.Count; i++)
                    {
                        toreturn.Add(messages[i]);
                    }

                    return toreturn;
                }
            }

            return new List<ChatMessage> { };
        }

        /// <summary>
        /// Adds a message with specified timestamp
        /// </summary>
        /// <param name="message">Message to add</param>
        /// <param name="timestamp">Unix Time Milliseconds Timestamp</param>
        /// <returns>Return true when the message is added</returns>
        public bool Add(string senderName, string message, long timestamp)
        {
            lock (messages)
            {
                messages.Add(new ChatMessage(senderName, message, timestamp));

                lock (subscribedThreads)
                {
                    foreach (var item in subscribedThreads)
                    {
                        lock (item)
                        {
                            item.Add(new ChatMessage(senderName, message, timestamp));
                        }
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// Adds a message with the current timestamp
        /// </summary>
        /// <param name="message">Message to add</param>
        /// <returns>Return true when the message is added</returns>
        public bool Add(string senderName, string message)
        {
            Add(senderName, message, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            return true;
        }

        /// <summary>
        /// Adds a message with the specified timestamp but only if the timestamp is within specified tolerance 
        /// </summary>
        /// <param name="message">Message to add</param>
        /// <param name="timestamp">Unix Time Milliseconds Timestamp</param>
        /// <param name="tolerance">Tolerance withing milliseconds the message has to be in compared to the server time</param>
        /// <returns></returns>
        public bool Add(string senderName, string message, long timestamp, long tolerance)
        {
            if (timestamp > (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + tolerance) || timestamp < (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - tolerance))
            {
                return false;
            }
            else
            {
                Add(senderName, message, timestamp);
            }
            return false;
        }
    }
}
