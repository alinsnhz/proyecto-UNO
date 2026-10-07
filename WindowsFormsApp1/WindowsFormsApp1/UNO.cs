using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using proyectoUNO;
using proyecto_UNO;

namespace WindowsFormsApp1
{
    public partial class UNO : Form
    {
        private JuegoUNO juego;
        private Timer timerMensaje;

        // Instancias de BD y control de sesión/partida
        private HistorialDAO historialDAO = new HistorialDAO();
        private LogJuegoDAO logJuegoDAO = new LogJuegoDAO();
        private JugadorDAO jugadorDAO = new JugadorDAO();

        private int idPartidaActual = 0;
        private bool partidaFinalizada = false;
        private bool yaRobo = false;
        private Carta cartaRobadaEnTurno = null;

        private string[] nombresJugadores = new string[] { "Jugador 1", "Jugador 2", "Jugador 3" };
        private int[] idsJugadores = new int[3];

        public UNO()
        {
            InitializeComponent();

            juego = new JuegoUNO();

            juego.jugadores.Add(new Jugador(1, nombresJugadores[0]));
            juego.jugadores.Add(new Jugador(2, nombresJugadores[1]));
            juego.jugadores.Add(new Jugador(3, nombresJugadores[2]));

            this.Resize += UNO_Resize;

            lblAvisoUno.Visible = false;

            timerMensaje = new Timer();
            timerMensaje.Interval = 2000;
            timerMensaje.Tick += timerMensaje_Tick;

            if (UnoJ1 != null) UnoJ1.Click += UnoJ1_Click;
            if (UnoJ2 != null) UnoJ2.Click += UnoJ2_Click;
            if (UnoJ3 != null) UnoJ3.Click += UnoJ3_Click;
        }

        // INICIO DEL JUEGO
        private void UNO_Load(object sender, EventArgs e)
        {
            juego.iniciarPartida();

            // Sacamos la primera carta del mazo para colocarla en el centro.
            juego.colocarCartaInicial();

            CentrarCarta();

            // 1. Obtener o crear los IDs de los jugadores en la BD
            for (int i = 0; i < nombresJugadores.Length; i++)
            {
                idsJugadores[i] = jugadorDAO.ObtenerOCrear(nombresJugadores[i]);
            }

            // 2. Crear registro de la partida en la BD
            idPartidaActual = historialDAO.GuardarPartida();
            historialDAO.GuardarParticipantes(idPartidaActual, nombresJugadores.ToList());
            logJuegoDAO.RegistrarTurno(idPartidaActual, idsJugadores[juego.jugadorActual], nombresJugadores[juego.jugadorActual]);

            ActualizarInterfaz();
        }

        // AJUSTAR CARTA CENTRAL
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

        // ACTUALIZAR TODA LA INTERFAZ
        private void ActualizarInterfaz()
        {
            if (juego.jugadores.Count < 3)
                return;

            lblNombreJ1.Text = juego.jugadores[0].Nombre;
            lblNombreJ2.Text = juego.jugadores[1].Nombre;
            lblNombreJ3.Text = juego.jugadores[2].Nombre;

            // Mostrar cartas de cada jugador
            MostrarCartas(panelMano, juego.jugadores[0].Cartas, 0);
            MostrarCartas(panelManoJugador2, juego.jugadores[1].Cartas, 1);
            MostrarCartas(panelManoJugador3, juego.jugadores[2].Cartas, 2);

            // Mostrar carta del centro
            MostrarCartaCentro();

            // Mostrar botón UNO solamente cuando el jugador actual tiene una carta
            UnoJ1.Visible = juego.jugadorActual == 0 && juego.jugadores[0].Cartas.Count == 2;
            UnoJ2.Visible = juego.jugadorActual == 1 && juego.jugadores[1].Cartas.Count == 2;
            UnoJ3.Visible = juego.jugadorActual == 2 && juego.jugadores[2].Cartas.Count == 2;

            // Mostrar botón ROBAR solamente para el jugador que tiene el turno
            RobarJ1.Visible = juego.jugadorActual == 0;
            RobarJ2.Visible = juego.jugadorActual == 1;
            RobarJ3.Visible = juego.jugadorActual == 2;

            // Resaltar al jugador actual
            lblNombreJ1.Font = new Font("Segoe UI", 11, juego.jugadorActual == 0 ? FontStyle.Bold : FontStyle.Regular);
            lblNombreJ2.Font = new Font("Segoe UI", 11, juego.jugadorActual == 1 ? FontStyle.Bold : FontStyle.Regular);
            lblNombreJ3.Font = new Font("Segoe UI", 11, juego.jugadorActual == 2 ? FontStyle.Bold : FontStyle.Regular);
        }

