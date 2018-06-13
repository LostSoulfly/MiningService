using OpenHardwareMonitor.Hardware;
using System.Collections.Generic;
using System.Linq;

namespace MiningService
{
    public class HardwareMonitor
    {
        private Computer computer = new Computer() { CPUEnabled = true, GPUEnabled = true };

        public HardwareMonitor()
        {
            computer.Open();
        }

        ~HardwareMonitor()
        {
            computer.Close();
        }

        public List<float> GetCpuTemperatures()
        {
            List<float> cpuTemps = new List<float>();

            foreach (IHardware hardware in computer.Hardware)
            {
                hardware.Update();

                foreach (ISensor sensor in hardware.Sensors.Where(hw => hw.Hardware.HardwareType == HardwareType.CPU))
                {
                    if (sensor.SensorType == SensorType.Temperature && sensor.Index == 1)
                    {
                        //Utilities.Log($"{sensor.Name}: {sensor.Value}°C Identifier:{sensor.Identifier} Index: {sensor.Index}");
                        cpuTemps.Add((float)sensor.Value);
                    }
                }
            }

            return cpuTemps;
        }

        public int GetCpuTemperaturesAverage()
        {
            return (int)this.GetCpuTemperatures().Average();
        }

        public List<float> GetGpuTemperatures()
        {
            List<float> gpuTemps = new List<float>();

            foreach (IHardware hardware in computer.Hardware)
            {
                hardware.Update();

                foreach (ISensor sensor in hardware.Sensors.Where(hw => hw.Hardware.HardwareType == HardwareType.GpuNvidia))
                {
                    if (sensor.SensorType == SensorType.Temperature)
                    {
                        //Utilities.Log($"{sensor.Name}: {sensor.Value}°C");
                        gpuTemps.Add((float)sensor.Value);
                    }
                }

                foreach (ISensor sensor in hardware.Sensors.Where(hw => hw.Hardware.HardwareType == HardwareType.GpuAti))
                {
                    if (sensor.SensorType == SensorType.Temperature)
                    {
                        //Utilities.Log($"{sensor.Name}: {sensor.Value}°C");
                        gpuTemps.Add((float)sensor.Value);
                    }
                }
            }

            return gpuTemps;
        }

        public int GetGpuTemperaturesAverage()
        {
            return (int)this.GetGpuTemperatures().Average();
        }

        public int GetNumberOfCpus()
        {
            return this.GetCpuTemperatures().Count;
        }

        public int GetNumberOfGpus()
        {
            return this.GetGpuTemperatures().Count;
        }
    }
}