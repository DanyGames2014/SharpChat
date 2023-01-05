namespace SharpChatClient
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if(args.Length == 0)
            {
                Console.WriteLine("NekoChat Client 0.1.0\n");
                Console.WriteLine("Usage : " + Environment.ProcessPath.Split(@"\").Last() + " [options]");
                Console.WriteLine("\nOptions : ");
                Console.WriteLine(" -a | -address <ip address>   Address of server to connect to.");
                Console.WriteLine(" -p | -port <port>            Specifies the port of the server, has no effect if -a isn't specified");
                Console.WriteLine(" -u | -username <username>    Username to connect with.");
                Console.WriteLine(" -s | -pass <password>        Password to login with");
            }

            Client client = new Client();


            try
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i][0].Equals('-'))
                    {
                        switch (args[i].Remove(0, 1))
                        {
                            case "a":
                            case "address":
                                //Console.WriteLine("ADDRESS : " + args[i + 1]);
                                client.serverAddress = args[i + 1];
                                i++;
                                break;

                            case "p":
                            case "port":
                                //Console.WriteLine("PORT : " + args[i + 1]);
                                client.serverPort = Convert.ToInt32(args[i + 1]);
                                i++;
                                break;

                            case "u":
                            case "username":
                                //Console.WriteLine("USERNAME : " + args[i + 1]);
                                client.username = args[i + 1];
                                i++;
                                break;

                            case "s":
                            case "pass":
                                //Console.WriteLine("PASSWORD : " + args[i + 1]);
                                client.password = args[i + 1];
                                i++;
                                break;
                        }
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }

            client.Run();

        }
    }
}