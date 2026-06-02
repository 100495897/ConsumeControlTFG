namespace ConsumeControlTFG.Models
{
    public class ProcesoAcumulado
    {
        public int Posicion { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public double EnergiaTotalWh { get; set; }
        public double CO2TotalG { get; set; }
    }
}