
using System;
using System.Collections.Generic;
using proyectoUNO;

namespace ProyectoUno.Servidor
{
    // clase responsable de administrar la sala y decidir cúando comienza la partida
    public class PartidaServidor
    {
        private const int MaxJugadores = 3;

        private List<JugadorServidor> jugadores =
            new List<JugadorServidor>();

        private readonly object candado = new object();

        public bool Iniciada { get; private set; }

        public bool Interrumpida { get; private set; }
        public JuegoUNO Juego { get; private set; }

        // Registra un jugador en la sala.
        public bool RegistrarJugador(JugadorServidor jugador)
        {
            lock (candado)
            {
                if (jugador == null || jugador.Registrado)
                    return false;

                // No permitir más de tres jugadores.
                if (jugadores.Count >= MaxJugadores)
                    return false;

                // No permitir nuevos jugadores si la partida
                // ya comenzó o fue interrumpida.
                if (Iniciada || Interrumpida)
                    return false;

                // Evitar IDs repetidos.
                foreach (JugadorServidor participante in jugadores)
                {
                    if (participante.Id == jugador.Id)
                        return false;
                }

                jugadores.Add(jugador);

                jugador.Registrado = true;

                // La partida comienza únicamente con tres jugadores.
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

        // Crea e inicia una única partida compartida.
        private bool IniciarPartida()
        {
            try
            {
                Console.WriteLine("Iniciando partida de UNO...");

                // Crear una sola instancia del motor.
                Juego = new JuegoUNO();

                // Agregar los tres jugadores al mismo juego.
                foreach (JugadorServidor participante in jugadores)
                {
                    Jugador nuevoJugador = new Jugador(
                        participante.Id,
                        participante.Nombre
                    );

                    Juego.jugadores.Add(nuevoJugador);
                }

                // Iniciar el juego y repartir las cartas.
                Juego.iniciarPartida();

                // Colocar la primera carta en el centro.
                Juego.colocarCartaInicial();

                Iniciada = true;
                Interrumpida = false;

                Console.WriteLine("¡PARTIDA INICIADA!");

                foreach (JugadorServidor participante in jugadores)
                {
                    Console.WriteLine(
                        "Jugador " + participante.Id +
                        ": " + participante.Nombre
                    );
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "Error al iniciar la partida: " + ex.Message
                );

                Juego = null;
                Iniciada = false;

                return false;
            }
        }

        // Busca al jugador correspondiente a un ID asignado
        // por el servidor.
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

        // Devuelve una copia de los jugadores conectados.
        public List<JugadorServidor> ObtenerJugadores()
        {
            lock (candado)
            {
                return new List<JugadorServidor>(jugadores);
            }
        }

        // Elimina a un jugador cuando se desconecta.
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
                    Iniciada = false;

                    Console.WriteLine(
                        "La partida fue interrumpida."
                    );
                }

                // Cuando todos se desconectan, la sala puede reiniciarse.
                if (jugadores.Count == 0)
                {
                    Juego = null;

                    Iniciada = false;
                    Interrumpida = false;

                    Console.WriteLine(
                        "Sala disponible para una nueva partida."
                    );
                }
            }
        }

        // Cantidad de jugadores registrados.
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

        // Indica si la sala puede recibir jugadores.
        public bool SalaLlena
        {
            get
            {
                lock (candado)
                {
                    return jugadores.Count >= MaxJugadores
                        || Interrumpida;
                }
            }
        }
    }
}
