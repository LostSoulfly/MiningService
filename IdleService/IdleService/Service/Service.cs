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

        //--------- Configuration ----------
        
        private HostControl host;
        
        internal NamedPipeClient<IdleMessage> client = new NamedPipeClient<IdleMessage>(@"Global\MINERPIPE");
        private Timer timer = new Timer(5000);
        private Timer sessionTimer = new Timer(60000);
        private Timer apiCheckTimer = new Timer(10000);
        
        #region TopShelf Start/Stop/Abort
        public bool Start(HostControl hc)
        {
            Utilities.Log("Start");
            host = hc;

            if (!initialized)
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
                Utilities.KillProcess(sessionExeName);
                Utilities.KillProcess(minerExeName);
            }
            catch (Exception ex)
            {
                Utilities.Log(ex.Message);
            }

            connected = false;
            timer.Start();
            sessionTimer.Start();
            apiCheckTimer.Start();
            
            client.Start();
            currentSession = ProcessExtensions.GetSession();

            CheckForSystem(currentSession);

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
            running = false;

            Utilities.AllowSleep();
            Utilities.KillProcess(minerExeName);
            Utilities.KillProcess(sessionExeName);
            Utilities.Log("Stopped!");
        }

        public void Abort()
        {
            host.Stop();
        }
        #endregion

        #region NamedPipe Events
        private void OnServerDisconnect(NamedPipeConnection<IdleMessage, IdleMessage> connection)
        {
            Utilities.Log("IdleService Pipe disconnected");
            connected = false;
            //pipeTimer.Start();
        }

        private void OnError(Exception exception)
        {
            Utilities.Log("IdleService Pipe Err: " + exception.Message);
            connected = false;

            client.Stop();
            client.Start();

            //pipeTimer.Start();
        }

        private void OnServerMessage(NamedPipeConnection<IdleMessage, IdleMessage> connection, IdleMessage message)
        {
            sessionFail = 0;
            connected = true;
            switch (message.request)
            {
                case ((int)PacketID.idle):
                    Utilities.Log("Idle received from " + message.Id + ": " + message.isIdle);

                    if (isLoggedIn)
                        isIdle = message.isIdle;

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
                    //pipeTimer.Stop();
                    Utilities.Log("idleMon user " + message.data + " connected " + message.Id);
                    isIdle = message.isIdle;
                    break;

                default:
                    //Utilities.Log("IdleService Idle default: " + message.request);
                    break;
            }
        }
