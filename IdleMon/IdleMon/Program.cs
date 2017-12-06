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

        enum PacketID
        {
            Hello,
            Goodbye,
            Idle,
            Pause,
            Resume
        }


        [STAThread]
        static void Main(string[] args)
        {
            //check if the -stealth argument was passed when launching IdleMon
            if (args.Contains<string>("-stealth"))
                stealthMode = true;

            if (args.Contains<string>("-log"))
                enableLogging = true;

            //This Main justs sets things up to run our IdleMonContext, so we don't have to display a Form or worry about hiding one.
            Utilities.Log("IdleMon starting" + (stealthMode ? " in stealth mode." : ""));

            //This creates some exception handling globally that logs all uncaught errors to LOGFILE-err.txt
            AppDomain.CurrentDomain.UnhandledException += (s, e) => Utilities.Log(e.ExceptionObject.ToString());
            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new IdleMonContext());

        }

       
        class IdleMonContext : ApplicationContext
        {
            //Where the actual program starts
            private NotifyIcon TrayIcon;
            private ContextMenuStrip TrayIconContextMenu;
            private ToolStripMenuItem CloseMenuItem;
            private ToolStripMenuItem PauseMenuItem;
            
            //create the NamedPipe server for our Service communication
            static NamedPipeServer<IdleMessage> server = new NamedPipeServer<IdleMessage>(@"Global\MINERPIPE");
            static System.Timers.Timer timer = new System.Timers.Timer(3000);
            static bool lowOnly;
            static bool sentFirstTime;
            static bool paused;
            public IdleMonContext()
            {
                Application.ApplicationExit += new EventHandler(this.OnApplicationExit);
                InitializeComponent();

                if (!stealthMode)
                {
                    TrayIcon.Visible = true;
                    Utilities.Log("TrayIcon initialized.");

                    TrayIcon.ShowBalloonTip(3000);
                }

                timer.Elapsed += new ElapsedEventHandler(OnTimedEvent);

                server.ClientConnected += OnClientConnected;
                server.ClientDisconnected += OnClientDisconnected;
                server.ClientMessage += OnClientMessage;
                server.Error += OnError;

                try
                {
                    server.Start();
                }
                catch
                {
                    Utilities.Log("Unable to start named pipe server!");
                }
                Utilities.Log("Named pipe server started.");

            }
            
            private void InitializeComponent()
            {
                TrayIcon = new NotifyIcon();

                TrayIcon.BalloonTipIcon = ToolTipIcon.Info;
                TrayIcon.BalloonTipText = "I am monitoring your computer for fullscreen programs and idle time!";
                TrayIcon.Text = "IdleMon";


                //The icon is added to the project resources.
                //Here I assume that the name of the file is 'TrayIcon.ico'
                TrayIcon.Icon = IdleService.Properties.Resources.TrayIcon;

                //Optional - handle doubleclicks on the icon:
                TrayIcon.DoubleClick += TrayIcon_DoubleClick;

                //Optional - Add a context menu to the TrayIcon:
                TrayIconContextMenu = new ContextMenuStrip();
                CloseMenuItem = new ToolStripMenuItem();
                PauseMenuItem = new ToolStripMenuItem();
                TrayIconContextMenu.SuspendLayout();

                // 
                // TrayIconContextMenu
                // 
                this.TrayIconContextMenu.Items.AddRange(new ToolStripItem[] {
            this.CloseMenuItem, this.PauseMenuItem});
                this.TrayIconContextMenu.Name = "TrayIconContextMenu";
                this.TrayIconContextMenu.Size = new Size(153, 70);
                // 
                // CloseMenuItem
                // 
                this.CloseMenuItem.Name = "CloseMenuItem";
                this.CloseMenuItem.Size = new Size(152, 22);
                this.CloseMenuItem.Text = "Exit IdleMon";
                this.CloseMenuItem.Click += new EventHandler(this.CloseMenuItem_Click);
                // 
                // PauseMenuItem
                // 
                this.PauseMenuItem.Name = "PauseMenuItem";
                this.PauseMenuItem.Size = new Size(152, 22);
                this.PauseMenuItem.Text = "Pause mining";
                this.PauseMenuItem.Click += new EventHandler(this.PauseMenuItem_Click);

                TrayIconContextMenu.ResumeLayout(false);
                TrayIcon.ContextMenuStrip = TrayIconContextMenu;
            }

            private void OnApplicationExit(object sender, EventArgs e)
            {
                //Cleanup so that the icon will be removed when the application is closed
                TrayIcon.Visible = false;
            }

            private void TrayIcon_DoubleClick(object sender, EventArgs e)
            {
                //Here you can do stuff if the tray icon is doubleclicked
                TrayIcon.ShowBalloonTip(5000);
            }

            private void CloseMenuItem_Click(object sender, EventArgs e)
            {
                if (MessageBox.Show("Do you want to stop mining?",
                        "Are you sure?", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation,
                        MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                {
                    //todo: send a command to the service to terminate
                    Application.Exit();
                }
            }

            private void PauseMenuItem_Click(object sender, EventArgs e)
            {
                paused = (!paused);
                if (paused)
                {
                    PauseMenuItem.Text = "Resume mining";
                    TrayIcon.BalloonTipText = "Pausing all mining until logoff or manually resumed.";
                    //todo: Send resume command
                } else
                {
                    PauseMenuItem.Text = "Pause mining";
                    TrayIcon.BalloonTipText = "Mining will resume once you are detected as idle!";
                    //todo: Send pause command
                }
                TrayIcon.ShowBalloonTip(3000);

            }

            private static void OnTimedEvent(object source, ElapsedEventArgs e)
            {
                bool _isIdle = false;

                if (!lowOnly)
                {
                    try
                    {
                        //ipc.PublishAsync(CreateIdlePacket());
                        _isIdle = Utilities.IsIdle();

                        if (_isIdle == Utilities.lastState)
                            return;

                    }
                    catch (Exception ex)
                    {
                        //Utilities.Log("OnTimedEvent: " + ex.Message + Environment.NewLine + ex.StackTrace);
                        Utilities.Log("OnTimedEvent: " + ex.Message);
                        _isIdle = false;
                        lowOnly = true;
                    }
                }

                if (_isIdle == Utilities.lastState && sentFirstTime)
                    return;

                server.PushMessage(new IdleMessage
                {
                    Id = System.Diagnostics.Process.GetCurrentProcess().Id,
                    isIdle = _isIdle,
                    request = (int)PacketID.Idle,
                    data = Environment.UserName
                });
                sentFirstTime = true;
                Utilities.lastState = _isIdle;

            }

            private static void OnError(Exception exception)
            {
                Utilities.Log("idlePipe Err: " + exception.Message);
                timer.Stop();
            }

            private static void OnClientMessage(NamedPipeConnection<IdleMessage, IdleMessage> connection, IdleMessage message)
            {
                switch (message.request)
                {

                    case ((int)PacketID.Idle):
                        Utilities.Log("idle received from " + message.Id + ": " + message.isIdle);
                        break;
                }
            }

            private static void OnClientDisconnected(NamedPipeConnection<IdleMessage, IdleMessage> connection)
            {
                Utilities.Log(string.Format("idleMon Client {0} has disconnected.", connection.Id));
                timer.Stop();
            }

            private static void OnClientConnected(NamedPipeConnection<IdleMessage, IdleMessage> connection)
            {
                Utilities.Log(string.Format("idleMon Client {0} is now connected!", connection.Id));
                timer.Start();

                try
                {
                    server.PushMessage(new IdleMessage
                    {
                        Id = System.Diagnostics.Process.GetCurrentProcess().Id,
                        isIdle = Utilities.IsIdle(),
                        request = (int)PacketID.Hello,
                        data = Environment.UserName
                    });
                }
                catch
                {

                }

            }
        }
    }
}
