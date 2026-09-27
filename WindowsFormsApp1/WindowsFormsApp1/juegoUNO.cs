using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace proyectoUNO
{
    public class juegoUNO
    {
        public List<Jugador> jugadores { get; set; }
        public int jugadorActual { get; set; }
        public Carta? CartaActual { get; set; }
        public int Direccion { get; set; } // 1 - hacia adelante / -1 hacia atrás
    }

    public juegoUNO()
    {
            jugadores = new List<Jugador>();
            jugadorActual = 0;
            CartaActual = null;
            Direccion = 1;
    }

     public void iniciarPartida()
    {
            jugadorActual = 0;
            Direccion = 1;
    }
}
