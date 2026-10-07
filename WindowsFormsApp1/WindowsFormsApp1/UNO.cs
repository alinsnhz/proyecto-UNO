using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
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
        private int[] idsJugadores = new int[3];
        private int turnoActual = 0;
        private string cartaEnMesa = "Rojo 5";
        private Timer timerMensaje;

        // Instancias de BD
        private HistorialDAO historialDAO = new HistorialDAO();
        private LogJuegoDAO logJuegoDAO = new LogJuegoDAO();
        private JugadorDAO jugadorDAO = new JugadorDAO();
        private int idPartidaActual = 0;
        private bool partidaFinalizada = false;

        public UNO()
        {
            InitializeComponent();
            this.Resize += UNO_Resize;
            lblAvisoUno.Visible = false;

            timerMensaje = new Timer();
            timerMensaje.Interval = 2000;
            timerMensaje.Tick += timerMensaje_Tick;

            if (UnoJ1 != null) UnoJ1.Click += UnoJ1_Click;
            if (UnoJ2 != null) UnoJ2.Click += UnoJ2_Click;
            if (UnoJ3 != null) UnoJ3.Click += UnoJ3_Click;
        }

        private void UNO_Load(object sender, EventArgs e)
        {
            CentrarCarta();

            // 1. Obtener o crear los IDs de los jugadores en la BD
            for (int i = 0; i < nombresJugadores.Length; i++)
            {
                idsJugadores[i] = jugadorDAO.ObtenerOCrear(nombresJugadores[i]);
            }

            // 2. Crear registro de la partida en la BD
            idPartidaActual = historialDAO.GuardarPartida();
            historialDAO.GuardarParticipantes(idPartidaActual, nombresJugadores.ToList());
            logJuegoDAO.RegistrarTurno(idPartidaActual, idsJugadores[turnoActual], nombresJugadores[turnoActual]);

            ActualizarInterfaz();
        }

        private void UNO_Resize(object sender, EventArgs e)
        {
            CentrarCarta();
        }

        private void CentrarCarta()
        {
            if (cartaCentro != null)
            {
                cartaCentro.Location = new Point(
                    (this.ClientSize.Width - cartaCentro.Width) / 2,
                    (this.ClientSize.Height - cartaCentro.Height) / 2
                );
            }
        }

        private void ActualizarInterfaz()
        {
            lblNombreJ1.Text = nombresJugadores[0];
            lblNombreJ2.Text = nombresJugadores[1];
            lblNombreJ3.Text = nombresJugadores[2];

            MostrarCartas(panelMano, manosJugadores[0], 0);
            MostrarCartas(panelManoJugador2, manosJugadores[1], 1);
            MostrarCartas(panelManoJugador3, manosJugadores[2], 2);

            if (cartaCentro != null)
            {
                cartaCentro.Text = cartaEnMesa;
                cartaCentro.BackColor = ColorSegunTexto(cartaEnMesa);
                cartaCentro.ForeColor = Color.Black;
                cartaCentro.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            }

            UnoJ1.Visible = (turnoActual == 0 && manosJugadores[0].Count == 1);
            UnoJ2.Visible = (turnoActual == 1 && manosJugadores[1].Count == 1);
            UnoJ3.Visible = (turnoActual == 2 && manosJugadores[2].Count == 1);

            RobarJ1.Visible = (turnoActual == 0);
            RobarJ2.Visible = (turnoActual == 1);
            RobarJ3.Visible = (turnoActual == 2);

            lblNombreJ1.Font = new Font("Segoe UI", 11, turnoActual == 0 ? FontStyle.Bold : FontStyle.Regular);
            lblNombreJ2.Font = new Font("Segoe UI", 11, turnoActual == 1 ? FontStyle.Bold : FontStyle.Regular);
            lblNombreJ3.Font = new Font("Segoe UI", 11, turnoActual == 2 ? FontStyle.Bold : FontStyle.Regular);
        }

        private void MostrarCartas(FlowLayoutPanel panel, List<string> cartas, int indiceJugador)
        {
            panel.Controls.Clear();

            int cantidad = cartas.Count;
            if (cantidad == 0 && !partidaFinalizada)
            {
                partidaFinalizada = true;
                FinalizarPartidaBD(indiceJugador);
                MessageBox.Show($"{nombresJugadores[indiceJugador]} se quedó sin cartas. ¡Ganó y sus datos fueron guardados!");
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
                    int altoDisponible = (panel.ClientSize.Height / Math.Max(1, cantidad)) - margen;
                    btn.Height = Math.Min(altoFijo, altoDisponible);
                }
                else
                {
                    int anchoDisponible = panel.ClientSize.Width - (margen * (cantidad + 1));
                    btn.Width = Math.Max(55, Math.Min(anchoDisponible / Math.Max(1, cantidad), 110));
                    btn.Height = panel.ClientSize.Height - 10;
                }

                btn.Click += BtnCarta_Click;
                panel.Controls.Add(btn);
            }
        }

        private void FinalizarPartidaBD(int indiceGanador)
        {
            string nombreGanador = nombresJugadores[indiceGanador];

            // Registrar al ganador de la partida
            historialDAO.GuardarGanador(idPartidaActual, nombreGanador);
            historialDAO.RegistrarGanadasPerdidas(idPartidaActual, nombreGanador);

            // Guardar cartas restantes de cada jugador
            for (int i = 0; i < nombresJugadores.Length; i++)
            {
                historialDAO.GuardarResultado(idPartidaActual, nombresJugadores[i], manosJugadores[i].Count);
            }
        }

        private void BtnRobar_Click_1(object sender, EventArgs e) => BtnRobar_Click(sender, e);
        private void RobarJ2_Click(object sender, EventArgs e) => BtnRobar_Click(sender, e);
        private void RobarJ3_Click(object sender, EventArgs e) => BtnRobar_Click(sender, e);

        private void BtnCarta_Click(object sender, EventArgs e)
        {
            var btn = (Button)sender;
            var datos = (object[])btn.Tag;
            string cartaJugada = (string)datos[0];
            int indiceJugador = (int)datos[1];

            if (indiceJugador != turnoActual) return;

            cartaEnMesa = cartaJugada;
            manosJugadores[indiceJugador].Remove(cartaJugada);
            lblMensaje.Text = $"{nombresJugadores[indiceJugador]} jugó: {cartaJugada}";

            // Registrar carta jugada en la BD
            logJuegoDAO.RegistrarCartaJugada(idPartidaActual, idsJugadores[indiceJugador], nombresJugadores[indiceJugador], cartaJugada);

            PasarTurno();
        }

        private void PasarTurno()
        {
            turnoActual = (turnoActual + 1) % nombresJugadores.Length;
            logJuegoDAO.RegistrarTurno(idPartidaActual, idsJugadores[turnoActual], nombresJugadores[turnoActual]);
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

        private void BtnRobar_Click(object sender, EventArgs e)
        {
            string[] cartasPosibles = { "Rojo 4", "Azul 8", "Verde 2", "Amarillo 5" };
            string cartaNueva = cartasPosibles[new Random().Next(cartasPosibles.Length)];

            manosJugadores[turnoActual].Add(cartaNueva);
            lblMensaje.Text = $"{nombresJugadores[turnoActual]} robó una carta";

            // Registrar carta robada en la BD
            logJuegoDAO.RegistrarCartaRobada(idPartidaActual, idsJugadores[turnoActual], nombresJugadores[turnoActual], cartaNueva);

            ActualizarInterfaz();
        }

        private void MostrarAvisoUno()
        {
            if (lblAvisoUno != null)
            {
                timerMensaje.Stop();
                lblAvisoUno.Text = $"¡{nombresJugadores[turnoActual].ToUpper()} DIJO UNO!";
                lblAvisoUno.ForeColor = Color.Red;
                lblAvisoUno.BringToFront();
                lblAvisoUno.Visible = true;
                timerMensaje.Start();
            }
        }

        private void UnoJ1_Click(object sender, EventArgs e) => MostrarAvisoUno();
        private void UnoJ2_Click(object sender, EventArgs e) => MostrarAvisoUno();
        private void UnoJ3_Click(object sender, EventArgs e) => MostrarAvisoUno();

        private void timerMensaje_Tick(object sender, EventArgs e)
        {
            timerMensaje.Stop();
            if (lblAvisoUno != null)
            {
                lblAvisoUno.Text = "";
                lblAvisoUno.Visible = false;
            }
        }
    }

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