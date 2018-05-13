using Message;
using Microsoft.Win32;
using NamedPipeWrapper;
using System;
using System.Collections.Generic;
using System.Timers;
using Topshelf;

namespace MiningService
{
    internal class MyService
    {
        //TopShelf service controller
        private HostControl host;

        private Timer minerTimer = new Timer(5000);
        private Timer sessionTimer = new Timer(10000);

        //Pipe that is used to connect to the IdleMon running in the user's desktop session
        public NamedPipeClient<IdleMessage> client;

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
            IdleTime,
            Message,
            IgnoreFullscreenApp,
            Notifications
        }

        #region Json API for XMR-STAK-CPU only

        public class Connection
        {
            public List<object> error_log { get; set; }
            public int ping { get; set; }
            public string pool { get; set; }
            public int uptime { get; set; }
        }

        public class Hashrate
        {
            public double highest { get; set; }
            public List<List<double?>> threads { get; set; }
            public List<double?> total { get; set; }
        }

        public class Results
        {
            public double avg_time { get; set; }
            public List<int> best { get; set; }
            public int diff_current { get; set; }
            public List<object> error_log { get; set; }
            public int hashes_total { get; set; }
            public int shares_good { get; set; }
            public int shares_total { get; set; }
        }

        public class XmrRoot
        {
            public Connection connection { get; set; }
            public Hashrate hashrate { get; set; }
            public Results results { get; set; }
        }

        #endregion Json API for XMR-STAK-CPU only

        // = new NamedPipeClient<IdleMessage>(@"Global\MINERPIPE");
        //private Timer apiCheckTimer = new Timer(10000);

        #region TopShelf Start/Stop/Abort

        public void Abort()
        {
            host.Stop();
        }

        public bool Start(HostControl hc)
        {
            Utilities.Log("Starting MiningService: " + Utilities.version);
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
                Utilities.Log("Initializing MiningService.. CPU Cores: " + Environment.ProcessorCount);

                if (Utilities.DoesBatteryExist())
                {
                    Config.doesBatteryExist = true;
                    Utilities.Log("Battery found. IsBatteryFull: " + Utilities.IsBatteryFull());
                }
                else
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

                Utilities.Log("MiningService Initialized. Is SYSTEM: " + Utilities.IsSystem(false) + ". User: " + Environment.UserName);
                Config.serviceInitialized = true;
            }

            //Kill and miners and IdleMons that may be running, just in case!
            Utilities.KillMiners();
            Utilities.KillIdlemon(client);

            Config.isPipeConnected = false;

            Timer networkTimer = new Timer(10000);

            networkTimer.Elapsed += (sender, e) =>
            {
                if (Utilities.CheckForInternetConnection())
                {
                    StartTimers();
                    networkTimer.Stop();
                }
            };

            if (!Utilities.CheckForInternetConnection())
                networkTimer.Start();
            else
                StartTimers();

