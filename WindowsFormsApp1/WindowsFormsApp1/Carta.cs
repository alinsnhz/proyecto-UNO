namespace proyectoUNO
{
    public class Carta
    {
        public string Color { get; set; }
        public string Valor { get; set; }
        public string Tipo { get; set; }

        public Carta(string color, string valor, string tipo)
        {
            Color = color;
            Valor = valor;
            Tipo = tipo;
        }
    }
}