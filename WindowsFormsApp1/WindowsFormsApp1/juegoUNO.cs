
using System;
using System.Collections.Generic;

namespace proyectoUNO
{
    public class juegoUNO
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

        public Jugador jugadorUNO { get; set; }

        // Control de la carta que se acaba de robar.
        public Jugador JugadorQueRobo { get; private set; }

        public Carta CartaRobada { get; private set; }

        public bool TieneCartaRobadaPendiente(Jugador jugador)
        {
            return jugador != null &&
                   JugadorQueRobo == jugador &&
                   CartaRobada != null;
        }

        public void LimpiarCartaRobada()
        {
            JugadorQueRobo = null;
            CartaRobada = null;
        }

        public juegoUNO()
        {
            jugadores = new List<Jugador>();
            pilaDescarte = new List<Carta>();
            registroAcciones = new List<string>();

            direccion = 1;
            jugadorActual = 0;
            cartasARobar = 0;

            declaroUNO = false;
            oportunidadUNO = false;
            partidaTerminada = false;

            Ganador = null;
            jugadorUNO = null;

            JugadorQueRobo = null;
            CartaRobada = null;

            Mazo = new Mazo();
        }

        public void iniciarPartida()
        {
            if (jugadores.Count != 3)
                throw new Exception("La partida necesita exactamente 3 jugadores.");

            Mazo = new Mazo();
            Mazo.crearMazo();
            Mazo.barajar();

            pilaDescarte.Clear();
            registroAcciones.Clear();

            jugadorActual = 0;
            direccion = 1;
            cartasARobar = 0;

            declaroUNO = false;
            oportunidadUNO = false;
            partidaTerminada = false;

            Ganador = null;
            jugadorUNO = null;

            JugadorQueRobo = null;
            CartaRobada = null;

            foreach (Jugador jugador in jugadores)
                jugador.Cartas.Clear();

            repartirCartasIniciales();
        }

        public bool puedeRepartirCartasIniciales()
        {
            return jugadores.Count == 3 &&
                   Mazo != null &&
                   Mazo.cartasRestantes() >= 21;
        }

        public void repartirCartasIniciales()
        {
            if (!puedeRepartirCartasIniciales())
                throw new Exception("No hay suficientes cartas para repartir.");

            foreach (Jugador jugador in jugadores)
            {
                for (int i = 0; i < 7; i++)
                {
                    Carta carta = Mazo.robarCarta();

                    if (carta == null)
                        throw new Exception("No fue posible repartir las cartas.");

                    jugador.agregarCarta(carta);
                }
            }
        }

        public void colocarCartaInicial()
        {
            Carta carta = null;

            while (Mazo.cartasRestantes() > 0)
            {
                carta = Mazo.robarCarta();

                if (carta != null &&
                    carta.Tipo != "Comodin" &&
                    carta.Tipo != "+4" &&
                    carta.Tipo != "Reversa" &&
                    carta.Tipo != "Salta" &&
                    carta.Tipo != "+2")
                    break;

                if (carta != null)
                    Mazo.Cartas.Add(carta);
            }

            if (carta == null)
                throw new Exception("No se encontró una carta inicial válida.");

            agregarCartaDescarte(carta);

            registroAcciones.Add("Se colocó la carta inicial.");
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
            if (pilaDescarte.Count <= 1)
                return;

            Carta superior = obtenerCartaSuperior();

            for (int i = pilaDescarte.Count - 2; i >= 0; i--)
                Mazo.Cartas.Add(pilaDescarte[i]);

            pilaDescarte.Clear();
            pilaDescarte.Add(superior);

            cartaActual = superior;
            Mazo.barajar();
        }

        public void aplicarReversa()
        {
            direccion *= -1;

            registroAcciones.Add("Se cambió el sentido del juego.");
        }

        public void aplicarSalta()
        {
            registroAcciones.Add("Se saltó el turno de un jugador.");

            cambiarTurno();
            cambiarTurno();
        }

