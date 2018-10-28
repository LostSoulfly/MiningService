﻿using Message;
using NamedPipeWrapper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MiningService
{
    internal static class Config
    {
        private static int cpuQueueLimit = 10;

        //global sync objects for locking
        internal static readonly object startLock = new object();

        internal static readonly object timeLock = new object();

        internal static NamedPipeClient<IdleMessage> client;
        internal static Queue<int> cpuUsageQueue = new Queue<int>();

        internal static bool hasClientAuthenticated;
        public static string idleMonExecutable = "IdleMon.exe";

        //Settings class instance for de/serialization
        public static Settings settings;

        internal static bool computerIsLocked { get; set; }

        internal static bool configInitialized { get; private set; }

        internal static int currentSessionId { get; set; }

        internal static bool doesBatteryExist { get; set; }

        internal static bool fullscreenDetected { get; set; }

        internal static bool isCpuTempThrottled { get; set; }
        internal static bool isCurrentlyMining { get; set; }

        internal static bool isGpuTempThrottled { get; set; }
        internal static bool isMinerServiceStopped { get; set; }
        internal static bool isMiningPaused { get; set; }
        internal static bool isPipeConnected { get; set; }

        //Global variables used in different classes
        internal static bool isUserIdle { get; set; }

        internal static bool isUserLoggedIn { get; set; }
        internal static int remoteDisconnectedSession { get; set; }
        internal static bool serviceInitialized { get; set; }
        internal static int sessionLaunchAttempts { get; set; }
        internal static int skipTimerCycles { get; set; }

        internal static int currentAfterburnerProfile { get; set; }


        public enum PacketID
        {
            None,
            Hello,
            Goodbye,
            Idle,
            Pause,
            Resume,
            Stop,
            Stealth,
            Log,
            Fullscreen,
            CheckFullscreenStillRunning,
            IdleTime,
            Message,
            IgnoreFullscreenApp,
            Notifications,
            RunProgram,
            RunInUserSession,
            Authenticate
        }

        //Hashrate monitoring
        /*
         * apiUptimeFailureAttempts
         * apiHashrateFailureAttempts
         * api
         */

        private static void LoadDefaultConfig()
        {
            //re-initialize our global settings object
            settings = new Settings();

            Utilities.Log("Creating default configuration file.");

            //Call SetupDefaultConfig(), which sets the pre-programmed defaults into the global settings object
            settings.SetupDefaultConfig();

            //config is now initialized with defaults
            configInitialized = true;

            //Write the defaults to file
            WriteConfigToFile();
        }

        private static bool VerifyMiners(List<MinerList> minerList)
        {
            foreach (var miner in minerList)
            {
                if (miner.executable.Length == 0 && !miner.minerDisabled)
                {
                    Utilities.Log("You have an empty Miner, this is not allowed.", force: true);
                    System.Environment.Exit(100);
                    return false;
                }

                if (!File.Exists(miner.executable) && !miner.minerDisabled)
                {
                    if (File.Exists(Utilities.ApplicationPath() + miner.executable))
                    {
                        miner.executable = Utilities.ApplicationPath() + miner.executable;
                    }
                    else
                    {
                        Utilities.Log("Unable to locate miner Exe: " + miner.executable, force: true);
                        System.Environment.Exit(100);
                        return false;
                    }
                }
            }

            return true;
        }

        private static void VerifySettings(ref Settings tempSettings)
        {
            //verify we don't have any negative numbers, numbers that are lower or higher than safe values, or empty strings (like the url, but only need to check if verifyNetwork is true).
            if (tempSettings.maxCpuTemp > 90 || tempSettings.maxCpuTemp < 0)
                tempSettings.maxCpuTemp = 70;

            if (tempSettings.maxGpuTemp > 100 || tempSettings.maxGpuTemp < 0)
                tempSettings.maxGpuTemp = 70;

            if (tempSettings.minutesUntilIdle > 3600 || tempSettings.minutesUntilIdle < 1)
                tempSettings.minutesUntilIdle = 10;

            if (tempSettings.cpuUsageThresholdWhileNotIdle > 100 || tempSettings.cpuUsageThresholdWhileNotIdle < 0)
                tempSettings.cpuUsageThresholdWhileNotIdle = 80;

            if (tempSettings.resumeMiningTempInPercent <= 0)
                tempSettings.resumeMiningTempInPercent = 5;

            if (tempSettings.afterBurnerIdleProfile < 0 || tempSettings.afterBurnerIdleProfile > 5)
                tempSettings.afterBurnerIdleProfile = 0;

            if (tempSettings.afterBurnerActiveProfile < 0 || tempSettings.afterBurnerActiveProfile > 5)
                tempSettings.afterBurnerActiveProfile = 0;

            //if (tempSettings.resumePausedMiningAfterMinutes > 3600 || tempSettings.resumePausedMiningAfterMinutes < 0)
            //    tempSettings.resumePausedMiningAfterMinutes = 0; //0 means don't resume!

            if (tempSettings.urlToCheckForNetwork.Length <= 0 && tempSettings.verifyNetworkConnectivity)
                tempSettings.urlToCheckForNetwork = "http://speedtest.net/";

            if (tempSettings.mineWithCpu) VerifyMiners(tempSettings.cpuMiners);
            if (tempSettings.mineWithGpu) VerifyMiners(tempSettings.gpuMiners);

            if (!File.Exists(tempSettings.afterBurnerExePath))
            {
                var path = @"C:\Program Files (x86)\MSI Afterburner\MSIAfterburner.exe";
                if (!File.Exists(tempSettings.afterBurnerExePath) && File.Exists(path))
                {
                    tempSettings.afterBurnerExePath = path;
                }
                else
                {
                    tempSettings.afterBurnerExePath = "";
                    Utilities.Log("MSIAfterBurner.exe not found; disabling automatic profile switching.", true);
                }
            }

            if (!File.Exists(idleMonExecutable))
            {
                if (File.Exists(Utilities.ApplicationPath() + idleMonExecutable))
                {
                    idleMonExecutable = Utilities.ApplicationPath() + idleMonExecutable;
                }
                else
                {
                    Utilities.Log("Unable to locate " + idleMonExecutable + ". Stopping MiningService.", force: true);
                    System.Environment.Exit(200);
                }
            }

            //return our verified settingsJson object
            //return tempSettings;
        }

        public static void AddCpuUsageQueue(int percent)
        {
            //We only want to keep x number of items, so if we go over/up to, remove the oldest one
            if (cpuUsageQueue.Count >= cpuQueueLimit)
                cpuUsageQueue.Dequeue();

            cpuUsageQueue.Enqueue(percent); //add the new usage percentage to the queue
        }

        //This is new to me. It was a Intellisense suggestion. I like it.
        public static int CpuUsageAverage() => Convert.ToInt32(cpuUsageQueue.Average());

        public static void LoadConfigFromFile(string jsonFilePath = "")
        {
            //Create a temporary Settings object
            Settings settingsJson = new Settings();

            if (jsonFilePath.Length == 0)
                jsonFilePath = Utilities.ApplicationPath() + "MiningService.json";

            try
            {
                if (File.Exists("MinerService.json"))
                {
                    string oldSettings = File.ReadAllText("MinerService.json");
                    File.WriteAllText(jsonFilePath, oldSettings);
                    File.Delete("MinerService.json");
                }
            }
            catch (Exception ex)
            {
                Utilities.Log("Error occurred trying to copy old settings to the new file name: " + ex.Message);
            }

            //If the passed file path does not exist, load defaults and save them to file
            if (!File.Exists(jsonFilePath))
            {
                LoadDefaultConfig();
                VerifySettings(ref settings);

                return;
            }

            try
            {
                //Try to read and deserialize the passed file path into the temporary Settings object
                settingsJson = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(jsonFilePath));

                //Pass our new settings to VerifySettings, check it for safe numbers etc
                VerifySettings(ref settingsJson);

                settings = settingsJson;
            }
            catch (Exception ex)
            {
                Utilities.Log("LoadConfigFromFile: " + ex.Message, force: true);
            }

            //if load was successful
            configInitialized = true;
        }

        public static void WriteConfigToFile(string jsonFilePath = "")
        {
            if (jsonFilePath.Length == 0)
                jsonFilePath = Utilities.ApplicationPath() + "MiningService.json";

            try
            {
                File.WriteAllText(jsonFilePath, JsonConvert.SerializeObject(settings, Formatting.Indented));
                Utilities.Log("Saved " + jsonFilePath + ".");
            }
            catch (Exception ex)
            {
                Utilities.Log("WriteConfigToFile: " + ex.Message);
            }
        }
    }
}