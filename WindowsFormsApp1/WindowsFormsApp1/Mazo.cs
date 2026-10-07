using System;
using System.Collections.Generic;

namespace proyectoUNO
{
    public class Mazo
    {
        public List<Carta> Cartas { get; set; }

        public Mazo()
        {
            Cartas = new List<Carta>();
        }

        public void crearMazo()
        {
            Cartas.Clear();

            string[] colores = { "Rojo", "Azul", "Amarillo", "Verde" };

            foreach (string color in colores)
            {
                Cartas.Add(new Carta(color, "0", "Número"));

                for (int numero = 1; numero <= 9; numero++)
                {
                    Cartas.Add(new Carta(color, numero.ToString(), "Número"));
                    Cartas.Add(new Carta(color, numero.ToString(), "Número"));
                }

                Cartas.Add(new Carta(color, "Reversa", "Reversa"));
                Cartas.Add(new Carta(color, "Reversa", "Reversa"));

                Cartas.Add(new Carta(color, "Salta", "Salta"));
                Cartas.Add(new Carta(color, "Salta", "Salta"));

                Cartas.Add(new Carta(color, "+2", "+2"));
                Cartas.Add(new Carta(color, "+2", "+2"));
            }

            for (int i = 0; i < 4; i++)
            {
                Cartas.Add(new Carta("Negro", "Comodin", "Comodin"));
            }

            for (int i = 0; i < 4; i++)
            {
                Cartas.Add(new Carta("Negro", "+4", "+4"));
            }
        }

        public void barajar()
        {
            Random random = new Random();

            for (int i = Cartas.Count - 1; i > 0; i--)
            {
                int posicion = random.Next(i + 1);

                Carta temporal = Cartas[i];
                Cartas[i] = Cartas[posicion];
                Cartas[posicion] = temporal;
            }
        }

        public Carta robarCarta()
        {
            if (Cartas.Count == 0)
            {
                return null;
            }

            Carta carta = Cartas[0];
            Cartas.RemoveAt(0);

            return carta;
        }

        public int cartasRestantes()
        {
            return Cartas.Count;
        }

        public bool tieneCantidadCorrecta()
        {
            return Cartas.Count == 108;
        }
    }
}