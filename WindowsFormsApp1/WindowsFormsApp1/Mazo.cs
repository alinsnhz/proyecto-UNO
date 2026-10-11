using System;
using System.Collections.Generic;

namespace proyectoUNO
{
    /*
     * Administra las cartas que todavía pueden robarse
    */
    public class Mazo
    {
        public List<Carta> Cartas { get; set; }

        //crera la lista vacía de cartas
        public Mazo()
        {
            Cartas = new List<Carta>();
        }

        // limpia el mazo y construye las cartas de cuatro colores, cartas numéricas, acciones
        // y comodines. La cantidad esperada es 108
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

        // mezcla las cartas usando el algoritmo de Fisher-Yates y posiciones aleatorias
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

        // devuelve y elimina la primera carta, si el mazo está vacío devuelve null
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

        // devuelve cuántas cartas quedan
        public int cartasRestantes()
        {
            return Cartas.Count;
        }

        // devuelve true cuando el mazo tiene 108 cartas
        public bool tieneCantidadCorrecta()
        {
            return Cartas.Count == 108;
        }
    }
}