using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace idleMon
{
    class Utilities
    {
        public static long minutesIdle = 10;
        public static bool lastState;

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(HandleRef hWnd, [In, Out] ref RECT rect);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        public static bool IsForegroundFullScreen()
        {
            //check each screen on the system
            System.Windows.Forms.Screen[] screen = System.Windows.Forms.Screen.AllScreens;
            foreach (var item in screen)
            {
                if (IsForegroundFullScreen(item))
                {
                    Utilities.Log("Fullscreen detected:");
                    return true;
                }
            }
            return IsForegroundFullScreen(null);
        }


        public static bool IsForegroundFullScreen(System.Windows.Forms.Screen screen)
        {

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

                Utilities.Log("Screen " + screen.DeviceName + " is currently fullscreen: " + proc.ProcessName);
                if (proc.ProcessName == "explorer")
                    return false;

                return true;
            }
            else
            {
                return false;
            }
            
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
        
        public static bool IsIdle() //In minutes
        {
            TimeSpan idleTime = TimeSpan.FromMilliseconds(IdleTimeFinder.GetIdleTime());
            
            TimeSpan timeUntilIdle = TimeSpan.FromMinutes(minutesIdle);
            
            if (TimeSpan.Compare(idleTime, timeUntilIdle) >= 0)
                return true;
            
            return false;
        }

        // credits https://goo.gl/VYDfZz
        internal struct LASTINPUTINFO
        {
            public uint cbSize;

            public uint dwTime;
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
        #endregion

        /// <summary>
        /// Helps to find the idle time, (in milliseconds) spent since the last user input
        /// </summary>
        public class IdleTimeFinder
        {
            [DllImport("User32.dll")]
            private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

            [DllImport("Kernel32.dll")]
            private static extern uint GetLastError();

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
