using Newtonsoft.Json;
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
        [JsonProperty]
        public bool enableDebug { get; private set; }
        [JsonProperty]
        public static bool enableLogging { get; private set; }
        //public static string logFilePath { get; private set; } Not sure how I want to do this yet.

        [JsonProperty]
        public bool stealthMode { get; private set; }
        [JsonProperty]
        public bool preventSleep { get; private set; }
        [JsonProperty]
        public bool monitorCpuTemp { get; private set; }
        [JsonProperty]
        public int maxCpuTemp { get; private set; }
        [JsonProperty]
        public int cpuUsageThresholdWhileNotIdle { get; private set; }
        [JsonProperty]
        public bool monitorGpuTemp { get; private set; }
        [JsonProperty]
        public int maxGpuTemp { get; private set; }
        [JsonProperty]
        public bool mineIfBatteryNotFull { get; private set; }
        [JsonProperty]
        public bool verifyNetworkConnectivity { get; private set; }
        [JsonProperty]
        public string urlToCheckForNetwork { get; private set; }
        [JsonProperty]
        public int minutesUntilIdle { get; private set; }
        [JsonProperty]
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
