using proyectoUNO;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace proyecto_UNO
{
    public class JuegoUNO
    {
        public List<Jugador> Jugadores { get; set; }
        public int JugadorActual { get; set; }
        public Carta? CartaActual { get; set; }
        public int Direccion { get; set; }
        public int CartasARobar { get; set; }

        public JuegoUNO()
        {
            Jugadores = new List<Jugador>();
            JugadorActual = 0;
            CartaActual = null;
            Direccion = 1;
            CartasARobar = 0;
        }

        public void IniciarPartida()
        {
            JugadorActual = 0;
            Direccion = 1;
            CartasARobar = 0;
        }

        public void CambiarTurno()
        {
            if(Jugadores.Count)
            {
                return;
            }

            JugadorActual += Direccion;

            if (JugadorActual >= Jugadores.Count)
            {
                JugadorActual = 0;
            }

            if (JugadorActual < 0)
            {
                JugadorActual = Jugadores.Count - 1;
            }
        }

        public void AplicarReversa()
        {
            cambiarDireccion();
        }

        public void AplicarSalta()
        {
            CambiarTurno();
            CambiarTurno();
        }

        public void AplicarMasDos()
        {
            CartasARobar += 2;
            CambiarTurno();
        }

        public void AplicarComodin(string color)
        {
            CartaActual.Color = color;
        }

        public void AplicarMasCuatro(string color)
        {
            CartasARobar += 4;
            CartaActual.Color = color;
            CambiarTurno();
        }

        public  Jugador obtenerJugadorActual()
        {
            return Jugadores[JugadorActual];
        }

        public bool esTurnoDe(Jugador jugador)
        {
            return Jugadores[JugadorActual] == jugador;
        }

        public void cambiarDireccion()
        {
            Direccion = Direccion * -1;
        }

        public void aplicarEfectoCarta(Carta carta)
        {
            if(carta.Tipo == "Reversa")
            {
                AplicarReversa();
            }
            else if (carta.Tipo == "Salta")
            {
                AplicarSalta();
            }
            else if (carta.Tipo == "+2")
            {
                AplicarMasDos();
            }
            else if (carta.Tipo == "Comodin")
            {
                // el color se selecciona aparte
            }
            else if (carta.Tipo == "+4")
            {
                //el color se selecciona aparte
            }
        }
    }
}