
using System;
using System.Collections.Generic;

namespace proyectoUNO
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
        public Jugador jugadorUNO { get; set; }

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
            jugadorUNO = null;

            partidaTerminada = false;
            Ganador = null;
        }

        public void iniciarPartida()
        {
            if (jugadores.Count != 3)
                throw new InvalidOperationException("Se necesitan exactamente tres jugadores");

            Mazo.crearMazo();
            Mazo.barajar();

            pilaDescarte.Clear();
            cartaActual = null;

            jugadorActual = 0;
            direccion = 1;
            cartasARobar = 0;

            declaroUNO = false;
            oportunidadUNO = false;
            jugadorUNO = null;

            partidaTerminada = false;
            Ganador = null;

            registroAcciones.Clear();

            foreach (Jugador jugador in jugadores)
                jugador.Cartas.Clear();

            if (!puedeRepartirCartasIniciales())
                throw new InvalidOperationException("No hay suficientes cartas para repartir.");

            repartirCartasIniciales();
        }

        public bool puedeRepartirCartasIniciales()
        {
            return jugadores.Count == 3 &&
                   Mazo.cartasRestantes() >= 21;
        }

        public void repartirCartasIniciales()
        {
            foreach (Jugador jugador in jugadores)
            {
                for (int i = 0; i < 7; i++)
                {
                    Carta carta = Mazo.robarCarta();

                    if (carta == null)
                        throw new InvalidOperationException("No fue posible repartir las cartas.");

                    jugador.agregarCarta(carta);
                }
            }
        }

        public void cambiarTurno()
        {
            if (jugadores.Count == 0)
                return;

            jugadorActual += direccion;

            if (jugadorActual >= jugadores.Count)
                jugadorActual = 0;

            if (jugadorActual < 0)
                jugadorActual = jugadores.Count - 1;
        }

        public void agregarCartaDescarte(Carta carta)
        {
            if (carta == null)
                return;

            pilaDescarte.Add(carta);
            cartaActual = carta;
        }

        public Carta obtenerCartaSuperior()
        {
            if (pilaDescarte.Count == 0)
                return null;

            return pilaDescarte[pilaDescarte.Count - 1];
        }

        public void reciclarMazo()
        {
            if (Mazo.cartasRestantes() > 0 || pilaDescarte.Count <= 1)
                return;

            Carta superior = obtenerCartaSuperior();

            pilaDescarte.RemoveAt(pilaDescarte.Count - 1);

            foreach (Carta carta in pilaDescarte)
            {
                if (carta.Tipo == "Comodin" || carta.Tipo == "+4")
                {
                    carta.Color = "Negro";
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
                Mazo.barajar();
                carta = Mazo.robarCarta();
            }

            if (carta != null)
                agregarCartaDescarte(carta);
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

        public Jugador aplicaPenalizacionRobo(int cantidad)
        {
            if (jugadores.Count == 0)
                return null;

            cambiarTurno();

            Jugador afectado = jugadores[jugadorActual];

            for (int i = 0; i < cantidad; i++)
                agregaCartaRobada(afectado);

            cambiarTurno();

            return afectado;
        }

        public Jugador aplicarMasDos()
        {
            return aplicaPenalizacionRobo(2);
        }

        public void aplicarComodin(string color)
        {
            if (cartaActual != null)
                cartaActual.Color = color;
        }

        public Jugador aplicarMasCuatro(string color)
        {
            if (cartaActual != null)
                cartaActual.Color = color;

            return aplicaPenalizacionRobo(4);
        }

        public Jugador obtenerJugadorActual()
        {
            if (jugadores.Count == 0)
                return null;

            return jugadores[jugadorActual];
        }

        public bool esTurnoDe(Jugador jugador)
        {
            return jugador != null &&
                   obtenerJugadorActual() == jugador;
        }

        public void cambiarDireccion()
        {
            direccion *= -1;
        }

        public bool esCartaValida(Carta carta)
        {
            if (carta == null || cartaActual == null)
                return false;

            if (carta.Tipo == "Comodin" ||
                carta.Tipo == "+4")
                return true;

            if (carta.Color == cartaActual.Color)
                return true;

            if (carta.Numero == cartaActual.Numero)
                return true;

            if (carta.Simbolo == cartaActual.Simbolo)
                return true;

            if (carta.Valor == cartaActual.Valor)
                return true;

            return false;
        }

        public bool puedeJugar(Jugador jugador, Carta carta)
        {
            if (partidaTerminada || jugador == null || carta == null)
                return false;

            if (!esTurnoDe(jugador))
                return false;

            return esCartaValida(carta);
        }

        public Carta agregaCartaRobada(Jugador jugador)
        {
            if (jugador == null)
                return null;

            if (Mazo.cartasRestantes() == 0)
                reciclarMazo();

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
            if (!esTurnoDe(jugador) || partidaTerminada)
                return null;

            return agregaCartaRobada(jugador);
        }

        public bool debeDeclararUNO(Jugador jugador)
        {
            return jugador != null &&
                   jugador.Cartas.Count == 2;
        }

        public bool declararUNO(Jugador jugador)
        {
            if (jugador == null || jugador != jugadorUNO ||
                jugador.Cartas.Count != 1 || !oportunidadUNO || declaroUNO)
                return false;

            declaroUNO = true;

            registroAcciones.Add(jugador.Nombre + " declaró UNO");

            return true;
        }

        public bool noDeclaroUNO(Jugador jugador)
        {
            return jugador != null &&
                   jugador == jugadorUNO &&
                   jugador.Cartas.Count == 1 &&
                   oportunidadUNO &&
                   !declaroUNO;
        }

        public bool esGanador(Jugador jugador)
        {
            return jugador != null && jugador.Cartas.Count == 0;
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
                    perdedores.Add(jugador);
            }

            return perdedores;
        }

        public string obtenerResultado()
        {
            if (Ganador == null)
                return "La partida no ha terminado";

            string resultado ="Ganador: " + Ganador.Nombre + "\n";

            resultado += "Perdedores:\n";

            foreach (Jugador jugador in obtenerPerdedores())
                resultado += jugador.Nombre + "\n";

            return resultado;
        }
    }
}
