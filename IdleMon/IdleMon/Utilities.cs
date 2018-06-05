using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace idleMon
{
    internal class Utilities
    {
        public static string fullscreenAppName;
        public static List<string> ignoredFullscreenApps = new List<string>();
        public static bool lastState;
        public static long minutesIdle = 15;
        public static bool ShowDesktopNotifications;

        // credits https://goo.gl/VYDfZz
        internal struct LASTINPUTINFO
        {
            public uint cbSize;

            public uint dwTime;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(HandleRef hWnd, [In, Out] ref RECT rect);

        public static string CalculateMD5(string input)
        {
            MD5 md5 = System.Security.Cryptography.MD5.Create();

            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }

            //Utilities.Log($"CalculateMD5: {sb.ToString()}");
            return sb.ToString();
        }

        public static string GenerateAuthString(string userName)
        {
            // Since this uses pipes, and should be connected on the same system, using the current
            // date as a sort of salt should work out fine. MD5 is fast enough that we could probably
            // use the current system seconds as well, but it's a small chance to fail, so we leave
            // that off.
            string date = DateTime.Now.ToString(@"yyyy\-MM\-dd HH\:mm");

            //Calculate the first auth string from GUID file:LSFMiningService:Current system date/time
            string auth = $"{ReadMachineGuid()}:LSFMiningService:{date}";

            //Calculate second auth string from first auth's MD5, plus machine name
            string auth2 = $"{CalculateMD5(auth)}:{Environment.MachineName}";

            //Calculate auth3 string from auth2's MD5, plus supplied username
            string auth3 = $"{CalculateMD5(auth2)}:{userName}";

            //Finally, calculate actual auth string with auth3's MD5
            string finalAuth = CalculateMD5(auth3);

            //Utilities.Log($"GenerateAuthString: Auth: {auth} \n Auth2: {auth2} \n Auth3: {auth3} \n finalAuth: {finalAuth}");

            return finalAuth;
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        public static string IsForegroundFullScreen()
        {
            string fullscreenApp;
            //check each screen on the system
            System.Windows.Forms.Screen[] screen = System.Windows.Forms.Screen.AllScreens;
            foreach (var item in screen)
            {
                fullscreenApp = IsForegroundFullScreen(item);
                return fullscreenApp;
            }
            return IsForegroundFullScreen(null);
        }

        public static string IsForegroundFullScreen(System.Windows.Forms.Screen screen)
        {
            string fullscreenApp = "";

            if (screen == null)
            {
                screen = System.Windows.Forms.Screen.PrimaryScreen;
            }
            RECT rect = new RECT();
            IntPtr hWnd = (IntPtr)GetForegroundWindow();

            GetWindowRect(new HandleRef(null, hWnd), ref rect);

            if (screen.Bounds.Width == (rect.right - rect.left) && screen.Bounds.Height == (rect.bottom - rect.top))
            {
                //get process information of foreground app
                uint procId = 0;
                GetWindowThreadProcessId(hWnd, out procId);
                var proc = System.Diagnostics.Process.GetProcessById((int)procId);

                if (Utilities.ignoredFullscreenApps.Contains(proc.ProcessName))
                    return string.Empty;

                if (proc.ProcessName != Utilities.fullscreenAppName)
                    Utilities.Log("Screen " + screen.DeviceName + " is currently fullscreen: " + proc.ProcessName);

                fullscreenApp = proc.ProcessName;

                return fullscreenApp;
            }
            else
            {
                return string.Empty;
            }
        }

        public static bool IsIdle() //In minutes
        {
            TimeSpan idleTime = TimeSpan.FromMilliseconds(IdleTimeFinder.GetIdleTime());

            TimeSpan timeUntilIdle = TimeSpan.FromMinutes(minutesIdle);

            if (TimeSpan.Compare(idleTime, timeUntilIdle) >= 0)
                return true;

            return false;
        }

        public static bool IsProcessRunning(string process)
        {
            Process[] proc = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(process));

            if (proc.Length == 0)
                return false;

            return true;
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

            Log("Starting Process " + exe + " " + args);

            try
            {
                proc = Process.Start(exe, args);
            }
            catch (Exception ex)
            {
                Log("LaunchProcess exe: " + ex.ToString());
            }
            return proc.Id;
        }

        public static void Log(string text)
        {
            if (!IdleMon.IdleMonContext.enableLogging)
                return;

            try
            {
                File.AppendAllText(ApplicationPath() + System.Environment.MachineName + ".txt", DateTime.Now.ToString()
                    + " (" + Process.GetCurrentProcess().Id + "): " + text + System.Environment.NewLine);
            }
            catch
            {
            }
        }

        public static string ReadMachineGuid()
        {
            string id = "";

            try
            {
                id = File.ReadAllText(ApplicationPath() + "MachineID.txt");
            }
            catch (Exception ex) { Utilities.Log("ReadMachineGuid: " + ex.Message); }

            //Log("ReadMachineGuid: " + id);

            return id;
        }

        public static bool VerifyAuthString(string md5Hash, string userName)
        {
            bool success = GenerateAuthString(userName) == md5Hash;
            Utilities.Log($"VerifyAuthString: {success} - {md5Hash}");

            return success;
        }

        public static bool WriteMachineGuid()
        {
            try
            {
                File.WriteAllText(ApplicationPath() + "MachineID.txt", Guid.NewGuid().ToString());
                return true;
            }
            catch { return false; }
        }

        #region ApplicationPath

        public static string ApplicationPath()
        {
            return PathAddBackslash(AppDomain.CurrentDomain.BaseDirectory);
            //return System.Reflection.Assembly.GetExecutingAssembly().Location;
        }

        public static string PathAddBackslash(string path)
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

        /// <summary>
        /// Helps to find the idle time, (in milliseconds) spent since the last user input
        /// </summary>
        public class IdleTimeFinder
        {
            [DllImport("Kernel32.dll")]
            private static extern uint GetLastError();

            [DllImport("User32.dll")]
            private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

            public static uint GetIdleTime()
            {
                LASTINPUTINFO lastInPut = new LASTINPUTINFO();
                lastInPut.cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(lastInPut);
                GetLastInputInfo(ref lastInPut);

                var time = (uint)Environment.TickCount - lastInPut.dwTime;

                return ((uint)Environment.TickCount - lastInPut.dwTime);
            }

            /// <summary>
            /// Get the Last input time in milliseconds
            /// </summary>
            /// <returns></returns>
            public static long GetLastInputTime()
            {
                LASTINPUTINFO lastInPut = new LASTINPUTINFO();
                lastInPut.cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(lastInPut);
                if (!GetLastInputInfo(ref lastInPut))
                {
                    throw new Exception(GetLastError().ToString());
                }
                return lastInPut.dwTime;
            }
        }
    }
}