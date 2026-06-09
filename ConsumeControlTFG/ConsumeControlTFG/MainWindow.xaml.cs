using ConsumeControlTFG.Modules;
using ConsumeControlTFG.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Media;
using System.Diagnostics;
using System.Linq;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.Win32;


namespace ConsumeControlTFG
{
    public partial class MainWindow : Window
    {
        //Servicios
        private readonly ElectricityMapsService _emissionService = new ElectricityMapsService();
        private readonly HardwareMonitorService _monitorService = new HardwareMonitorService();
        private readonly DispatcherTimer _timerUI = new DispatcherTimer();

        //Variables de Estado
        private DateTime _fechaInicioSesion;
        private double _intensidadCarbonoActual = 434.0;
        private bool _aplicacionIniciada = false;
        private bool _ultimoDatoEsReal = false;
        private TimeSpan _tiempoAcumulado = TimeSpan.Zero;
        private int _segundosParaMedicion = 0;
        private int _ultimoPeriodo = 0;
        private double _emisionesAcumuladasCpuG = 0;
        private double _emisionesAcumuladasGpuG = 0;

        //Diccionarios para cálculos de CPU y Acumulados
        private DateTime _ultimoChequeoProcesos = DateTime.Now;
        private readonly Dictionary<int, TimeSpan> _tiemposProcesosAnteriores = new Dictionary<int, TimeSpan>();
        private readonly Dictionary<string, Models.ProcesoAcumulado> _historicoProcesos = new Dictionary<string, Models.ProcesoAcumulado>();

        public MainWindow()
        {
            //Constructor principal que inicializa la interfaz
            InitializeComponent();
            ComprobarDriverPawnIO();
            ConfigurarInicial();
        }

        private void ConfigurarInicial()
        {
            //Establece los valores de arranque
            var paisesSoportados = new List<Models.Pais>
            {
                new Models.Pais("Alemania", "DE"),
                new Models.Pais("Australia", "AU"),
                new Models.Pais("Austria", "AT"),
                new Models.Pais("Bélgica", "BE"),
                new Models.Pais("Brasil", "BR"),
                new Models.Pais("Canadá (Ontario)", "CA-ON"),
                new Models.Pais("Chile", "CL"),
                new Models.Pais("Dinamarca", "DK"),
                new Models.Pais("Eslovaquia", "SK"),
                new Models.Pais("Eslovenia", "SI"),
                new Models.Pais("España", "ES"),
                new Models.Pais("Estonia", "EE"),
                new Models.Pais("Finlandia", "FI"),
                new Models.Pais("Francia", "FR"),
                new Models.Pais("Grecia", "GR"),
                new Models.Pais("Hungría", "HU"),
                new Models.Pais("Irlanda", "IE"),
                new Models.Pais("Italia", "IT"),
                new Models.Pais("Letonia", "LV"),
                new Models.Pais("Lituania", "LT"),
                new Models.Pais("Noruega", "NO"),
                new Models.Pais("Polonia", "PL"),
                new Models.Pais("Portugal", "PT"),
                new Models.Pais("Reino Unido", "GB"),
                new Models.Pais("Suecia", "SE"),
                new Models.Pais("Suiza", "CH"),
                new Models.Pais("Uruguay", "UY")
            };
            cbPaises.ItemsSource = paisesSoportados.OrderBy(p => p.Nombre).ToList();

            _timerUI.Interval = TimeSpan.FromSeconds(1);
            _timerUI.Tick += (s, e) => UpdateUI();

            MainTabs.IsEnabled = false;
            txtTiempo.Text = "Tiempo: 00:00:00";
            AutodetectarPorRegion();
        }

