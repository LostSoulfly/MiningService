using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdleService
{
    public class MinerList
    {

        public MinerList(string executable, string idleArguments, string activeArguments)
        {
            this.executable = executable;
            this.idleArguments = idleArguments;
            this.activeArguments = activeArguments;
            shouldMinerBeRunning = false;
            isMiningIdleSpeed = false;
            minerDisabled = false;
            launchAttempts = 0;
        }

        public string executable { get; set; }
        public string idleArguments { get; set; }
        public string activeArguments { get; set; }
        internal int launchAttempts { get; set; }
        internal bool minerDisabled { get; set; }
        internal bool shouldMinerBeRunning { get; set; }
        internal bool isMiningIdleSpeed { get; set; }
    }
}
