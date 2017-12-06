using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using Topshelf;
using System.Timers;
using NamedPipeWrapper;
using Message;
using System.IO.Pipes;
using System.Security.Principal;
using System.Security.AccessControl;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;

namespace IdleService
{
    class MyService
    {

        enum PacketID
        {
            hello, //should include the logged in username
            goodbye,
            idle,
            active,
            idletime,
            battery,
            cpu,
            internet,
            pause,
            resume
        }

        #region Json API for XMR-STAK-CPU only
        public class Hashrate
        {
            public List<List<double?>> threads { get; set; }
            public List<double?> total { get; set; }
            public double highest { get; set; }
        }

        public class Results
        {
            public int diff_current { get; set; }
            public int shares_good { get; set; }
            public int shares_total { get; set; }
            public double avg_time { get; set; }
            public int hashes_total { get; set; }
            public List<int> best { get; set; }
            public List<object> error_log { get; set; }
        }

        public class Connection
        {
            public string pool { get; set; }
            public int uptime { get; set; }
            public int ping { get; set; }
            public List<object> error_log { get; set; }
        }

        public class XmrRoot
        {
            public Hashrate hashrate { get; set; }
            public Results results { get; set; }
            public Connection connection { get; set; }
        }
        #endregion

        //TopShelf service controller
        private HostControl host;

        //Pipe that is used to connect to the IdleMon running in the user's desktop session
        internal NamedPipeClient<IdleMessage> client; // = new NamedPipeClient<IdleMessage>(@"Global\MINERPIPE");
        private Timer timer = new Timer(5000);
        private Timer sessionTimer = new Timer(60000);
        private Timer apiCheckTimer = new Timer(10000);
        
        #region TopShelf Start/Stop/Abort
        public bool Start(HostControl hc)
        {
            Utilities.Log("Starting service");
            host = hc;

            if (!Config.serviceInitialized)
            {
                Utilities.Log("isSys: " + Utilities.IsSystem() + " - " + Environment.UserName);
                SystemEvents.PowerModeChanged += OnPowerChange;
                Initialize();
                client.ServerMessage += OnServerMessage;
                client.Error += OnError;
                client.Disconnected += OnServerDisconnect;
            }

            try
            {
                //Utilities.KillProcess(sessionExeName);
                //Utilities.KillProcess(minerExeName);
            }
            catch (Exception ex)
            {
                Utilities.Log(ex.Message);
            }

            Config.isPipeConnected = false;
            timer.Start();
            sessionTimer.Start();
            apiCheckTimer.Start();
            
            client.Start();
            Config.currentSessionId = ProcessExtensions.GetSession();

            Utilities.CheckForSystem(Config.currentSessionId);

            Utilities.Log("Service running");
            return true;
        }

        public void Stop()
        {
            // write code here that runs when the Windows Service stops. 
            Utilities.Log("Stopping..");
            timer.Stop();
            sessionTimer.Stop();
            apiCheckTimer.Stop();
            client.Stop();
            //pipeTimer.Stop();
            Config.isCurrentlyMining = false;

            Utilities.AllowSleep();
            Utilities.KillProcess("");
            Utilities.KillProcess("");
            Utilities.Log("Stopped!");
        }

        public void Abort()
        {
            host.Stop();
        }

        private void Initialize()
        {

            Utilities.Log("Initializing IdleService.. CPU Cores: " + Environment.ProcessorCount);

            SetupFiles();

            if (Utilities.DoesBatteryExist())
            {
                Config.doesBatteryExist = true;
                Utilities.Log("Battery found");
            }

            timer.Elapsed += OnTimedEvent;
            sessionTimer.Elapsed += OnSessionTimer;

            timer.AutoReset = true;

            Config.serviceInitialized = true;

            client = new NamedPipeClient<IdleMessage>(@"Global\MINERPIPE");

        }

        #endregion

        #region NamedPipe Events
        private void OnServerDisconnect(NamedPipeConnection<IdleMessage, IdleMessage> connection)
        {
            Utilities.Log("IdleService Pipe disconnected");
            Config.isPipeConnected = false;
        }

        private void OnError(Exception exception)
        {
            Utilities.Log("IdleService Pipe Err: " + exception.Message);
            Config.isPipeConnected = false;

            client.Stop();
            client.Start();
            
        }

