using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdleService
{
    class Settings
    {
        //Settings loaded from config file
        public bool enableDebug { get; private set; }
        public static bool enableLogging { get; private set; }
        //public static string logFilePath { get; private set; } Not sure how I want to do this yet.

        public bool stealthMode { get; private set; }
        public bool preventSleep { get; private set; }
        public bool monitorCpuTemp { get; private set; }
        public int maxCpuTemp { get; private set; }
        public int cpuUsageThresholdWhileNotIdle { get; private set; }
        public bool monitorGpuTemp { get; private set; }
        public int maxGpuTemp { get; private set; }
        public bool mineIfBatteryNotFull { get; private set; }
        public bool verifyNetworkConnectivity { get; private set; }
        public string urlToCheckForNetwork { get; private set; }
        public int minutesUntilIdle { get; private set; }
        public int resumePausedMiningAfterMinutes { get; private set; }

        public void SetupDefaultConfig()
        {
            stealthMode = false;
            preventSleep = true;
            monitorCpuTemp = true;
            monitorGpuTemp = true;
            maxCpuTemp = 60;
            maxGpuTemp = 75;
            cpuUsageThresholdWhileNotIdle = 90;
            mineIfBatteryNotFull = false;
            verifyNetworkConnectivity = false;
            urlToCheckForNetwork = "http://google.com";
            minutesUntilIdle = 30;
            resumePausedMiningAfterMinutes = 120;
        }
    }
}
