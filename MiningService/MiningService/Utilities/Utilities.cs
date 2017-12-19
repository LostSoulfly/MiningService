using Message;
using NamedPipeWrapper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MiningService
{
    internal static class Utilities
    {
        #region Public variables

        //This version string is actually quite useless. I just use it to verify the running version in log files.
        public static string version = "0.1.2";

        #endregion Public variables

        #region DLLImports and enums (ThreadExecutionState, WTSQuerySession)

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern EXECUTION_STATE SetThreadExecutionState(
        EXECUTION_STATE flags);

        [Flags]
        public enum EXECUTION_STATE : uint
        {
            ES_SYSTEM_REQUIRED = 0x00000001,
            ES_DISPLAY_REQUIRED = 0x00000002,
            ES_CONTINUOUS = 0x80000000
        }

        [DllImport("Wtsapi32.dll")]
        private static extern bool WTSQuerySessionInformation(IntPtr hServer, int sessionId, WtsInfoClass wtsInfoClass, out System.IntPtr ppBuffer, out int pBytesReturned);

        [DllImport("Wtsapi32.dll")]
        private static extern void WTSFreeMemory(IntPtr pointer);

        public enum WtsInfoClass
        {
            WTSInitialProgram,
            WTSApplicationName,
            WTSWorkingDirectory,
            WTSOEMId,
            WTSSessionId,
            WTSUserName,
            WTSWinStationName,
            WTSDomainName,
            WTSConnectState,
            WTSClientBuildNumber,
            WTSClientName,
            WTSClientDirectory,
            WTSClientProductId,
            WTSClientHardwareId,
            WTSClientAddress,
            WTSClientDisplay,
            WTSClientProtocolType,
            WTSIdleTime,
            WTSLogonTime,
            WTSIncomingBytes,
            WTSOutgoingBytes,
            WTSIncomingFrames,
            WTSOutgoingFrames,
            WTSClientInfo,
            WTSSessionInfo,
        }

        #endregion DLLImports and enums (ThreadExecutionState, WTSQuerySession)

        #region Check for network connection/MinerProxy server status

        public static bool CheckForInternetConnection()
        {
            //This is called to verify network connectivity, I personally use a MinerProxy instance's built-in web server API at /status, which returns "True".
            //In theory, anything that actually loads should work.

            if (!Config.settings.verifyNetworkConnectivity)
                return true;

            try
            {
                using (var client = new System.Net.WebClient())
                using (var stream = client.OpenRead(Config.settings.urlToCheckForNetwork))
                {
                    Debug("Network Connectivity verified.");
                    return true;
                }
            }
            catch
            {
                Debug("Network Conectivity URL unreachable.");
                return false;
            }
        }

        #endregion Check for network connection/MinerProxy server status

        #region CPU utils

        //todo: Get CPU temperature function
        public static int GetCpuUsage()
        {
            // This returns, in a % of 100, the current CPU usage over a 1 second period.
            try
            {
                var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", true);
                cpuCounter.NextValue();
                System.Threading.Thread.Sleep(1000);
                int cpuPercent = (int)cpuCounter.NextValue();

                //Add to the rolling average of CPU temps
                Config.AddCpuUsageQueue(cpuPercent);

                return cpuPercent;
            }
            catch (Exception ex)
            {
                Debug("GetCpuUsage: " + ex.Message);
                return 50;
            }
        }

        #endregion CPU utils

        #region GPU utils

        //todo: Get GPU temperatures

        #endregion GPU utils

        #region Process utilities

        public static bool IsProcessRunning(string process)
        {
            Process[] proc = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(process));

            if (proc.Length == 0)
                return false;

            if (proc.Length > 1)
            {
                Utilities.Debug("More than one " + process);
                Utilities.KillProcess(process);
                return false;
            }

            return true;
        }

        public static bool IsProcessRunning(MinerList miner)
        {
            Process[] proc = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(miner.executable));

            if (proc.Length == 0)
                return false;

            return true;
        }

        public static bool AreMinersRunning(List<MinerList> miners, bool isUserIdle)
        {
            bool areMinersRunning = true;
            int disabled = 0;

            //Debug("AreMinersRunning entered");

            foreach (var miner in miners)
            {
                Process[] proc = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(miner.executable));

                if (miner.minerDisabled || (!miner.mineWhileNotIdle && !Config.isUserIdle))
                {
                    if (proc.Length > 0)
                        KillProcess(miner.executable);

                    disabled++;
                }
                else
                {
                    if (miner.isMiningIdleSpeed != isUserIdle && !miner.minerDisabled && (miner.idleArguments != miner.activeArguments))
                    {
                        Utilities.Debug("Miner " + miner.executable + " is not running in correct mode!");
                        KillProcess(miner.executable);
                        areMinersRunning = false;
                    }
                    else if (proc.Length == 0)
                    {
                        areMinersRunning = false;
                    }
                    else if (proc.Length > 0)
                    {
                        areMinersRunning = true;
                        miner.launchAttempts = 0;
                    }
                }
            }

            if (disabled == miners.Count && disabled > 0)
                areMinersRunning = true;

            //Debug("AreMinersRunning exited. areMinersRunning: " + areMinersRunning + " " + disabled + " " + miners.Count);
            return areMinersRunning;
        }

        public static int LaunchProcess(string exe, string args)
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.RedirectStandardOutput = false;
            psi.RedirectStandardError = false;
            psi.UseShellExecute = false;
            psi.WorkingDirectory = Path.GetDirectoryName(exe);

            Process proc = new Process();
            proc.StartInfo = psi;

            Debug("Starting Process " + exe + " " + args);

            proc = Process.Start(exe, args);
            return proc.Id;
        }

        //This one accepts a MinerList as the passed argument, and uses the Executable and Arguments of that particular miner.
        public static int LaunchProcess(MinerList miner)
        {
            if (miner.minerDisabled)
                return 0;

            string arguments = Config.isUserIdle ? miner.idleArguments : miner.activeArguments;
            miner.isMiningIdleSpeed = Config.isUserIdle;

            if (arguments.Length == 0)
                return 0;

            ProcessStartInfo psi = new ProcessStartInfo();
            psi.RedirectStandardOutput = false;
            psi.RedirectStandardError = false;
            psi.UseShellExecute = false;
            psi.WorkingDirectory = Path.GetDirectoryName(miner.executable);

            Process proc = new Process();
            proc.StartInfo = psi;

            Debug("Starting Process " + miner.executable + " " + arguments);

            miner.isMiningIdleSpeed = Config.isUserIdle;

            proc = Process.Start(miner.executable, arguments);
            miner.launchAttempts++;
            return proc.Id;
        }

        public static bool LaunchMiners(List<MinerList> minerList)
        {
            bool launchIssues = false;
            bool isRunning = false;

            //Debug("LaunchMiners entered");

            foreach (var miner in minerList)
            {
                isRunning = IsProcessRunning(miner);
                Debug("shouldMinerBeRunning: " + miner.shouldMinerBeRunning + " minerDisabled: " + miner.minerDisabled + " isRunning:" + isRunning + " isMiningIdleSpeed:" + miner.isMiningIdleSpeed + " launchAttempts: " + miner.launchAttempts);

                if ((miner.shouldMinerBeRunning && !miner.minerDisabled) &&
                    (!isRunning || (miner.isMiningIdleSpeed != Config.isUserIdle)) &&
                    miner.launchAttempts <= 4 && !Config.isMiningPaused)
                {
                    if (LaunchProcess(miner) <= 0)  //returns PID
                    {
                        Utilities.Log("LaunchMiners: Unable to launch " + miner.executable + " " + (Config.isUserIdle ? miner.idleArguments : miner.activeArguments));
                        launchIssues = true;
                    }
                    miner.shouldMinerBeRunning = true;
                    miner.isMiningIdleSpeed = Config.isUserIdle;
                }
                else if (miner.shouldMinerBeRunning && isRunning && miner.launchAttempts <= 4)
                {
                    miner.launchAttempts = 0;
                }
                else if (miner.shouldMinerBeRunning && isRunning && miner.launchAttempts > 4 && !miner.minerDisabled)
                {
                    Log("Miner " + miner.executable + " has failed to launch 5 times, and is now disabled.");
                    miner.minerDisabled = true;
                }
            }

            //Debug("LaunchMiners exited. LaunchIssues: " + launchIssues);

            Config.isCurrentlyMining = true;
            return !launchIssues;
        }

        public static void MinersShouldBeRunning(List<MinerList> minerList)
        {
            foreach (var miner in minerList)
            {
                if ((!miner.minerDisabled && miner.launchAttempts < 4) && (miner.mineWhileNotIdle || Config.isUserIdle) && !Config.isMiningPaused)
                {
                    miner.shouldMinerBeRunning = true;
                }
                else
                {
                    miner.shouldMinerBeRunning = false;
                }
                miner.isMiningIdleSpeed = false;
            }
        }

        public static void KillMiners()
        {
            //Debug("KillMiners entered");
            //loop through the CPU miner list and kill all miners
            if (Config.settings.mineWithCpu)
            {
                foreach (var miner in Config.settings.cpuMiners)
                {
                    if (miner.shouldMinerBeRunning || IsProcessRunning(miner))
                    {
                        KillProcess(Path.GetFileNameWithoutExtension(miner.executable));
                        miner.shouldMinerBeRunning = false;
                    }
                }
            }

            //loop through the GPU miner list and kill all miners
            if (Config.settings.mineWithGpu)
            {
                foreach (var miner in Config.settings.gpuMiners)
                {
                    if (miner.shouldMinerBeRunning || IsProcessRunning(miner))
                    {
                        Debug("Killing miner " + miner.executable);
                        KillProcess(Path.GetFileNameWithoutExtension(miner.executable));
                        miner.shouldMinerBeRunning = false;
                    }
                }
            }

            //Debug("KillMiners exited");

            //we're no longer mining
            Config.isCurrentlyMining = false;
        }

        public static void KillIdlemon(NamedPipeClient<IdleMessage> client)
        {
            //Debug("KillMiners entered");
            //loop through the CPU miner list and kill all miners
            if (IsProcessRunning(Config.idleMonExecutable) && Config.isPipeConnected)
            {
                client.PushMessage(new IdleMessage
                {
                    packetId = (int)MyService.PacketID.Stop,
                    isIdle = false,
                    requestId = (int)MyService.PacketID.None,
                    data = ""
                });

                System.Threading.Thread.Sleep(1000);

                if (IsProcessRunning(Config.idleMonExecutable))
                    KillProcess(Config.idleMonExecutable);
            }
        }

            public static bool KillProcess(string proc)
            {
            bool cantKillProcess = false;

            //Debug("KillProcess entered: " + proc);

            try
            {
                foreach (Process p in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(proc)))
                {
                    Debug("Process found: " + p.Id);
                    p.Kill();
                    p.WaitForExit(3000);    //wait a max of 3 seconds for the process to terminate

                    if (!p.HasExited)
                        cantKillProcess = true;

                    Debug("Killed " + proc + "(" + cantKillProcess + ")");
                }
            }
            catch (Exception ex)
            {
                Utilities.Log("KillProcess: " + ex.Message + '\n' + ex.Source);
                return false;
            }

            //Debug("KillProcess exited. cantKillProcess: " + cantKillProcess);

            //if we can't kill one of the processes, we should return FALSE!
            return !cantKillProcess;
        }

        #endregion Process utilities

        #region System/OS Utils

        public static string GetUsernameBySessionId(int sessionId, bool prependDomain)
        {
            //This returns the current logged in user, or if none is found, SYSTEM.
            IntPtr buffer;
            int strLen;
            string username = "SYSTEM"; //This may cause issues on other locales, so we may need to find a better method of detecting no logged in user.
            if (WTSQuerySessionInformation(IntPtr.Zero, sessionId, WtsInfoClass.WTSUserName, out buffer, out strLen) && strLen > 1)
            {
                username = Marshal.PtrToStringAnsi(buffer);
                WTSFreeMemory(buffer);
                if (prependDomain)
                {
                    if (WTSQuerySessionInformation(IntPtr.Zero, sessionId, WtsInfoClass.WTSDomainName, out buffer, out strLen) && strLen > 1)
                    {
                        username = Marshal.PtrToStringAnsi(buffer) + "\\" + username;
                        WTSFreeMemory(buffer);
                    }
                }
            }
            return username;
        }

        public static bool IsSystem()
        {
            //This is used to verify the service is running as an actual Service (Running as the SYSTEM user)
            bool isSystem;
            using (var identity = System.Security.Principal.WindowsIdentity.GetCurrent())
            {
                isSystem = identity.IsSystem;
            }

            return isSystem;
        }

        public static void CheckForSystem(int sessionId)
        {
            //todo: use the new static Config class, and set these variables
            //This checks who is currently logged into the active Windows Session (think Desktop user)
            if (Utilities.GetUsernameBySessionId(sessionId, false) == "SYSTEM")
            {
                Debug("CheckForSystem: SYSTEM");
                KillMiners();
                KillProcess(Config.idleMonExecutable);
                Config.isUserLoggedIn = false;
                Config.isPipeConnected = false;
                Config.isUserIdle = true;
            }
            else
            {
                Config.isUserLoggedIn = true;
            }
        }

        public static bool IsWinVistaOrHigher()
        {
            //This returns true if the OS version is higher than XP. XP machines generally speaking won't work well for mining.
            //Often times there will be .net issues as well, as we will be using a newer version of the .net framework
            OperatingSystem OS = Environment.OSVersion;
            return (OS.Platform == PlatformID.Win32NT) && (OS.Version.Major >= 6);
        }

        public static bool Is64BitOS()
        {
            //Returns true if the computer is 64bit
            return (System.Environment.Is64BitOperatingSystem);
        }

        public static void AllowSleep()
        {
            //this sets the ThreadExecutionState to allow the computer to sleep.
            SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS);
        }

        public static void PreventSleep()
        {
            //This sets the ThreadExecutionState to (attempt) to prevent the computer from sleeping
            //todo: This may need to be implemented into IdleMon instead of MiningService. Needs testing.
            SetThreadExecutionState(
              EXECUTION_STATE.ES_SYSTEM_REQUIRED |
              EXECUTION_STATE.ES_CONTINUOUS);
        }

        #endregion System/OS Utils

        #region Battery utils

        public static bool IsBatteryFull()
        {
            System.Windows.Forms.PowerStatus pw = SystemInformation.PowerStatus;

            float floatBatteryPercent = 100 * SystemInformation.PowerStatus.BatteryLifePercent;
            int batteryPercent = (int)floatBatteryPercent;

            if (pw.BatteryChargeStatus.HasFlag(BatteryChargeStatus.NoSystemBattery))
                return true;

            if (batteryPercent == 100)
                return true;

            return false;
        }

        public static bool DoesBatteryExist()
        {
            System.Windows.Forms.PowerStatus pw = SystemInformation.PowerStatus;

            if (pw.BatteryChargeStatus == BatteryChargeStatus.NoSystemBattery)
                return false;

            return true;
        }

        #endregion Battery utils

        #region Logging

        public static void Log(string text, bool force = false)
        {
            if (!IsSystem())
                Console.WriteLine(DateTime.Now.ToString() + " LOG: " + text);

            try
            {
                if (force || Config.settings.enableLogging)
                    File.AppendAllText(ApplicationPath() + System.Environment.MachineName + ".txt", DateTime.Now.ToString() + " (" + Process.GetCurrentProcess().Id + ") LOG: " + text + System.Environment.NewLine);
            }
            catch
            {
            }
        }

        public static void Debug(string text)
        {
            if (!IsSystem() && Config.settings.enableDebug)
                Console.WriteLine(DateTime.Now.ToString() + " DEBUG: " + text);

            try
            {
                if (Config.settings.enableLogging && Config.settings.enableDebug)
                    File.AppendAllText(ApplicationPath() + System.Environment.MachineName + ".txt", DateTime.Now.ToString() + " (" + Process.GetCurrentProcess().Id + ") DEBUG: " + text + System.Environment.NewLine);
            }
            catch
            {
            }
        }

        #endregion Logging

        #region ApplicationPath

        public static string ApplicationPath()
        {
            return PathAddBackslash(AppDomain.CurrentDomain.BaseDirectory);
            //return System.Reflection.Assembly.GetExecutingAssembly().Location;
        }

        private static string PathAddBackslash(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            path = path.TrimEnd();

            if (PathEndsWithDirectorySeparator())
                return path;

            return path + GetDirectorySeparatorUsedInPath();

            bool PathEndsWithDirectorySeparator()
            {
                char lastChar = path.Last();
                return lastChar == Path.DirectorySeparatorChar
                    || lastChar == Path.AltDirectorySeparatorChar;
            }

            char GetDirectorySeparatorUsedInPath()
            {
                if (path.Contains(Path.DirectorySeparatorChar))
                    return Path.DirectorySeparatorChar;

                return Path.AltDirectorySeparatorChar;
            }
        }

        #endregion ApplicationPath
    }
}