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
using idleMon;

namespace IdleMon
{
    class IdleMonContext : ApplicationContext
    {

        public enum PacketID
        {
            None,
            Hello,
            Goodbye,
            Idle,
            Pause,
            Resume,
            Stop
        }

        //Where the actual program starts
        private NotifyIcon TrayIcon;
        private ContextMenuStrip TrayIconContextMenu;
        private ToolStripMenuItem CloseMenuItem;
        private ToolStripMenuItem PauseMenuItem;

        //create the NamedPipe server for our Service communication
        private NamedPipeServer<IdleMessage> server = new NamedPipeServer<IdleMessage>(@"Global\MINERPIPE");
        private System.Timers.Timer timer = new System.Timers.Timer(3000);
        private System.Timers.Timer fullscreenTimer = new System.Timers.Timer(20000);

        private bool lowOnly;
        private bool sentFirstTime;
        private bool miningPaused;
        private bool monitorFullscreen;
        private bool fullscreenDetected;
        private bool connectedToService;

        public IdleMonContext()
        {
            InitializeComponent();

            if (!Program.stealthMode)
            {
                
                Application.ApplicationExit += new EventHandler(this.OnApplicationExit);
                TrayIcon.Visible = true;
                Utilities.Log("TrayIcon initialized.");

                //TrayIcon.ShowBalloonTip(3000);
            }

            timer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            fullscreenTimer.Elapsed += new ElapsedEventHandler(OnFullscreenTimer);

            server.ClientConnected += OnClientConnected;
            server.ClientDisconnected += OnClientDisconnected;
            server.ClientMessage += OnClientMessage;
            server.Error += OnError;

            try
            {
                server.Start();

                System.Timers.Timer myTimer = new System.Timers.Timer(3000);
                myTimer.Elapsed += delegate {
                    if (!connectedToService && !Program.stealthMode) {
                        TrayIcon.BalloonTipText = "Unable to connect to IdleService. Please make sure it is running!";
                        TrayIcon.BalloonTipIcon = ToolTipIcon.Error;
                        TrayIcon.ShowBalloonTip(3000);
                        }
                };
                myTimer.AutoReset = false;
                myTimer.Start();

            }
            catch
            {
                Utilities.Log("Unable to start named pipe server!");
            }
            Utilities.Log("Named pipe server started.");

        }

        private void OnFullscreenTimer(object sender, ElapsedEventArgs e)
        {
            fullscreenDetected = Utilities.IsForegroundFullScreen();
        }

        private void InitializeComponent()
        {
            TrayIcon = new NotifyIcon();

            TrayIcon.BalloonTipIcon = ToolTipIcon.Info;
            TrayIcon.BalloonTipText = "I am monitoring your computer for fullscreen programs and idle time!";
            TrayIcon.BalloonTipIcon = ToolTipIcon.None;
            TrayIcon.Text = "IdleMon";
            
            TrayIcon.Icon = IdleService.Properties.Resources.TrayIcon;

            //Optional - handle doubleclicks on the icon:
            TrayIcon.DoubleClick += TrayIcon_DoubleClick;

            //Optional - Add a context menu to the TrayIcon:
            TrayIconContextMenu = new ContextMenuStrip();
            CloseMenuItem = new ToolStripMenuItem();
            PauseMenuItem = new ToolStripMenuItem();
            TrayIconContextMenu.SuspendLayout();
            
            // TrayIconContextMenu
            this.TrayIconContextMenu.Items.AddRange(new ToolStripItem[] {
                    this.CloseMenuItem, this.PauseMenuItem});
            this.TrayIconContextMenu.Name = "TrayIconContextMenu";
            this.TrayIconContextMenu.Size = new Size(153, 70);
            
            // CloseMenuItem
            this.CloseMenuItem.Name = "CloseMenuItem";
            this.CloseMenuItem.Size = new Size(152, 22);
            this.CloseMenuItem.Text = "Exit IdleMon";
            this.CloseMenuItem.Click += new EventHandler(this.CloseMenuItem_Click);
            
            // PauseMenuItem
            this.PauseMenuItem.Name = "PauseMenuItem";
            this.PauseMenuItem.Size = new Size(152, 22);
            this.PauseMenuItem.Text = "Pause mining";
            this.PauseMenuItem.Click += new EventHandler(this.PauseMenuItem_Click);

            TrayIconContextMenu.ResumeLayout(false);
            TrayIcon.ContextMenuStrip = TrayIconContextMenu;
        }

        private void OnApplicationExit(object sender, EventArgs e)
        {
            StopIdleMon();
        }

        private void StopIdleMon()
        {
            //Cleanup so that the icon will be removed when the application is closed
            TrayIcon.Visible = false;

            //stop the PipeServer
            server.Stop();

            //and finally, exit IdleMon
            System.Environment.Exit(0);
        }

        private void TrayIcon_DoubleClick(object sender, EventArgs e)
        {
            //Show the last BalloonTip message
            TrayIcon.ShowBalloonTip(3000);
        }