        private void ExportarDatosCSV(DateTime fechaFin)
        {
            //Exportamos los datos de la sesion a un archivo csv
            if (_monitorService.Historial.Count == 0) return;

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Archivo CSV (*.csv)|*.csv",
                FileName = $"MedicionTFG_{(cbPaises.SelectedItem as Models.Pais)?.Codigo}_{DateTime.Now:yyyyMMdd_HHmm}.csv"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                StringBuilder csv = new StringBuilder();

                // --- BLOQUE 1: METADATOS ---
                csv.AppendLine("=== METADATOS DE LA SESION ===");
                csv.AppendLine($"Fecha de Inicio:;{_fechaInicioSesion:yyyy-MM-dd}");
                csv.AppendLine($"Hora de Inicio:;{_fechaInicioSesion:HH:mm:ss}");
                csv.AppendLine($"Fecha de Fin:;{fechaFin:yyyy-MM-dd}");
                csv.AppendLine($"Hora de Fin:;{fechaFin:HH:mm:ss}");
                csv.AppendLine($"Duracion Total:;{_tiempoAcumulado:hh\\:mm\\:ss}");
                csv.AppendLine($"Ubicacion Seleccionada:;{(cbPaises.SelectedItem as Models.Pais)?.Nombre}");

                // --- CÁLCULO DE PICOS MÁXIMOS ---

                double maxPotenciaCpu = _monitorService.Historial.Max(x => x.Potencia);
                double maxTempCpu = _monitorService.Historial.Max(x => x.Temperatura);

                double maxPotenciaGpu = _monitorService.HistorialGpu.Count > 0 ? _monitorService.HistorialGpu.Max(x => x.Potencia) : 0;
                double maxTempGpu = _monitorService.HistorialGpu.Count > 0 ? _monitorService.HistorialGpu.Max(x => x.Temperatura) : 0;

                double maxPotenciaTotal = _monitorService.HistorialTotal.Max(x => x.Potencia);

                csv.AppendLine($"Pico Máximo Potencia CPU (W):;{maxPotenciaCpu:F2}");
                csv.AppendLine($"Pico Máximo Potencia GPU (W):;{maxPotenciaGpu:F2}");
                csv.AppendLine($"Pico Máximo Temperatura CPU (ºC):;{maxTempCpu:F1}");
                csv.AppendLine($"Pico Máximo Temperatura GPU (ºC):;{maxTempGpu:F1}");
                csv.AppendLine($"Pico Máximo Potencia Sistema (W):;{maxPotenciaTotal:F2}");

                // --- CÁLCULO DE PROMEDIOS ---

                double potenciaMediaCpu = _monitorService.Historial.Average(x => x.Potencia);
                double potenciaMediaGpu = _monitorService.HistorialGpu.Average(x => x.Potencia);

                double voltajeMediaCpu = _monitorService.Historial.Average(x => x.Voltaje);
                double voltajeMediaGpu = _monitorService.HistorialGpu.Average(x => x.Voltaje);

                double tempMediaCpu = _monitorService.Historial.Average(x => x.Temperatura);
                double tempMediaGpu = _monitorService.HistorialGpu.Average(x => x.Temperatura);

                double potenciaMediaTotal = _monitorService.HistorialTotal.Average(x => x.Potencia);
                double gridMedio = _monitorService.Historial.Average(x => x.IntensidadCarbono);

                csv.AppendLine($"Potencia Media CPU (W):;{potenciaMediaCpu:F2}");
                csv.AppendLine($"Potencia Media GPU (W):;{potenciaMediaGpu:F2}");
                csv.AppendLine($"Potencia Media Sistema (W):;{potenciaMediaTotal:F2}");
                csv.AppendLine($"Voltaje Medio CPU (V):;{voltajeMediaCpu:F2}");
                csv.AppendLine($"Voltaje Medio GPU (V):;{voltajeMediaGpu:F2}");
                csv.AppendLine($"Temperatura Media CPU (ºC):;{tempMediaCpu:F1}");
                csv.AppendLine($"Temperatura Media GPU (ºC):;{tempMediaGpu:F1}");
                csv.AppendLine($"Intensidad Carbono Media Grid (gCO2/kWh):;{gridMedio:F1}");

                // --- CÁLCULO DE TOTALES ---

                double totalEnergiaCpuWh = _monitorService.EnergiaWh;
                double totalEnergiaGpuWh = _monitorService.EnergiaGpuWh;
                double totalEnergiaWh = totalEnergiaCpuWh + totalEnergiaGpuWh;

                double emisionesCpuG = _emisionesAcumuladasCpuG;
                double emisionesGpuG = _emisionesAcumuladasGpuG;
                double emisionesTotalesG = emisionesCpuG + emisionesGpuG;

                csv.AppendLine($"Energia Total CPU (Wh):;{totalEnergiaCpuWh:F6}");
                csv.AppendLine($"Energia Total GPU (Wh):;{totalEnergiaGpuWh:F6}");
                csv.AppendLine($"Energia Total Sistema (Wh):;{totalEnergiaWh:F6}");
                csv.AppendLine($"Emisiones Totales CPU (gCO2):;{emisionesCpuG:F6}");
                csv.AppendLine($"Emisiones Totales GPU (gCO2):;{emisionesGpuG:F6}");
                csv.AppendLine($"Emisiones Totales Sistema (gCO2):;{emisionesTotalesG:F6}");
                csv.AppendLine();

                // --- BLOQUE 2: HISTORIAL TEMPORAL ---
                csv.AppendLine("=== EVOLUCION TEMPORAL DETALLADA ===");
                csv.AppendLine("Periodo;Fecha;Hora;PotenciaCPU(W);PotenciaGPU(W);PotenciaTotal(W);" +
                               "ConsumoPeriodoCPU(Wh);ConsumoPeriodoGPU(Wh);ConsumoPeriodoTotal(Wh);" +
                               "ConsumoAcumuladoCPU(Wh);ConsumoAcumuladoGPU(Wh);ConsumoAcumuladoTotal(Wh);" +
                               "TempCPU(C);TempGPU(C);VoltajeCPU(V);VoltajeGPU(V);Grid(gCO2/kWh);" +
                               "EmisionesPeriodoCPU(gCO2);EmisionesPeriodoGPU(gCO2);EmisionesPeriodoTotal(gCO2);" +
                               "EmisionesAcumuladasCPU(gCO2);EmisionesAcumuladasGPU(gCO2);EmisionesAcumuladasTotal(gCO2)");

                for (int i = 0; i < _monitorService.Historial.Count; i++)
                {
                    var cpu = _monitorService.Historial[i];
                    var gpu = _monitorService.HistorialGpu[i];
                    var total = _monitorService.HistorialTotal[i];

                    csv.AppendLine(
                        $"{cpu.Periodo};" +
                        $"{cpu.Timestamp:yyyy-MM-dd};" +
                        $"{cpu.Timestamp:HH:mm:ss};" +
                        $"{cpu.Potencia:F2};" +             
                        $"{gpu.Potencia:F2};" +
                        $"{total.Potencia:F2};" +
                        $"{cpu.ConsumoPeriodo:F6};" +       
                        $"{gpu.ConsumoPeriodo:F6};" +
                        $"{total.ConsumoPeriodo:F6};" + 
                        $"{cpu.ConsumoTotal:F6};" +        
                        $"{gpu.ConsumoTotal:F6};" +
                        $"{total.ConsumoTotal:F6};" +
                        $"{cpu.Temperatura:F1};" +
                        $"{gpu.Temperatura:F1};" +
                        $"{cpu.Voltaje:F2};" +  
                        $"{gpu.Voltaje:F2};" +
                        $"{cpu.IntensidadCarbono:F1};" +
                        $"{cpu.EmisionesPeriodo:F6};" + 
                        $"{gpu.EmisionesPeriodo:F6};" +
                        $"{total.EmisionesPeriodo:F6};" +
                        $"{cpu.EmisionesTotales:F6};" +
                        $"{gpu.EmisionesTotales:F6};" +
                        $"{total.EmisionesTotales:F6}"
                    );
                }
                csv.AppendLine();

                // --- BLOQUE 3: RANKING DE PROCESOS (CPU) ---
                csv.AppendLine("=== RANKING DE PROCESOS (CPU) ===");
                csv.AppendLine("Ranking;Aplicacion;Energia Total(Wh);Emisiones Totales(gCO2)");

                var ranking = _historicoProcesos.Values.OrderByDescending(x => x.CO2TotalG).ToList();
                foreach (var proc in ranking)
                {
                    csv.AppendLine($"{proc.Posicion};{proc.Nombre};{proc.EnergiaTotalWh:F6};{proc.CO2TotalG:F6}");
                }

                File.WriteAllText(saveFileDialog.FileName, csv.ToString(), Encoding.UTF8);
                MessageBox.Show("Datos exportados correctamente.", "Exportación", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        

        private void AutodetectarPorRegion()
        {
            //Detecta automáticamente la región configurada en el sistema operativo
            try
            {
                RegionInfo region = RegionInfo.CurrentRegion;
                string codigoPais = region.TwoLetterISORegionName;
                string nombreRegion = region.DisplayName;

                var listaPaises = cbPaises.ItemsSource as List<Models.Pais>;

                if (listaPaises != null)
                {
                    var paisDetectado = listaPaises.FirstOrDefault(p => p.Codigo == codigoPais);

                    if (paisDetectado != null)
                    {
                        MessageBox.Show(
                            $"Se ha detectado '{paisDetectado.Nombre}' como tu ubicación basándose en la configuración de tu ordenador.\n\n" +
                            "La intensidad de carbono varía mucho según la zona geográfica. Si te encuentras físicamente en otro país, cierra este aviso y cambia el país manualmente en el menú superior.",
                            "Detección Automática",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                        var opcionSistema = new Models.Pais($"Sistema ({paisDetectado.Nombre})", paisDetectado.Codigo);
                        listaPaises.Insert(0, opcionSistema);
                        cbPaises.ItemsSource = null;
                        cbPaises.ItemsSource = listaPaises;
                        cbPaises.SelectedIndex = 0;
                    }
                    else
                    {
                        MessageBox.Show(
                            $"Tu sistema está configurado en la región '{nombreRegion}' ({codigoPais}), la cual no está incluida en la lista de zonas monitorizadas.\n\n" +
                            "Por favor, selecciona un país de referencia manualmente en el menú desplegable para comenzar la medición.",
                            "Región no soportada",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);

                        lblEstadoApi.Text = "Esperando selección manual...";
                        lblEstadoApi.Foreground = Brushes.Orange;
                    }
                }
            }
            catch
            {
                lblEstadoApi.Text = "Esperando selección...";
            }
        }

        private async void cbPaises_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //Gestiona el inicio de la medición
            if (cbPaises.SelectedItem is Models.Pais paisSeleccionado && !_aplicacionIniciada)
            {
                _fechaInicioSesion = DateTime.Now;
                _aplicacionIniciada = true;
                cbPaises.IsEnabled = false;
                _emissionService.ZonaActual = paisSeleccionado.Codigo;

                lblEstadoApi.Text = "Conectando con la API...";

                _ultimoChequeoProcesos = DateTime.Now;
                foreach (var p in Process.GetProcesses())
                {
                    try { _tiemposProcesosAnteriores[p.Id] = p.TotalProcessorTime; }
                    catch { continue; }
                }
                // Esperamos 100ms para que haya una diferencia de tiempo calculable
                await Task.Delay(100);

                await CargarIntensidadCarbono();

                RealizarCapturaCompleta();

                HardwareTypeTabs.IsEnabled = true;
                MainTabs.IsEnabled = true;
                GpuTabs.IsEnabled = true; btnPausar.IsEnabled = true;
                TotalTabs.IsEnabled = true;
                btnReiniciar.IsEnabled = true;
                this.Title = "Monitor Energético TFG - [EN CURSO]";
                _timerUI.Start();
            }
        }

        private void RealizarCapturaCompleta()
        {
            //Sincroniza la lectura de hardware, el cálculo de procesos
            //y la actualización del historial en cada periodo.
            _monitorService.ActualizarLecturas();
            _ultimoPeriodo++;

            var listaProcesos = ObtenerConsumoProcesos(_monitorService.PotenciaW, _intensidadCarbonoActual);
            dgProcesos.ItemsSource = listaProcesos;

            ActualizarAcumuladosProcesos(listaProcesos);

            double consumoEstePeriodoWh = (_monitorService.PotenciaW * 5.0) / 3600.0;
            double emisionesEstePeriodoG = (consumoEstePeriodoWh / 1000.0) * _intensidadCarbonoActual;
            _emisionesAcumuladasCpuG += emisionesEstePeriodoG;

            _monitorService.Historial.Add(new Models.RegistroMedicion(
                _ultimoPeriodo, _monitorService.PotenciaW, consumoEstePeriodoWh,
                _monitorService.EnergiaWh, emisionesEstePeriodoG, _emisionesAcumuladasCpuG,
                _monitorService.Voltaje, _monitorService.Temperatura, _intensidadCarbonoActual
            ));

            double consumoEstePeriodoGpuWh = (_monitorService.PotenciaGpuW * 5.0) / 3600.0;
            double emisionesEstePeriodoGpuG = (consumoEstePeriodoGpuWh / 1000.0) * _intensidadCarbonoActual;
            _emisionesAcumuladasGpuG += emisionesEstePeriodoGpuG;

            _monitorService.HistorialGpu.Add(new Models.RegistroMedicion(
                _ultimoPeriodo, _monitorService.PotenciaGpuW, consumoEstePeriodoGpuWh,
                _monitorService.EnergiaGpuWh, emisionesEstePeriodoGpuG, _emisionesAcumuladasGpuG,
                _monitorService.VoltajeGpu, _monitorService.TemperaturaGpu, _intensidadCarbonoActual
            ));

            double consumoEstePeriodoTotalWh = consumoEstePeriodoWh + consumoEstePeriodoGpuWh;
            double emisionesEstePeriodoTotalG = emisionesEstePeriodoG + emisionesEstePeriodoGpuG;
            double emisionesTotalesGlobalG = _emisionesAcumuladasCpuG + _emisionesAcumuladasGpuG;

            _monitorService.HistorialTotal.Add(new Models.RegistroMedicion(
                _ultimoPeriodo,
                _monitorService.PotenciaW + _monitorService.PotenciaGpuW, 
                consumoEstePeriodoTotalWh,
                _monitorService.EnergiaWh + _monitorService.EnergiaGpuWh, 
                emisionesEstePeriodoTotalG, emisionesTotalesGlobalG,
                0, 0, _intensidadCarbonoActual
            ));

            // Actualizamos llamando con los 3 valores
            ActualizarUIPantalla(_emisionesAcumuladasCpuG, _emisionesAcumuladasGpuG, emisionesTotalesGlobalG);
        }

        private void ActualizarUIPantalla(double emisionesTotalesCpu, double emisionesTotalesGpu, double emisionesTotalesGlobal)
        {
            //Refresca los indicadores numéricos (W, Wh, V, ºC)
            //y los listados de datos en las diferentes pestañas de la interfaz.
            txtPotencia.Text = $"{_monitorService.PotenciaW:F2} W";
            txtAcumulado.Text = $"{_monitorService.EnergiaWh:F6} Wh";
            txtVoltaje.Text = $"{_monitorService.Voltaje:F2} V";
            txtTemp.Text = $"{_monitorService.Temperatura:F1} °C";
            txtEmisiones.Text = $"{emisionesTotalesCpu:F6} gCO2";

            txtPotenciaGpu.Text = $"{_monitorService.PotenciaGpuW:F2} W";
            txtAcumuladoGpu.Text = $"{_monitorService.EnergiaGpuWh:F6} Wh";
            txtVoltajeGpu.Text = $"{_monitorService.VoltajeGpu:F2} V";
            txtTempGpu.Text = $"{_monitorService.TemperaturaGpu:F1} °C";

            double emisionesGpuG = (_monitorService.EnergiaGpuWh / 1000.0) * _intensidadCarbonoActual;
            txtEmisionesGpu.Text = $"{emisionesGpuG:F6} gCO2";

            dgHistorial.ItemsSource = null;
            dgHistorial.ItemsSource = _monitorService.Historial;

            txtPotenciaGpu.Text = $"{_monitorService.PotenciaGpuW:F2} W";
            txtAcumuladoGpu.Text = $"{_monitorService.EnergiaGpuWh:F4} Wh";
            txtVoltajeGpu.Text = $"{_monitorService.VoltajeGpu:F2} V";
            txtTempGpu.Text = $"{_monitorService.TemperaturaGpu:F1} °C";
            txtEmisionesGpu.Text = $"{emisionesTotalesGpu:F6} gCO2";

            dgHistorialGpu.ItemsSource = null;
            dgHistorialGpu.ItemsSource = _monitorService.HistorialGpu;

            txtPotenciaTotal.Text = $"{_monitorService.PotenciaW + _monitorService.PotenciaGpuW:F2} W";
            txtAcumuladoTotal.Text = $"{_monitorService.EnergiaWh + _monitorService.EnergiaGpuWh:F6} Wh";
            txtEmisionesTotal.Text = $"{emisionesTotalesGlobal:F6} gCO2";

            dgHistorialTotal.ItemsSource = null;
            dgHistorialTotal.ItemsSource = _monitorService.HistorialTotal;
        }

        private void UpdateUI()
        {
            //Se ejecuta cada segundo para actualizar el cronómetro visual
            //y disparar la captura de datos completa cada 5 segundos.
            _tiempoAcumulado = _tiempoAcumulado.Add(TimeSpan.FromSeconds(1));
            _segundosParaMedicion++;

            txtTiempo.Text = $"Tiempo: {_tiempoAcumulado:hh\\:mm\\:ss}";

            if (_segundosParaMedicion >= 5)
            {
                RealizarCapturaCompleta();
                if (_ultimoPeriodo % 60 == 0) _ = CargarIntensidadCarbono();
                _segundosParaMedicion = 0;
            }
        }

        private List<Models.ProcesoConsumo> ObtenerConsumoProcesos(double potenciaTotalW, double intensidadCO2)
        {
            //Calcula el uso de CPU real
            //y asigna a los procesos su parte.
            var lista = new List<Models.ProcesoConsumo>();
            DateTime ahora = DateTime.Now;
            double tiempoTranscurridoSec = (ahora - _ultimoChequeoProcesos).TotalSeconds;
            if (tiempoTranscurridoSec <= 0) tiempoTranscurridoSec = 0.1;
            int numNucleos = Environment.ProcessorCount;

            foreach (var p in Process.GetProcesses())
            {
                try
                {
                    if (p.Id == 0) continue;

                    TimeSpan tiempoCpuActual = p.TotalProcessorTime;

                    if (_tiemposProcesosAnteriores.TryGetValue(p.Id, out TimeSpan tiempoAnterior))
                    {
                        double tiempoUsadoSec = (tiempoCpuActual - tiempoAnterior).TotalSeconds;
                        if (tiempoUsadoSec < 0) tiempoUsadoSec = 0;
                        double usoCPU = (tiempoUsadoSec / (tiempoTranscurridoSec * numNucleos)) * 100;

                        if (usoCPU > 0.1)
                        {
                            lista.Add(new Models.ProcesoConsumo
                            {
                                Nombre = p.ProcessName,
                                Id = p.Id,
                                UsoCPU = usoCPU
                            });
                        }
                    }
                    _tiemposProcesosAnteriores[p.Id] = tiempoCpuActual;
                }
                catch { continue; }
            }

            _ultimoChequeoProcesos = ahora;

            double sumaUso = lista.Sum(x => x.UsoCPU);
            if (sumaUso > 100.0)
            {
                double factorNormalizacion = 100.0 / sumaUso;
                foreach (var item in lista)
                {
                    item.UsoCPU *= factorNormalizacion;
                }
            }

            foreach (var item in lista)
            {
                item.PotenciaEstimada = (potenciaTotalW * (item.UsoCPU / 100.0));
                double consumoWh = (item.PotenciaEstimada * 5.0) / 3600.0;
                item.EmisionesG = (consumoWh / 1000.0) * intensidadCO2;
            }

            return lista.OrderByDescending(x => x.UsoCPU).Take(20).ToList();
        }

        private void ActualizarAcumuladosProcesos(List<Models.ProcesoConsumo> lecturaActual)
        {
            //Mantiene un registro del consumo de cada proceso
            //y actualiza el ranking de emisiones de la sesión.
            foreach (var proc in lecturaActual)
            {
                if (!_historicoProcesos.ContainsKey(proc.Nombre))
                    _historicoProcesos[proc.Nombre] = new Models.ProcesoAcumulado { Nombre = proc.Nombre };

                var nodo = _historicoProcesos[proc.Nombre];
                double consumoWh = (proc.PotenciaEstimada * 5.0) / 3600.0;
                nodo.EnergiaTotalWh += consumoWh;
                nodo.CO2TotalG += (consumoWh / 1000.0) * _intensidadCarbonoActual;
            }

            var listaOrdenada = _historicoProcesos.Values.OrderByDescending(x => x.CO2TotalG).ToList();
            for (int i = 0; i < listaOrdenada.Count; i++) listaOrdenada[i].Posicion = i + 1;

            dgAcumuladoProcesos.ItemsSource = null;
            dgAcumuladoProcesos.ItemsSource = listaOrdenada;
        }

        private async Task CargarIntensidadCarbono()
        {
            //Consulta la API externa para obtener el factor de emisión de la zona seleccionada.
            var resultado = await _emissionService.GetCarbonIntensityAsync();
            _intensidadCarbonoActual = resultado.Valor;
            _ultimoDatoEsReal = resultado.EsReal;

            Dispatcher.Invoke(() =>
            {
                lblEstadoApi.Text = resultado.EsReal ? $"✓ Conectado: {resultado.Valor} gCO2/kWh" : "Por defecto (434 gCO2/kWh).";
                lblEstadoApi.Foreground = resultado.EsReal ? Brushes.Green : Brushes.Red;
            });
        }

        private void btnPausar_Click(object sender, RoutedEventArgs e)
        {
            //Detiene temporalmente las mediciones
            _timerUI.Stop();
            btnPausar.IsEnabled = false;
            btnContinuar.IsEnabled = true;

            lblEstadoApi.Foreground = Brushes.Gray;
            this.Title = "Monitor Energético TFG - [PAUSADO]";
        }

        private void btnContinuar_Click(object sender, RoutedEventArgs e)
        {
            //Reanuda las mediciones
            _timerUI.Start();
            btnPausar.IsEnabled = true;
            btnContinuar.IsEnabled = false;

            lblEstadoApi.Foreground = _ultimoDatoEsReal ? Brushes.Green : Brushes.Red;
            this.Title = "Monitor Energético TFG - [EN CURSO]";
        }

        private void btnReiniciar_Click(object sender, RoutedEventArgs e)
        {
            //Solicita permiso para reiniciar mediciones y guardar los datos actuales
            if (_monitorService.Historial.Count > 0)
            {
                bool medicionEnCurso = _timerUI.IsEnabled;
                _timerUI.Stop();
                DateTime instanteCierre = DateTime.Now;
                var result = MessageBox.Show(
                    "La medición actual se detendrá. ¿Deseas guardar los datos en un archivo CSV antes de reiniciar?",
                    "Guardar Datos",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Cancel)
                {
                    if (medicionEnCurso) _timerUI.Start();
                    return;
                }
                if (result == MessageBoxResult.Yes)
                {
                    ExportarDatosCSV(instanteCierre);
                }
            }
            _timerUI.Stop();
            ResetearAplicacion();
        }

        private void ResetearAplicacion()
        {
            //Devuelve la aplicación a su estado original
            _aplicacionIniciada = false;
            _tiempoAcumulado = TimeSpan.Zero;
            _ultimoDatoEsReal = false;
            _segundosParaMedicion = 0;
            _ultimoPeriodo = 0;
            _emisionesAcumuladasCpuG = 0;
            _emisionesAcumuladasGpuG = 0;
            _historicoProcesos.Clear();
            _tiemposProcesosAnteriores.Clear();

            _monitorService.EnergiaWh = 0;
            _monitorService.Historial.Clear();
            _monitorService.EnergiaGpuWh = 0;

            txtPotencia.Text = "0.00 W";
            txtAcumulado.Text = "0.000000 Wh";
            txtEmisiones.Text = "0.000000 gCO2";
            txtTiempo.Text = "Tiempo: 00:00:00";
            txtVoltaje.Text = "0.0 V";
            txtTemp.Text = "0.0 °C";
            lblEstadoApi.Text = "Esperando selección...";
            lblEstadoApi.Foreground = Brushes.Orange;
            this.Title = "Monitor Energético TFG";

            _monitorService.EnergiaGpuWh = 0;
            _monitorService.HistorialGpu.Clear();

            txtPotenciaGpu.Text = "0.00 W";
            txtAcumuladoGpu.Text = "0.000000 Wh";
            txtEmisionesGpu.Text = "0.000000 gCO2";
            txtVoltajeGpu.Text = "0.0 V";
            txtTempGpu.Text = "0.0 °C";

            dgHistorial.ItemsSource = null;
            dgAcumuladoProcesos.ItemsSource = null;
            MainTabs.IsEnabled = false;
            cbPaises.IsEnabled = true;
            cbPaises.SelectedItem = null;
            btnPausar.IsEnabled = false;
            btnContinuar.IsEnabled = false;
            btnReiniciar.IsEnabled = false;

            _monitorService.HistorialTotal.Clear();

            txtPotenciaTotal.Text = "0.00 W";
            txtAcumuladoTotal.Text = "0.000000 Wh";
            txtEmisionesTotal.Text = "0.000000 gCO2";

            dgHistorialTotal.ItemsSource = null;
            TotalTabs.IsEnabled = false;
        }

        private void ComprobarDriverPawnIO()
        {
            //Comprobamos si PawnIO esta instalado
            using (RegistryKey? key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\PawnIO"))
            {
                if (key == null)
                {
                    MessageBox.Show(
                        "No se ha detectado el controlador de núcleo 'PawnIO' en el sistema.\n\n" +
                        "Esta aplicación requiere este controlador para acceder a los sensores de energía físicos de la placa base (Anillo 0). ",
                        "Requisito del Sistema Faltante",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //Pregunta si guardar los datos al cerrar la aplicacion
            if (_aplicacionIniciada && _monitorService.Historial.Count > 0)
            {
                bool estabaMidiendo = _timerUI.IsEnabled;
                _timerUI.Stop();
                DateTime instanteCierre = DateTime.Now;
                var result = MessageBox.Show(
                    "¿Deseas guardar los datos de la medición actual antes de salir?",
                    "Salir de la Aplicación",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                    if (estabaMidiendo)
                    {
                        _timerUI.Start();
                    }
                    return;
                }
                
                if (result == MessageBoxResult.Yes)
                {
                    ExportarDatosCSV(instanteCierre);
                }
            }
        }
    }
}