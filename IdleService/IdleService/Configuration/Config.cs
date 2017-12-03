using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdleService
{
    static class Config
    {
        public static bool useBuiltInSessionEXE = false;

        //Settings loaded from config file
        public static bool enableDebug { get; private set; }
        public static bool enableLogging { get; private set; }
        //public static string logFilePath { get; private set; } Not sure how I want to do this yet.

        public static bool stealthMode { get; private set; }
        public static bool preventSleep { get; private set; }
        public static bool monitorCpuTemp { get; private set; }
        public static int maxCpuTemp { get; private set; }
        public static bool monitorGpuTemp { get; private set; }
        public static bool cpuUsageThresholdWhileNotIdle { get; private set; }
        public static int maxGpuTemp { get; private set; }
        public static bool mineIfBatteryNotFull { get; private set; }
        public static bool verifyNetworkConnectivity { get; private set; }
        public static string urlToCheckForNetwork { get; private set; }
        public static int minutesUntilIdle { get; private set; }
        public static int resumePausedMiningAfterMinutes { get; private set; }

        internal static List<string> cpuMiners { get; private set; }
        internal static List<string> gpuMiners { get; private set; }
        internal static string idleMonFileName { get; private set; }

        //Global variables used in different classes
        internal static bool isUserIdle { get; set; }
        internal static bool isUserLoggedIn { get; set; }
        internal static bool isPipeConnected { get; set; }
        internal static bool isCurrentlyMining { get; set; }
        internal static bool isIdleMining { get; set; }
        internal static bool configInitialized { get; private set; }
        internal static bool serviceInitialized { get; set; }
        internal static bool isMiningPaused { get; set; }
        internal static int currentSessionId { get; set; }

        internal static int sessionLaunchAttempts { get; set; }
        internal static int minerLaunchAttempts { get; set; }

        //Hashrate monitoring
        /*
         * apiUptimeFailureAttempts
         * apiHashrateFailureAttempts
         * api
         */

        //global sync objects for locking
        internal static readonly object startLock = new object();
        internal static readonly object timeLock = new object();

        public static void LoadConfigFromFile (string jsonFilePath)
        {
            //deserialize the json file into this class' objects

            //if load was successful
            configInitialized = true;
        }

        public static void WriteConfigToFile(string jsonFilePath)
        {
            //serialize the objects in this class, then write them to a file
        }

    }
}
