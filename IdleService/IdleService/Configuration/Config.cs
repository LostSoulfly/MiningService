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
        internal static bool configInitialized { get; private set; }
        internal static bool serviceInitialized { get; set; }
        internal static bool isMiningPaused { get; set; }
        internal static bool doesBatteryExist { get; set; }
        internal static int  currentSessionId { get; set; }
        internal static int  skipTimerCycles { get; set; }

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
                
                //Pass our new settings to VerifySettings, check it for safe numbers etc
                VerifySettings(ref settingsJson);
                
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
            }
            catch (Exception ex)
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

        private static void VerifySettings(ref Settings tempSettings)
        {
            //verify we don't have any negative numbers, numbers that are lower or higher than safe values, or empty strings (like the url, but only need to check if verifyNetwork is true).
            if (tempSettings.maxCpuTemp > 90 || tempSettings.maxCpuTemp < 0)
                tempSettings.maxCpuTemp = 70;

            if (tempSettings.maxGpuTemp > 100 || tempSettings.maxGpuTemp < 0)
                tempSettings.maxGpuTemp = 70;

            if (tempSettings.minutesUntilIdle > 3600 || tempSettings.minutesUntilIdle < 3)
                tempSettings.minutesUntilIdle = 10;

            if (tempSettings.cpuUsageThresholdWhileNotIdle > 100 || tempSettings.cpuUsageThresholdWhileNotIdle < 0)
                tempSettings.cpuUsageThresholdWhileNotIdle = 80;

            if (tempSettings.resumePausedMiningAfterMinutes > 3600 || tempSettings.resumePausedMiningAfterMinutes < 0)
                tempSettings.resumePausedMiningAfterMinutes = 0; //0 means don't resume!

            if (tempSettings.urlToCheckForNetwork.Length <= 0 && tempSettings.verifyNetworkConnectivity)
                tempSettings.urlToCheckForNetwork = "http://beta.speedtest.net/";

            VerifyMiners(tempSettings.cpuMiners);
            VerifyMiners(tempSettings.gpuMiners);
            
            if (!File.Exists(idleMonExecutable))
            {
                if (File.Exists(Utilities.ApplicationPath() + idleMonExecutable))
                {
                    idleMonExecutable = Utilities.ApplicationPath() + idleMonExecutable;
                } else
                { 
                    Utilities.Log("Unable to locate " + idleMonExecutable + ". Stopping IdleService.", force: true);
                    System.Environment.Exit(200);
                }
            }

            //return our verified settingsJson object
            //return tempSettings;
        }

        private static bool VerifyMiners(List<MinerList> minerList)
        {
            foreach (var miner in minerList)
            {
                if (miner.executable.Length == 0)
                {

                    Utilities.Log("You have an empty Miner, this is not allowed.", force: true);
                    System.Environment.Exit(100);
                    return false;
                }

                if (!File.Exists(miner.executable))
                {
                    if (File.Exists(Utilities.ApplicationPath() + miner.executable))
                    {
                        miner.executable = Utilities.ApplicationPath() + miner.executable;
                    } else
                    { 
                        Utilities.Log("Unable to locate miner Exe: " + miner.executable, force: true);
                        System.Environment.Exit(100);
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
