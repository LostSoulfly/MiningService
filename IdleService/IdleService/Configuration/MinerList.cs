using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdleService
{
    public class MinerList
    {

        public MinerList(string executable, string arguments)
        {
            this.executable = executable;
            this.arguments = arguments;
        }

        public string executable { get; set; }
        public string arguments { get; set; }
        internal int launchAttempts { get; set; }
        internal bool shouldMinerBeRunning { get; set; }
    }
}
