using LibreHardwareMonitor.Hardware;
using System.Linq;
using System.Collections.Generic;
using ConsumeControlTFG.Models;

namespace ConsumeControlTFG.Modules
{
    public class HardwareMonitorService
    {
        private Computer _computer;

        public double PotenciaW { get; private set; }
        public double EnergiaWh { get; set; }
        public double Voltaje { get; private set; }
        public double Temperatura { get; private set; }
        public double PotenciaGpuW { get; private set; }
        public double EnergiaGpuWh { get; set; }
        public double VoltajeGpu { get; private set; }
        public double TemperaturaGpu { get; private set; }

        public List<RegistroMedicion> Historial { get; private set; } = new List<RegistroMedicion>();
        public List<Models.RegistroMedicion> HistorialGpu { get; set; } = new List<Models.RegistroMedicion>();
        public List<Models.RegistroMedicion> HistorialTotal { get; set; } = new List<Models.RegistroMedicion>();
        public HardwareMonitorService()
        {
            //Inicializa la comunicación con la librería LibreHardwareMonitor,
            //activando específicamente el acceso a los sensores de la CPU.
            _computer = new Computer { IsCpuEnabled = true, IsGpuEnabled = true };
            _computer.Open();
        }

        public void ActualizarLecturas()
        {
            //Realiza un escaneo de los sensores de potencia (W), voltaje (V) y temperatura (°C) del chip,
            //calculando además el incremento de energía (Wh) consumida en el intervalo de tiempo actual
            foreach (IHardware cpu in _computer.Hardware.Where(h => h.HardwareType == HardwareType.Cpu))
            {
                cpu.Update();

                var powerSensors = cpu.Sensors.Where(s => s.SensorType == SensorType.Power).ToList();
                var powerSensor = powerSensors.FirstOrDefault(s => s.Name.Contains("Package"));
                if (powerSensor == null)
                {
                    powerSensor = powerSensors.FirstOrDefault(s => s.Name.Contains("Total") || s.Name.Contains("PPT"));
                }
                if (powerSensor == null)
                {
                    powerSensor = powerSensors.FirstOrDefault();
                }

                var voltSensor = cpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Voltage);
                var tempSensor = cpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature);

                PotenciaW = powerSensor?.Value ?? 0;
                Voltaje = voltSensor?.Value ?? 0;
                Temperatura = tempSensor?.Value ?? 0;
                EnergiaWh += (PotenciaW * 5.0) / 3600.0;
            }

            foreach (IHardware gpu in _computer.Hardware.Where(h => h.HardwareType == HardwareType.GpuNvidia || h.HardwareType == HardwareType.GpuAmd))
            {
                gpu.Update();

                var powerSensors = gpu.Sensors.Where(s => s.SensorType == SensorType.Power).ToList();

                // En las gráficas el sensor suele llamarse "GPU Power", "Package" o "Total Board Power"
                var powerSensor = powerSensors.FirstOrDefault(s => s.Name.Contains("Package") || s.Name.Contains("Total"));
                if (powerSensor == null)
                {
                    powerSensor = powerSensors.FirstOrDefault(); // Cogemos el primero que haya si no encuentra los específicos
                }

                var voltSensor = gpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Voltage);

                // Para la temperatura, buscamos el "Core" que es el chip principal
                var tempSensor = gpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature && s.Name.Contains("Core"));
                if (tempSensor == null)
                {
                    tempSensor = gpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature);
                }

                PotenciaGpuW = powerSensor?.Value ?? 0;
                VoltajeGpu = voltSensor?.Value ?? 0;
                TemperaturaGpu = tempSensor?.Value ?? 0;
                EnergiaGpuWh += (PotenciaGpuW * 5.0) / 3600.0;
            }
        }
    }
}