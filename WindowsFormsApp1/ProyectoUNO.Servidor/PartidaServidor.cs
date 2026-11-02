using System.Collections.Generic;
using proyectoUNO;

namespace ProyectoUno.Servidor
{
    public class PartidaServidor
    {
        private const int MaxJugadores = 3;

        private List<JugadorServidor> jugadores =
            new List<JugadorServidor>();

        public bool Iniciada { get; private set; }
        public bool Interrumpida { get; private set; }
        public JuegoUNO Juego { get; private set; }
        private readonly object candado = new object();

        public bool RegistrarJugador(JugadorServidor jugador)
        {
            lock (candado)
            {
                // No permitir más de tres jugadores.
                if (jugadores.Count >= MaxJugadores)
                    return false;

                // No permitir nuevos jugadores si la partida
                // ya comenzó o fue interrumpida.
                if (Iniciada || Interrumpida)
                    return false;

                jugadores.Add(jugador);
                jugador.Registrado = true;

                // La partida comienza únicamente con tres jugadores.
                if (jugadores.Count == MaxJugadores)
                {
                    // Crear una sola instancia del motor de UNO.
                    Juego = new JuegoUNO();

                    // Agregar los tres jugadores al motor.
                    foreach (JugadorServidor participante in jugadores)
                    {
                        Jugador nuevoJugador = new Jugador(
                            participante.Id,
                            participante.Nombre
                        );

                        Juego.jugadores.Add(nuevoJugador);
                    }

                    // Comprobar que se puedan repartir las cartas.
                    if (Juego.puedeRepartirCartasIniciales())
                    {
                        Juego.iniciarPartida();
                        Juego.colocarCartaInicial();

                        Iniciada = true;
                    }
                    else
                    {
                        Juego = null;
                        jugadores.Remove(jugador);
                        jugador.Registrado = false;

                        return false;
                    }
                }

                return true;
            }
        }

        public Jugador ObtenerJugador(int id)
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

        public List<JugadorServidor> ObtenerJugadores()
        {
            lock (candado)
            {
                return new List<JugadorServidor>(jugadores);
            }
        }

        public void EliminarJugador(JugadorServidor jugador)
        {
            lock (candado)
            {
                jugadores.Remove(jugador);
                jugador.Registrado = false;

                // Si alguien abandona una partida que ya comenzó,
                // se interrumpe la partida.
                if (Iniciada && jugadores.Count > 0)
                {
                    Interrumpida = true;
                }

                // Cuando todos se desconectan, la sala puede reiniciarse.
                if (jugadores.Count == 0)
                {
                    Iniciada = false;
                    Interrumpida = false;
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
    }
}