#endregion
        
        public void SessionChanged(SessionChangedArguments args)
        {
            Utilities.KillProcess(sessionExeName);
            switch (args.ReasonCode)
            {
                case Topshelf.SessionChangeReasonCode.SessionLock:
                    Utilities.Log(string.Format("Session: {0} - Reason: {1} - Lock", args.SessionId, args.ReasonCode));
                    isLoggedIn = false;
                    currentSession = args.SessionId;
                    isIdle = true;
                    break;

                case Topshelf.SessionChangeReasonCode.SessionLogoff:
                    isLoggedIn = false;
                    Utilities.Log(string.Format("Session: {0} - Reason: {1} - Logoff", args.SessionId, args.ReasonCode));
                    currentSession = 0;
                    isIdle = true;
                    break;

                case Topshelf.SessionChangeReasonCode.SessionUnlock:
                    isLoggedIn = true;
                    Utilities.Log(string.Format("Session: {0} - Reason: {1} - Unlock", args.SessionId, args.ReasonCode));
                    currentSession = args.SessionId;
                    isIdle = false;
                    break;

                case Topshelf.SessionChangeReasonCode.SessionLogon:
                    isLoggedIn = true;
                    Utilities.Log(string.Format("Session: {0} - Reason: {1} - Login", args.SessionId, args.ReasonCode));
                    currentSession = args.SessionId;
                    isIdle = false;
                    break;

                case Topshelf.SessionChangeReasonCode.RemoteDisconnect:
                    isLoggedIn = false;
                    Utilities.Log(string.Format("Session: {0} - Reason: {1} - RemoteDisconnect", args.SessionId, args.ReasonCode));
                    currentSession = ProcessExtensions.GetSession();
                    if (currentSession > 0)
                        isLoggedIn = true;
                    isIdle = true;
                    break;

                case Topshelf.SessionChangeReasonCode.RemoteConnect:
                    isLoggedIn = true;
                    Utilities.Log(string.Format("Session: {0} - Reason: {1} - RemoteConnect", args.SessionId, args.ReasonCode));
                    currentSession = ProcessExtensions.GetSession();
                    isIdle = false;
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
                    //Utilities.Log("Power changed"); // ie. weak battery
                    break;

                default:
                    Utilities.Log("OnPowerChange: " + e.ToString());
                    break;
            }
        }
        
        private void Initialize()
        {

            Utilities.Log("Starting IdleService.. Proc: " + Environment.ProcessorCount);
            
            SetupFiles();
            
            if (Utilities.DoesBatteryExist())
                Utilities.Log("Battery found");
                                    
            timer.Elapsed += OnTimedEvent;
            sessionTimer.Elapsed += OnSessionTimer;
            //apiCheckTimer.Elapsed += OnApiTimer;
            
            timer.AutoReset = true;
            //apiCheckTimer.AutoReset = true;

            //apiCheckTimer.Start();
            //pipeTimer.Elapsed += OnPipeTimerEvent;
            
            initialized = true;

            client = new NamedPipeClient<IdleMessage>(@"Global\BFXMRPIPE");


        /*
          // No longer necessary, as we are using an AppContext instead of just a Main, so it will keep running anyway.
        while (true)
        {
            Thread.Sleep(1);
        }
        */
    }
        #region Old API Json reading section (not used)
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
                if (running != true)
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

                /*
                if (test.hashrate.total.Average() <= 5)
                    failHashrate++;
                else
                    failHashrate = 0;
                */

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
#endregion

        #region Timers/Events
        private void OnSessionTimer(object sender, ElapsedEventArgs e)
        {
            currentSession = ProcessExtensions.GetSession();

            CheckForSystem(currentSession);

            //Utilities.Log(string.Format("Session: {0} - isLoggedIn: {1} - connected: {2} - sessionFail: {3} - isIdle: {4}", currentSession, isLoggedIn, connected, sessionFail, isIdle));

            if (sessionFail > 3)
            {
                Utilities.Log("sessionFail > 3");
                host.Stop();
                return;
            }

            if (isLoggedIn && !connected)
            {
                sessionFail++;
                Utilities.KillProcess(sessionExeName);
                ProcessExtensions.StartProcessAsCurrentUser(sessionExe, null, null, false);
                Utilities.Log("Starting IdleMon in " + currentSession);
            }
            else if (!isLoggedIn && connected)
            {
                sessionFail = 0;
                Utilities.KillProcess(sessionExeName);
            }
            else if (!isLoggedIn)
            {
                sessionFail = 0;
                
                if (currentSession > 0)
                    isLoggedIn = true;
            }
        }

        private void OnTimedEvent(object sender, ElapsedEventArgs e)
        {

            //Utilities.Log("CPU Usage: " + Utilities.GetCpuUsage().ToString() + "%");
            lock (timeLock)
            {
                //Utilities.Log(string.Format("OnTimedEvent. {0} - {1}", running, isIdle));
                bool isRunning = Running(minerExeName);
                //Utilities.Log("1 " + isRunning + " " + minerExeName);
                

                //if (isRunning && (object.ReferenceEquals(null, process) == false))
                //    if (process.Responding == false)
                //        Utilities.Log("process not responding.", "-nr");

                                
                if (fail > 5)
                {
                    Utilities.Log("Fail >5");
                    Abort();
                }
                
                if (Utilities.DoesBatteryExist() && !Utilities.IsBatteryFull())
                {
                    if (isRunning)
                    {
                        Utilities.Log("Battery level is not full");
                        Utilities.KillProcess();
                    }
                    return;
                }
                
               //todo: Need to do a 60 second average of this, sometimes spikes happen and can cause
               //unnecessary opening/closing of the miner!
                if ((Utilities.GetCpuUsage() > 97) && !idleMode && isRunning)
                {
                    //Utilities.Log("High CPU usage, bail..");
                    if (Utilities.GetCpuUsage() > 98)
                        Utilities.KillProcess();

                    return;
                }
                
                if (running && !isRunning)
                {
                    //it should be running, but isn't.
                    Utilities.Log("Restarting m..");
                    StartMiner(!idleMode);
                    fail++;
                    return;
                }

                if (!running && !isRunning)
                {
                    Utilities.Log("Not running!");
                    StartMiner(true);
                    fail++;
                    return;
                }
                
                if (isRunning && !running)
                {
                    running = true;
                    return;
                }

                fail = 0;
                
                if (isIdle)
                {
                    if (!idleMode)
                    {
                        //Utilities.Log("Changing status: idle.");
                        Utilities.KillProcess();
                        if (Utilities.IsBatteryFull())
                        {
                            StartMiner(false);
                        }
                        else
                        {
                            StartMiner(true);
                            idleMode = true;
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
                    if (idleMode)
                    {
                        //Utilities.Log("Changing status: not idle. Low power config running.");
                        Utilities.KillProcess();
                        StartMiner(true);
                        idleMode = false;
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

            lock (startLock)
            {
                int pid;

                //Utilities.Log("StartM..");

                if (!File.Exists(minerExe))
                {
                    Utilities.Log(minerExe + " doesn't exist");
                    Abort();
                }
                if (Running(minerExeName))
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
                        idleMode = false;
                    }
                    else
                    {
                        //var count = Environment.ProcessorCount;
                        //todo: Launch all miners from IDLE CPU MODE (High speed mode) from list
                        
                        //Utilities.Log("Started idlecpu mining: " + pid);
                        idleMode = true;
                    }

                    //Sets whether the miner is running based on Process ID from the Launch method
                    running = (pid > 0);

                    Utilities.Log("IdleMode: " + idleMode + " - running: " + running);
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
            sessionExe = Utilities.ApplicationPath() + "IdleMon.exe";
            sessionExeName = Path.GetFileNameWithoutExtension(sessionExe);
            //minerExeName = Path.GetFileNameWithoutExtension(minerExe);
            //string args = string.Format("xmrig -o 127.0.0.1:9001 -u MONEROADDRESS.{0} -p x -k --safe --max-cpu-usage=90 -B --nicehash", System.Environment.MachineName);
            //string args = File.ReadAllText();
            //idleConfig = args;

            //lowCpuConfig = string.Format("xmrig -o 127.0.0.1:9001 -u MONEROADDRESS.{0} -p x -k --max-cpu-usage=25 -B --nicehash", System.Environment.MachineName);
            //Utilities.Log(args);
            //Utilities.Log("bfdef complete");


            if (!File.Exists(minerExe))
            {
                Utilities.Log("1");
                Abort();
            }
            
        }
                
    }
}
