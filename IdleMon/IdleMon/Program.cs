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
        
        [STAThread]
        static void Main(string[] args)
        {
            
            //This creates some exception handling globally that logs all uncaught errors to LOGFILE-err.txt
            AppDomain.CurrentDomain.UnhandledException += (s, e) => Utilities.Log(e.ExceptionObject.ToString());
            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new IdleMon.IdleMonContext());

        }
        
    }
}
