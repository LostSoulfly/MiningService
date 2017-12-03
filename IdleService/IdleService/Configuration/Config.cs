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
        public static bool useBuiltInSessionEXE = false;

        //Settings class instance for de/serialization
        public static Settings settings;

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

            } catch
            {

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

            //return our verified settingsJson object
            return settingsJson;
        }

    }
}
