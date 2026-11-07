
using System;
using System.Collections.Generic;
using proyectoUNO;
using ProyectoUno.Protocolo;

namespace ProyectoUno.Servidor
{
    public class PartidaServidor
    {
        private const int MaxJugadores = 3;

        private readonly List<JugadorServidor> jugadores = new List<JugadorServidor>();

        private readonly object candado = new object();

        public juegoUNO? Juego { get; private set; }

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
                    Jugador nuevoJugador =
                        new Jugador(participante.Id, participante.Nombre);

                    nuevoJuego.jugadores.Add(nuevoJugador);
                }

                nuevoJuego.iniciarPartida();
                nuevoJuego.colocarCartaInicial();

                if (nuevoJuego.cartaActual == null)
                    throw new Exception("No fue posible colocar la carta inicial.");

                Juego = nuevoJuego;

                Iniciada = true;
                Interrumpida = false;

                Console.WriteLine("¡PARTIDA INICIADA!");

                foreach (JugadorServidor participante in jugadores)
                {
                    Console.WriteLine(
                        "Jugador " + participante.Id + ": " + participante.Nombre);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "Error al iniciar la partida: " + ex.Message);

                Juego = null;
                Iniciada = false;

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
                return ObtenerJugadorInterno(id);
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
                    return CrearRespuesta(
                        solicitud, false, "La partida no está disponible.");
                }

                if (solicitud == null ||
                    string.IsNullOrWhiteSpace(solicitud.Tipo))
                {
                    return CrearRespuesta(
                        solicitud, false, "La acción no es válida.");
                }

                Jugador jugador = ObtenerJugadorInterno(id);

                if (jugador == null)
                {
                    return CrearRespuesta(
                        solicitud, false,
                        "El jugador no pertenece a esta partida.");
                }

                if (Juego.partidaTerminada)
                {
                    return CrearRespuesta(
                        solicitud, false, "La partida ya terminó.");
                }

                string tipo = solicitud.Tipo.Trim().ToLowerInvariant();

                switch (tipo)
                {
                    case "jugarcarta":
                        return ProcesarJugarCarta(jugador, solicitud);

                    case "robarcarta":
                    case "robar":
                        return ProcesarRobarCarta(jugador, solicitud);

                    case "pasarturno":
                    case "pasar":
                        return ProcesarPasarTurno(jugador, solicitud);

                    case "declararuno":
                        return ProcesarDeclararUNO(jugador, solicitud);

                    case "acusaruno":
                        return ProcesarAcusarUNO(jugador, solicitud);

                    default:
                        return CrearRespuesta(
                            solicitud, false, "Acción desconocida.");
                }
            }
        }

        private Mensajes ProcesarJugarCarta(
            Jugador jugador,
            Mensajes solicitud)
        {
            if (string.IsNullOrWhiteSpace(solicitud.CartaId))
            {
                return CrearRespuesta(
                    solicitud, false,
                    "No se indicó la carta que deseas jugar.");
            }

            if (!Juego.esTurnoDe(jugador))
            {
                return CrearRespuesta(
                    solicitud, false, "No es tu turno.");
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
                return CrearRespuesta(
                    solicitud, false,
                    "La carta no pertenece a tu mano.");
            }

            // Se valida todo antes de modificar el estado.
            if (!ReglasUNO.PuedeJugar(Juego, jugador, carta))
            {
                return CrearRespuesta(
                    solicitud, false,
                    "No puedes jugar esa carta.");
            }

            if (!ReglasUNO.PuedeElegirColor(
                carta, solicitud.ColorElegido))
            {
                return CrearRespuesta(
                    solicitud, false,
                    "Debes elegir un color válido para el comodín.");
            }

            // Si había una oportunidad UNO anterior, se cierra
            // cuando otro jugador comienza una acción válida.
            CerrarOportunidadUNO(jugador);

            CartaEstadoDto cartaJugada = new CartaEstadoDto
            {
                Id = carta.Id,
                Color = carta.Color,
                Valor = carta.Valor,
                Tipo = carta.Tipo
            };

            // Se retira la carta únicamente después de validarla.
            jugador.quitarCarta(carta);

            // Si era la carta recién robada, se limpia el estado.
            if (Juego.TieneCartaRobadaPendiente(jugador))
            {
                Juego.LimpiarCartaRobada();
            }

            Juego.agregarCartaDescarte(carta);

            Juego.registroAcciones.Add(
                jugador.Nombre + " jugó " + carta.Valor);

            // Si se quedó sin cartas, gana inmediatamente.
            if (Juego.esGanador(jugador))
            {
                Juego.comprobarGanador(jugador);

                Juego.jugadorUNO = null;
                Juego.oportunidadUNO = false;
                Juego.declaroUNO = false;

                return CrearRespuesta(
                    solicitud,
                    true,
                    "¡Has ganado la partida!",
                    cartaJugada);
            }

            // Se registra la oportunidad de declarar UNO.
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

            // Los efectos de las cartas se aplican una sola vez.
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

            return CrearRespuesta(solicitud, true, "Carta jugada correctamente.", cartaJugada);
        }

        private Mensajes ProcesarRobarCarta(
            Jugador jugador,
            Mensajes solicitud)
        {
            if (!ReglasUNO.PuedeRobar(Juego, jugador))
            {
                return CrearRespuesta(
                    solicitud, false,
                    "No puedes robar una carta en este momento.");
            }

            Carta carta = Juego.robarDuranteTurno(jugador);

            if (carta == null)
            {
                return CrearRespuesta(
                    solicitud, false,
                    "No hay cartas disponibles para robar.");
            }

            CartaEstadoDto cartaRobada = new CartaEstadoDto
            {
                Id = carta.Id,
                Color = carta.Color,
                Valor = carta.Valor,
                Tipo = carta.Tipo
            };

            CerrarOportunidadUNO(jugador);

            // El turno NO cambia. El jugador puede jugar
            // la carta recién robada o pasar.
            return CrearRespuesta(
                solicitud,
                true,
                "Has robado una carta. Puedes jugarla si es válida o pasar tu turno.",
                cartaRobada);
        }

        private Mensajes ProcesarPasarTurno(
            Jugador jugador,
            Mensajes solicitud)
        {
            if (!ReglasUNO.PuedePasarTurno(Juego, jugador))
            {
                return CrearRespuesta(
                    solicitud, false,
                    "Debes robar una carta antes de pasar el turno.");
            }

            CerrarOportunidadUNO(jugador);

            if (!Juego.PasarTurno(jugador))
            {
                return CrearRespuesta(
                    solicitud, false,
                    "No fue posible terminar tu turno.");
            }

            return CrearRespuesta(
                solicitud, true, "Tu turno ha terminado.");
        }

        private Mensajes ProcesarDeclararUNO(
            Jugador jugador,
            Mensajes solicitud)
        {
            if (Juego.declararUNO(jugador))
            {
                return CrearRespuesta(
                    solicitud, true,
                    "¡UNO declarado correctamente!");
            }

            return CrearRespuesta(
                solicitud, false,
                "No puedes declarar UNO en este momento.");
        }

        private Mensajes ProcesarAcusarUNO(
            Jugador jugador,
            Mensajes solicitud)
        {
            if (!solicitud.JugadorObjetivoId.HasValue)
            {
                return CrearRespuesta(
                    solicitud, false,
                    "Debes indicar al jugador que deseas acusar.");
            }

            Jugador objetivo =
                ObtenerJugadorInterno(solicitud.JugadorObjetivoId.Value);

            if (!ReglasUNO.PuedeAcusarUNO(Juego, jugador, objetivo))
            {
                return CrearRespuesta(
                    solicitud, false,
                    "No puedes acusar a ese jugador por UNO.");
            }

            // La acusación fue válida: se aplican dos cartas.
            for (int i = 0; i < 2; i++)
                Juego.agregaCartaRobada(objetivo);

            Juego.registroAcciones.Add(
                jugador.Nombre + " acusó a " + objetivo.Nombre +
                " por no declarar UNO.");

            Juego.jugadorUNO = null;
            Juego.oportunidadUNO = false;
            Juego.declaroUNO = false;

            return CrearRespuesta(
                solicitud, true,
                "La acusación fue correcta. El jugador recibió dos cartas.");
        }

        private void CerrarOportunidadUNO(Jugador jugadorQueActua)
        {
            if (!Juego.oportunidadUNO ||
                Juego.jugadorUNO == null)
                return;

            // El jugador que quedó con una carta conserva
            // la oportunidad hasta que otro jugador actúe.
            if (Juego.jugadorUNO == jugadorQueActua)
                return;

            Juego.jugadorUNO = null;
            Juego.oportunidadUNO = false;
            Juego.declaroUNO = false;
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

        private Mensajes CrearRespuesta(
            Mensajes solicitud,
            bool aceptado,
            string mensaje,
            CartaEstadoDto cartaJugada = null)
        {
            return new Mensajes
            {
                Tipo = "Respuesta",
                IdSolicitud = solicitud == null
                    ? null
                    : solicitud.IdSolicitud,
                Aceptado = aceptado,
                Mensaje = mensaje,
                CartaJugada = cartaJugada
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
                            });
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
                        });

                    // Cada jugador solamente recibe sus propias cartas.
                    if (jugador.ID == id)
                    {
                        foreach (Carta carta in jugador.Cartas)
                        {
                            estado.MisCartas.Add(
                                ConvertirCarta(carta));
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

                    Console.WriteLine(
                        "La partida fue interrumpida.");
                }

                if (jugadores.Count == 0)
                {
                    Juego = null;
                    Iniciada = false;
                    Interrumpida = false;

                    Console.WriteLine(
                        "Sala disponible para una nueva partida.");
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
                    return jugadores.Count >= MaxJugadores ||
                           Interrumpida ||
                           Iniciada;
                }
            }
        }
    }
}
