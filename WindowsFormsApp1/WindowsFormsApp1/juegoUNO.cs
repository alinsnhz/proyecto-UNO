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

        public void cambiarTurno()
        {
            jugadorActual += Direccion;
            if (jugadorActual >= jugadores.Count)
            {
                jugadorActual = 0;
            }

            if (jugadorActual < 0)
            {
                jugadorActual = jugadores.Count - 1;
            }
        }

        // cambiar nombre de las variables según la clase Carta
        public bool esCartaValida(Carta carta)
        {
            if(CartaActual == null)
            {
                return true;
            }

            if(carta.Color == CartaActual.Color)
            {
                return true;
            }

            if(carta.Numero == CartaActual.Numero)
            {
                return true;
            }

            if(carta.Simbolo == CartaActual.Simbolo)
            {
                return true;
            }

            if(carta.Tipo == "Comodin")
            {
                return true;
            }

            if(carta.Tipo == "+4")
            {
                return true;
            }

            return false;
        }
    }
}