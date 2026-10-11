using proyectoUNO;
using System;
using System.Collections.Generic;

namespace proyecto_UNO
{
    /*
     * Es el motor de las reglas: conserva jugadores, turnos, carta superior, 
     * dirección, robos pendientes, declaración de UNO y ganador 
    */
    public class JuegoUNO
    {
        //guarda participantes de la partida
        public List<Jugador> jugadores { get; set; }
        //inidca el índice del turno
        public int jugadorActual { get; set; }
        //guarda la carta superior
        public Carta cartaActual { get; set; }
        //inidca el sentido de la partida
        public int direccion { get; set; }
        //guarda el contador de penalización
        public int cartasARobar { get; set; }
        //administra las cartas
        public Mazo Mazo { get; set; }
        // guarda descartes
        public List<Carta> pilaDescarte { get; set; }
        //conserva mensajes para log
        public List<string> registroAcciones { get; set; }
        // controlan UNO
        public bool declaroUNO { get; set; }
        public bool oportunidadUNO { get; set; }
        //inidca si finalizó la partida
        public bool partidaTerminada { get; set; }
        // guarda al ganador de la partida
        public Jugador Ganador { get; set; }

        //inicializa listas, mazo, turno inicial, dirección y estados de partida
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

        //crea y mezcla el mazo, reinicia estados y manos, y reparte las cartas iniciales
        public void iniciarPartida()
        {
            Mazo.crearMazo();
            Mazo.barajar();

            pilaDescarte.Clear();
            cartaActual = null;

            jugadorActual = 0;
            direccion = 1;
            cartasARobar = 0;

            declaroUNO = false;
            oportunidadUNO = false;

            partidaTerminada = false;
            Ganador = null;

            registroAcciones.Clear();

            //limpiar cartas de partida anterior 
            foreach(Jugador jugador in jugadores)
            {
                jugador.Cartas.Clear();
            }

            repartirCartasIniciales();

        }

        //comprueba que haya tres jugadores y al menos 21 cartas
        public bool puedeRepartirCartasIniciales()
        {
            return jugadores.Count == 3 && Mazo.cartasRestantes() >= 21;
        }

        // entrega 7 cartas a cada jugador
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

        //avanza o retrocede según la dirección y vuelve al principio/final cuando hace falta
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

        //coloca una carta en la pila de descarte y la establece como carta actual; ignora null 
        public void agregarCartaDescarte(Carta carta)
        {
            if(carta == null)
            {
                return;
            }
            pilaDescarte.Add(carta);
            cartaActual = carta;
        }

        //devuelve la última carta de la pila de descarte o null si está vacía
        public Carta obtenerCartaSuperior()
        {
            if (pilaDescarte.Count == 0)
            {
                return null;
            }

            return pilaDescarte[pilaDescarte.Count - 1];
        }

        //cuando el mazo se agota, conserva la carta superior, devuelve las demás cartas de descarte al mazo y lo mezcla
        public void reciclarMazo()
        {
            if (Mazo.cartasRestantes() > 0 || pilaDescarte.Count <= 1)
            {
                return;
            }

            Carta superior = obtenerCartaSuperior();
            pilaDescarte.RemoveAt(pilaDescarte.Count - 1);

            foreach(Carta carta in pilaDescarte)
            {
                if (carta.Tipo == "Comodín" || carta.Tipo == "+4")
                    carta.Color = "Negro";
            }
            Mazo.Cartas.AddRange(pilaDescarte);

            pilaDescarte.Clear();
            pilaDescarte.Add(superior);

            Mazo.barajar();
        }

        //roba cartas hasta encontrar una carta numérica para iniciar el descarte
        public void colocarCartaInicial()
        {
            Carta carta = Mazo.robarCarta();

            while(carta != null && carta.Tipo != "Número")
            {
                Mazo.Cartas.Add(carta);
                Mazo.barajar();
                carta = Mazo.robarCarta();
            }

            if(carta != null)
            {
                agregarCartaDescarte(carta);
            }
        }

        //invierte el sentido de los turnos
        public void aplicarReversa()
        {
            cambiarDireccion();
        }


        //avanza dos veces para saltar el turno de un jugador
        public void aplicarSalta()
        {
            cambiarTurno();
            cambiarTurno();
        }

        // pasa al jugador afectado, le agrega la cantidad indicada de cartas
        // y avanza el turno después de la penalización
        public Jugador aplicaPenalizacionRobo(int cantidad)
        {
            cambiarTurno();
            Jugador afectado = jugadores[jugadorActual];
            for(int i = 0; i < cantidad; i++)
            {
                agregaCartaRobada(afectado);
            }
            cambiarTurno();
            return afectado;
        }

