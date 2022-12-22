using ChatThreadTest.Networking;

namespace ChatThreadTest
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            Server server = new Server();
            server.Start();
        }
    }
}