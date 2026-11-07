using System.Collections.Generic;

namespace proyectoUNO
{
    public class Jugador
    {
        public int ID { get; set; }
        public string Nombre { get; set; }
        public List<Carta> Cartas { get; set; }

        public Jugador(int id, string nombre)
        {
            ID = id;
            Nombre = nombre;
            Cartas = new List<Carta>();
        }

        public void agregarCarta(Carta carta)
        {
            if (carta != null)
                Cartas.Add(carta);
        }

        public void quitarCarta(Carta carta)
        {
            if (carta != null)
                Cartas.Remove(carta);
        }

        public int cantidadCartas()
        {
            return Cartas.Count;
        }
    }
}
