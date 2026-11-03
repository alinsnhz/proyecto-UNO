
using System;
using System.Collections.Generic;
using proyectoUNO;
using ProyectoUno.Protocolo;

namespace ProyectoUno.Servidor
{
    public class PartidaServidor
    {
        private const int MaxJugadores = 3;

        private readonly List<JugadorServidor> jugadores =
            new List<JugadorServidor>();

        private readonly object candado = new object();

        public juegoUNO Juego { get; private set; }

        public bool Iniciada { get; private set; }

        public bool Interrumpida { get; private set; }

        public bool RegistrarJugador(JugadorServidor jugador)
        {
            lock (candado)
            {
                if (jugador == null || jugador.Registrado)
                    return false;

                if (jugadores.Count >= MaxJugadores)
                    return false;

                if (Iniciada || Interrumpida)
                    return false;

                foreach (JugadorServidor participante in jugadores)
                {
                    if (participante.Id == jugador.Id)
                        return false;
                }

                jugadores.Add(jugador);
                jugador.Registrado = true;

                if (jugadores.Count == MaxJugadores)
                {
                    if (!IniciarPartida())
                    {
                        jugadores.Remove(jugador);
                        jugador.Registrado = false;

                        return false;
                    }
                }

                return true;
            }
        }

        private bool IniciarPartida()
        {
            try
            {
                Console.WriteLine("Iniciando partida de UNO...");

                juegoUNO nuevoJuego = new juegoUNO();

                foreach (JugadorServidor participante in jugadores)
                {
                    Jugador nuevoJugador = new Jugador(participante.Id, participante.Nombre);

                    nuevoJuego.jugadores.Add(nuevoJugador);
                }

                nuevoJuego.iniciarPartida();
                nuevoJuego.colocarCartaInicial();

                if (nuevoJuego.cartaActual == null)
                    throw new Exception("No fue posible colocar la carta inicial");

                Juego = nuevoJuego;

                Iniciada = true;
                Interrumpida = false;

                Console.WriteLine("¡PARTIDA INICIADA!");

                foreach (JugadorServidor participante in jugadores)
                {
                    Console.WriteLine("Jugador " + participante.Id + ": " + participante.Nombre);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al iniciar la partida: " + ex.Message);

                Juego = null;
                Iniciada = false;

                // Si falla la creación, se libera la sala completa.
                foreach (JugadorServidor participante in jugadores)
                    participante.Registrado = false;

                jugadores.Clear();

                return false;
            }
        }

        public Jugador ObtenerJugador(int id)
        {
            lock (candado)
            {
                if (Juego == null)
                    return null;

                foreach (Jugador jugador in Juego.jugadores)
                {
                    if (jugador.ID == id)
                        return jugador;
                }

                return null;
            }
        }

        public List<JugadorServidor> ObtenerJugadores()
        {
            lock (candado)
            {
                return new List<JugadorServidor>(jugadores);
            }
        }

        public Mensajes ProcesarAccion(int id, Mensajes solicitud)
        {
            lock (candado)
            {
                if (!Iniciada || Interrumpida || Juego == null)
                {
                    return CrearRespuesta(solicitud, false, "La partida no está disponible");
                }

                Jugador jugador = ObtenerJugadorInterno(id);

                if (jugador == null)
                {
                    return CrearRespuesta(solicitud, false, "El jugador no pertenece a esta partida");
                }

                if (solicitud == null ||
                    string.IsNullOrWhiteSpace(solicitud.Tipo))
                {
                    return CrearRespuesta(solicitud, false, "La acción no es válida");
                }

                string tipo = solicitud.Tipo.ToLowerInvariant();

                switch (tipo)
                {
                    case "jugarcarta":
                        return ProcesarJugarCarta(jugador, solicitud);

                    case "rob ar":
                    case "robarcarta":
                        return ProcesarRobarCarta(jugador, solicitud);

                    case "declararuno":
                        return ProcesarDeclararUNO(jugador, solicitud);

                    case "acus aruno":
                    case "acusaruno":
                        return ProcesarAcusarUNO(jugador, solicitud);

                    default:
                        return CrearRespuesta(solicitud, false, "Acción desconocida");
                }
            }
        }

        private Mensajes ProcesarJugarCarta(Jugador jugador, Mensajes solicitud)
        {
            if (Juego.partidaTerminada)
            {
                return CrearRespuesta(solicitud, false, "La partida ya terminó");
            }

            if (!Juego.esTurnoDe(jugador))
            {
                return CrearRespuesta(solicitud, false, "No es tu turno");
            }

            if (string.IsNullOrWhiteSpace(solicitud.CartaId))
            {
                return CrearRespuesta(solicitud, false, "No se indicó la carta que deseas jugar");
            }

            Carta carta = null;

            foreach (Carta cartaJugador in jugador.Cartas)
            {
                if (cartaJugador.Id == solicitud.CartaId)
                {
                    carta = cartaJugador;
                    break;
                }
            }

            if (carta == null)
            {
                return CrearRespuesta(solicitud, false, "La carta no pertenece a tu mano");
            }

            if (!Juego.puedeJugar(jugador, carta))
            {
                return CrearRespuesta(solicitud, false, "No puedes jugar esa carta");
            }

            bool esComodin =
                carta.Tipo == "Comodin" || carta.Tipo == "+4";

            if (esComodin && !ColorValido(solicitud.ColorElegido))
            {
                return CrearRespuesta(solicitud, false, "Debes elegir un color válido para el comodín");
            }

            // La carta se retira de la mano del jugador.
            jugador.quitarCarta(carta);

            // Se coloca en el centro.
            Juego.agregarCartaDescarte(carta);

            // Se registra la acción.
            Juego.registroAcciones.Add(jugador.Nombre + " jugó " + carta.Valor);

            // Determinar si el jugador se quedó con una carta.
            if (jugador.Cartas.Count == 1)
            {
                Juego.jugadorUNO = jugador;
                Juego.oportunidadUNO = true;
                Juego.declaroUNO = false;
            }
            else
            {
                Juego.jugadorUNO = null;
                Juego.oportunidadUNO = false;
                Juego.declaroUNO = false;
            }

            // Si se quedó sin cartas, gana.
            if (Juego.esGanador(jugador))
            {
                Juego.comprobarGanador(jugador);

                return CrearRespuesta(solicitud, true, "¡Has ganado la partida!");
            }

            // Aplicar el efecto de la carta.
            switch (carta.Tipo)
            {
                case "Reversa":
                    Juego.aplicarReversa();
                    Juego.cambiarTurno();
                    break;

                case "Salta":
                    Juego.aplicarSalta();
                    break;

                case "+2":
                    Juego.aplicarMasDos();
                    break;

                case "+4":
                    Juego.aplicarMasCuatro(solicitud.ColorElegido);
                    break;

                case "Comodin":
                    Juego.aplicarComodin(solicitud.ColorElegido);
                    Juego.cambiarTurno();
                    break;

                default:
                    Juego.cambiarTurno();
                    break;
            }

            return CrearRespuesta(solicitud, true, "Carta jugada correctamente");
        }

        private Mensajes ProcesarRobarCarta(Jugador jugador, Mensajes solicitud)
        {
            if (Juego.partidaTerminada)
            {
                return CrearRespuesta(solicitud, false, "La partida ya terminó");
            }

            if (!Juego.esTurnoDe(jugador))
            {
                return CrearRespuesta(solicitud, false, "No es tu turno");
            }

            Carta carta = Juego.robarDuranteTurno(jugador);

            if (carta == null)
            {
                return CrearRespuesta(solicitud, false, "No hay cartas disponibles para robar");
            }

            // En esta implementación, robar termina el turno.
            Juego.cambiarTurno();

            return CrearRespuesta(solicitud, true, "Has robado una carta");
        }

        private Mensajes ProcesarDeclararUNO(Jugador jugador, Mensajes solicitud)
        {
            if (Juego.declararUNO(jugador))
            {
                return CrearRespuesta(solicitud, true, "¡UNO declarado correctamente!");
            }

            return CrearRespuesta(solicitud, false, "No puedes declarar UNO en este momento");
        }

        private Mensajes ProcesarAcusarUNO(
            Jugador jugador,
            Mensajes solicitud)
        {
            if (!solicitud.JugadorObjetivoId.HasValue)
            {
                return CrearRespuesta(solicitud, false, "Debes indicar al jugador que deseas acusar");
            }

            Jugador objetivo = ObtenerJugadorInterno(solicitud.JugadorObjetivoId.Value);

            if (objetivo == null || objetivo == jugador)
            {
                return CrearRespuesta(solicitud, false, "El jugador indicado no es válido");
            }

            if (!Juego.noDeclaroUNO(objetivo))
            {
                return CrearRespuesta(solicitud, false, "El jugador no puede recibir una penalización por UNO");
            }

            for (int i = 0; i < 2; i++)
                Juego.agregaCartaRobada(objetivo);

            Juego.oportunidadUNO = false;
            Juego.jugadorUNO = null;

            Juego.registroAcciones.Add(jugador.Nombre + " acusó a " + objetivo.Nombre + " por no declarar UNO");

            return CrearRespuesta(solicitud, true, "La acusación fue correcta. El jugador recibió dos cartas");
        }

        private bool ColorValido(string color)
        {
            return color == "Rojo" ||
                   color == "Azul" ||
                   color == "Amarillo" ||
                   color == "Verde";
        }

        private Jugador ObtenerJugadorInterno(int id)
        {
            if (Juego == null)
                return null;

            foreach (Jugador jugador in Juego.jugadores)
            {
                if (jugador.ID == id)
                    return jugador;
            }

            return null;
        }

        private Mensajes CrearRespuesta(Mensajes solicitud, bool aceptado, string mensaje)
        {
            return new Mensajes
            {
                Tipo = "Respuesta",
                IdSolicitud = solicitud == null
                    ? null
                    : solicitud.IdSolicitud,
                Aceptado = aceptado,
                Mensaje = mensaje
            };
        }

        // Genera un estado personalizado para cada jugador.
        public EstadoPartidaDto CrearEstadoPara(int id)
        {
            lock (candado)
            {
                EstadoPartidaDto estado = new EstadoPartidaDto();

                estado.PartidaIniciada = Iniciada;
                estado.PartidaTerminada =
                    Juego != null && Juego.partidaTerminada;

                estado.JugadoresRegistrados = jugadores.Count;

                if (Juego == null)
                {
                    foreach (JugadorServidor participante in jugadores)
                    {
                        estado.Jugadores.Add(
                            new JugadorEstadoDto
                            {
                                Id = participante.Id,
                                Nombre = participante.Nombre,
                                CantidadCartas = 0
                            }
                        );
                    }

                    return estado;
                }

                Jugador actual = Juego.obtenerJugadorActual();

                if (actual != null)
                    estado.JugadorActual = actual.Nombre;

                estado.Sentido = Juego.direccion;

                Carta centro = Juego.cartaActual;

                if (centro != null)
                {
                    estado.CartaActual =
                        centro.Color + " " + centro.Valor;

                    estado.CartaCentro = ConvertirCarta(centro);
                }

                if (Juego.Ganador != null)
                    estado.Ganador = Juego.Ganador.Nombre;

                foreach (Jugador jugador in Juego.jugadores)
                {
                    estado.Jugadores.Add(
                        new JugadorEstadoDto
                        {
                            Id = jugador.ID,
                            Nombre = jugador.Nombre,
                            CantidadCartas = jugador.cantidadCartas(),
                            EsTurnoActual = jugador == actual,
                            DebeDeclararUNO =
                                Juego.noDeclaroUNO(jugador)
                        }
                    );

                    // Únicamente se incluyen las cartas del destinatario.
                    if (jugador.ID == id)
                    {
                        foreach (Carta carta in jugador.Cartas)
                        {
                            estado.MisCartas.Add(ConvertirCarta(carta));
                        }
                    }
                }

                return estado;
            }
        }

        private CartaEstadoDto ConvertirCarta(Carta carta)
        {
            if (carta == null)
                return null;

            return new CartaEstadoDto
            {
                Id = carta.Id,
                Color = carta.Color,
                Valor = carta.Valor,
                Tipo = carta.Tipo
            };
        }

        public void EliminarJugador(JugadorServidor jugador)
        {
            if (jugador == null)
                return;

            lock (candado)
            {
                bool eliminado = jugadores.Remove(jugador);

                jugador.Registrado = false;

                if (!eliminado)
                    return;

                if (Iniciada)
                {
                    Interrumpida = true;
                    Iniciada = false;

                    Console.WriteLine("La partida fue interrumpida");
                }

                if (jugadores.Count == 0)
                {
                    Juego = null;
                    Iniciada = false;
                    Interrumpida = false;

                    Console.WriteLine("Sala disponible para una nueva partida");
                }
            }
        }

        public int CantidadJugadores
        {
            get
            {
                lock (candado)
                {
                    return jugadores.Count;
                }
            }
        }

        public bool SalaLlena
        {
            get
            {
                lock (candado)
                {
                    return jugadores.Count >= MaxJugadores || Interrumpida || Iniciada;
                }
            }
        }
    }
}
