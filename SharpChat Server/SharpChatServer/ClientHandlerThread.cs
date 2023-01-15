using System.Diagnostics;

namespace SharpChatServer
{
    /// <summary>
    /// Thread handling multiple clients
    /// </summary>
    internal class ClientHandlerThread
    {
        private List<ClientHandler> clientHandlers;
        private Stopwatch stopwatch;
        private ThreadManager threadManager;
        public ThreadStats threadStats;
        private long lastExecutionMilliseconds;
        private long waitTime;
        private int threadID;

        /// <summary>
        /// Constructors
        /// </summary>
        /// <param name="threadManager">Back Reference to the Thread Manager</param>
        /// <param name="threadStats">ThreadStats to write statistics to</param>
        public ClientHandlerThread(ThreadManager threadManager, ThreadStats threadStats)
        {
            clientHandlers = new List<ClientHandler>();
            stopwatch = new Stopwatch();
            this.threadManager = threadManager;
            this.threadStats = threadStats;
            threadID = threadStats.threadID;
        }

        /// <summary>
        /// Cleans client handlers that have been marked for cleanup
        /// </summary>
        public void Cleanup()
        {
            lock (clientHandlers)
            {
                for (int i = 0; i < clientHandlers.Count; i++)
                {
                    if (clientHandlers[i].cleanable)
                    {
                        clientHandlers.RemoveAt(i);
                        i--;
                    }
                }
            }
        }

        /// <summary>
        /// Starts the client handling
        /// </summary>
        public void Start()
        {
            while (true)
            {
                // Run the Handlers
                stopwatch.Start();
                foreach (var item in clientHandlers)
                {
                    item.Run();
                }
                stopwatch.Stop();

                // Reset the Stopwatch and Start measuring the Thread Management Time
                lastExecutionMilliseconds = stopwatch.ElapsedMilliseconds;
                stopwatch.Reset();
                stopwatch.Start();

                // Cleanup
                Cleanup();

                // Calculate Wait Time from the last execution time
                waitTime = Math.Clamp(threadManager.targetMillisecondsMinimum - lastExecutionMilliseconds, 0, threadManager.targetMillisecondsMinimum);

                // Write Thread Stats
                lock (threadStats)
                {
                    threadStats.numberOfHandlers = clientHandlers.Count;
                    threadStats.executionTime = lastExecutionMilliseconds;
                    threadStats.lastExecutionTimes.RemoveAt(0);
                    threadStats.lastExecutionTimes.Add(lastExecutionMilliseconds);
                    threadStats.averageExecutionTime = threadStats.lastExecutionTimes.Sum() / 10;
                    threadStats.waitTime = waitTime;
                    stopwatch.Stop();
                    threadStats.threadManagementTime = stopwatch.ElapsedMilliseconds;
                    stopwatch.Reset();

                    // Calculate Viability
                    if (threadStats.numberOfHandlers == 0 || threadStats.averageExecutionTime == 0f || threadStats.executionTime == 0f)
                    {
                        threadStats.viability = 1f;
                    }
                    else
                    {
                        threadStats.viability =
                            // Average Execution time compared to target, The lower the value, the higher the load compared to target
                            // Weight = 0.7
                            Math.Clamp(threadManager.targetMillisecondsMinimum / threadStats.averageExecutionTime, 0, 1) * 0.7f +

                            // Number of clients the thread is handling compared to the supposed maximum it should
                            // Weight = 0.2
                            Math.Clamp(threadManager.maximumClientsPerThread / threadStats.numberOfHandlers, 0, 1) * 0.2f +

                            // Last execution compared to average
                            // Weight = 0.1
                            Math.Clamp(threadStats.averageExecutionTime / threadStats.executionTime, 0, 1) * 0.1f
                        ;
                    }
                }

                // Get new Handlers
                lock (threadManager.waitingThreads[threadID])
                {
                    foreach (var item in threadManager.waitingThreads[threadID])
                    {
                        clientHandlers.Add(item);
                    }
                    threadManager.waitingThreads[threadID].Clear();
                }

                // Sleep for waitTime
                Thread.Sleep((int)waitTime);
            }
        }
    }
}
