using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace proyectoUNO
{
    internal class Jugador
    {
        public int ID { get; set; }
        public string Nombre { get; set; }
        public List<Carta> Cartas { get; set; }

        public Jugador (int id, string nombre)
        {
            ID = id;
            Nombre = nombre;
            Cartas = new List<Carta>();
        }

        public void AgregarCarta(Carta carta)
        {
            Cartas.Add(carta);
        }
    }
}
