using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using NamedPipeWrapper;
using Message;
using System.Drawing;

namespace idleMon
{
    static class Program
    {

        public static bool stealthMode;
        public static bool enableLogging;
        
        [STAThread]
        static void Main(string[] args)
        {
            //check if the -stealth argument was passed when launching IdleMon
            if (args.Contains<string>("-stealth"))
                stealthMode = true;

            //if (args.Contains<string>("-log"))
                enableLogging = true;

            Utilities.Log("Main Args:" + String.Join(" ", args));

            //This Main justs sets things up to run our IdleMonContext, so we don't have to display a Form or worry about hiding one.
            Utilities.Log("IdleMon starting" + (stealthMode ? " in stealth mode." : ""));

            //This creates some exception handling globally that logs all uncaught errors to LOGFILE-err.txt
            AppDomain.CurrentDomain.UnhandledException += (s, e) => Utilities.Log(e.ExceptionObject.ToString());
            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new IdleMon.IdleMonContext());

        }
        
    }
}