        private void OnServerMessage(NamedPipeConnection<IdleMessage, IdleMessage> connection, IdleMessage message)
        {
            Config.sessionLaunchAttempts = 0;
            Config.isPipeConnected = true;
            switch (message.request)
            {
                case ((int)PacketID.idle):
                    Utilities.Log("Idle received from " + message.Id + ": " + message.isIdle);

                    if (Config.isUserLoggedIn)
                        Config.isUserIdle = message.isIdle;

                    /*
                    connection.PushMessage(new IdleMessage
                    {
                        Id = System.Diagnostics.Process.GetCurrentProcess().Id,
                        isIdle = false,
                        request = (int)PacketID.idle
                    });
                    */

                    break;

                case ((int)PacketID.pause):

                    //pause mining until logoff or resumed manually
                    break;

                case ((int)PacketID.resume):

                    //resume mining
                    break;                    

                case ((int)PacketID.hello):
                    Utilities.Log("idleMon user " + message.data + " connected " + message.Id);
                    Config.isUserIdle = message.isIdle;
                    break;

                default:
                    //Utilities.Log("IdleService Idle default: " + message.request);
                    break;
            }
        }
#endregion
        
        public void SessionChanged(SessionChangedArguments args)
        {
            //todo Put this somewhere better
            Utilities.KillMiners();

            switch (args.ReasonCode)
            {
                case Topshelf.SessionChangeReasonCode.SessionLock:
                    Utilities.Log(string.Format("Session: {0} - Reason: {1} - Lock", args.SessionId, args.ReasonCode));
                    Config.isUserLoggedIn = false;
                    Config.currentSessionId = args.SessionId;
                    Config.isUserIdle = true;
                    break;

                case Topshelf.SessionChangeReasonCode.SessionLogoff:
                    Config.isUserLoggedIn = false;
                    Utilities.Log(string.Format("Session: {0} - Reason: {1} - Logoff", args.SessionId, args.ReasonCode));
                    Config.currentSessionId = 0;
                    Config.isUserIdle = true;
                    break;

                case Topshelf.SessionChangeReasonCode.SessionUnlock:
                    Config.isUserLoggedIn = true;
                    Utilities.Log(string.Format("Session: {0} - Reason: {1} - Unlock", args.SessionId, args.ReasonCode));
                    Config.currentSessionId = args.SessionId;
                    Config.isUserIdle = false;
                    break;

                case Topshelf.SessionChangeReasonCode.SessionLogon:
                    Config.isUserLoggedIn = true;
                    Utilities.Log(string.Format("Session: {0} - Reason: {1} - Login", args.SessionId, args.ReasonCode));
                    Config.currentSessionId = args.SessionId;
                    Config.isUserIdle = false;
                    break;

                case Topshelf.SessionChangeReasonCode.RemoteDisconnect:
                    Config.isUserLoggedIn = false;
                    Utilities.Log(string.Format("Session: {0} - Reason: {1} - RemoteDisconnect", args.SessionId, args.ReasonCode));
                    Config.currentSessionId = ProcessExtensions.GetSession();
                    if (Config.currentSessionId > 0)
                        Config.isUserLoggedIn = true;
                    Config.isUserIdle = true;
                    break;

                case Topshelf.SessionChangeReasonCode.RemoteConnect:
                    Config.isUserLoggedIn = true;
                    Utilities.Log(string.Format("Session: {0} - Reason: {1} - RemoteConnect", args.SessionId, args.ReasonCode));
                    Config.currentSessionId = ProcessExtensions.GetSession();
                    Config.isUserIdle = false;
                    break;

                default:
                    Utilities.Log(string.Format("Session: {0} - Other - Reason: {1}", args.SessionId, args.ReasonCode));
                    break;
            }

        }

        private void OnPowerChange(object sender, PowerModeChangedEventArgs e)
        {
            switch (e.Mode)
            {
                case PowerModes.Resume:
                    Utilities.Log("Resuming");
                    Start(host);
                    break;
                case PowerModes.Suspend:
                    Utilities.Log("Suspending");
                    Stop();
                    break;
                case PowerModes.StatusChange:
                    Utilities.Log("Power changed"); // ie. weak battery
                    break;

                default:
                    Utilities.Log("OnPowerChange: " + e.ToString());
                    break;
            }
        }
        
