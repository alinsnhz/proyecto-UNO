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
        public List<Jugador> jugadores { get; set; }
        public int jugadorActual { get; set; }
        public Carta? cartaActual { get; set; }
        public int direccion { get; set; }
        public int cartasARobar { get; set; }
        public List<Carta> Mazo { get; set; }
        public List<string> registroAcciones { get; set; }
        public bool declaroUNO { get; set; }
        public bool oportunidadUNO { get; set; }
    }
    public JuegoUNO()
    {
            jugadores = new List<Jugador>();
            jugadorActual = 0;
            cartaActual = null;
            direccion = 1;
            cartasARobar = 0;
            Mazo = new List<Carta>();
            registroAcciones = new List<string>();
            declaroUNO = false;
            oportunidadUNO = false;
    }

        public void iniciarPartida()
        {
            jugadorActual = 0;
            direccion = 1;
            cartasARobar = 0;
        }

        public void cambiarTurno()
        {
            if(jugadores.Count == 0)
            {
                return;
            }

            jugadorActual += direccion;

            if (jugadorActual >= jugadores.Count)
            {
                jugadorActual = 0;
            }

            if (jugadorActual < 0)
            {
                jugadorActual = jugadores.Count - 1;
            }
        }

        public void aplicarReversa()
        {
            cambiarDireccion();
        }

        public void aplicarSalta()
        {
            cambiarTurno();
            cambiarTurno();
        }

        public void aplicarMasDos()
        {
            cartasARobar += 2;
            cambiarTurno();
        }

        public void aplicarComodin(string color)
        {
            cartaActual.Color = color;
        }

        public void aplicarMasCuatro(string color)
        {
            cartasARobar += 4;
            cartaActual.Color = color;
            cambiarTurno();
        }

        public  Jugador obtenerJugadorActual()
        {
            return jugadores[jugadorActual];
        }

        public bool esTurnoDe(Jugador jugador)
        {
            return jugadores[jugadorActual] == jugador;
        }

        public void cambiarDireccion()
        {
            direccion = direccion * -1;
        }

        public void aplicarEfectoCarta(Carta carta)
        {
            if(carta.Tipo == "Reversa")
            {
                aplicarReversa();
            }
            else if (carta.Tipo == "Salta")
            {
                aplicarSalta();
            }
            else if (carta.Tipo == "+2")
            {
                aplicarMasDos();
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

        public bool esCartaValida(Carta carta)
        {
            if(cartaActual == null)
            {
                return true;
            }
            if(carta.Color == cartaActual.Color)
            {
                return true;
            }
            if(carta.Numero == cartaActual.Numero)
            {
                return true;
            }
            if(carta.Simbolo == cartaActual.Simbolo)
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
            if(carta.Tipo == "+2")
            {
                return true;
            }
            return false;
        }

        public bool puedeJugar(Jugador jugador, Carta carta)
        {
            if(!esTurnoDe(jugador))
            {
                return false;
            }
            return esCartaValida(carta);
        }

        public Carta robarCarta()
        {
            if (Mazo.Count == 0)
            {
                return null;
            }

            Carta carta = Mazo[0];
            Mazo.RemoveAt(0);

            return carta;
        }

        public Carta agregaCartaRobada(Jugador jugador)
        {
            Carta carta = robarCarta();

            if(carta != null)
            {
                jugador.agregarCarta(carta);
                registroAcciones.Add(jugador.Nombre + " robó una carta");
            }

            return carta;
        }

        public bool puedeJugarCartaRobada(Jugador jugador, Carta carta)
        {
            return puedeJugar(jugador, carta);
        }

        public Carta robarDuranteTurno(Jugador jugador)
        {
            if(!esTurnoDe(jugador))
            {
                return null;
            }

            Carta carta = agregaCartaRobada(jugador);

            if(carta != null)
            {
                bool puedeJugar = puedeJugarCartaRobada(jugador, carta);
            }

            return carta;
        }

        public bool debeDeclararUNO(Jugador jugador)
        {
            return jugador.Cartas.Count == 1;
        }

        public bool debeDecirUNO (Jugador jugador)
        {
            return jugador.Cartas.Count == 1 && !declaroUNO;
        }

        public bool declararUNO(Jugador jugador)
        {
            if(!esTurnoDe(jugador))
            {
                return false;
            }

            if(!debeDeclararUNO(jugador))
            {
                return false;
            }

            declaroUNO = true;
            registroAcciones.Add(jugador.Nombre + "declaró UNO");
            return true;
        }

        public void activarOportunidadUNO(Jugador jugador)
        {
            if(jugador.Cartas.Count == 1)
            {
                oportunidadUNO = true;
                declaroUNO = false;
            }
        }
    }