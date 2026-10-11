using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace proyectoUNO
{
    /*
     * Representa a un jugador que participa en la partida y mantiene 
     * su mano de cartas
    */
    public class Jugador
    {
        //propiedades que alamcenan el id, nombre y mano del jugador
        public int ID { get; set; }
        public string Nombre { get; set; }
        public List<Carta> Cartas { get; set; }

        // asigna el identificador y el nombre, e inicializa la lista de cartas vacía
        public Jugador (int id, string nombre)
        {
            ID = id;
            Nombre = nombre;
            Cartas = new List<Carta>();
        }

        // añade una carta a la mano del jugador
        public void agregarCarta(Carta carta)
        {
            Cartas.Add(carta);
        }

        // elimina de la mano la carta indicada
        public void quitarCarta(Carta carta)
        {
            Cartas.Remove(carta);
        }

        // devuelve el número de cartas que tiene el jugador
        public int cantidadCartas()
        {
            return Cartas.Count;
        }
    }
}
