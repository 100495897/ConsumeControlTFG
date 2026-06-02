using System;

namespace ConsumeControlTFG.Models
{
    public class RegistroMedicion
    {
        public int Periodo { get; set; }
        public string Hora { get; set; }
        public DateTime Timestamp { get; set; }
        public double Potencia { get; set; }
        public double ConsumoPeriodo { get; set; }
        public double ConsumoTotal { get; set; }
        public double EmisionesPeriodo { get; set; }
        public double EmisionesTotales { get; set; }
        public double Voltaje { get; set; }
        public double Temperatura { get; set; }
        public double IntensidadCarbono { get; set; }

        public RegistroMedicion(int periodo, double potencia, double consPer, double consTot,
                                    double emiPer, double emiTot, double volt, double temp,
                                    double intensidadCarbono)
        {
            Periodo = periodo;
            Timestamp = DateTime.Now;
            Hora = Timestamp.ToString("HH:mm:ss");
            Potencia = potencia;
            ConsumoPeriodo = consPer;
            ConsumoTotal = consTot;
            EmisionesPeriodo = emiPer;
            EmisionesTotales = emiTot;
            Voltaje = volt;
            Temperatura = temp;
            IntensidadCarbono = intensidadCarbono;
        }
    }
}