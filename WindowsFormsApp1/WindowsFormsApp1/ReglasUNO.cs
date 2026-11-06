
using System;

namespace proyectoUNO
{
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
                   (carta.Tipo == "Comodin" ||
                    carta.Tipo == "+4");
        }

        public static bool EsCartaDePenalizacion(Carta carta)
        {
            return carta != null &&
                   (carta.Tipo == "+2" ||
                    carta.Tipo == "+4");
        }

        public static bool EsCartaValida(
            Carta carta,
            Carta cartaActual)
        {
            if (carta == null || cartaActual == null)
                return false;

            if (EsComodin(carta))
                return true;

            if (carta.Color == cartaActual.Color)
                return true;

            if (carta.Valor == cartaActual.Valor)
                return true;

            if (carta.Tipo == cartaActual.Tipo)
                return true;

            if (carta.Simbolo == cartaActual.Simbolo)
                return true;

            return false;
        }

        public static bool PuedeJugar(
            juegoUNO juego,
            Jugador jugador,
            Carta carta)
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
                if (juego.CartaRobada == null ||
                    carta.Id != juego.CartaRobada.Id)
                    return false;
            }

            return EsCartaValida(carta, juego.cartaActual);
        }

        public static bool PuedeElegirColor(
            Carta carta,
            string colorElegido)
        {
            if (!EsComodin(carta))
                return true;

            return EsColorValido(colorElegido);
        }

        public static bool PuedeRobar(
            juegoUNO juego,
            Jugador jugador)
        {
            if (juego == null || jugador == null)
                return false;

            if (juego.partidaTerminada)
                return false;

            if (!juego.esTurnoDe(jugador))
                return false;

            if (juego.TieneCartaRobadaPendiente(jugador))
                return false;

            return true;
        }

        public static bool PuedePasarTurno(
            juegoUNO juego,
            Jugador jugador)
        {
            if (juego == null || jugador == null)
                return false;

            if (juego.partidaTerminada)
                return false;

            if (!juego.esTurnoDe(jugador))
                return false;

            return juego.TieneCartaRobadaPendiente(jugador);
        }

        public static bool PuedeDeclararUNO(
            juegoUNO juego,
            Jugador jugador)
        {
            if (juego == null || jugador == null)
                return false;

            return juego.jugadorUNO == jugador &&
                   juego.oportunidadUNO &&
                   !juego.declaroUNO &&
                   jugador.Cartas.Count == 1;
        }

        public static bool PuedeAcusarUNO(
            juegoUNO juego,
            Jugador acusador,
            Jugador objetivo)
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
