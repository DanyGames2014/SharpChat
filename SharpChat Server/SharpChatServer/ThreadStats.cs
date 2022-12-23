namespace ChatThreadTest
{
    internal class ThreadStats
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
            this.numberOfHandlers = 0;
            this.waitTime = 0;
            this.executionTime = 0;
            this.threadManagementTime = 0;
            this.lastExecutionTimes = new() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            this.threadID = threadID;
        }

        public override string? ToString()
        {
            return 
                "Thread : " + this.threadName.ToString().PadLeft(2, ' ') +
                " | Handlers : " + this.numberOfHandlers.ToString().PadLeft(3, ' ') +
                " | Execution Time Last/Avg: " + this.executionTime.ToString().PadLeft(4, ' ') + "ms /" + this.averageExecutionTime.ToString().PadLeft(4, ' ') + "ms" +
                " | Wait Time : " + this.waitTime.ToString().PadLeft(4, ' ') + "ms" +
                " | Overhead : " + this.threadManagementTime + "ms" +
                " | Via : " + this.viability
                
                ;
        }
    }
}