        // OBTENER IMAGEN DE UNA CARTA
        private string ObtenerRutaImagen(Carta carta)
        {
            if (carta == null) return "";

            string carpetaImagenes = Path.Combine(Application.StartupPath, "Imagenes");
            string nombreColor = carta.Color != null ? carta.Color.ToLower() : "";

            string carpeta;
            string nombreArchivo;

            if (carta.Tipo == "Comodin")
            {
                carpeta = "Comodines";
                nombreArchivo = "comodin_cambioColor.png";
            }
            else if (carta.Tipo == "+4")
            {
                carpeta = "Comodines";
                nombreArchivo = "comodin_+4.png";
            }
            else
            {
                carpeta = carta.Color;

                if (carta.Tipo == "Número")
                {
                    nombreArchivo = nombreColor + "_" + carta.Valor + ".png";
                }
                else if (carta.Tipo == "+2")
                {
                    nombreArchivo = nombreColor + "_+2.png";
                }
                else if (carta.Tipo == "Reversa")
                {
                    nombreArchivo = nombreColor + "_reversa.png";
                }
                else if (carta.Tipo == "Salta")
                {
                    nombreArchivo = nombreColor + "_cancelar.png";
                }
                else
                {
                    return "";
                }
            }

            return Path.Combine(carpetaImagenes, carpeta, nombreArchivo);
        }