        #region Old API Json reading section (not used)
        /*
        private async Task<String> getTestObjects(string url)
        {
            var httpClient = new HttpClient();
            httpClient.Timeout = System.TimeSpan.FromMilliseconds(2000);
            var response = await httpClient.GetAsync(url);
            var result = await response.Content.ReadAsStringAsync();

            return result;
        }

        
        private void OnApiTimer(object sender, ElapsedEventArgs e)
        {

            //this timer is disabled, but just in case let's return out of it asap as it's not setup for xmrig
            return;

            try
            {
                if (Config.isCurrentlyMining != true)
                    return;

                XmrRoot test = JsonConvert.DeserializeObject<XmrRoot>(getTestObjects("http://127.0.0.1:16000/api.json").Result);

                //Utilities.Log("Pool: " + test.connection.pool);
                //Utilities.Log("Average Hashrate: " + test.hashrate.total.Average());
                //Utilities.Log("Highest Hashrate: " + test.hashrate.highest);
                //Utilities.Log("Shares Acc: " + test.results.shares_good);
                //Utilities.Log("Uptime: " + test.connection.uptime);
                //Utilities.Log("Ping: " + test.connection.ping);

                if (test.connection.uptime == 0)
                    failUptime++;
                else
                    failUptime = 0;

                
                //if (test.hashrate.total.Average() <= 5)
                //    failHashrate++;
                //else
                //    failHashrate = 0;
                

                if (failUptime >= 5 || failHashrate >= 5)
                {
                    failUptime = 0;
                    failHashrate = 0;
                    Utilities.KillProcess();

                }

                //Utilities.Log(failHashrate + " - " + failUptime);

            } catch (Exception ex)
            {
                Utilities.Log("api: " + ex.Message);
            }
        }
        */
#endregion

        #region Timers/Events
        private void OnSessionTimer(object sender, ElapsedEventArgs e)
        {
            Config.currentSessionId = ProcessExtensions.GetSession();

            Utilities.CheckForSystem(Config.currentSessionId);

            //Utilities.Log(string.Format("Session: {0} - isLoggedIn: {1} - connected: {2} - sessionFail: {3} - isIdle: {4}", currentSession, isLoggedIn, connected, sessionFail, isIdle));

            if (Config.sessionLaunchAttempts > 3)
            {
                Utilities.Log("sessionFail > 3");
                host.Stop();
                return;
            }

            if (Config.isUserLoggedIn && !Config.isPipeConnected)
            {
                Config.sessionLaunchAttempts++;
                Utilities.KillProcess("");
                ProcessExtensions.StartProcessAsCurrentUser("", null, null, false);
                Utilities.Log("Starting IdleMon in " + Config.currentSessionId);
            }
            else if (!Config.isUserLoggedIn && Config.isPipeConnected)
            {
                Config.sessionLaunchAttempts = 0;
                Utilities.KillProcess("");
            }
            else if (!Config.isUserLoggedIn)
            {
                Config.sessionLaunchAttempts = 0;
                
                if (Config.currentSessionId > 0)
                    Config.isUserLoggedIn = true;
            }
        }

        private void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            
            lock (Config.timeLock)
            {
                //todo: Basically rewrite this whole mess.
                bool cpuMinersRunning = Utilities.AreMinersRunning(Config.settings.cpuMiners);
                bool gpuMinersRunning = Utilities.AreMinersRunning(Config.settings.gpuMiners);

                if (Config.minerLaunchAttempts > 5)
                {
                    Utilities.Log("Failure to start >5");
                    Abort();
                }
                
                if (Config.doesBatteryExist && !Utilities.IsBatteryFull())
                {
                    if (isRunning)
                    {
                        Utilities.Log("Battery level is not full");
                        Utilities.KillProcess("");
                    }
                    return;
                }
                
               //todo: Need to do a 60 second average of this, sometimes spikes happen and can cause
               //unnecessary opening/closing of the miner!
                if ((Utilities.GetCpuUsage() > 97) && !Config.isUserIdle && isRunning)
                {
                    //Utilities.Log("High CPU usage, bail..");
                    if (Utilities.GetCpuUsage() > 98)
                        Utilities.KillProcess("");

                    return;
                }
                
                if (Config.isCurrentlyMining && !isRunning)
                {
                    //it should be running, but isn't.
                    Utilities.Log("Restarting m..");
                    StartMiner(!Config.isUserIdle);
                    Config.minerLaunchAttempts++;
                    return;
                }

                if (!Config.isCurrentlyMining && !isRunning)
                {
                    Utilities.Log("Not running!");
                    StartMiner(true);
                    Config.minerLaunchAttempts++;
                    return;
                }
                
                if (isRunning && !Config.isCurrentlyMining)
                {
                    Config.isCurrentlyMining = true;
                    return;
                }

                Config.minerLaunchAttempts = 0;
                
                if (Config.isUserIdle)
                {
                    if (!Config.isIdleMining)
                    {
                        //Utilities.Log("Changing status: idle.");
                        Utilities.KillProcess("");
                        if (Utilities.IsBatteryFull())
                        {
                            StartMiner(false);
                        }
                        else
                        {
                            StartMiner(true);
                            Config.isIdleMining = true;
                        }
                        return;
                    }
                    else
                    {
                        //Utilities.Log("Not changing status.1");
                    }
                }
                else
                {
                    if (Config.isIdleMining)
                    {
                        //Utilities.Log("Changing status: not idle. Low power config running.");
                        Utilities.KillProcess("");
                        StartMiner(true);
                        Config.isIdleMining = false;
                        return;
                    }
                    else
                    {
                        //Utilities.Log("Not changing status.2");
                    }
                }
            }
        }
#endregion
        
