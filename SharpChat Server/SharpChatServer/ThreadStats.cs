namespace SharpChatServer
{
    /// <summary>
    /// Thread statistics, used for monitoring and calculating thread load
    /// </summary>
    public class ThreadStats
    {
        public long executionTime { get; set; }
        public int numberOfHandlers { get; set; }
        public long waitTime { get; set; }
        public string threadName { get; set; }
        public List<long> lastExecutionTimes { get; set; }
        public float averageExecutionTime { get; set; }
        public long threadManagementTime { get; set; }
        public float viability { get; set; }
        public int threadID { get; set; }

        /// <summary>
        /// Initiliazed the thread with a name and id
        /// </summary>
        /// <param name="threadName">name for the thread</param>
        /// <param name="threadID">id of the thread</param>
        public ThreadStats(string threadName, int threadID)
        {
            this.threadName = threadName;
            numberOfHandlers = 0;
            waitTime = 0;
            executionTime = 0;
            threadManagementTime = 0;
            lastExecutionTimes = new() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            this.threadID = threadID;
        }

        /// <summary>
        /// Human readable representaion of the statistics
        /// </summary>
        public override string? ToString()
        {
            return
                "Thread : " + threadName.ToString().PadLeft(2, ' ') +
                " | Handlers : " + numberOfHandlers.ToString().PadLeft(3, ' ') +
                " | Execution Time Last/Avg: " + executionTime.ToString().PadLeft(4, ' ') + "ms /" + averageExecutionTime.ToString().PadLeft(4, ' ') + "ms" +
                " | Wait Time : " + waitTime.ToString().PadLeft(4, ' ') + "ms" +
                " | Overhead : " + threadManagementTime + "ms" +
                " | Via : " + viability

                ;
        }
    }
}
