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
        private Timer timerBarajar;
        private Timer timerAnimacionCarta;
        private Random random = new Random();
        private bool partidaIniciada = false;
        private bool animacionEnCurso = false;
        private TabControl pestañas;
        private TabPage pestañaInicio;
        private TabPage pestañaPartida;
        private Button btnIniciarPartida;
        private Button cartaAnimacionInicio;
        private Label lblEstadoInicio;
        private Label lblTurnoActual;
        private Label lblTituloHistorial;
        private Label lblContadorMazo;

        private List<string> historialVisual = new List<string>();
        private int pasoBarajado = 0;

        // Datos de la animación de una carta
        private Button cartaAnimada;
        private Point destinoAnimacion;

        private int pasoAnimacionCarta;
        private int totalPasosAnimacionCarta;

        private Action finalizarAnimacionCarta;

        private string carpetaImagenes = null;
        private bool avisoImagenesMostrado = false;
        private Dictionary<string, Image> cacheImagenes = new Dictionary<string, Image>();

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

            this.MinimumSize = new Size(900, 600);

            cartaCentro.Size = new Size(96, 135);

            CrearPestanas();
            CrearElementosVisuales();
            AplicarEstiloInterfaz();

            this.Resize += UNO_Resize;

            juego = new JuegoUNO();

            juego.jugadores.Add(new Jugador(1, nombresJugadores[0]));
            juego.jugadores.Add(new Jugador(2, nombresJugadores[1]));
            juego.jugadores.Add(new Jugador(3, nombresJugadores[2]));

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
            juego.cartaActual = juego.Mazo.robarCarta();

            CentrarCarta();
            AplicarReversoMazo();
            ActualizarInterfaz();
            }

        // AJUSTAR CARTA CENTRAL
        private void UNO_Resize(object sender, EventArgs e)
        {
            AjustarInterfazResponsive();
        }

        // Busca la carpeta "Imagenes" junto al .exe y, si no está, sube por las carpetas padre
        private string buscarCarpetaImagenes()
        {
            if (carpetaImagenes != null)
                return carpetaImagenes;

            string[] bases = { Application.StartupPath, AppDomain.CurrentDomain.BaseDirectory };

            foreach (string inicio in bases)
            {
                DirectoryInfo dir = new DirectoryInfo(inicio);

                for (int i = 0; i < 6 && dir != null; i++)
                {
                    string candidata = Path.Combine(dir.FullName, "Imagenes");
                    if (Directory.Exists(candidata))
                    {
                        carpetaImagenes = candidata;
                        return carpetaImagenes;
                    }
                    dir = dir.Parent;
                }
            }

            if (!avisoImagenesMostrado)
            {
                avisoImagenesMostrado = true;
                MessageBox.Show("No se encontró la carpeta 'Imagenes'. Se buscó desde:\n" + Application.StartupPath,
                    "Imágenes no encontradas", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            return null;
        }

        // Carga una imagen una sola vez y la reutiliza (no deja el archivo bloqueado)
        private Image cargarImagen(string ruta)
        {
            if (string.IsNullOrEmpty(ruta) || !File.Exists(ruta))
                return null;

            Image img;
            if (!cacheImagenes.TryGetValue(ruta, out img))
            {
                using (Image temporal = Image.FromFile(ruta))
                {
                    img = new Bitmap(temporal);
                }
                cacheImagenes[ruta] = img;
            }
            return img;
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
            UnoJ1.Visible = juego.jugadorActual == 0 && juego.jugadores[0].Cartas.Count == 1;
            UnoJ2.Visible = juego.jugadorActual == 1 && juego.jugadores[1].Cartas.Count == 1;
            UnoJ3.Visible = juego.jugadorActual == 2 && juego.jugadores[2].Cartas.Count == 1;

            // Mostrar botón ROBAR solamente para el jugador que tiene el turno
            RobarJ1.Visible = juego.jugadorActual == 0;
            RobarJ2.Visible = juego.jugadorActual == 1;
            RobarJ3.Visible = juego.jugadorActual == 2;

            // Resaltar al jugador actual
            lblNombreJ1.Font = new Font("Segoe UI", 11, juego.jugadorActual == 0 ? FontStyle.Bold : FontStyle.Regular);
            lblNombreJ2.Font = new Font("Segoe UI", 11, juego.jugadorActual == 1 ? FontStyle.Bold : FontStyle.Regular);
            lblNombreJ3.Font = new Font("Segoe UI", 11, juego.jugadorActual == 2 ? FontStyle.Bold : FontStyle.Regular);

            if (juego.jugadores.Count > 0)
            {
                Jugador jugadorActual = juego.jugadores[juego.jugadorActual];

                lblTurnoActual.Text = "TURNO: " + jugadorActual.Nombre;
                lblTurnoActual.BackColor = Color.FromArgb(255, 245, 245);
                lblTurnoActual.ForeColor = Color.FromArgb(220, 35, 50);
        }

            lblContadorMazo.Text = "Mazo: " + juego.Mazo.cartasRestantes();}

        // OBTENER IMAGEN DE UNA CARTA
        private string ObtenerRutaImagen(Carta carta)
        {
            if (carta == null) return "";

            string carpetaBase = buscarCarpetaImagenes();
            if (carpetaBase == null) return "";

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
                    nombreArchivo = nombreColor + "_" + carta.Valor + ".png";
                else if (carta.Tipo == "+2")
                    nombreArchivo = nombreColor + "_+2.png";
                else if (carta.Tipo == "Reversa")
                    nombreArchivo = nombreColor + "_reversa.png";
                else if (carta.Tipo == "Salta")
                    nombreArchivo = nombreColor + "_cancelar.png";
                else
                    return "";
            }

            return Path.Combine(carpetaBase, carpeta, nombreArchivo);
        }

        private void AplicarReversoMazo()
        {
            string carpetaBase = buscarCarpetaImagenes();
            if (carpetaBase == null) return;

            Image reverso = cargarImagen(Path.Combine(carpetaBase, "reverso.png"));
            if (reverso == null) return;

            foreach (Button boton in new Button[] { RobarJ1, RobarJ2, RobarJ3 })
            {
                boton.BackgroundImage = reverso;
                boton.BackgroundImageLayout = ImageLayout.Stretch;
            }
        }

        // MOSTRAR CARTAS DE UN JUGADOR
        private void MostrarCartas(FlowLayoutPanel panel, List<Carta> cartas, int indiceJugador)
        {
            if (panel == null)
                return;

            panel.SuspendLayout();
            panel.Controls.Clear();

            int cantidad = cartas.Count;

            if (cantidad == 0)
            {
                panel.ResumeLayout();
                return;
            }

            int anchoPanel = panel.ClientSize.Width;
            int altoPanel = panel.ClientSize.Height;

            int anchoCarta;
            int altoCarta;
            // JUGADOR 1
            if (indiceJugador == 0)
            {
                altoCarta = Math.Max(75, Math.Min(125, altoPanel - 10));

                // Mientras más cartas haya, menor separación
                int separacion;

                if (cantidad <= 7)
                    separacion = 70;
                else if (cantidad <= 10)
                    separacion = 48;
                else if (cantidad <= 15)
                    separacion = 35;
                else
                    separacion = 25;

                anchoCarta =
                    Math.Max(
                        45,
                        Math.Min(
                            80,
                            separacion + 15
                        )
                    );

                for (int i = 0;
                     i < cartas.Count;
                     i++)
                {
                    Button boton =
                        CrearBotonCarta(
                            cartas[i],
                            i,
                            indiceJugador
                        );

                    boton.Width = anchoCarta;
                    boton.Height = altoCarta;

                    boton.Margin =
                        new Padding(
                            0,
                            0,
                            -(
                                boton.Width -
                                separacion
                            ),
                            0
                        );

                    panel.Controls.Add(boton);
                }
            }
            else
            {
                // ========================================================
                // JUGADORES 2 Y 3
                // ========================================================

                anchoCarta =
                    Math.Max(
                        45,
                        Math.Min(
                            75,
                            anchoPanel - 10
                        )
                    );

                altoCarta =
                    Math.Max(
                        75,
                        Math.Min(
                            115,
                            altoPanel / 2
                        )
                    );

            foreach (Carta carta in cartas)
            {
                    Button boton =
                        CrearBotonCarta(
                            carta,
                            -1,
                            indiceJugador
                        );

                    boton.Width =
                        anchoCarta;

                    boton.Height =
                        altoCarta;

                    boton.Margin =
                        new Padding(
                            0,
                            0,
                            0,
                            -20
                        );

                    panel.Controls.Add(
                        boton
                    );
                }
            }

            panel.ResumeLayout();
        }

        private void AnimarCartasDesdeMazo(
    List<Carta> cartas,
    int indiceJugador,
    Action alFinalizar)
        {
            if (cartas == null ||
                cartas.Count == 0)
            {
                alFinalizar();
                return;
            }

            animacionEnCurso = true;

            AnimarUnaCarta(
                cartas,
                0,
                indiceJugador,
                alFinalizar
            );
        }

        private void AnimarUnaCarta(
    List<Carta> cartas,
    int indiceCarta,
    int indiceJugador,
    Action alFinalizar)
        {
            if (indiceCarta >= cartas.Count)
            {
                animacionEnCurso = false;

                alFinalizar();

                return;
            }

            cartaAnimada = new Button();

            cartaAnimada.Width = 70;
            cartaAnimada.Height = 100;

            cartaAnimada.FlatStyle =
                FlatStyle.Flat;

            cartaAnimada.FlatAppearance.BorderSize =
                0;

            cartaAnimada.BackgroundImage =
                cargarImagen(
                    ObtenerRutaReverso()
                );

            cartaAnimada.BackgroundImageLayout =
                ImageLayout.Stretch;

            cartaAnimada.BringToFront();

            pestañaPartida.Controls.Add(
                cartaAnimada
            );

            Point inicio =
                new Point(
                    (pestañaPartida.ClientSize.Width -
                    cartaAnimada.Width) / 2,

                    (pestañaPartida.ClientSize.Height -
                    cartaAnimada.Height) / 2
                );

            FlowLayoutPanel panelDestino;

            if (indiceJugador == 0)
                {
                panelDestino = panelMano;
                }
            else if (indiceJugador == 1)
            {
                panelDestino = panelManoJugador2;
            }
                else
                {
                panelDestino = panelManoJugador3;
            }

            Point destinoPantalla =
                panelDestino.PointToScreen(
                    new Point(
                        Math.Max(
                            0,
                            panelDestino.ClientSize.Width /
                            2 -
                            cartaAnimada.Width /
                            2
                        ),

                        Math.Max(
                            0,
                            panelDestino.ClientSize.Height /
                            2 -
                            cartaAnimada.Height /
                            2
                        )
                    )
                );

            destinoAnimacion =
                pestañaPartida.PointToClient(
                    destinoPantalla
                );

            cartaAnimada.Location =
                inicio;

            pasoAnimacionCarta = 0;

            totalPasosAnimacionCarta = 12;

            finalizarAnimacionCarta = delegate
            {
                if (cartaAnimada != null)
                {
                    cartaAnimada.Dispose();
                    cartaAnimada = null;
                }

                if (indiceCarta + 1 <
                    cartas.Count)
                {
                    AnimarUnaCarta(
                        cartas,
                        indiceCarta + 1,
                        indiceJugador,
                        alFinalizar
                    );
                }
                else
                {
                    animacionEnCurso = false;

                    alFinalizar();
                }
            };

            if (timerAnimacionCarta != null)
            {
                timerAnimacionCarta.Stop();
                timerAnimacionCarta.Dispose();
            }

            timerAnimacionCarta =
                new Timer();

            timerAnimacionCarta.Interval =
                25;

            timerAnimacionCarta.Tick +=
                timerAnimacionCarta_Tick;

            timerAnimacionCarta.Start();
        }

        private void timerAnimacionCarta_Tick(
    object sender,
    EventArgs e)
        {
            pasoAnimacionCarta++;

            if (cartaAnimada == null)
            {
                timerAnimacionCarta.Stop();
                return;
                }

            float progreso =
                (float)pasoAnimacionCarta /
                totalPasosAnimacionCarta;

            int x =
                (int)(
                    cartaAnimada.Left +
                    (
                        destinoAnimacion.X -
                        cartaAnimada.Left
                    ) *
                    progreso
                );

            int y =
                (int)(
                    cartaAnimada.Top +
                    (
                        destinoAnimacion.Y -
                        cartaAnimada.Top
                    ) *
                    progreso
                );

            cartaAnimada.Location =
                new Point(x, y);

            if (pasoAnimacionCarta >=
                totalPasosAnimacionCarta)
            {
                timerAnimacionCarta.Stop();

                finalizarAnimacionCarta();
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
            Image imagen = cargarImagen(ObtenerRutaImagen(carta));

            cartaCentro.BackgroundImageLayout = ImageLayout.Stretch;

            if (imagen != null)
            {
                cartaCentro.Text = "";
                cartaCentro.BackgroundImage = imagen;
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

            if (!juego.puedeJugar(jugador, cartaJugada))
            {
                MessageBox.Show("No puedes jugar esa carta", "Carta no válida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Quitar carta de la mano
            jugador.quitarCarta(cartaJugada);

            string colorCarta = cartaJugada.Color ?? "SinColor";
            string valorCarta = cartaJugada.Valor ?? cartaJugada.Tipo;
            string tipoCarta = cartaJugada.Tipo;
            string descripcionCartaBD = $"{colorCarta} {valorCarta}".Trim();

            // Registrar carta jugada en la BD (una sola vez)
            logJuegoDAO.RegistrarCartaJugada(idPartidaActual, idsJugadores[indiceJugador], nombresJugadores[indiceJugador], descripcionCartaBD);

            // Registrar carta jugada en la API
            _ = APICliente.RegistrarJugadaAsync(
                idPartidaActual,
                idsJugadores[indiceJugador],
                nombresJugadores[indiceJugador],
                colorCarta,
                valorCarta,
                tipoCarta
            );

            // Colocarla en el centro
            juego.cartaActual = cartaJugada;
            lblMensaje.Text = jugador.Nombre + " jugó: " + cartaJugada.Valor;

            // COMODÍN
            if (cartaJugada.Tipo == "Comodin")
            {
                string color = SeleccionarColor();
                if (color == "")
                {
                    jugador.agregarCarta(cartaJugada);
                    ActualizarInterfaz();
                    return;
                }
                juego.aplicarComodin(color);
                juego.cambiarTurno();
            }
            // +4
            else if (cartaJugada.Tipo == "+4")
            {
                string color = SeleccionarColor();
                if (color == "")
                {
                    jugador.agregarCarta(cartaJugada);
                    ActualizarInterfaz();
                    return;
                }
                juego.aplicarMasCuatro(color);
            }
            // EFECTOS DE OTRAS CARTAS
            else if (cartaJugada.Tipo == "Reversa")
            {
                juego.aplicarReversa();
                juego.cambiarTurno();
            }
            else if (cartaJugada.Tipo == "Salta")
            {
                juego.aplicarSalta();
            }
            else if (cartaJugada.Tipo == "+2")
            {
                juego.aplicarMasDos();
            }
            else
            {
                juego.cambiarTurno();
            }

            // Registrar cambio de turno en BD y API
            logJuegoDAO.RegistrarTurno(idPartidaActual, idsJugadores[juego.jugadorActual], nombresJugadores[juego.jugadorActual]);
            _ = APICliente.RegistrarTurnoAsync(idPartidaActual, idsJugadores[juego.jugadorActual], nombresJugadores[juego.jugadorActual]);

            // COMPROBAR GANADOR
            if (juego.esGanador(jugador))
            {
                juego.comprobarGanador(jugador);

                if (!partidaFinalizada)
                {
                    partidaFinalizada = true;
                    FinalizarPartidaBD(indiceJugador);
                }

                ActualizarInterfaz();
                MessageBox.Show(jugador.Nombre + " ganó la partida. 🎉", "¡Tenemos ganador!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            ActualizarInterfaz();
        }

        // ROBAR
        private void BtnRobar_Click(object sender, EventArgs e)
        {
            Jugador jugador = juego.obtenerJugadorActual();
            Carta cartaNueva = juego.robarDuranteTurno(jugador);

            if (cartaNueva == null)
            {
                MessageBox.Show("No quedan cartas en el mazo", "Mazo vacío", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string colorCarta = cartaNueva.Color ?? "SinColor";
            string valorCarta = cartaNueva.Valor ?? cartaNueva.Tipo;
            string tipoCarta = cartaNueva.Tipo;
            string descripcionCartaBD = $"{colorCarta} {valorCarta}".Trim();

            // Registrar carta robada en la BD (una sola vez)
            logJuegoDAO.RegistrarCartaRobada(idPartidaActual, idsJugadores[juego.jugadorActual], nombresJugadores[juego.jugadorActual], descripcionCartaBD);

            // Registrar carta robada en la API
            _ = APICliente.RegistrarCartaRobadaAsync(
                idPartidaActual,
                idsJugadores[juego.jugadorActual],
                nombresJugadores[juego.jugadorActual],
                colorCarta,
                valorCarta,
                tipoCarta
            );

            lblMensaje.Text = jugador.Nombre + " robó una carta.";

            if (juego.puedeJugarCartaRobada(jugador, cartaNueva))
            {
                lblMensaje.Text = jugador.Nombre + " robó una carta que puede jugar.";
            }

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

        private void CrearPestanas()
        {
            pestañas = new TabControl();

            pestañas.Dock = DockStyle.Fill;

            pestañaInicio = new TabPage("Inicio");
            pestañaPartida = new TabPage("Partida");

            pestañaInicio.BackColor = Color.White;
            pestañaPartida.BackColor = Color.White;

            pestañas.TabPages.Add(pestañaInicio);
            pestañas.TabPages.Add(pestañaPartida);

            this.Controls.Add(pestañas);

            pestañas.SelectedIndexChanged += pestañas_SelectedIndexChanged;

            pestañas.BringToFront();
        }

        private void CrearElementosVisuales()
        {
            // ============================================================
            // INICIO
            // ============================================================

            Label titulo = new Label();

            titulo.Text = "UNO";
            titulo.Font = new Font(
                "Segoe UI",
                32,
                FontStyle.Bold
            );

            titulo.AutoSize = true;

            titulo.Location = new Point(20, 30);

            titulo.ForeColor = Color.FromArgb(
                220,
                35,
                50
            );

            pestañaInicio.Controls.Add(titulo);


            lblEstadoInicio = new Label();

            lblEstadoInicio.Text =
                "¡Prepárate para jugar!";

            lblEstadoInicio.Font = new Font(
                "Segoe UI",
                14,
                FontStyle.Bold
            );

            lblEstadoInicio.AutoSize = true;

            pestañaInicio.Controls.Add(
                lblEstadoInicio
            );


            cartaAnimacionInicio = new Button();

            cartaAnimacionInicio.Width = 110;
            cartaAnimacionInicio.Height = 155;

            cartaAnimacionInicio.FlatStyle =
                FlatStyle.Flat;

            cartaAnimacionInicio.FlatAppearance.BorderSize = 0;

            cartaAnimacionInicio.BackgroundImageLayout =
                ImageLayout.Stretch;

            pestañaInicio.Controls.Add(
                cartaAnimacionInicio
            );


            btnIniciarPartida = new Button();

            btnIniciarPartida.Text =
                "▶  INICIAR PARTIDA";

            btnIniciarPartida.Width = 230;
            btnIniciarPartida.Height = 55;

            btnIniciarPartida.Font = new Font(
                "Segoe UI",
                12,
                FontStyle.Bold
            );

            btnIniciarPartida.BackColor =
                Color.FromArgb(
                    220,
                    35,
                    50
                );

            btnIniciarPartida.ForeColor =
                Color.White;

            btnIniciarPartida.FlatStyle =
                FlatStyle.Flat;

            btnIniciarPartida.FlatAppearance.BorderSize =
                0;

            btnIniciarPartida.Cursor =
                Cursors.Hand;

            btnIniciarPartida.Click +=
                btnIniciarPartida_Click;

            pestañaInicio.Controls.Add(
                btnIniciarPartida
            );

            PosicionarElementosInicio();


            // ============================================================
            // ELEMENTOS EXTRA DE PARTIDA
            // ============================================================

            lblTurnoActual = new Label();

            lblTurnoActual.Text =
                "TURNO: Jugador 1";

            lblTurnoActual.Font =
                new Font(
                    "Segoe UI",
                    12,
                    FontStyle.Bold
                );

            lblTurnoActual.ForeColor =
                Color.FromArgb(
                    220,
                    35,
                    50
                );

            lblTurnoActual.TextAlign =
                ContentAlignment.MiddleCenter;

            lblTurnoActual.AutoSize = false;

            lblTurnoActual.Size =
                new Size(220, 35);


            lblTituloHistorial = new Label();

            lblTituloHistorial.Text =
                "HISTORIAL";

            lblTituloHistorial.Font =
                new Font(
                    "Segoe UI",
                    10,
                    FontStyle.Bold
                );

            lblTituloHistorial.ForeColor =
                Color.DimGray;

            lblTituloHistorial.AutoSize = true;


            lblContadorMazo = new Label();

            lblContadorMazo.Text =
                "Mazo: 0";

            lblContadorMazo.Font =
                new Font(
                    "Segoe UI",
                    9,
                    FontStyle.Bold
                );

            lblContadorMazo.ForeColor =
                Color.DimGray;

            lblContadorMazo.TextAlign =
                ContentAlignment.MiddleCenter;

            lblContadorMazo.AutoSize = false;

            lblContadorMazo.Size =
                new Size(100, 24);


            pestañaPartida.Controls.Add(
                lblTurnoActual
            );

            pestañaPartida.Controls.Add(
                lblTituloHistorial
            );

            pestañaPartida.Controls.Add(
                lblContadorMazo
            );

            lblTurnoActual.BringToFront();
            lblTituloHistorial.BringToFront();
            lblContadorMazo.BringToFront();
        }

        private void PosicionarElementosInicio()
        {
            if (pestañaInicio == null ||
                cartaAnimacionInicio == null ||
                btnIniciarPartida == null)
                return;

            int centroX =
                (pestañaInicio.ClientSize.Width -
                cartaAnimacionInicio.Width) / 2;

            cartaAnimacionInicio.Location =
                new Point(
                    centroX,
                    175
                );

            if (lblEstadoInicio != null)
            {
                lblEstadoInicio.Location =
                    new Point(
                        (pestañaInicio.ClientSize.Width -
                        lblEstadoInicio.Width) / 2,
                        115
                    );
            }

            int botonX =
                (pestañaInicio.ClientSize.Width -
                btnIniciarPartida.Width) / 2;

            btnIniciarPartida.Location =
                new Point(
                    botonX,
                    360
                );
        }

        private void btnIniciarPartida_Click(
    object sender,
    EventArgs e)
        {
            if (partidaIniciada ||
                (timerBarajar != null &&
                 timerBarajar.Enabled))
                return;

            btnIniciarPartida.Enabled = false;

            btnIniciarPartida.Text =
                "♠  BARAJANDO...";

            lblEstadoInicio.Text =
                "Preparando las cartas...";

            pasoBarajado = 0;

            if (timerBarajar == null)
            {
                timerBarajar = new Timer();

                timerBarajar.Interval = 120;

                timerBarajar.Tick +=
                    timerBarajar_Tick;
            }

            string reverso =
                ObtenerRutaReverso();

            cartaAnimacionInicio.BackgroundImage =
                cargarImagen(reverso);

            timerBarajar.Start();
        }

        private void timerBarajar_Tick(
            object sender,
            EventArgs e)
        {
            pasoBarajado++;

            string carpeta =
                buscarCarpetaImagenes();

            if (carpeta == null)
                return;

            string[] cartasAnimacion =
            {
        ObtenerRutaReverso(),

        Path.Combine(
            carpeta,
            "Rojo",
            "rojo_5.png"
        ),

        Path.Combine(
            carpeta,
            "Azul",
            "azul_+2.png"
        ),

        Path.Combine(
            carpeta,
            "Verde",
            "verde_9.png"
        ),

        Path.Combine(
            carpeta,
            "Amarillo",
            "amarillo_reversa.png"
        )
    };

            string ruta =
                cartasAnimacion[
                    pasoBarajado %
                    cartasAnimacion.Length
                ];

            cartaAnimacionInicio.BackgroundImage =
                cargarImagen(ruta);

            int centroX =
                (pestañaInicio.ClientSize.Width -
                cartaAnimacionInicio.Width) / 2;

            int desplazamiento =
                (pasoBarajado % 2 == 0)
                ? -12
                : 12;

            cartaAnimacionInicio.Location =
                new Point(
                    centroX + desplazamiento,
                    175
                );

            if (pasoBarajado >= 14)
            {
                timerBarajar.Stop();

                IniciarPartidaReal();
            }
        }

        private void IniciarPartidaReal()
        {
            partidaIniciada = true;

            partidaFinalizada = false;
            yaRobo = false;

            cartaRobadaEnTurno = null;

            historialVisual.Clear();

            juego.iniciarPartida();

            juego.colocarCartaInicial();

            // ============================================================
            // BASE DE DATOS
            // ============================================================

            for (int i = 0;
                 i < nombresJugadores.Length;
                 i++)
            {
                idsJugadores[i] =
                    jugadorDAO.ObtenerOCrear(
                        nombresJugadores[i]
                    );
            }

            idPartidaActual =
                historialDAO.GuardarPartida();

            historialDAO.GuardarParticipantes(
                idPartidaActual,
                nombresJugadores.ToList()
            );

            logJuegoDAO.RegistrarTurno(
                idPartidaActual,
                idsJugadores[juego.jugadorActual],
                nombresJugadores[juego.jugadorActual]
            );

            _ = APICliente.IniciarPartidaAsync(
                nombresJugadores.ToList()
            );

            pestañas.SelectedIndex = 1;

            AplicarReversoMazo();

            AgregarHistorial(
                "La partida comenzó. " +
                nombresJugadores[
                    juego.jugadorActual
                ] +
                " inicia."
            );

            ActualizarInterfaz();

            AjustarInterfazResponsive();
        }

        private void pestañas_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!partidaIniciada &&
                pestañas.SelectedIndex == 1)
            {
                pestañas.SelectedIndex = 0;
            }
        }

        private void AjustarInterfazResponsive()
        {
            if (pestañaPartida == null)
                return;

            int ancho =
                pestañaPartida.ClientSize.Width;

            int alto =
                pestañaPartida.ClientSize.Height;

            int lado =
                Math.Max(
                    105,
                    Math.Min(
                        145,
                        ancho / 8
                    )
                );

            int margen = 12;

            int altoMano =
                Math.Max(
                    90,
                    Math.Min(
                        145,
                        alto / 4
                    )
                );

            int topLateral = 55;

            int altoLateral =
                Math.Max(
                    120,
                    alto - 220
                );

            // ============================================================
            // JUGADOR 1
            // ============================================================

            panelMano.Location =
                new Point(
                    lado + 30,
                    alto - altoMano - 12
                );

            panelMano.Size =
                new Size(
                    Math.Max(
                        250,
                        ancho -
                        (lado * 2) -
                        60
                    ),
                    altoMano
                );

            panelMano.WrapContents = false;

            panelMano.AutoScroll = false;


            // ============================================================
            // JUGADOR 2
            // ============================================================

            panelManoJugador2.Location =
                new Point(
                    margen,
                    topLateral
                );

            panelManoJugador2.Size =
                new Size(
                    lado,
                    altoLateral
                );


            // ============================================================
            // JUGADOR 3
            // ============================================================

            panelManoJugador3.Location =
                new Point(
                    ancho -
                    lado -
                    margen,
                    topLateral
                );

            panelManoJugador3.Size =
                new Size(
                    lado,
                    altoLateral
                );


            // ============================================================
            // NOMBRES
            // ============================================================

            lblNombreJ1.Location =
                new Point(
                    margen,
                    alto -
                    altoMano -
                    38
                );

            lblNombreJ1.Size =
                new Size(
                    lado,
                    26
                );


            lblNombreJ2.Location =
                new Point(
                    margen,
                    28
                );

            lblNombreJ2.Size =
                new Size(
                    lado,
                    26
                );


            lblNombreJ3.Location =
                new Point(
                    ancho -
                    lado -
                    margen,
                    28
                );

            lblNombreJ3.Size =
                new Size(
                    lado,
                    26
                );


            // ============================================================
            // CARTA CENTRAL
            // ============================================================

            cartaCentro.Location =
                new Point(
                    (ancho -
                    cartaCentro.Width) / 2,

                    Math.Max(
                        115,
                        (alto -
                        cartaCentro.Height) / 2
                    )
                );


            // ============================================================
            // TURNO
            // ============================================================

            int centroX =
                (ancho -
                lblTurnoActual.Width) / 2;

            lblTurnoActual.Location =
                new Point(
                    centroX,
                    8
                );


            // ============================================================
            // HISTORIAL
            // ============================================================

            lblTituloHistorial.Location =
                new Point(
                    centroX - 10,
                    52
                );


            lblMensaje.Location =
                new Point(
                    centroX - 10,
                    80
                );

            lblMensaje.Size =
                new Size(
                    245,
                    70
                );


            // ============================================================
            // MAZO
            // ============================================================

            lblContadorMazo.Location =
                new Point(
                    (ancho -
                    lblContadorMazo.Width) / 2,

                    cartaCentro.Bottom + 8
                );


            // ============================================================
            // BOTONES
            // ============================================================

            RobarJ1.Location =
                new Point(
                    ancho - 90,
                    alto - altoMano - 42
                );

            UnoJ1.Location =
                new Point(
                    ancho - 90,
                    alto - altoMano - 12
                );


            RobarJ2.Location =
                new Point(
                    20,
                    altoLateral +
                    topLateral +
                    4
                );

            UnoJ2.Location =
                new Point(
                    20,
                    altoLateral +
                    topLateral +
                    34
                );


            RobarJ3.Location =
                new Point(
                    ancho - lado - 5,
                    altoLateral +
                    topLateral +
                    4
                );

            UnoJ3.Location =
                new Point(
                    ancho - lado - 5,
                    altoLateral +
                    topLateral +
                    34
                );


            // VOLVER A DIBUJAR CARTAS
            if (juego != null &&
                juego.jugadores.Count >= 3)
            {
                MostrarCartas(panelMano, juego.jugadores[0].Cartas, 0);

                MostrarCartas(panelManoJugador2, juego.jugadores[1].Cartas, 1);
                MostrarCartas(panelManoJugador3, juego.jugadores[2].Cartas, 2);
            }

            PosicionarElementosInicio();
        }

        private void AgregarHistorial(string mensaje)
        {
            if (string.IsNullOrWhiteSpace(mensaje))
                return;

            historialVisual.Insert(0, "• " + mensaje);

            while (historialVisual.Count > 4)
            {
                historialVisual.RemoveAt(historialVisual.Count - 1);
            }

            lblMensaje.Text = string.Join(Environment.NewLine, historialVisual.ToArray());
            lblMensaje.BackColor = Color.FromArgb(248, 248, 248);
            lblMensaje.ForeColor = Color.FromArgb(70, 70, 70);
            lblMensaje.Font = new Font("Segoe UI", 8, FontStyle.Regular);
            lblMensaje.BorderStyle = BorderStyle.FixedSingle;
        }

        // Ruta de la imagen del reverso
        private string ObtenerRutaReverso()
        {
            string carpetaBase = buscarCarpetaImagenes();
            if (carpetaBase == null) return "";
            return Path.Combine(carpetaBase, "reverso.png");
        }

        // Crea el botón de una carta
        private Button CrearBotonCarta(Carta carta, int indiceCarta, int indiceJugador)
        {
            BotonRotado boton = new BotonRotado();

            boton.FlatStyle = FlatStyle.Flat;
            boton.FlatAppearance.BorderSize = 0;
            boton.BackgroundImageLayout = ImageLayout.Stretch;
            boton.Cursor = Cursors.Hand;
            boton.Tag = new object[] { carta, indiceJugador };

            // Jugadores laterales: carta rotada
            if (indiceJugador == 1) boton.Angulo = 90;
            else if (indiceJugador == 2) boton.Angulo = -90;

            Image imagen = cargarImagen(ObtenerRutaImagen(carta));
            if (imagen != null)
            {
                boton.BackgroundImage = imagen;
            }
            else
            {
                // Respaldo si no hay imagen
                boton.Text = carta.Color + "\n" + carta.Valor;
                boton.BackColor = ColorSegunCarta(carta);
                boton.ForeColor = Color.Black;
                boton.Font = new Font("Segoe UI", 8, FontStyle.Bold);
            }

            boton.Click += BtnCarta_Click;
            return boton;
        }

        // Estilo general de la interfaz
        private void AplicarEstiloInterfaz()
        {
            this.BackColor = Color.White;
            this.Font = new Font("Segoe UI", 9, FontStyle.Regular);

            if (panelMano != null) panelMano.BackColor = Color.Transparent;
            if (panelManoJugador2 != null) panelManoJugador2.BackColor = Color.Transparent;
            if (panelManoJugador3 != null) panelManoJugador3.BackColor = Color.Transparent;

            if (cartaCentro != null)
            {
                cartaCentro.FlatStyle = FlatStyle.Flat;
                cartaCentro.FlatAppearance.BorderSize = 0;
                cartaCentro.BackgroundImageLayout = ImageLayout.Stretch;
            }

            foreach (Button b in new Button[] { RobarJ1, RobarJ2, RobarJ3 })
            {
                if (b == null) continue;
                b.FlatStyle = FlatStyle.Flat;
                b.FlatAppearance.BorderSize = 0;
                b.Cursor = Cursors.Hand;
            }

            foreach (Button b in new Button[] { UnoJ1, UnoJ2, UnoJ3 })
            {
                if (b == null) continue;
                b.FlatStyle = FlatStyle.Flat;
                b.BackColor = Color.FromArgb(220, 35, 50);
                b.ForeColor = Color.White;
                b.Font = new Font("Segoe UI", 9, FontStyle.Bold);
                b.Cursor = Cursors.Hand;
            }
        }

        //AQUI AGREGA
    }
}