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
        public string nombre { get; set; }
        public List<Carta> Cartas { get; set; }
    }
}
