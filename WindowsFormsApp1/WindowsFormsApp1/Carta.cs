
using System;

namespace proyectoUNO
{
    public class Carta
    {
        public string Id { get; set; }
        public string Color { get; set; }
        public string Valor { get; set; }
        public string Numero { get; set; }
        public string Simbolo { get; set; }
        public string Tipo { get; set; }

        public Carta(string color, string valor, string tipo)
        {
            Id = Guid.NewGuid().ToString("N");
            Color = color;
            Valor = valor;
            Numero = valor;
            Simbolo = valor;
            Tipo = tipo;
        }

        public Carta()
        {
            Id = Guid.NewGuid().ToString("N");
        }
    }
}