        public void StartMiner(bool lowCpu)
        {

            lock (Config.startLock)
            {
                int pid = 0;

                //Utilities.Log("StartM..");

                if (!File.Exists(""))
                {
                    Utilities.Log("" + " doesn't exist");
                    Abort();
                }
                if (Utilities.IsProcessRunning(""))
                {
                    Utilities.Log("Already running, but startm?");
                    return;
                }

                try
                {
                    if (lowCpu)
                    {

                        //todo: Launch all miners in LOW CPU MODE from list
                        //pid = LaunchProcess(minerExe, lowCpuConfig);
                        //Utilities.Log("Started lowcpu mining: " + pid);
                        Config.isIdleMining = false;
                    }
                    else
                    {
                        //var count = Environment.ProcessorCount;
                        //todo: Launch all miners from IDLE CPU MODE (High speed mode) from list
                        
                        //Utilities.Log("Started idlecpu mining: " + pid);
                        Config.isIdleMining = true;
                    }

                    //Sets whether the miner is running based on Process ID from the Launch method
                    Config.isCurrentlyMining = (pid > 0);

                    Utilities.Log("Config.isIdleMining: " + Config.isIdleMining + " - running: " + Config.isCurrentlyMining);
                }
                catch (Exception ex)
                {
                    Utilities.Log("cpu ex:" + ex.Message + Environment.NewLine + ex.StackTrace);
                    
                    if (ex.Message.Contains("platform"))
                    {
                        Abort();
                    }
                }
            }
        }
        
        public void Uninstall()
        {
            //Run an external batch file to clean up the miner and remove all traces of it
            //todo: this, and move to Utilities
        }
        
        //todo:
        //rewrite this to read mining programs from two files, low cpu and idle cpu.
        //Have each line be a complete program w/ args
        //store these in a list, one for low/idle cpu configs
        //when launching or stopping mining, loop through all the list items to start/stop them.
        //Check for CPU temperature and potentially throttle down
        //Change to LoadFiles or something
        public void SetupFiles()
        {
            
            //todo: redo this section with config files; move this to Utilities class
            //sessionExe = Utilities.ApplicationPath() + "IdleMon.exe";
            //sessionExeName = Path.GetFileNameWithoutExtension(sessionExe);
            //minerExeName = Path.GetFileNameWithoutExtension(minerExe);
            //string args = string.Format("xmrig -o 127.0.0.1:9001 -u MONEROADDRESS.{0} -p x -k --safe --max-cpu-usage=90 -B --nicehash", System.Environment.MachineName);
            //string args = File.ReadAllText();
            //idleConfig = args;

            //lowCpuConfig = string.Format("xmrig -o 127.0.0.1:9001 -u MONEROADDRESS.{0} -p x -k --max-cpu-usage=25 -B --nicehash", System.Environment.MachineName);
            //Utilities.Log(args);
            //Utilities.Log("bfdef complete");

            /*
            if (!File.Exists(""))
            {
                Utilities.Log("1");
                Abort();
            }
            */
            
        }
                
    }
}
