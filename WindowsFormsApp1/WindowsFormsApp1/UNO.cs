using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class UNO : Form
    {
        private List<List<string>> manosJugadores = new List<List<string>>
        {
            new List<string> { "Rojo 5", "Azul 7", "Verde 7", "Amarillo 1", "Amarillo 3", "Amarillo 6" },
            new List<string> { "Rojo 2", "Verde 9", "Azul 4" },
            new List<string> { "Amarillo 8", "Rojo 6", "Verde 1", "Azul 3" }
        };

        private string[] nombresJugadores = { "Jugador 1", "Jugador 2", "Jugador 3" };
        private int turnoActual = 0;

        public UNO()
        {
            InitializeComponent();
        }

        private void UNO_Load(object sender, EventArgs e)
        {
            ActualizarInterfaz();
        }

        private void ActualizarInterfaz()
        {
            lblNombreJ1.Text = nombresJugadores[0];
            lblNombreJ2.Text = nombresJugadores[1];
            lblNombreJ3.Text = nombresJugadores[2];

            /*MostrarCartas(panelMano, manosJugadores[0], 0);
            MostrarCartas(panelManoJugador2, manosJugadores[1], 1);
            MostrarCartas(panelManoJugador3, manosJugadores[2], 2);*/

            // Resalta de quién es el turno (opcional, pero se ve bien)
            lblNombreJ1.Font = new Font("Segoe UI", 11, turnoActual == 0 ? FontStyle.Bold : FontStyle.Regular);
            lblNombreJ2.Font = new Font("Segoe UI", 11, turnoActual == 1 ? FontStyle.Bold : FontStyle.Regular);
            lblNombreJ3.Font = new Font("Segoe UI", 11, turnoActual == 2 ? FontStyle.Bold : FontStyle.Regular);
        }
    }
}