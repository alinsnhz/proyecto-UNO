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

            MostrarCartas(panelMano, manosJugadores[0], 0);
            MostrarCartas(panelManoJugador2, manosJugadores[1], 1);
            MostrarCartas(panelManoJugador3, manosJugadores[2], 2);

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

            float angulo = 0;
            if (indiceJugador == 1) angulo = 90;
            else if (indiceJugador == 2) angulo = 270; 

            foreach (var carta in cartas)
            {
                var btn = new BotonRotado
                {
                    Text = carta,
                    BackColor = ColorSegunTexto(carta),
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    Margin = new Padding(margen / 2),
                    Tag = new object[] { carta, indiceJugador },
                    Enabled = (indiceJugador == turnoActual),
                    Angulo = angulo
                };

                if (indiceJugador == 1 || indiceJugador == 2)
                {
                    int altoFijo = 60;
                    btn.Width = panel.ClientSize.Width - 10;

                    int altoDisponible = (panel.ClientSize.Height / cantidad) - margen;
                    btn.Height = Math.Min(altoFijo, altoDisponible); 
                }
                else
                {
                    int anchoDisponible = panel.ClientSize.Width - (margen * (cantidad + 1));
                    btn.Width = Math.Max(55, Math.Min(anchoDisponible / cantidad, 110));
                    btn.Height = panel.ClientSize.Height - 10;
                }

                btn.Click += BtnCarta_Click;
                panel.Controls.Add(btn);
            }
        }

        private void BtnCarta_Click(object sender, EventArgs e)
        {
            var btn = (Button)sender;
            var datos = (object[])btn.Tag;
            string cartaJugada = (string)datos[0];
            int indiceJugador = (int)datos[1];

            if (indiceJugador != turnoActual) return;

            manosJugadores[indiceJugador].Remove(cartaJugada);
            lblMensaje.Text = $"{nombresJugadores[indiceJugador]} jugó: {cartaJugada}";

            PasarTurno();
        }

        private void PasarTurno()
        {
            turnoActual = (turnoActual + 1) % nombresJugadores.Length;
            ActualizarInterfaz();
        }

        private Color ColorSegunTexto(string carta)
        {
            if (carta.StartsWith("Rojo")) return Color.LightCoral;
            if (carta.StartsWith("Azul")) return Color.LightSkyBlue;
            if (carta.StartsWith("Verde")) return Color.LightGreen;
            if (carta.StartsWith("Amarillo")) return Color.LightYellow;
            return Color.LightGray;
        }

        private void BtnRobar_Click_1(object sender, EventArgs e)
        {
            string[] cartasPosibles = { "Rojo 4", "Azul 8", "Verde 2", "Amarillo 5" };
            string cartaNueva = cartasPosibles[new Random().Next(cartasPosibles.Length)];

            manosJugadores[turnoActual].Add(cartaNueva);
            lblMensaje.Text = $"{nombresJugadores[turnoActual]} robó una carta";

            ActualizarInterfaz();
        }
    }

    //Rotar tarjetas de los jugadores
    public class BotonRotado : Button
    {
        public float Angulo { get; set; } = 0;

        protected override void OnPaint(PaintEventArgs pevent)
        {
            if (Angulo == 0) { base.OnPaint(pevent); return; }

            Graphics g = pevent.Graphics;
            g.Clear(Parent.BackColor);

            using (Brush b = new SolidBrush(Enabled ? BackColor : Color.LightGray))
                g.FillRectangle(b, ClientRectangle);

            g.DrawRectangle(Pens.Gray, 0, 0, Width - 1, Height - 1);

            g.TranslateTransform(Width / 2f, Height / 2f);
            g.RotateTransform(Angulo);

            using (Brush bTexto = new SolidBrush(ForeColor))
            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                g.DrawString(Text, Font, bTexto, 0, 0, sf);
            }
        }
    }
}