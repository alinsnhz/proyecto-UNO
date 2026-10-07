namespace proyectoUNO
{
    public class Carta
    {
        public string Color { get; set; }
        public string Valor { get; set; }
        public string Numero { get; set; }
        public string Simbolo { get; set; }
        public string Tipo { get; set; }
        public string Numero { get; set; }
        public string Simbolo { get; set; }

        public Carta(string color, string valor, string tipo)
        {
            Color = color;
            Valor = valor;
            Numero = valor;
            Simbolo = valor;
            Tipo = tipo;
            Numero = valor;  
            Simbolo = valor; 
        }

        public Carta() { }
    }
}