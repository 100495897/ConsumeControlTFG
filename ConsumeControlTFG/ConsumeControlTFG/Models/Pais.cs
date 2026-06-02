using System;

namespace ConsumeControlTFG.Models
{
    public class Pais
    {
        public string Nombre { get; set; }
        public string Codigo { get; set; }

        public Pais(string nombre, string codigo)
        {
            Nombre = nombre;
            Codigo = codigo;
        }
    }
}