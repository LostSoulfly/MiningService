using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;

namespace IdleService
{
    static class Config
    {
        //Settings class instance for de/serialization
        public static Settings settings;
        public static string idleMonExecutable = "IdleMon.exe";

        //Global variables used in different classes
        internal static bool isUserIdle { get; set; }
        internal static bool isUserLoggedIn { get; set; }
        internal static bool isPipeConnected { get; set; }
        internal static bool isCurrentlyMining { get; set; }
        internal static bool isIdleMining { get; set; }
        internal static bool configInitialized { get; private set; }
        internal static bool serviceInitialized { get; set; }
        internal static bool isMiningPaused { get; set; }
        internal static bool doesBatteryExist { get; set; }
        internal static int  currentSessionId { get; set; }

        internal static int sessionLaunchAttempts { get; set; }

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
            //Create a temporary Settings object
            Settings settingsJson = new Settings();

            //If the passed file path does not exist, load defaults and save them to file
            if (!File.Exists(jsonFilePath))
            {
                LoadDefaultConfig();
                return;
            }

            try
            {
                //Try to read and deserialize the passed file path into the temporary Settings object
                settingsJson = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(jsonFilePath));

                //at this point, we can replace the original global settings object with our temp one
                settings = settingsJson;

            } catch (Exception ex)
            {
                Utilities.Log("LoadConfigFromFile: " + ex.Message, force: true);
            }

            //if load was successful
            configInitialized = true;
        }
        
        public static void WriteConfigToFile(string jsonFilePath)
        {
            try
            {
                File.WriteAllText(jsonFilePath, JsonConvert.SerializeObject(settings, Formatting.Indented));
            } catch (Exception ex)
            {
                Utilities.Log("WriteConfigToFile: " + ex.Message);
            }
        }

        private static void LoadDefaultConfig()
        {
            //re-initialize our global settings object
            settings = new Settings();

            //Call SetupDefaultConfig(), which sets the pre-programmed defaults into the global settings object
            settings.SetupDefaultConfig();

            //config is now initialized with defaults
            configInitialized = true;

            //Write the defaults to file
            WriteConfigToFile("MinerService.json");

        }

        private static Settings VerifySettings(Settings settingsJson)
        {
            //verify we don't have any negative numbers, numbers that are lower or higher than safe values, or empty strings (like the url, but only need to check if verifyNetwork is true).
            if (settings.maxCpuTemp > 90 || settings.maxCpuTemp < 0)
                settings.maxCpuTemp = 70;

            if (settings.maxGpuTemp > 100 || settings.maxGpuTemp < 0)
                settings.maxGpuTemp = 70;

            if (settings.minutesUntilIdle > 3600 || settings.minutesUntilIdle < 3)
                settings.minutesUntilIdle = 10;

            if (settings.cpuUsageThresholdWhileNotIdle > 100 || settings.cpuUsageThresholdWhileNotIdle < 0)
                settings.cpuUsageThresholdWhileNotIdle = 80;

            if (settings.resumePausedMiningAfterMinutes > 3600 || settings.resumePausedMiningAfterMinutes < 0)
                settings.resumePausedMiningAfterMinutes = 0; //0 means don't resume!

            if (settings.urlToCheckForNetwork.Length <= 0 && settings.verifyNetworkConnectivity)
                settings.urlToCheckForNetwork = "http://beta.speedtest.net/";

            if (!VerifyCpuMiners())
                Utilities.Log("There is a problem with your CPU Miner configuration! Make sure there are no empty executables!");

            if (!VerifyGpuMiners())
                Utilities.Log("There is a problem with your GPU Miner configuration! Make sure there are no empty executables!");

            //return our verified settingsJson object
            return settingsJson;
        }
        
        private static bool VerifyCpuMiners()
        {
            foreach (var miner in Config.settings.cpuMiners)
            {
                if (miner.executable.Length == 0)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool VerifyGpuMiners()
        {
            foreach (var miner in Config.settings.gpuMiners)
            {
                if (miner.executable.Length == 0)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
