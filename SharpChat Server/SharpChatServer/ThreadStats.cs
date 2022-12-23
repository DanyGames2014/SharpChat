namespace SharpChatServer
{
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
