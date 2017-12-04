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

        public static bool IsProcessRunning(string process, bool excludeOwnID = false)
        {
            Process[] proc = Process.GetProcessesByName(process);
            
            //int count = Process.GetProcessesByName(process).Length;
            //if (minerExe.Length == 0)
            //Utilities.Log(process + " not running..");

            if (proc.Length == 0)
                return false;

            if (proc.Length > 1)
            {
                Utilities.Log("More than one " + process);
                Utilities.KillProcess(process, excludeOwnID);
                if (!excludeOwnID) return false;
            }

            return true;
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

        public static void KillProcess(string proc, bool excludeOwnID = false)
        {
            //if (string.IsNullOrEmpty(proc)) proc = minerExeName;
            int thisProcID = Process.GetCurrentProcess().Id;

            /*
            if (proc == minerExeName)
            {
                running = false;
            }
            */

            try
            {
                foreach (Process p in Process.GetProcessesByName(proc))
                {
                    if (!excludeOwnID)
                    {
                        p.Kill();
                        Utilities.Log(string.Format("Killed {0}.", proc));
                    }
                    else if (p.Id == thisProcID)
                    {
                        Utilities.Log(string.Format("ignoring this process"));
                    }
                    else
                    {
                        p.Kill();
                        Utilities.Log(string.Format("Killed {0}.", proc));
                    }
                }
            }
            catch (Exception ex)
            {
                Utilities.Log("KillProcess: " + ex.Message + '\n' + ex.Source);
            }
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
                Utilities.KillProcess("");
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
        public static void Log(string text, string extra = "")
        {
            try
            {
                if (Config.settings.enableDebug)
                    File.AppendAllText(ApplicationPath() + System.Environment.MachineName + extra + ".txt", DateTime.Now.ToString() + " (" + Process.GetCurrentProcess().Id + "): " + text + System.Environment.NewLine);
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

