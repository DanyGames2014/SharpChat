using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Configuration;

namespace ChatThreadTest
{
    internal class Server
    {
        // Thread Management
        public ThreadManager threadManager;

        // Chat
        public ChatManager chatManager;

        // TCP Networking
        private TcpListener tcpListener;
        private int tcpPort;

        // Diagnostics & Monitoring
        public Logger logger;

        public Server()
        {
            // Initialize Logger
            logger = new Logger();
            logger.WriteInfo("Initializing Server");

            // Thread Manager
            Thread.CurrentThread.Name = "Main Thread";
            logger.WriteDebug("Initializing Thread Manager");
            threadManager = new ThreadManager(this);
            threadManager.CreateThreads();
            threadManager.StartThreads();

            // TCP Networking
            // Try reading the port from App.config
            try
            {
                logger.WriteDebug("Reading the port from App.config");

                tcpPort = Convert.ToInt32(ConfigurationManager.AppSettings.Get("ServerPort"));

                if (tcpPort > 65535 || tcpPort < 1)
                {
                    logger.WriteFatal("Port is outside of the TCP Port Range!");
                    Environment.Exit(1);
                }
            }
            catch (Exception e)
            {
                logger.WriteFatal("Invalid Value ServerPort in App.config!", e);
                Environment.Exit(1);
            }

            // TCP Listener
            logger.WriteDebug("Initializing TCP Listener for port " + tcpPort);
            tcpListener = new TcpListener(IPAddress.Loopback, tcpPort);

            // Chat Manager
            chatManager = new();

            // Init Complete
            logger.WriteInfo("Server Initialized Succesfully");
        }

        public void Start()
        {
            logger.WriteInfo("Starting Server on port " + tcpPort);
            try
            {
                tcpListener.Start();
            }
            catch (Exception e)
            {
                logger.WriteError("Failed to start TCP Listener! Check if the port isn't occupied by another application. Exiting!", e);
                Environment.Exit(1);
            }

            logger.WriteInfo("Server Started");
            logger.WriteDebug("Main Server Thread running on Thread " + Thread.CurrentThread.Name + " with ID " + Thread.CurrentThread.ManagedThreadId);

            Thread temp = new(threadManager.runConsole);
            temp.Start();
            

            while (true)
            {
                TcpClient tcpClient = tcpListener.AcceptTcpClient();
                threadManager.assignThread(tcpClient);
            }
        }
    }
}