            Utilities.Log("MiningService is running");
            return true;
        }

        public void StartTimers()
        {
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
        }

        public void Stop()
        {
            lock (Config.timeLock)
            {
                Utilities.Log("Stopping MiningService..");

                Utilities.KillMiners();
                Utilities.KillIdlemon(client);
                minerTimer.Stop();
                sessionTimer.Stop();
                //apiCheckTimer.Stop();
                client.Stop();

                Config.isCurrentlyMining = false;

                if (Config.settings.preventSleep)
                    Utilities.AllowSleep();
            }
            Utilities.Log("Successfully stopped MiningService.");
        }

        #endregion TopShelf Start/Stop/Abort

        #region NamedPipe Events

        private void OnError(Exception exception)
        {
            Utilities.Debug("MiningService Pipe Err: " + exception.Message);
            Config.isPipeConnected = false;

            client.Stop();
            client.Start();
        }

        private void OnServerDisconnect(NamedPipeConnection<IdleMessage, IdleMessage> connection)
        {
            Utilities.Log("MiningService Pipe disconnected");
            Config.isPipeConnected = false;
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
                        if (!Config.isMiningPaused)
                        {
                            client.PushMessage(new IdleMessage
                            {
                                packetId = (int)PacketID.Message,
                                isIdle = false,
                                requestId = (int)PacketID.None,
                                data = "You have been detected as " + (message.isIdle ? "idle." : "active.")
                            });
                        } else
                        {
                            if (!message.isIdle)
                            {
                                client.PushMessage(new IdleMessage
                                {
                                    packetId = (int)PacketID.Message,
                                    isIdle = false,
                                    requestId = (int)PacketID.None,
                                    data = "You have been detected as active but mining is paused!"
                                });
                            }
                        }

                        Config.cpuUsageQueue = new Queue<int>();

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

                    Abort();

                    break;

                case ((int)PacketID.IgnoreFullscreenApp):

                    Config.settings.ignoredFullscreenApps.Add(message.data);

                    Config.WriteConfigToFile();

                    break;

                case ((int)PacketID.Fullscreen):

                    lock (Config.timeLock)
                    {
                        if (message.isIdle && Config.fullscreenDetected != true && !Config.isMiningPaused)
                        {
                            Utilities.Log("idleMon detected Fullscreen program: " + message.data);

                            client.PushMessage(new IdleMessage
                            {
                                packetId = (int)PacketID.Message,
                                isIdle = false,
                                requestId = (int)PacketID.None,
                                data = "Mining has been stopped because " + message.data + " was detected fullscreen."
                            });
                        }

                        if (message.isIdle)
                            Utilities.KillMiners();

                        Config.fullscreenDetected = message.isIdle;
                    }

                    break;

                case ((int)PacketID.Hello):
                    Utilities.Log("idleMon user " + message.data + " connected.");
                    Config.isUserIdle = message.isIdle;

                    connection.PushMessage(new IdleMessage
                    {
                        packetId = (int)PacketID.Log,
                        isIdle = Config.settings.enableLogging,
                        requestId = (int)PacketID.None,
                        data = ""
                    });

                    connection.PushMessage(new IdleMessage
                    {
                        packetId = (int)PacketID.Notifications,
                        isIdle = Config.settings.showDesktopNotifications,
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

                    foreach (var app in Config.settings.ignoredFullscreenApps)
                    {
                        connection.PushMessage(new IdleMessage
                        {
                            packetId = (int)PacketID.IgnoreFullscreenApp,
                            isIdle = false,
                            requestId = (int)PacketID.None,
                            data = app
                        });
                    }

                    if (Config.isMiningPaused)
                    {
                        connection.PushMessage(new IdleMessage
                        {
                            packetId = (int)PacketID.Hello,
                            isIdle = false,
                            requestId = (int)PacketID.Pause,
                            data = ""
                        });
                    }
                    else
                    {
                        connection.PushMessage(new IdleMessage
                        {
                            packetId = (int)PacketID.Hello,
                            isIdle = false,
                            requestId = (int)PacketID.Resume,
                            data = ""
                        });
                    }

                    break;

                default:
                    Utilities.Debug("MiningService Idle default: " + message.packetId);
                    break;
            }
        }

        #endregion NamedPipe Events

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

        public void SessionChanged(SessionChangedArguments args)
        {
            lock (Config.timeLock)
            {
                switch (args.ReasonCode)
                {
                    case Topshelf.SessionChangeReasonCode.SessionLock:
                        Utilities.Log(string.Format("Session: {0} - Reason: {1} - Lock", args.SessionId, args.ReasonCode));
                        Config.isUserLoggedIn = false;
                        Config.computerIsLocked = true;
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
                    case Topshelf.SessionChangeReasonCode.ConsoleConnect:
                        Config.isUserLoggedIn = true;
                        Config.computerIsLocked = false;
                        Utilities.Log(string.Format("Session: {0} - Reason: {1} - Unlock", args.SessionId, args.ReasonCode));
                        Config.currentSessionId = args.SessionId;
                        Config.isUserIdle = false;
                        break;

                    case Topshelf.SessionChangeReasonCode.SessionLogon:
                        Config.isUserLoggedIn = true;
                        Config.computerIsLocked = false;
                        Utilities.Log(string.Format("Session: {0} - Reason: {1} - Login", args.SessionId, args.ReasonCode));
                        Config.currentSessionId = args.SessionId;
                        Config.isUserIdle = false;
                        break;

                    case Topshelf.SessionChangeReasonCode.RemoteDisconnect:
                        Config.isUserLoggedIn = false;
                        Config.computerIsLocked = true;
                        Utilities.Log(string.Format("Session: {0} - Reason: {1} - RemoteDisconnect", args.SessionId, args.ReasonCode));
                        Config.currentSessionId = args.SessionId;
                        //if (Config.currentSessionId > 0)
                        //    Config.isUserLoggedIn = true;
                        Config.remoteDisconnectedSession = args.SessionId;
                        Config.isUserIdle = true;
                        return;
                        break;

                    case Topshelf.SessionChangeReasonCode.RemoteConnect:
                        Config.isUserLoggedIn = true;
                        Config.computerIsLocked = false;
                        Utilities.Log(string.Format("Session: {0} - Reason: {1} - RemoteConnect", args.SessionId, args.ReasonCode));
                        Config.currentSessionId = ProcessExtensions.GetSession();
                        Config.isUserIdle = false;
                        break;

                    default:
                        Utilities.Debug(string.Format("Session: {0} - Other - Reason: {1}", args.SessionId, args.ReasonCode));
                        break;
                }
                Config.remoteDisconnectedSession = -1;
                Config.fullscreenDetected = false;
                Utilities.KillMiners();
                Utilities.KillProcess(Config.idleMonExecutable);
                Config.cpuUsageQueue = new Queue<int>();
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

        #endregion Old API Json reading section (not used)

        #region Timers/Events

        private void CheckSession()
        {
            if (!Utilities.IsSystem())
            {
                if (!Config.isPipeConnected)
                {
                    Utilities.KillProcess(Config.idleMonExecutable);
                    Utilities.LaunchProcess(Config.idleMonExecutable, "");
                }
                Config.isUserLoggedIn = true;
                return;
            }

            Config.currentSessionId = ProcessExtensions.GetSession();

            //Utilities.Debug("OnSessionTimer: SessionID " + Config.currentSessionId);

            Utilities.CheckForSystem(Config.currentSessionId);

            //Utilities.Debug(string.Format("Session: {0} - isLoggedIn: {1} - connected: {2} - sessionAttempts: {3} - isUserIdle: {4}", Config.currentSessionId, Config.isUserLoggedIn, Config.isPipeConnected, Config.sessionLaunchAttempts, Config.isUserIdle));

            if (Config.sessionLaunchAttempts > 4)
            {
                Utilities.Log("Unable to start IdleMon in user session; stopping service.", force: true);
                host.Stop();
                return;
            }

            if (Config.isUserLoggedIn && Config.computerIsLocked)
            {
                if (Config.isPipeConnected)
                {
                    Utilities.KillProcess(Config.idleMonExecutable);
                    Config.sessionLaunchAttempts = 0;
                    Config.isUserIdle = true;
                }
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

                Utilities.Log("Attempting to start IdleMon in SessionID " + Config.currentSessionId + ". User: " + Utilities.GetUsernameBySessionId(Config.currentSessionId, false));

                ProcessExtensions.StartProcessAsCurrentUser(Config.idleMonExecutable, null, null, false, Config.currentSessionId);
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

                if (Config.currentSessionId > 0 && Config.remoteDisconnectedSession == -1)
                    Config.isUserLoggedIn = true;
            }
        }

        private void OnMinerTimerEvent(object sender, ElapsedEventArgs e)
        {
            //Utilities.Debug("OnMinerTimerEvent entered");
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
                    //Utilities.Debug("Mining is paused");
                    return;
                }

                if (Config.fullscreenDetected)
                {
                    //Utilities.Debug("Mining is paused by fullscreen program.");
                    return;
                }

                Utilities.GetCpuUsage();

                //Utilities.Debug("CpuUsageAverage: " + Config.CpuUsageAverage());

                //If not idle, and currently mining
                if ((!Config.isUserIdle && Config.isCurrentlyMining))
                {
                    //If our CPU threshold is over 0, and CPU usage is over that, then stop mining and skip the next 6 timer cycles
                    if (Config.settings.cpuUsageThresholdWhileNotIdle > 0 && (Config.CpuUsageAverage() > Config.settings.cpuUsageThresholdWhileNotIdle))
                    {
                        Utilities.KillMiners();
                        Config.skipTimerCycles = (int)(60000 / minerTimer.Interval);
                        Utilities.Debug("Stop Mining, cpu threshold hit");
                        client.PushMessage(new IdleMessage
                        {
                            packetId = (int)PacketID.Message,
                            isIdle = false,
                            requestId = (int)PacketID.None,
                            data = "CPU Threshold exceeded; stopped mining for 1 minute."
                        });

                        return;
                    }
                }

                if (Config.doesBatteryExist && !Utilities.IsBatteryFull())
                {
                    if (Config.isCurrentlyMining)
                    {
                        Utilities.Debug("Battery level is not full; stop mining..");

                        client.PushMessage(new IdleMessage
                        {
                            packetId = (int)PacketID.Message,
                            isIdle = false,
                            requestId = (int)PacketID.None,
                            data = "Battery level is not full; stopping mining."
                        });

                        Utilities.KillMiners();
                    }
                    // regardless if we're mining, we can exit this method as we don't want to start
                    // mining now
                    return;
                }

                //check if resumePausedMiningAfterMinutes has passed, eventually..

                bool didStartMiners = false;

                if (Config.settings.mineWithCpu && !Config.isMiningPaused)
                {
                    if (!Utilities.AreMinersRunning(Config.settings.cpuMiners, Config.isUserIdle))
                    {
                        didStartMiners = true;
                        Utilities.Log("CPU Miners are being started in " + (Config.isUserIdle ? "idle" : "active") + " mode.");
                        Utilities.MinersShouldBeRunning(Config.settings.cpuMiners);
                        Utilities.LaunchMiners(Config.settings.cpuMiners);
                    }
                }

                if (Config.settings.mineWithGpu && !Config.isMiningPaused)
                {
                    if (!Utilities.AreMinersRunning(Config.settings.gpuMiners, Config.isUserIdle))
                    {
                        didStartMiners = true;
                        Utilities.Log("GPU Miners are being started in " + (Config.isUserIdle ? "idle" : "active") + " mode.");
                        Utilities.MinersShouldBeRunning(Config.settings.gpuMiners);
                        Utilities.LaunchMiners(Config.settings.gpuMiners);
                    }
                }

                if (didStartMiners)
                {
                    client.PushMessage(new IdleMessage
                    {
                        packetId = (int)PacketID.Message,
                        isIdle = false,
                        requestId = (int)PacketID.None,
                        data = "Mining has been started in " + (Config.isUserIdle ? "idle" : "active") + " mode."
                    });
                }

                //Check cpu/gpu miners running, if not all running, start the ones that aren't running

                //Prevent sleep

                //check cpu/gpu temps
            }
            //Utilities.Debug("OnMinerTimerEvent exited");
        }

        private void OnSessionTimer(object sender, ElapsedEventArgs e)
        {
            CheckSession();
        }

        #endregion Timers/Events
    }
}