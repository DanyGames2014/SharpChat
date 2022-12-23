using System.Configuration;
using System.Net.Sockets;

namespace SharpChatServer
{
    public enum EnforceLevel
    {
        NONE = 0,
        LIGHT = 1,
        STRICT = 2,
        MAXIMUM = 3
    }

    public class ThreadManager
    {
        Server server;
        Logger logger;

        public Thread[] clientHandlerThreads;
        public ThreadStats[] threadStats;
        public List<ClientHandler>[] waitingThreads;

        public long targetMillisecondsMinimum;
        public long targetMillisecondsMaximum;

        public int maximumClients;
        public int maximumClientsPerThread;
        public EnforceLevel enforceLevel;

        /// <summary>
        /// Constructor
        /// </summary>
        public ThreadManager(Server server)
        {
            // Shorthands
            var cfg = ConfigurationManager.AppSettings;

            // References to Server Instance
            this.server = server;
            logger = server.logger;

            // Initialize Threads according to App.Config
            int threadCount = 1;

            // Read App.Config values
            try
            {
                logger.WriteDebug("Reading thread count from App.config");
                threadCount = Convert.ToInt32(cfg.Get("ThreadCount"));
            }
            catch (Exception e)
            {
                logger.WriteError("Invalid Value ThreadCount in App.Config, using system core count as a default", e);
                threadCount = Environment.ProcessorCount;
            }
            finally
            {
                if (threadCount == 0)
                {
                    threadCount = Environment.ProcessorCount;
                }
                logger.WriteInfo("Initializing " + threadCount + " threads");

                clientHandlerThreads = new Thread[threadCount];
                threadStats = new ThreadStats[threadCount];

                // Create Lists for waiting threads
                waitingThreads = new List<ClientHandler>[threadCount];
                for (int i = 0; i < waitingThreads.Length; i++)
                {
                    waitingThreads[i] = new List<ClientHandler>();
                }
            }

            // Reads the values for desired response times from App.config
            try
            {
                logger.WriteDebug("Reading target response times from App.config");
                targetMillisecondsMinimum = Convert.ToInt64(cfg.Get("MinimumTargetResponseTime"));
                targetMillisecondsMaximum = Convert.ToInt64(cfg.Get("MaximumTargetResponseTime"));
            }
            catch (Exception e)
            {
                logger.WriteError("Invalid Value MinimumTargetResponseTime or MaximumTargetResponseTime in App.config, using default values", e);
                targetMillisecondsMaximum = 5000;
                targetMillisecondsMinimum = 250;
            }
            finally
            {
                logger.WriteDebug("Minimum Response Time Target : " + targetMillisecondsMinimum + ", Maximum Response Time Target : " + targetMillisecondsMaximum);
            }

            // Reads the desired client count from App.config
            try
            {
                logger.WriteDebug("Reading target client count and enforcion level");
                maximumClients = Convert.ToInt32(cfg.Get("MaximumClientCount"));
                enforceLevel = Enum.Parse<EnforceLevel>(cfg.Get("MaximumClientCountEnforceLevel") + "");

            }
            catch (Exception e)
            {
                logger.WriteError("Invalid Value MaximumClientCount or MaximumClientCountEnforceLevel in App.config. Disabling client count limits.", e);
                maximumClients = 0;
                enforceLevel = 0;
            }
            finally
            {
                maximumClientsPerThread = maximumClients / threadCount;
                logger.WriteDebug("Maximum Client Count : " + maximumClients + ", Maximum clients per thread : " + maximumClientsPerThread + ", enforce level : " + enforceLevel.ToString());
            }

            logger.WriteDebug("Thread Manager Initialized");
        }

        /// <summary>
        /// Creates threads, how many is decided by the threadCount variable
        /// </summary>
        public void CreateThreads()
        {
            logger.WriteDebug("Initializing Client Handler Threads");
            for (int i = 0; i < clientHandlerThreads.Length; i++)
            {
                threadStats[i] = new ThreadStats(i + "", i);
                ClientHandlerThread clientHandlerThread = new(this, threadStats[i]);
                clientHandlerThreads[i] = new Thread(clientHandlerThread.Start);
            }
        }

        /// <summary>
        /// Starts all threads
        /// </summary>
        public void StartThreads()
        {
            logger.WriteDebug("Starting Client Handler Threads");
            foreach (var item in clientHandlerThreads)
            {
                item.Start();
            }
        }

        /// <summary>
        /// Assigns the ClientHandler to a thread with the least load on it
        /// </summary>
        public void assignThread(TcpClient client)
        {
            float highestViability = 0;
            int threadID = int.MaxValue;

            for (int i = 0; i < threadStats.Length; i++)
            {
                lock (threadStats[i])
                {
                    if (
                         // Thread is handling less clients than maximum
                         threadStats[i].numberOfHandlers < maximumClientsPerThread ||
                         // Enforcing is disabled
                         enforceLevel == EnforceLevel.NONE ||
                         // Enforce is LIGHT && Execution Time is lower than target && Average Execution time is lower than target
                         enforceLevel == EnforceLevel.LIGHT && threadStats[i].averageExecutionTime < targetMillisecondsMinimum && threadStats[i].averageExecutionTime < targetMillisecondsMinimum
                       )
                    {
                        if (
                            threadStats[i].viability > highestViability &&
                            (threadStats[i].averageExecutionTime < targetMillisecondsMaximum || threadStats[i].executionTime < targetMillisecondsMaximum)
                           )
                        {
                            highestViability = threadStats[i].viability;
                            threadID = i;
                        }

                    }
                }
            }

            if (threadID != int.MaxValue)
            {
                lock (waitingThreads[threadID])
                {
                    waitingThreads[threadID].Add(new ClientHandler(client, server));
                }
            }
            else
            {
                client.Close();
            }
        }

        /// <summary>
        /// Handle the Console Output
        /// </summary>
        public void runConsole()
        {
            while (true)
            {
                Console.Clear();
                foreach (var item in threadStats)
                {
                    lock (item)
                    {
                        Console.WriteLine(item.ToString());
                    }
                }

                Thread.Sleep(500);
            }
        }

    }
}
