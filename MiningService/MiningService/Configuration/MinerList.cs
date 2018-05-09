namespace MiningService
{
    public class MinerList
    {
        internal bool isMiningIdleSpeed { get; set; }

        internal int launchAttempts { get; set; }

        internal bool shouldMinerBeRunning { get; set; }

        public string activeArguments { get; set; }

        public string executable { get; set; }

        public string idleArguments { get; set; }

        public bool minerDisabled { get; set; }

        public bool mineWhileNotIdle { get; set; }

        public MinerList(string executable, string idleArguments, string activeArguments, bool mineWhileNotIdle)
        {
            this.executable = executable;
            this.idleArguments = idleArguments;
            this.activeArguments = activeArguments;
            this.mineWhileNotIdle = mineWhileNotIdle;
            shouldMinerBeRunning = false;
            isMiningIdleSpeed = false;
            minerDisabled = false;
            launchAttempts = 0;
        }
    }
}