        public void aplicaPenalizacionRobo(int cantidad)
        {
            if (jugadores.Count == 0)
                return;

            // Avanza al jugador que recibe la penalización.
            cambiarTurno();

            Jugador afectado = obtenerJugadorActual();

            for (int i = 0; i < cantidad; i++)
            {
                Carta carta = RobarDelMazo();

                if (carta != null)
                    afectado.agregarCarta(carta);
            }

            registroAcciones.Add(
                afectado.Nombre + " recibió " + cantidad + " cartas de penalización.");

            // El jugador penalizado pierde su turno.
            cambiarTurno();
        }

        public void aplicarMasDos()
        {
            aplicaPenalizacionRobo(2);
        }

        public void aplicarComodin(string color)
        {
            if (!ReglasUNO.EsColorValido(color))
                return;

            cartaActual.Color = color;

            registroAcciones.Add("El color cambió a " + color + ".");
        }

        public void aplicarMasCuatro(string color)
        {
            if (!ReglasUNO.EsColorValido(color))
                return;

            cartaActual.Color = color;

            registroAcciones.Add("Se eligió el color " + color + ".");
            aplicaPenalizacionRobo(4);
        }

        public Jugador obtenerJugadorActual()
        {
            if (jugadores.Count == 0 ||
                jugadorActual < 0 ||
                jugadorActual >= jugadores.Count)
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
            return ReglasUNO.EsCartaValida(carta, cartaActual);
        }

        public bool puedeJugar(Jugador jugador, Carta carta)
        {
            return ReglasUNO.PuedeJugar(this, jugador, carta);
        }

        public Carta agregaCartaRobada(Jugador jugador)
        {
            if (jugador == null)
                return null;

            Carta carta = RobarDelMazo();

            if (carta != null)
                jugador.agregarCarta(carta);

            return carta;
        }

        public bool puedeJugarCartaRobada(Jugador jugador, Carta carta)
        {
            return TieneCartaRobadaPendiente(jugador) &&
                   CartaRobada != null &&
                   carta != null &&
                   carta.Id == CartaRobada.Id &&
                   ReglasUNO.EsCartaValida(carta, cartaActual);
        }

        public Carta robarDuranteTurno(Jugador jugador)
        {
            if (!ReglasUNO.PuedeRobar(this, jugador))
                return null;

            Carta carta = RobarDelMazo();

            if (carta == null)
                return null;

            jugador.agregarCarta(carta);

            JugadorQueRobo = jugador;
            CartaRobada = carta;

            registroAcciones.Add(jugador.Nombre + " robó una carta.");

            return carta;
        }

        public bool PasarTurno(Jugador jugador)
        {
            if (!ReglasUNO.PuedePasarTurno(this, jugador))
                return false;

            JugadorQueRobo = null;
            CartaRobada = null;

            registroAcciones.Add(jugador.Nombre + " terminó su turno.");

            cambiarTurno();

            return true;
        }

        private Carta RobarDelMazo()
        {
            if (Mazo == null)
                return null;

            if (Mazo.cartasRestantes() == 0)
                reciclarMazo();

            if (Mazo.cartasRestantes() == 0)
                return null;

            return Mazo.robarCarta();
        }

        public bool debeDeclararUNO(Jugador jugador)
        {
            return jugador != null &&
                   jugador.Cartas.Count == 1 &&
                   jugadorUNO == jugador &&
                   oportunidadUNO &&
                   !declaroUNO;
        }

        public bool declararUNO(Jugador jugador)
        {
            if (!ReglasUNO.PuedeDeclararUNO(this, jugador))
                return false;

            declaroUNO = true;

            registroAcciones.Add(jugador.Nombre + " declaró UNO.");

            return true;
        }

        public bool noDeclaroUNO(Jugador jugador)
        {
            return jugador != null &&
                   jugadorUNO == jugador &&
                   oportunidadUNO &&
                   !declaroUNO &&
                   jugador.Cartas.Count == 1;
        }

        public bool esGanador(Jugador jugador)
        {
            return jugador != null &&
                   jugador.Cartas.Count == 0;
        }

        public void comprobarGanador(Jugador jugador)
        {
            if (!esGanador(jugador))
                return;

            Ganador = jugador;
            partidaTerminada = true;

            JugadorQueRobo = null;
            CartaRobada = null;

            registroAcciones.Add(jugador.Nombre + " ganó la partida.");
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
            if (!partidaTerminada || Ganador == null)
                return "La partida continúa.";

            return "El ganador es " + Ganador.Nombre + ".";
        }
    }
}
