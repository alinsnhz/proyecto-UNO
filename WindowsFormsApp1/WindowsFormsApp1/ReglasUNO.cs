using System;
using System.Collections.Generic;
using proyectoUNO;

namespace proyectoUNO
{
    // clase para validar las jugadas antes de que el motor modifique la partida
    public static class ReglasUNO
    {
        public static bool EsColorValido(string color)
        {
            return color == "Rojo" ||
                   color == "Azul" ||
                   color == "Amarillo" ||
                   color == "Verde";
        }

        public static bool EsComodin(Carta carta)
        {
            return carta != null &&
                (carta.Tipo == "Comodin" || carta.Tipo == "+4");
        }

        public static bool EsCartaDePenalizacion(Carta carta)
        {
            return carta != null &&
                   (carta.Tipo == "+2" || carta.Tipo == "+4");
        }

        public static bool EsCartaValida(Carta carta, Carta cartaActual)
        {
            if (carta == null || cartaActual == null)
                return false;

            // Los comodines pueden jugarse sobre cualquier carta.
            if (EsComodin(carta))
                return true;

            // Coincidencia por color
            if (carta.Color == cartaActual.Color)
                return true;

            // Coincidencia por valor
            if (carta.Valor == cartaActual.Valor)
                return true;

            // Coincidencia por tipo o símbolo
            if (carta.Tipo == cartaActual.Tipo)
                return true;

            if (carta.Simbolo == cartaActual.Simbolo)
                return true;

            return false;
        }

        public static bool PuedeJugar(JuegoUNO juego, Jugador jugador, Carta carta)
        {
            if (juego == null ||
                jugador == null ||
                carta == null)
                return false;

            if (juego.partidaTerminada)
                return false;

            if (!juego.esTurnoDe(jugador))
                return false;

            if (!jugador.Cartas.Contains(carta))
                return false;

            if (juego.TieneCartaRobadaPendiente(jugador))
            {
                // Después de robar, solamente puede jugar
                // la carta que acaba de tomar.
                if (juego.CartaRobada == null ||
                    carta.Id != juego.CartaRobada.Id)
                    return false;
            }

            return EsCartaValida(carta, juego.cartaActual);
        }

        public static bool PuedeElegirColor(Carta carta, string colorElegido)
        {
            if (!EsComodin(carta))
                return true;

            return EsColorValido(colorElegido);
        }

        public static bool PuedeRobar(JuegoUNO juego, Jugador jugador)
        {
            if (juego == null || jugador == null)
                return false;

            if (juego.partidaTerminada)
                return false;

            if (!juego.esTurnoDe(jugador))
                return false;

            // No se permite robar varias veces en el mismo turno.
            if (juego.TieneCartaRobadaPendiente(jugador))
                return false;

            return true;
        }

        public static bool PuedePasarTurno(JuegoUNO juego, Jugador jugador)
        {
            if (juego == null || jugador == null)
                return false;

            if (juego.partidaTerminada)
                return false;

            if (!juego.esTurnoDe(jugador))
                return false;

            // Solo puede pasar si ya robó una carta.
            return juego.TieneCartaRobadaPendiente(jugador);
        }

        public static bool PuedeDeclararUNO(JuegoUNO juego, Jugador jugador)
        {
            if (juego == null || jugador == null)
                return false;

            return juego.jugadorUNO == jugador &&
                   juego.oportunidadUNO &&
                   !juego.declaroUNO &&
                   jugador.Cartas.Count == 1;
        }

        public static bool PuedeAcusarUNO(JuegoUNO juego, Jugador acusador, Jugador objetivo)
        {
            if (juego == null ||
                acusador == null ||
                objetivo == null)
                return false;

            if (acusador == objetivo)
                return false;

            if (juego.partidaTerminada)
                return false;

            return juego.jugadorUNO == objetivo &&
                   juego.oportunidadUNO &&
                   !juego.declaroUNO &&
                   objetivo.Cartas.Count == 1;
        }
    }
}
