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

        public enum PacketID
        {
            None,
            Hello,
            Goodbye,
            Idle,
            Pause,
            Resume,
            Stop,
            Stealth,
            Log,
            Fullscreen,
            IdleTime
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
        private Timer minerTimer = new Timer(7000);
        private Timer sessionTimer = new Timer(30000);
        //private Timer apiCheckTimer = new Timer(10000);
        
        #region TopShelf Start/Stop/Abort
        public bool Start(HostControl hc)
        {
            Utilities.Log("Starting IdleService");
            host = hc;

            if (!Config.configInitialized)
            {
                Utilities.Log("Configuration not loaded; something went wrong!", force: true);
                //host.Stop();
                return false;
            }

            //These only need to be set up once, and this may get called again if the system
            //wakes up from sleep, so we make sure it is only initialized once.
            if (!Config.serviceInitialized)
            {
                Utilities.Log("Initializing IdleService.. CPU Cores: " + Environment.ProcessorCount);

                if (Utilities.DoesBatteryExist())
                {
                    Config.doesBatteryExist = true;
                    Utilities.Log("Battery found. IsBatteryFull: " + Utilities.IsBatteryFull());
                } else
                {
                    Utilities.Debug("No battery found.");
                }

                SystemEvents.PowerModeChanged += OnPowerChange;
                minerTimer.Elapsed += OnMinerTimerEvent;
                sessionTimer.Elapsed += OnSessionTimer;

                minerTimer.AutoReset = true;

                //setup the NamedPipeClient and events 
                client = new NamedPipeClient<IdleMessage>(@"Global\MINERPIPE");
                client.ServerMessage += OnServerMessage;
                client.Error += OnError;
                client.Disconnected += OnServerDisconnect;
                
                Utilities.Log("IdleService Initialized. Is SYSTEM: " + Utilities.IsSystem() + ". User: " + Environment.UserName);
                Config.serviceInitialized = true;
            }

            //Kill and miners and IdleMons that may be running, just in case!
            Utilities.KillMiners();
            Utilities.KillProcess(Config.idleMonExecutable);

            Config.isPipeConnected = false;
            minerTimer.Start();
            sessionTimer.Start();
            //apiCheckTimer.Start();

            //Let's try to start IdleMon now
            CheckSession();

            //start attempting to connect to IdleMon through a NamedPipe
            client.Start();

            if (Config.settings.preventSleep)
                Utilities.PreventSleep();

            Config.currentSessionId = ProcessExtensions.GetSession();
            Utilities.CheckForSystem(Config.currentSessionId);

            Utilities.Log("IdleService is running");
            return true;
        }

        public void Stop()
        {

            Utilities.Log("Stopping IdleService..");
            minerTimer.Stop();
            sessionTimer.Stop();
            //apiCheckTimer.Stop();
            client.Stop();

            Config.isCurrentlyMining = false;

            if (Config.settings.preventSleep)
                Utilities.AllowSleep();

            Utilities.KillMiners();
            Utilities.KillProcess(Config.idleMonExecutable);
            Utilities.Log("Successfully stopped IdleService.");
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
            Config.isPipeConnected = false;
        }

        private void OnError(Exception exception)
        {
            Utilities.Debug("IdleService Pipe Err: " + exception.Message);
            Config.isPipeConnected = false;

            client.Stop();
            client.Start();
            
        }

        private void OnServerMessage(NamedPipeConnection<IdleMessage, IdleMessage> connection, IdleMessage message)
        {
            Config.sessionLaunchAttempts = 0;
            Config.isPipeConnected = true;
            switch (message.packetId)
            {
                case ((int)PacketID.Idle):
                    Utilities.Debug("Idle received from " + message.data + ": " + message.isIdle);

                    if (Config.isUserLoggedIn)
                    {
                        Config.isUserIdle = message.isIdle;
                        OnMinerTimerEvent(minerTimer, null);    //call the minerTime event immediately to process the change.
                    }
                    break;

                case ((int)PacketID.Pause):
                    Config.isMiningPaused = true;
                    Utilities.KillMiners();
                    Utilities.Log("Mining has been paused by IdleMon.");

                    connection.PushMessage(new IdleMessage
                    {
                        packetId = (int)PacketID.Pause,
                        isIdle = false,
                        requestId = (int)PacketID.None,
                        data = ""
                    });

                    break;

                case ((int)PacketID.Resume):
                    Config.isMiningPaused = false;
                    Utilities.Log("Mining has been resumed by IdleMon.");

                    connection.PushMessage(new IdleMessage
                    {
                        packetId = (int)PacketID.Resume,
                        isIdle = false,
                        requestId = (int)PacketID.None,
                        data = ""
                    });

                    break;

                case ((int)PacketID.Stop):

                    //stop all service timers and etc
                    Stop();

                    //actually call host.Stop
                    Abort();

                    break;

                case ((int)PacketID.Hello):
                    Utilities.Log("idleMon user " + message.data + " connected.");
                    Config.isUserIdle = message.isIdle;

                    if (Config.isMiningPaused)
                    {
                        connection.PushMessage(new IdleMessage
                        {
                            packetId = (int)PacketID.Hello,
                            isIdle = false,
                            requestId = (int)PacketID.Pause,
                            data = ""
                        });
                    } else
                    {
                        connection.PushMessage(new IdleMessage
                        {
                            packetId = (int)PacketID.Hello,
                            isIdle = false,
                            requestId = (int)PacketID.Resume,
                            data = ""
                        });
                    }

                    connection.PushMessage(new IdleMessage
                    {
                        packetId = (int)PacketID.Log,
                        isIdle = Config.settings.enableLogging,
                        requestId = (int)PacketID.None,
                        data = ""
                    });

                    connection.PushMessage(new IdleMessage
                    {
                        packetId = (int)PacketID.Stealth,
                        isIdle = Config.settings.stealthMode,
                        requestId = (int)PacketID.None,
                        data = ""
                    });

                    connection.PushMessage(new IdleMessage
                    {
                        packetId = (int)PacketID.IdleTime,
                        isIdle = false,
                        requestId = (int)PacketID.None,
                        data = Config.settings.minutesUntilIdle.ToString()
                    });

                    connection.PushMessage(new IdleMessage
                    {
                        packetId = (int)PacketID.Fullscreen,
                        isIdle = Config.settings.monitorFullscreen,
                        requestId = (int)PacketID.None,
                        data = ""
                    });
                    break;

                default:
                    Utilities.Debug("IdleService Idle default: " + message.packetId);
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
                    Utilities.Debug(string.Format("Session: {0} - Reason: {1} - Lock", args.SessionId, args.ReasonCode));
                    Config.isUserLoggedIn = false;
                    Config.currentSessionId = args.SessionId;
                    Config.isUserIdle = true;
                    break;

                case Topshelf.SessionChangeReasonCode.SessionLogoff:
                    Config.isUserLoggedIn = false;
                    Utilities.Debug(string.Format("Session: {0} - Reason: {1} - Logoff", args.SessionId, args.ReasonCode));
                    Config.currentSessionId = 0;
                    Config.isUserIdle = true;
                    break;

                case Topshelf.SessionChangeReasonCode.SessionUnlock:
                    Config.isUserLoggedIn = true;
                    Utilities.Debug(string.Format("Session: {0} - Reason: {1} - Unlock", args.SessionId, args.ReasonCode));
                    Config.currentSessionId = args.SessionId;
                    Config.isUserIdle = false;
                    break;

                case Topshelf.SessionChangeReasonCode.SessionLogon:
                    Config.isUserLoggedIn = true;
                    Utilities.Debug(string.Format("Session: {0} - Reason: {1} - Login", args.SessionId, args.ReasonCode));
                    Config.currentSessionId = args.SessionId;
                    Config.isUserIdle = false;
                    break;

                case Topshelf.SessionChangeReasonCode.RemoteDisconnect:
                    Config.isUserLoggedIn = false;
                    Utilities.Debug(string.Format("Session: {0} - Reason: {1} - RemoteDisconnect", args.SessionId, args.ReasonCode));
                    Config.currentSessionId = ProcessExtensions.GetSession();
                    if (Config.currentSessionId > 0)
                        Config.isUserLoggedIn = true;
                    Config.isUserIdle = true;
                    break;

                case Topshelf.SessionChangeReasonCode.RemoteConnect:
                    Config.isUserLoggedIn = true;
                    Utilities.Debug(string.Format("Session: {0} - Reason: {1} - RemoteConnect", args.SessionId, args.ReasonCode));
                    Config.currentSessionId = ProcessExtensions.GetSession();
                    Config.isUserIdle = false;
                    break;

                default:
                    Utilities.Debug(string.Format("Session: {0} - Other - Reason: {1}", args.SessionId, args.ReasonCode));
                    break;
            }

        }

        private void OnPowerChange(object sender, PowerModeChangedEventArgs e)
        {
            switch (e.Mode)
            {
                case PowerModes.Resume:
                    Utilities.Debug("Resuming service");
                    Start(host);
                    break;

                case PowerModes.Suspend:
                    Utilities.Debug("Suspending service");
                    Stop();
                    break;

                case PowerModes.StatusChange:
                    //Utilities.Debug("Power changed: " + e.Mode.ToString()); // ie. weak battery
                    break;

                default:
                    //Utilities.Debug("OnPowerChange: " + e.ToString());
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
            CheckSession();
        }

        private void CheckSession()
        {

            if (!Utilities.IsSystem())
            {
                if (!Config.isPipeConnected)
                {
                    Utilities.KillProcess(Config.idleMonExecutable);
                    Utilities.LaunchProcess(Config.idleMonExecutable, "");
                }
                return;
            }
                

            Config.currentSessionId = ProcessExtensions.GetSession();

            Utilities.Debug("OnSessionTimer: SessionID " + Config.currentSessionId);

            Utilities.CheckForSystem(Config.currentSessionId);

            Utilities.Debug(string.Format("Session: {0} - isLoggedIn: {1} - connected: {2} - sessionAttempts: {3} - isUserIdle: {4}", Config.currentSessionId, Config.isUserLoggedIn, Config.isPipeConnected, Config.sessionLaunchAttempts, Config.isUserIdle));

            if (Config.sessionLaunchAttempts > 4)
            {
                Utilities.Log("Unable to start IdleMon in user session; stopping service.", force: true);
                host.Stop();
                return;
            }

            if (Config.isUserLoggedIn && !Config.isPipeConnected)
            {
                Config.sessionLaunchAttempts++;
                Utilities.KillProcess(Config.idleMonExecutable);

                /* Not currently working.. May have to find a different way.
                string args = Config.settings.stealthMode ? "-stealth" : "";
                args += Config.settings.enableLogging ? "-log" : "";
                */
                ProcessExtensions.StartProcessAsCurrentUser(Config.idleMonExecutable, "", null, false);
                Utilities.Log("Attempting to start IdleMon in SessionID " + Config.currentSessionId);
                return;
            }
            else if (!Config.isUserLoggedIn && Config.isPipeConnected)
            {
                Config.sessionLaunchAttempts = 0;
                Utilities.KillProcess(Config.idleMonExecutable);
            }
            else if (!Config.isUserLoggedIn)
            {
                Config.sessionLaunchAttempts = 0;

                if (Config.currentSessionId > 0)
                    Config.isUserLoggedIn = true;
            }
        }

        private void OnMinerTimerEvent(object sender, ElapsedEventArgs e)
        {
            Utilities.Debug("OnMinerTimerEvent entered");
            lock (Config.timeLock)
            {
                if (Config.skipTimerCycles > 0)
                {
                    Utilities.Debug("skipTimerCycles: " + Config.skipTimerCycles);
                    Config.skipTimerCycles--;
                    return;
                }

                if (Config.isMiningPaused)
                {
                    Utilities.Debug("Mining is paused");
                    return;
                }
                
                //If not idle, and currently mining
                if ((!Config.isUserIdle && Config.isCurrentlyMining))
                {   
                    //If our CPU threshold is over 0, and CPU usage is over that, then stop mining and skip the next 6 timer cycles
                    if (Config.settings.cpuUsageThresholdWhileNotIdle > 0 && (Utilities.GetCpuUsage() > Config.settings.cpuUsageThresholdWhileNotIdle))
                    {
                        Utilities.KillMiners();
                        Config.skipTimerCycles = 6;
                        return;
                    }
                }

                if (Config.doesBatteryExist && !Utilities.IsBatteryFull())
                {
                    if (Config.isCurrentlyMining)
                    {
                        Utilities.Debug("Battery level is not full; stop mining..");
                        Utilities.KillMiners();
                    }
                    // regardless if we're mining, we can exit this method as we don't want to start mining now
                    return;
                }

                //check if resumePausedMiningAfterMinutes has passed, eventually..

                if (Config.settings.mineWithCpu)
                {
                    if (!Utilities.AreMinersRunning(Config.settings.cpuMiners, Config.isUserIdle))
                    {
                        //Utilities.KillMiners();
                        Utilities.MinersShouldBeRunning(Config.settings.cpuMiners);
                        Utilities.LaunchMiners(Config.settings.cpuMiners);
                    }
                }

                if  (Config.settings.mineWithGpu)
                {
                    if (!Utilities.AreMinersRunning(Config.settings.gpuMiners, Config.isUserIdle))
                    {
                        //Utilities.KillMiners();
                        Utilities.MinersShouldBeRunning(Config.settings.gpuMiners);
                        Utilities.LaunchMiners(Config.settings.gpuMiners);
                    }
                }                
                
                //Check cpu/gpu miners running, if not all running, start the ones that aren't running

                //Prevent sleep

                //check cpu/gpu temps

            }
            Utilities.Debug("OnMinerTimerEvent exited");
        }
#endregion
                   
    }
}