        private void CloseMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("This will stop IdleService as well, if it is running and connected.\n\nAre you sure?",
                    "Stop Mining?", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation,
                    MessageBoxDefaultButton.Button2) == DialogResult.Yes)
            {
                //Can make this more graceful, but IdleService will kill IdleMon if stopping is successful.
                SendPipeMessage(PacketID.Stop, false);

                System.Timers.Timer myTimer = new System.Timers.Timer(3000);
                myTimer.Elapsed += delegate {
                    StopIdleMon();
                };
                myTimer.AutoReset = false;
                myTimer.Start();

            }
        }

        private void PauseMenuItem_Click(object sender, EventArgs e)
        {

            //PauseMining(!this.miningPaused);
            SendPipeMessage(PacketID.Pause, false, Environment.UserName, PacketID.Pause);

        }

        private void PauseMining(bool stateToSet, bool showTrayNotification = true)
        {
            this.miningPaused = (stateToSet);
            if (stateToSet)
            {
                PauseMenuItem.Text = "Resume mining";
                TrayIcon.BalloonTipText = "Pausing all mining until logoff or manually resumed.";
                TrayIcon.BalloonTipIcon = ToolTipIcon.Info;
            }
            else
            {
                PauseMenuItem.Text = "Pause mining";
                TrayIcon.BalloonTipText = "Mining will resume once you are detected as idle!";
                TrayIcon.BalloonTipIcon = ToolTipIcon.None;
            }

            if (showTrayNotification) TrayIcon.ShowBalloonTip(3000);
        }

        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            bool _isIdle = false;

            if (!lowOnly)
            {
                try
                {
                    _isIdle = Utilities.IsIdle();

                    if (_isIdle == Utilities.lastState)
                        return;

                }
                catch (Exception ex)
                {
                    Utilities.Log("OnTimedEvent: " + ex.Message);
                    _isIdle = false;
                    lowOnly = true;
                }
            }

            if (!miningPaused && fullscreenDetected)
            {
                SendPipeMessage(PacketID.Pause, _isIdle, Environment.UserName);
                sentFirstTime = false;
            }

            //If isIdle is the same as the lastState we sent, exit, unless we didn't send the first message yet.
            if (_isIdle == Utilities.lastState && sentFirstTime)
                return;

            //Send updated Idle status.
            SendPipeMessage(PacketID.Idle, _isIdle, Environment.UserName);

            sentFirstTime = true;
            Utilities.lastState = _isIdle;

        }

        private void OnError(Exception exception)
        {
            Utilities.Log("idlePipe Err: " + exception.Message);
            timer.Stop();
            connectedToService = false;
        }

        private void OnClientMessage(NamedPipeConnection<IdleMessage, IdleMessage> connection, IdleMessage message)
        {
            switch (message.packetId)
            {

                case ((int)PacketID.Pause):
                    Utilities.Log("Pause received from SYSTEM.");
                    PauseMining(true);
                    break;

                case ((int)PacketID.Resume):
                    Utilities.Log("Resume received from SYSTEM.");
                    PauseMining(false);
                    break;

                case ((int)PacketID.Hello):

                    if (message.requestId == (int)PacketID.Pause)
                    {
                        PauseMining(stateToSet: true, showTrayNotification: false);
                        TrayIcon.BalloonTipText = "Connected to IdleService! Mining is currently Paused.";
                        TrayIcon.BalloonTipIcon = ToolTipIcon.Warning;
                        TrayIcon.ShowBalloonTip(3000);
                    }

                    if (message.requestId == (int)PacketID.Resume)
                    {
                        PauseMining(stateToSet: false, showTrayNotification: false);
                        TrayIcon.BalloonTipText = "Connected to IdleService! Mining will resume once you are idle.";
                        TrayIcon.BalloonTipIcon = ToolTipIcon.None;
                        TrayIcon.ShowBalloonTip(3000);
                    }
                    break;

            }
        }

        private void OnClientDisconnected(NamedPipeConnection<IdleMessage, IdleMessage> connection)
        {
            Utilities.Log(string.Format("idleMon Client {0} has disconnected.", connection.Id));

            miningPaused = false;
            fullscreenDetected = false;
            connectedToService = false;

            fullscreenTimer.Stop();
            timer.Stop();
        }

        private void OnClientConnected(NamedPipeConnection<IdleMessage, IdleMessage> connection)
        {
            Utilities.Log(string.Format("idleMon Client {0} is now connected!", connection.Id));
            timer.Start();
            fullscreenTimer.Start();
            connectedToService = true;

            SendPipeMessage(PacketID.Hello, Utilities.IsIdle(), Environment.UserName, PacketID.None);
        }

        private void SendPipeMessage(PacketID packetId, bool isIdle = false, string data = "", PacketID requestId = PacketID.None)
        {
            try
            {
                server.PushMessage(new IdleMessage
                {
                    packetId = (int)packetId,
                    isIdle = isIdle,
                    requestId = (int)requestId,
                    data = data
                });
            }
            catch (Exception ex)
            {
                Utilities.Log("SendPipeMessage: " + ex.Message);
            }
        }

    }
}
