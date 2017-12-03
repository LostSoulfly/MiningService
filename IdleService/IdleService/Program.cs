using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdleService
{
    class Program
    {
        static void Main(string[] args)
        {
            //Load the configuration from a file, in the current directory
            Config.LoadConfigFromFile(Utilities.ApplicationPath() + "MinerService.json");



            return;

            //Start the TopShelf library, and begin starting the actual Service
            ConfigureService.Configure();
        }
    }
}
