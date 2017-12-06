using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Topshelf;

namespace IdleService
{
    static class Utilities
    {
        #region Public variables
        
        //This version string is actually quite useless. I just use it to verify the running version in log files.
        public static string version = "0.0.6";
        
#endregion

        #region DLLImports and enums (ThreadExecutionState, WTSQuerySession)
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern EXECUTION_STATE SetThreadExecutionState(
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
        #endregion

        #region Check for network connection/MinerProxy server status
        public static bool CheckForInternetConnection()
        {
            //This is called to verify network connectivity, I personally use a MinerProxy instance's built-in web server API at /status, which returns "True".
            //In theory, anything that actually loads should work.

            //todo: Allow setting this from a config file
            try
            {
                using (var client = new System.Net.WebClient())
                using (var stream = client.OpenRead("http://127.0.0.1:9091/status"))
                {
                    //Log("Proxy online and reachable.");
                    return true;
                }
            }
            catch
            {
                //Log("Proxy offline or unreachable.");
                return false;
            }
        }
        #endregion

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
                return (int)cpuCounter.NextValue();
            }
            catch
            {
                //Log("GetCpuUsage: " + ex.Message);
                return 50;
            }
        }
        #endregion

        #region GPU utils
        //todo: Get GPU temperatures
        #endregion

        #region Process utilities

        public static bool IsProcessRunning(string process)
        {
            Process[] proc = Process.GetProcessesByName(process);

            if (proc.Length == 0)
                return false;

            if (proc.Length > 1)
            {
                Utilities.Log("More than one " + process);
                Utilities.KillProcess(process);
                return false;
            }

            return true;
        }

        public static bool IsProcessRunning(MinerList miner)
        {
            Process[] proc = Process.GetProcessesByName(miner.executable);

            if (proc.Length == 0)
                return false;
            
            return true;
        }

        public static bool AreMinersRunning(List<MinerList> miners)
        {
            bool minerNotRunning = false;

            foreach (var miner in miners)
            {
                Process[] proc = Process.GetProcessesByName(miner.executable);

                if (proc.Length == 0)
                    minerNotRunning = true;
            }

            return minerNotRunning;
        }

        public static int LaunchProcess(string exe, string args)
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.RedirectStandardOutput = false;
            psi.RedirectStandardError = false;
            psi.UseShellExecute = false;

            Process proc = new Process();
            proc.StartInfo = psi;

