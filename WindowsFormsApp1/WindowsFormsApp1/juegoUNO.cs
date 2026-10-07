using proyectoUNO;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.Serialization.Formatters;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace proyecto_UNO
{
    public class JuegoUNO
    {
        public List<Jugador> jugadores { get; set; }
        public int jugadorActual { get; set; }
        public Carta cartaActual { get; set; }
        public int direccion { get; set; }
        public int cartasARobar { get; set; }
        public Mazo Mazo { get; set; }
        public List<Carta> pilaDescarte { get; set; }
        public List<string> registroAcciones { get; set; }
        public bool declaroUNO { get; set; }
        public bool oportunidadUNO { get; set; }
        public bool partidaTerminada { get; set; }
        public Jugador Ganador { get; set; }


        public JuegoUNO()
        {
            jugadores = new List<Jugador>();
            jugadorActual = 0;
            cartaActual = null;
            direccion = 1;
            cartasARobar = 0;
            Mazo = new Mazo();
            pilaDescarte = new List<Carta>();
            registroAcciones = new List<string>();
            declaroUNO = false;
            oportunidadUNO = false;
            partidaTerminada = false;
            Ganador = null;
        }

        public void iniciarPartida()
        {
            Mazo.crearMazo();
            Mazo.barajar();

            pilaDescarte.Clear();
            cartaActual = null;

            jugadorActual = 0;
            direccion = 1;
            cartasARobar = 0;

            repartirCartasIniciales();

        }

        public bool puedeRepartirCartasIniciales()
        {
            return jugadores.Count == 3 && Mazo.cartasRestantes() >= 21;
        }

        public void repartirCartasIniciales()
        {
            foreach (Jugador jugador in jugadores)
            {
                for (int i = 0; i < 7; i++)
                {
                    Carta carta = Mazo.robarCarta();

                    if (carta == null)
                    {
                        return;
                    }

                    jugador.agregarCarta(carta);
                }
            }
        }

        public void cambiarTurno()
        {
            if (jugadores.Count == 0)
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

        public void agregarCartaDescarte(Carta carta)
        {
            pilaDescarte.Add(carta);
            cartaActual = carta;
        }

        public Carta obtenerCartaSuperior()
        {
            if (pilaDescarte.Count == 0)
            {
                return null;
            }

            return pilaDescarte[pilaDescarte.Count - 1];
        }

        public void reciclarMazo()
        {
            if (Mazo.cartasRestantes() > 0)
            {
                return;
            }

            if (pilaDescarte.Count <= 1)
            {
                return;
            }

            Carta superior = obtenerCartaSuperior();
            pilaDescarte.RemoveAt(pilaDescarte.Count - 1);
            foreach (Carta c in pilaDescarte)
            {
                if(c.Tipo == "Conodin" || c.Tipo == "+4")
                {
                    c.Color = "Negro";
                }
            }

            Mazo.Cartas.AddRange(pilaDescarte);
            pilaDescarte.Clear();
            pilaDescarte.Add(superior);
            Mazo.barajar();
        }

        public void colocarCartaInicial()
        {
            Carta carta = Mazo.robarCarta();
            while (carta != null && carta.Tipo != "Número")
            {
                Mazo.Cartas.Add(carta);
                carta = Mazo.robarCarta();
            }
            if (carta != null)
            {
                agregarCartaDescarte(carta);
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

        public Jugador aplicarPenalizacionRobo(int cantidad)
        {
            cambiarTurno();
            Jugador afectado = jugadores[jugadorActual];
            for (int i = 0; i < cantidad; i++)
            {
                agregaCartaRobada(afectado);
            }
            cambiarTurno();
            return afectado;
        }

        public Jugador aplicarMasDos()
        {
            return aplicarPenalizacionRobo(2);
        }

        public void aplicarComodin(string color)
        {
            cartaActual.Color = color;
        }

        public Jugador aplicarMasCuatro(string color)
        {
            cartaActual.Color = color;
            return aplicarPenalizacionRobo(4);
        }

        public Jugador obtenerJugadorActual()
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
            if (carta.Tipo == "Reversa")
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
            if (cartaActual == null)
            {
                return true;
            }
            if (carta.Color == cartaActual.Color)
            {
                return true;
            }
            if (carta.Numero == cartaActual.Numero)
            {
                return true;
            }
            if (carta.Simbolo == cartaActual.Simbolo)
            {
                return true;
            }
            if (carta.Tipo == "Comodin" || carta.Tipo == "+4")
            {
                return true;
            }
            if (carta.Valor == cartaActual.Valor)
            {
                return true;
            }

            return false;
        }

        public bool puedeJugar(Jugador jugador, Carta carta)
        {
            if (partidaTerminada)
            {
                return false;
            }
            if (!esTurnoDe(jugador))
            {
                return false;
            }
            return esCartaValida(carta);
        }

        public Carta agregaCartaRobada(Jugador jugador)
        {
            if (Mazo.cartasRestantes() == 0)
            {
                reciclarMazo();
            }

            Carta carta = Mazo.robarCarta();

            if (carta != null)
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
            if (!esTurnoDe(jugador))
            {
                return null;
            }

            Carta carta = agregaCartaRobada(jugador);

            if (carta != null)
            {
                bool puedeJugar = puedeJugarCartaRobada(jugador, carta);
            }

            return carta;
        }

        public bool debeDeclararUNO(Jugador jugador)
        {
            return jugador.Cartas.Count == 2;
        }

        public bool debeDecirUNO(Jugador jugador)
        {
            return jugador.Cartas.Count == 1 && !declaroUNO;
        }

        public bool declararUNO(Jugador jugador)
        {
            if (!esTurnoDe(jugador))
            {
                return false;
            }

            if (!debeDeclararUNO(jugador))
            {
                return false;
            }

            declaroUNO = true;
            registroAcciones.Add(jugador.Nombre + " declaró UNO");
            return true;
        }

        public void activarOportunidadUNO(Jugador jugador)
        {
            if (jugador.Cartas.Count == 1)
            {
                oportunidadUNO = true;
                declaroUNO = false;
            }
        }

        public void terminarOportunidadUNO()
        {
            oportunidadUNO = false;
        }

        public bool noDeclaroUNO(Jugador jugador)
        {
            return jugador.Cartas.Count == 1 && oportunidadUNO && !declaroUNO;
        }

        public void penalizarUNO(Jugador jugador)
        {
            if (!noDeclaroUNO(jugador))
            {
                return;
            }

            for (int i = 0; i < 2; i++)
            {
                agregaCartaRobada(jugador);
            }

            registroAcciones.Add(jugador.Nombre + " recibió una penalización de 2 cartas por no declarar UNO");
        }

        public void reiniciarUNO()
        {
            oportunidadUNO = false;
            declaroUNO = false;
        }

        public void procesarUNO(Jugador jugador)
        {
            if (noDeclaroUNO(jugador))
            {
                penalizarUNO(jugador);
            }

            reiniciarUNO();
        }

        public bool esGanador(Jugador jugador)
        {
            return jugador.Cartas.Count == 0;
        }

        public void comprobarGanador(Jugador jugador)
        {
            if (esGanador(jugador))
            {
                Ganador = jugador;
                partidaTerminada = true;
            }
        }

        public List<Jugador> obtenerPerdedores()
        {
            List<Jugador> perdedores = new List<Jugador>();
            foreach (Jugador jugador in jugadores)
            {
                if (jugador != Ganador)
                {
                    perdedores.Add(jugador);
                }
            }
            return perdedores;
        }

        public String obtenerResultado()
        {
            if (Ganador == null)
            {
                return "La partida no ha terminado";
            }
            string resultado = "Ganador: " + Ganador.Nombre + "\n";
            resultado += "Perdedores: \n";
            foreach (Jugador jugador in obtenerPerdedores())
            {
                resultado += jugador.Nombre + "\n";
            }
            return resultado;
        }
    }
}