        // MOSTRAR CARTAS DE UN JUGADOR
        private void MostrarCartas(FlowLayoutPanel panel, List<Carta> cartas, int indiceJugador)
        {
            panel.Controls.Clear();

            if (cartas == null) return;

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

            if (indiceJugador == 1)
                angulo = 90;
            else if (indiceJugador == 2)
                angulo = 270;

            foreach (Carta carta in cartas)
            {
                BotonRotado btn = new BotonRotado();
                btn.Text = "";
                btn.Margin = new Padding(margen / 2);

                btn.Tag = new object[] { carta, indiceJugador };
                btn.Angulo = angulo;
                btn.Enabled = (indiceJugador == juego.jugadorActual);
                btn.BackgroundImageLayout = ImageLayout.Stretch;

                string rutaImagen = ObtenerRutaImagen(carta);

                if (File.Exists(rutaImagen))
                {
                    using (Image imagen = Image.FromFile(rutaImagen))
                    {
                        btn.BackgroundImage = new Bitmap(imagen);
                    }
                }
                else
                {
                    btn.Text = carta.Color + "\n" + carta.Valor;
                    btn.BackColor = ColorSegunCarta(carta);
                }

                // JUGADORES LATERALES
                if (indiceJugador == 1 || indiceJugador == 2)
                {
                    int altoFijo = 60;
                    btn.Width = panel.ClientSize.Width - 10;
                    int altoDisponible = (panel.ClientSize.Height / Math.Max(1, cantidad)) - margen;
                    btn.Height = Math.Max(30, Math.Min(altoFijo, altoDisponible));
                }
                // JUGADOR PRINCIPAL
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

            historialDAO.GuardarGanador(idPartidaActual, nombreGanador);
            historialDAO.RegistrarGanadasPerdidas(idPartidaActual, nombreGanador);

            for (int i = 0; i < nombresJugadores.Length; i++)
            {
                historialDAO.GuardarResultado(idPartidaActual, nombresJugadores[i], juego.jugadores[i].Cartas.Count);
            }
        }

        // MOSTRAR CARTA CENTRAL
        private void MostrarCartaCentro()
        {
            if (cartaCentro == null || juego.cartaActual == null)
                return;

            Carta carta = juego.cartaActual;
            string rutaImagen = ObtenerRutaImagen(carta);

            cartaCentro.Text = "";
            cartaCentro.BackgroundImageLayout = ImageLayout.Stretch;

            if (File.Exists(rutaImagen))
            {
                using (Image imagen = Image.FromFile(rutaImagen))
                {
                    cartaCentro.BackgroundImage = new Bitmap(imagen);
                }
            }
            else
            {
                cartaCentro.BackgroundImage = null;
                cartaCentro.Text = carta.Color + "\n" + carta.Valor;
                cartaCentro.BackColor = ColorSegunCarta(carta);
                cartaCentro.ForeColor = Color.Black;
                cartaCentro.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            }
        }

        // JUGAR CARTA
        private void BtnCarta_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            object[] datos = (object[])btn.Tag;

            Carta cartaJugada = (Carta)datos[0];
            int indiceJugador = (int)datos[1];

            if (indiceJugador != juego.jugadorActual)
                return;

            Jugador jugador = juego.jugadores[indiceJugador];
            int idJugador = idsJugadores[indiceJugador];

            // Después de robar solo puede jugar la carta robada
            if (yaRobo && cartaJugada != cartaRobadaEnTurno)
            {
                MessageBox.Show("Después de robar solo puedes jugar la carta que robaste, o pasar el turno haciendo clic en el mazo.",
                    "Carta no válida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!juego.puedeJugar(jugador, cartaJugada))
            {
                MessageBox.Show("No puedes jugar esa carta", "Carta no válida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Elegir color ANTES de jugar (si cancela, la carta sigue en la mano)
            string colorElegido = "";
            if (cartaJugada.Tipo == "Comodin" || cartaJugada.Tipo == "+4")
            {
                colorElegido = SeleccionarColor();
                if (colorElegido == "")
                    return;
            }

            jugador.quitarCarta(cartaJugada);
            juego.agregarCartaDescarte(cartaJugada);
            logJuegoDAO.RegistrarCartaJugada(idPartidaActual, idJugador, jugador.Nombre, cartaJugada.Color + " " + cartaJugada.Valor);

            string mensaje = jugador.Nombre + " jugó: " + cartaJugada.Valor;

            // Penalización por no decir UNO
            if (jugador.Cartas.Count == 1 && !juego.declaroUNO)
            {
                juego.agregaCartaRobada(jugador);
                juego.agregaCartaRobada(jugador);
                logJuegoDAO.registrarMensaje(idPartidaActual, idJugador,
                    jugador.Nombre + " no declaró UNO y robó 2 cartas de penalización");
                mensaje += ". No dijo UNO y roba 2 cartas";
            }

            Jugador afectado;

            switch (cartaJugada.Tipo)
            {
                case "Comodin":
                    juego.aplicarComodin(colorElegido);
                    logJuegoDAO.RegistrarCambioColor(idPartidaActual, idJugador, jugador.Nombre, colorElegido);
                    mensaje += " (color: " + colorElegido + ")";
                    juego.cambiarTurno();
                    break;

                case "+4":
                    afectado = juego.aplicarMasCuatro(colorElegido);
                    logJuegoDAO.RegistrarAccionEspecial(idPartidaActual, idJugador, jugador.Nombre, "+4");
                    logJuegoDAO.RegistrarCambioColor(idPartidaActual, idJugador, jugador.Nombre, colorElegido);
                    mensaje += " (color: " + colorElegido + "). " + afectado.Nombre + " roba 4 y pierde su turno";
                    break;

                case "Reversa":
                    if (juego.jugadores.Count == 2)
                    {
                        juego.aplicarSalta();   // con 2 jugadores la reversa funciona como salto
                    }
                    else
                    {
                        juego.aplicarReversa();
                        juego.cambiarTurno();
                    }
                    logJuegoDAO.RegistrarAccionEspecial(idPartidaActual, idJugador, jugador.Nombre, "Reversa");
                    break;

                case "Salta":
                    juego.aplicarSalta();
                    logJuegoDAO.RegistrarAccionEspecial(idPartidaActual, idJugador, jugador.Nombre, "Salto");
                    break;

                case "+2":
                    afectado = juego.aplicarMasDos();
                    logJuegoDAO.RegistrarAccionEspecial(idPartidaActual, idJugador, jugador.Nombre, "+2");
                    mensaje += ". " + afectado.Nombre + " roba 2 y pierde su turno";
                    break;

                default:
                    juego.cambiarTurno();
                    break;
            }

            lblMensaje.Text = mensaje;

            // COMPROBAR GANADOR
            if (juego.esGanador(jugador))
            {
                juego.comprobarGanador(jugador);
                logJuegoDAO.registrarMensaje(idPartidaActual, idJugador, jugador.Nombre + " ganó la partida");
                ActualizarInterfaz();   // aquí MostrarCartas guarda el resultado y muestra el aviso
                return;
            }

            finalizarTurno();
        }

        //ROBAR
        private void BtnRobar_Click(object sender, EventArgs e)
        {
            if (juego.partidaTerminada)
                return;

            Jugador jugador = juego.obtenerJugadorActual();

            // Segundo clic en el mazo en el mismo turno = pasar turno
            if (yaRobo)
            {
                pasarTurno(jugador);
                return;
            }

            Carta cartaNueva = juego.robarDuranteTurno(jugador);

            if (cartaNueva == null)
            {
                MessageBox.Show("No quedan cartas en el mazo. Se pasa el turno.", "Mazo vacío",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                pasarTurno(jugador);
                return;
            }

            logJuegoDAO.RegistrarCartaRobada(idPartidaActual, idsJugadores[juego.jugadorActual],
                nombresJugadores[juego.jugadorActual], cartaNueva.Color + " " + cartaNueva.Valor);

            yaRobo = true;
            cartaRobadaEnTurno = cartaNueva;

            if (juego.puedeJugarCartaRobada(jugador, cartaNueva))
            {
                lblMensaje.Text = jugador.Nombre + " robó una carta que puede jugar. Juégala o haz clic en el mazo para pasar.";
                ActualizarInterfaz();
            }
            else
            {
                lblMensaje.Text = jugador.Nombre + " robó una carta y no puede jugarla. Pasa el turno.";
                pasarTurno(jugador);
            }
        }

        private void pasarTurno(Jugador jugador)
        {
            logJuegoDAO.registrarMensaje(idPartidaActual, idsJugadores[juego.jugadorActual], jugador.Nombre + " pasó su turno");
            juego.cambiarTurno();
            finalizarTurno();
        }

        private void finalizarTurno()
        {
            yaRobo = false;
            cartaRobadaEnTurno = null;
            juego.declaroUNO = false;

            logJuegoDAO.RegistrarTurno(idPartidaActual, idsJugadores[juego.jugadorActual], nombresJugadores[juego.jugadorActual]);
            ActualizarInterfaz();
        }

        private void BtnRobar_Click_1(object sender, EventArgs e) => BtnRobar_Click(sender, e);
        private void RobarJ2_Click(object sender, EventArgs e) => BtnRobar_Click(sender, e);
        private void RobarJ3_Click(object sender, EventArgs e) => BtnRobar_Click(sender, e);

        // SELECCIONAR COLOR
        private string SeleccionarColor()
        {
            Form ventanaColor = new Form();
            ventanaColor.Text = "Elige un color";
            ventanaColor.StartPosition = FormStartPosition.CenterParent;
            ventanaColor.FormBorderStyle = FormBorderStyle.FixedDialog;
            ventanaColor.MaximizeBox = false;
            ventanaColor.MinimizeBox = false;
            ventanaColor.Width = 420;
            ventanaColor.Height = 160;

            Label mensaje = new Label();
            mensaje.Text = "¿Qué color quieres elegir?";
            mensaje.AutoSize = true;
            mensaje.Location = new Point(20, 15);
            ventanaColor.Controls.Add(mensaje);

            string colorSeleccionado = "";

            Button rojo = CrearBotonColor("Rojo", Color.Red);
            rojo.Location = new Point(20, 55);
            rojo.Click += (s, e) => { colorSeleccionado = "Rojo"; ventanaColor.Close(); };

            Button azul = CrearBotonColor("Azul", Color.Blue);
            azul.Location = new Point(115, 55);
            azul.Click += (s, e) => { colorSeleccionado = "Azul"; ventanaColor.Close(); };

            Button verde = CrearBotonColor("Verde", Color.Green);
            verde.Location = new Point(210, 55);
            verde.Click += (s, e) => { colorSeleccionado = "Verde"; ventanaColor.Close(); };

            Button amarillo = CrearBotonColor("Amarillo", Color.Gold);
            amarillo.Location = new Point(305, 55);
            amarillo.Click += (s, e) => { colorSeleccionado = "Amarillo"; ventanaColor.Close(); };

            ventanaColor.Controls.Add(rojo);
            ventanaColor.Controls.Add(azul);
            ventanaColor.Controls.Add(verde);
            ventanaColor.Controls.Add(amarillo);

            ventanaColor.ShowDialog(this);

            return colorSeleccionado;
        }

        private Button CrearBotonColor(string texto, Color color)
        {
            Button boton = new Button();
            boton.Text = texto;
            boton.Width = 85;
            boton.Height = 40;
            boton.BackColor = color;
            boton.ForeColor = texto == "Amarillo" ? Color.Black : Color.White;
            boton.Font = new Font("Segoe UI", 9, FontStyle.Bold);

            return boton;
        }

        // BOTONES UNO
        private void MostrarAvisoUno(int indiceJugador)
        {
            if (lblAvisoUno == null)
                return;

            Jugador jugador = juego.jugadores[indiceJugador];

            if (!juego.declararUNO(jugador))
            {
                return;
            }

            logJuegoDAO.registrarMensaje(idPartidaActual, idsJugadores[indiceJugador], jugador.Nombre + " declaró UNO");

            timerMensaje.Stop();
            lblAvisoUno.Text = "¡" + jugador.Nombre.ToUpper() + " DIJO UNO!";
            lblAvisoUno.ForeColor = Color.Red;
            lblAvisoUno.BringToFront();
            lblAvisoUno.Visible = true;
            timerMensaje.Start();
        }

        private void UnoJ1_Click(object sender, EventArgs e)
        {
            if (juego.jugadorActual == 0)
                MostrarAvisoUno(0);
        }

        private void UnoJ2_Click(object sender, EventArgs e)
        {
            if (juego.jugadorActual == 1)
                MostrarAvisoUno(1);
        }

        private void UnoJ3_Click(object sender, EventArgs e)
        {
            if (juego.jugadorActual == 2)
                MostrarAvisoUno(2);
        }

        private void timerMensaje_Tick(object sender, EventArgs e)
        {
            timerMensaje.Stop();

            if (lblAvisoUno != null)
            {
                lblAvisoUno.Text = "";
                lblAvisoUno.Visible = false;
            }
        }

        // COLOR DE RESPALDO
        private Color ColorSegunCarta(Carta carta)
        {
            if (carta == null || carta.Color == null)
                return Color.LightGray;

            if (carta.Color == "Rojo") return Color.LightCoral;
            if (carta.Color == "Azul") return Color.LightSkyBlue;
            if (carta.Color == "Verde") return Color.LightGreen;
            if (carta.Color == "Amarillo") return Color.LightYellow;

            return Color.LightGray;
        }
    }

    // BOTÓN QUE PERMITE ROTAR LAS CARTAS DE LOS JUGADORES LATERALES
    public class BotonRotado : Button
    {
        public float Angulo { get; set; }

        public BotonRotado()
        {
            Angulo = 0;
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            if (Angulo == 0)
            {
                base.OnPaint(pevent);
                return;
            }

            Graphics g = pevent.Graphics;
            g.Clear(Parent != null ? Parent.BackColor : SystemColors.Control);
            g.TranslateTransform(Width / 2f, Height / 2f);
            g.RotateTransform(Angulo);

            Rectangle rect = new Rectangle(-Height / 2, -Width / 2, Height, Width);

            if (BackgroundImage != null)
            {
                g.DrawImage(BackgroundImage, rect);
            }
            else
            {
                using (Brush fondo = new SolidBrush(Enabled ? BackColor : Color.LightGray))
                {
                    g.FillRectangle(fondo, rect);
                }

                using (Brush texto = new SolidBrush(ForeColor))
                {
                    using (StringFormat sf = new StringFormat())
                    {
                        sf.Alignment = StringAlignment.Center;
                        sf.LineAlignment = StringAlignment.Center;
                        g.DrawString(Text, Font, texto, 0, 0, sf);
                    }
                }
            }
            g.ResetTransform();
        }
    }
}