        //aplica una penalización de dos cartas
        public Jugador aplicarMasDos()
        {
            return aplicaPenalizacionRobo(2);
        }

        //cambia el color de la carta actual al color elegido, si existe una carta actual
        public void aplicarComodin(string color)
        {
            if(cartaActual != null)
                cartaActual.Color = color;
        }

        //cambia el color actual y aplica una penalización de cuatro cartas
        public Jugador aplicarMasCuatro(string color)
        {
            if (cartaActual != null)
                cartaActual.Color = color;

            return aplicaPenalizacionRobo(4);
        }

        // devuelve el jugador que corresponde el turno al índice actual
        public Jugador obtenerJugadorActual()
        {
            return jugadores[jugadorActual];
        }

        //comprueba si el jugador recibido es el que tiene el turno
        public bool esTurnoDe(Jugador jugador)
        {
            return jugadores[jugadorActual] == jugador;
        }

        //multiplica la dirección por -1 para invertirla
        public void cambiarDireccion()
        {
            direccion = direccion * -1;
        }

        //valida que la carta coincida por color, número, símbolo o valor
        //con la carta actual; los comodines se aceptan siempre
        public bool esCartaValida(Carta carta)
        {
            if (carta == null || cartaActual == null)
            {
                return false;
            }
            // Comodines siempre se pueden tirar
            if (carta.Tipo == "Comodin" || carta.Tipo == "+4")
                return true;

            // Mismo color
            if (carta.Color == cartaActual.Color)
                return true;

            // Mismo número
            if (carta.Numero == cartaActual.Numero)
                return true;

            // Mismo símbolo
            if (carta.Simbolo == cartaActual.Simbolo)
                return true;

            // Mismo valor
            if (carta.Valor == cartaActual.Valor)
                return true;

            return false;
        }

        //comprueba que la partida no haya terminado, que los argumentos existan,
        //que sea el turno del jugador y que la carta sea válida
        public bool puedeJugar(Jugador jugador, Carta carta)
        {
            if(partidaTerminada || jugador == null || carta == null)
            {
                return false;
            }
            if (!esTurnoDe(jugador))
            {
                return false;
            }
            return esCartaValida(carta);
        }

        //intenta reciclar el mazo si está vacío, roba una carta, la añade al
        //jugador y registra la acción
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

        //comprueba si la carta recién robada se puede jugar usando la validación general
        public bool puedeJugarCartaRobada(Jugador jugador, Carta carta)
        {
            return puedeJugar(jugador, carta);
        }

        //permite robar únicamente al jugador que tiene el turno y devuelve la carta robada
        public Carta robarDuranteTurno(Jugador jugador)
        {
            if (!esTurnoDe(jugador))
                return null;
           
            Carta carta = agregaCartaRobada(jugador);

            if (carta != null)
            {
                bool puedeJugar = puedeJugarCartaRobada(jugador, carta);
            }

            return carta;
        }

        //devuelve true si el jugador tiene dos cartas, condición usada antes de jugar
        //una carta para quedarse con una
        public bool debeDeclararUNO(Jugador jugador)
        {
            return jugador != null && jugador.Cartas.Count == 2;
        }

        //registra la declaración si es el turno del jugador y cumple la condición comprobada por debeDeclararUNO()
        public bool declararUNO(Jugador jugador)
        {
            if (!esTurnoDe(jugador))
                return false;

            if (!debeDeclararUNO(jugador))
                return false;

            declaroUNO = true;
            registroAcciones.Add(jugador.Nombre + " declaró UNO");
            return true;
        }

        //comprueba si el jugador tiene una carta, existía oportunidad y no declaró UNO
        public bool noDeclaroUNO(Jugador jugador)
        {
            return jugador != null && jugador.Cartas.Count == 1 && oportunidadUNO && !declaroUNO;
        }

        //devuelve true si el jugador no tiene cartas
        public bool esGanador(Jugador jugador)
        {
            return jugador != null && jugador.Cartas.Count == 0;
        }

        //si el jugador ganó, lo guarda como ganador y marca la partida como terminada
        public void comprobarGanador(Jugador jugador)
        {
            if (esGanador(jugador))
            {
                Ganador = jugador;
                partidaTerminada = true;
            }
        }

        //devuelve una lista con todos los jugadores excepto el ganador actual
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

        //construye un texto con el ganador y los perdedores, o informa que la partida no ha terminado
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
