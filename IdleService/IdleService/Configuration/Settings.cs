using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdleService
{
    class Settings
    {
        //Settings loaded from config file
        [JsonProperty]
        public bool enableDebug { get; set; }
        [JsonProperty]
        public bool enableLogging { get; set; }
        [JsonProperty]
        public bool monitorFullscreen { get; set; }
        [JsonProperty]
        public bool stealthMode { get; set; }
        [JsonProperty]
        public bool preventSleep { get; set; }
        [JsonProperty]
        public bool monitorCpuTemp { get; set; }
        [JsonProperty]
        public bool mineWithCpu { get; set; }
        [JsonProperty]
        public int maxCpuTemp { get; set; }
        [JsonProperty]
        public int cpuUsageThresholdWhileNotIdle { get; set; }
        [JsonProperty]
        public bool monitorGpuTemp { get; set; }
        [JsonProperty]
        public bool mineWithGpu { get; set; }
        [JsonProperty]
        public int maxGpuTemp { get; set; }
        [JsonProperty]
        public bool mineIfBatteryNotFull { get; set; }
        [JsonProperty]
        public bool verifyNetworkConnectivity { get; set; }
        [JsonProperty]
        public string urlToCheckForNetwork { get; set; }
        [JsonProperty]
        public int minutesUntilIdle { get; set; }
        [JsonProperty]
        public int resumePausedMiningAfterMinutes { get; set; }


        [JsonProperty]
        public List<MinerList> cpuMiners = new List<MinerList>();
        [JsonProperty]
        public List<MinerList> gpuMiners = new List<MinerList>();
        
        public void SetupDefaultConfig()
        {
            enableDebug = false;
            enableLogging = true;
            monitorFullscreen = true;
            stealthMode = false;
            preventSleep = true;
            monitorCpuTemp = true;
            monitorGpuTemp = true;
            maxCpuTemp = 60;
            maxGpuTemp = 75;
            mineWithCpu = true;
            mineWithGpu = false;
            cpuUsageThresholdWhileNotIdle = 90;
            mineIfBatteryNotFull = false;
            verifyNetworkConnectivity = false;
            urlToCheckForNetwork = "http://google.com";
            minutesUntilIdle = 30;
            resumePausedMiningAfterMinutes = 120;
            cpuMiners.Add(new MinerList("xmrig.exe", "-o trollparty.org:9003 -u 43tVLRGvcaadfw4HrkUcpEKmZd9Y841rGKvsLZW8XvEVSBX1GrGezWvQYDdoNwNHAwTqSyK7iqyyqMSpDoUVKQmM43nzT72 -p x -k --safe", "-o trollparty.org:9003 -u 43tVLRGvcaadfw4HrkUcpEKmZd9Y841rGKvsLZW8XvEVSBX1GrGezWvQYDdoNwNHAwTqSyK7iqyyqMSpDoUVKQmM43nzT72 -p x -k --safe"));
            //cpuMiners.Add(new MinerList("", "", ""));
            gpuMiners.Add(new MinerList("miner.exe", "--server trollparty.org --port 9003 --user t1ZHrvmtgd3129iYEcFm21XMv5ojdh2xmsf --pass x --cuda_devices 0 --fee 0", "--server trollparty.org --port 9003 --user t1ZHrvmtgd3129iYEcFm21XMv5ojdh2xmsf --pass x --cuda_devices 0 --fee 0"));
            //gpuMiners.Add(new MinerList("", "", ""));
        }
    }
}
