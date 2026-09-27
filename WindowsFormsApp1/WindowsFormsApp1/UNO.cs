using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class UNO : Form
    {
        public UNO()
        {
            InitializeComponent();
        }

        private void UNO_Load(object sender, EventArgs e)
        {
            MostrarCartasDeEjemplo(); //remplazar por namo del jugador 
        }

        private void MostrarCartasDeEjemplo()
        {
            panelMano.Controls.Clear();

            string[] cartasEjemplo = { "Rojo 5", "Azul 7", "Verde 7", "Amarillo 1", "Amarillo 3", "Amarillo 6" };
            Color[] coloresEjemplo = { Color.LightCoral, Color.LightSkyBlue, Color.LightGreen, Color.LightYellow, Color.LightYellow, Color.LightYellow };

            for (int i = 0; i < cartasEjemplo.Length; i++)
            {
                var btn = new Button
                {
                    Width = 90,
                    Height = 130,
                    Text = cartasEjemplo[i],
                    BackColor = coloresEjemplo[i],
                    Font = new Font("Segoe UI", 12, FontStyle.Bold)
                };
                panelMano.Controls.Add(btn);
            }
        }
    }
}