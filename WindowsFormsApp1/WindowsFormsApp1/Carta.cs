namespace proyectoUNO
{
    /*
     * Representa una carta individual del UNO. Sus propiedades guardan
     * el color, valor, número, símbolo y tipo
    */
    public class Carta
    {
        /*
         * propiedades públicas que permiten consultar o modificar los 
         * datos de la carta
        */
        public string Color { get; set; }
        public string Valor { get; set; }
        public string Numero { get; set; }
        public string Simbolo { get; set; }
        public string Tipo { get; set; }

        /*
         * inicializa una carta con los datos recibidos y asigna 
         * el valor también a Número y Símbolo
        */
        public Carta(string color, string valor, string tipo)
        {
            Color = color;
            Valor = valor;
            Numero = valor;
            Simbolo = valor;
            Tipo = tipo;
        }

        /*
         * constructor vacío que permite crear una carta y llenar sus 
         * propiedades después
        */
        public Carta() { }
    }
}