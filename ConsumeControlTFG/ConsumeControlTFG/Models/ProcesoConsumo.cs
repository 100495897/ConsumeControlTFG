using System;

namespace ConsumeControlTFG.Models
{
    public class ProcesoConsumo
    {
        public string Nombre { get; set; } = string.Empty;
        public int Id { get; set; }
        public double UsoCPU { get; set; }
        public double PotenciaEstimada { get; set; }
        public double EmisionesG { get; set; }
    }
}