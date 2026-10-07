namespace proyectoUNO
{
    public class Carta
    {
        public string Color { get; set; }
        public string Valor { get; set; }
        public string Numero { get; set; }
        public string Simbolo { get; set; }
        public string Tipo { get; set; }
<<<<<<< HEAD
=======
        
>>>>>>> 8a199ee1524f602e87b55d7c4ff29631403192d0

        public Carta(string color, string valor, string tipo)
        {
            Color = color;
            Valor = valor;
            Numero = valor;
            Simbolo = valor;
            Tipo = tipo;
        }

        public Carta() { }
    }
}