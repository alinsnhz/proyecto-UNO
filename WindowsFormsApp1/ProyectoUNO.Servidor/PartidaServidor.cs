using System.Collections.Generic;

namespace ProyectoUno.Servidor
{
    // clase responsable de administrar la sala y decidir cúando comienza la partida
    public class PartidaServidor
    {
        private const int MaxJugadores = 3;

        private List<JugadorServidor> jugadores =
            new List<JugadorServidor>();

        public bool Iniciada { get; private set; }
        public bool Interrumpida { get; private set; }

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
                    Iniciada = true;
                }

                return true;
            }
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