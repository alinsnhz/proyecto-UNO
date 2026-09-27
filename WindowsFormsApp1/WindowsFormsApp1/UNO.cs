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

        // Ahora actualiza los 3 paneles cada vez, no solo el del turno actual
        private void ActualizarInterfaz()
        {
            lblNombreJ1.Text = nombresJugadores[0];
            lblNombreJ2.Text = nombresJugadores[1];
            lblNombreJ3.Text = nombresJugadores[2];

            MostrarCartas(panelMano, manosJugadores[0], 0);
            MostrarCartas(panelManoJugador2, manosJugadores[1], 1);
            MostrarCartas(panelManoJugador3, manosJugadores[2], 2);

            // Resalta de quién es el turno (opcional, pero se ve bien)
            lblNombreJ1.Font = new Font("Segoe UI", 11, turnoActual == 0 ? FontStyle.Bold : FontStyle.Regular);
            lblNombreJ2.Font = new Font("Segoe UI", 11, turnoActual == 1 ? FontStyle.Bold : FontStyle.Regular);
            lblNombreJ3.Font = new Font("Segoe UI", 11, turnoActual == 2 ? FontStyle.Bold : FontStyle.Regular);
        }

        private void MostrarCartas(FlowLayoutPanel panel, List<string> cartas, int indiceJugador)
        {
            panel.Controls.Clear();

            int cantidad = cartas.Count;
            if (cantidad == 0)
            {
                MessageBox.Show($"{nombresJugadores[indiceJugador]} se quedó sin cartas. ¡Ganó!");
                return;
            }

            int margen = 4;
            int anchoMinimo = 55;
            int anchoMaximo = 110;
            int anchoDisponible = panel.ClientSize.Width - (margen * (cantidad + 1));
            int anchoBoton = Math.Max(anchoMinimo, Math.Min(anchoDisponible / cantidad, anchoMaximo));

            foreach (var carta in cartas)
            {
                var btn = new Button
                {
                    Width = anchoBoton,
                    Height = panel.ClientSize.Height - 10,
                    Text = carta,
                    BackColor = ColorSegunTexto(carta),
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    Margin = new Padding(margen / 2),
                    Tag = new object[] { carta, indiceJugador }, // ahora guardamos también DE QUIÉN es la carta
                    Enabled = (indiceJugador == turnoActual) // solo se puede clickear si es su turno
                };
                btn.Click += BtnCarta_Click;
                panel.Controls.Add(btn);
            }
        }

        
    }
}