            proc = Process.Start(exe, args);
            return proc.Id;
        }

        //This one accepts a MinerList as the passed argument, and uses the Executable and Arguments of that particular miner.
        public static int LaunchProcess(MinerList miner)
        {

            if (miner.minerDisabled)
                return 0;

            string arguments = Config.isUserIdle ? miner.idleArguments : miner.activeArguments;

            if (arguments.Length == 0)
                return 0;

            ProcessStartInfo psi = new ProcessStartInfo();
            psi.RedirectStandardOutput = false;
            psi.RedirectStandardError = false;
            psi.UseShellExecute = false;

            Process proc = new Process();
            proc.StartInfo = psi;

            proc = Process.Start(miner.executable, arguments);
            miner.launchAttempts++;
            return proc.Id;
        }

        public static bool LaunchMiners(List<MinerList> minerList)
        {
            bool launchIssues = false;
            bool isRunning = false;
            
            foreach (var miner in minerList)
            {
                isRunning = IsProcessRunning(miner);
                if (miner.shouldMinerBeRunning && !isRunning && miner.launchAttempts <= 4)
                {
                    if (LaunchProcess(miner) <= 0)  //returns PID
                    {
                        Utilities.Log("LaunchMiners: Unable to launch " + miner.executable + " " + (Config.isUserIdle ? miner.idleArguments : miner.activeArguments));
                        launchIssues = true;
                    }
                    miner.shouldMinerBeRunning = true;

                } else if (miner.shouldMinerBeRunning && isRunning && miner.launchAttempts <= 4)
                {
                    miner.launchAttempts = 0;
                } else if (miner.shouldMinerBeRunning && isRunning && miner.launchAttempts > 4)
                {
                    miner.minerDisabled = true;
                }
            }
            
            return !launchIssues;
        }

        public static bool LaunchMiners()
        {
            bool launchIssues = false;

            launchIssues = LaunchMiners(Config.settings.cpuMiners);

            if (!LaunchMiners(Config.settings.cpuMiners))
                launchIssues = true;

            Config.isCurrentlyMining = true;
            return !launchIssues;
        }

        public static void KillMiners()
        {

            //loop through the CPU miner list and kill all miners
            if (Config.settings.mineWithCpu)
            {
                foreach (var miner in Config.settings.cpuMiners)
                {
                    if (miner.shouldMinerBeRunning)
                    {
                        KillProcess(miner.executable);
                        miner.shouldMinerBeRunning = false;
                    }
                }
            }

            //loop through the GPU miner list and kill all miners
            if (Config.settings.mineWithGpu)
            {
                foreach (var miner in Config.settings.gpuMiners)
                {
                    if (miner.shouldMinerBeRunning)
                    {
                        KillProcess(miner.executable);
                        miner.shouldMinerBeRunning = false;
                    }
                }
            }

            //we're no longer mining
            Config.isCurrentlyMining = false;
        }

        public static bool KillProcess(string proc)
        {
            bool cantKillProcess = false;

            try
            {
                foreach (Process p in Process.GetProcessesByName(proc))
                {
                    p.Kill();
                    p.WaitForExit(3000);    //wait a max of 3 seconds for the process to terminate

                    if (!p.HasExited)
                        cantKillProcess = true;

                    Utilities.Log(string.Format("Killed {0}.", proc));
                }
            }
            catch (Exception ex)
            {
                Utilities.Log("KillProcess: " + ex.Message + '\n' + ex.Source);
                return false;
            }

            //if we can't kill one of the processes, we should return FALSE!
            if (cantKillProcess) return false;

            //otherwise, all processes were stopped, so return true
            return true;
        }
        #endregion

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
            //todo: Allow or disallow this setting from a config file, same with AllowSleep.
            //Log("PreventSleep");
            SetThreadExecutionState(
              EXECUTION_STATE.ES_SYSTEM_REQUIRED |
              EXECUTION_STATE.ES_CONTINUOUS);

        }
#endregion
        
        #region Battery utils
        public static bool IsBatteryFull()
        {
            System.Windows.Forms.PowerStatus pw = SystemInformation.PowerStatus;

            //Utilities.Log("IsBatteryFull: " + pw.BatteryChargeStatus.ToString());

            if (pw.BatteryChargeStatus.HasFlag(BatteryChargeStatus.NoSystemBattery) |
                pw.BatteryChargeStatus.HasFlag(BatteryChargeStatus.High) |
                pw.BatteryChargeStatus.HasFlag(BatteryChargeStatus.Unknown) |
                pw.BatteryChargeStatus.HasFlag(BatteryChargeStatus.Charging))
            {
                return true;
            }

            return false;
        }

        public static bool DoesBatteryExist()
        {
            System.Windows.Forms.PowerStatus pw = SystemInformation.PowerStatus;
            int powerPercent = (int)(pw.BatteryLifePercent * 100);
            if (powerPercent > 0)
            {
                //Utilities.Log("Battery exists: " + powerPercent);
                return true;
            }

            return false;
        }
#endregion

        #region Logging
        public static void Log(string text, bool force = false)
        {
            try
            {
                if (force || Config.settings.enableDebug)
                    File.AppendAllText(ApplicationPath() + System.Environment.MachineName + ".txt", DateTime.Now.ToString() + " (" + Process.GetCurrentProcess().Id + "): " + text + System.Environment.NewLine);
            }
            catch
            {

            }
        }
        #endregion

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
#endregion
    }
}

