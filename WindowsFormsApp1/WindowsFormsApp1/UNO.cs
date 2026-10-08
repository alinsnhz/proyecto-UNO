using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using proyectoUNO;
using proyecto_UNO;
using static WindowsFormsApp1.EstiloUI;

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

        private string[] nombresJugadores = new string[] { "Jugador 1", "Jugador 2", "Jugador 3" };
        private int[] idsJugadores = new int[3];

        // Control del turno (robar una sola carta por turno)
        private bool yaRobo = false;
        private Carta cartaRobadaEnTurno = null;

        // Interfaz nueva
        private AbanicoCartas[] abanicos = new AbanicoCartas[3];
        private Label lblTurno;
        private bool animando = false;
        private string[] iconosJugadores = null;
        private List<string> historial = new List<string>();

        private readonly Font fuenteNombre = new Font("Segoe UI Semibold", 12f);
        private readonly Font fuenteNombreActivo = new Font("Segoe UI", 12f, FontStyle.Bold);
        private readonly Color[] coloresJugador =
        {
            Color.FromArgb(25, 118, 210),
            Color.FromArgb(46, 160, 67),
            Color.FromArgb(230, 140, 0)
        };

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

            ConfigurarInterfaz();
        }

        // Constructor que usa la pantalla de inicio (nombres e iconos de cada jugador)
        public UNO(string[] nombres, string[] rutasIconos) : this()
        {
            for (int i = 0; i < 3 && i < nombres.Length; i++)
            {
                nombresJugadores[i] = nombres[i];
                juego.jugadores[i].Nombre = nombres[i];
            }
            iconosJugadores = rutasIconos;
            this.WindowState = FormWindowState.Maximized;
        }

        // INICIO DEL JUEGO
        private async void UNO_Load(object sender, EventArgs e)
        {
            AcomodarControles();
            AsignarIconos();

            juego.iniciarPartida();
            juego.colocarCartaInicial();

            // 1. Obtener o crear los IDs de los jugadores en la BD
            for (int i = 0; i < nombresJugadores.Length; i++)
            {
                idsJugadores[i] = jugadorDAO.ObtenerOCrear(nombresJugadores[i]);
            }

            // 2. Crear registro de la partida en la BD
            idPartidaActual = historialDAO.GuardarPartida();
            historialDAO.GuardarParticipantes(idPartidaActual, nombresJugadores.ToList());
            logJuegoDAO.RegistrarTurno(idPartidaActual, idsJugadores[juego.jugadorActual], nombresJugadores[juego.jugadorActual]);

            // 3. Animación: se barajan y reparten las cartas
            await AnimacionInicio();

            AgregarHistorial("Comienza la partida. Turno de " + nombresJugadores[juego.jugadorActual]);
            ActualizarInterfaz();
        }

        // CONFIGURACIÓN, ESTILO Y DISTRIBUCIÓN DE LA INTERFAZ
        private void ConfigurarInterfaz()
        {
            this.DoubleBuffered = true;
            this.MinimumSize = new Size(900, 620);
            this.BackgroundImageLayout = ImageLayout.Stretch;

            // Las manos se dibujan en abanico: los paneles originales quedan ocultos
            FlowLayoutPanel[] paneles = { panelMano, panelManoJugador2, panelManoJugador3 };
            float[] orientaciones = { 0f, 90f, 270f };

            for (int i = 0; i < 3; i++)
            {
                int indice = i;
                paneles[i].Visible = false;

                AbanicoCartas abanico = new AbanicoCartas();
                abanico.Orientacion = orientaciones[i];
                abanico.Atenuar = true;
                abanico.CartaClick += async (carta) => await JugarCarta(carta, indice);
                this.Controls.Add(abanico);
                abanicos[i] = abanico;
            }

            // Etiqueta de turno
            lblTurno = new Label();
            lblTurno.AutoSize = false;
            lblTurno.AutoEllipsis = true;
            lblTurno.TextAlign = ContentAlignment.MiddleCenter;
            lblTurno.ForeColor = Color.White;
            lblTurno.BackColor = Color.FromArgb(40, 40, 40);
            lblTurno.Font = new Font("Segoe UI", 13f, FontStyle.Bold);
            this.Controls.Add(lblTurno);

            // Historial
            lblMensaje.AutoSize = false;
            lblMensaje.AutoEllipsis = true;
            lblMensaje.TextAlign = ContentAlignment.MiddleLeft;
            lblMensaje.Padding = new Padding(14, 4, 14, 4);
            lblMensaje.BackColor = Color.FromArgb(170, 30, 10, 10);
            lblMensaje.ForeColor = Color.White;
            lblMensaje.Font = new Font("Segoe UI", 10.5f);
            lblMensaje.Text = "";

            // Nombres de jugadores
            foreach (Label l in new Label[] { lblNombreJ1, lblNombreJ2, lblNombreJ3 })
            {
                l.AutoSize = false;
                l.AutoEllipsis = true;
                l.TextAlign = ContentAlignment.MiddleCenter;
                l.Font = fuenteNombre;
            }

            // Aviso "UNO"
            lblAvisoUno.AutoSize = false;
            lblAvisoUno.TextAlign = ContentAlignment.MiddleCenter;
            lblAvisoUno.BackColor = Color.Transparent;
            lblAvisoUno.Font = new Font("Segoe UI Black", 20f, FontStyle.Bold);

            foreach (Button b in new Button[] { RobarJ1, RobarJ2, RobarJ3 }) EstilizarRobar(b);
            foreach (Button b in new Button[] { UnoJ1, UnoJ2, UnoJ3 }) EstilizarUno(b);
            AplicarReversoMazo();

            // Se muestran cuando termina la animación de inicio
            cartaCentro.Visible = false;
            foreach (Button b in new Button[] { RobarJ1, RobarJ2, RobarJ3, UnoJ1, UnoJ2, UnoJ3 })
                b.Visible = false;
        }

        //Aquí se agrega el cambio de carta de reversa 
        private void EstilizarRobar(Button b)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.BackColor = Color.Transparent;
            b.UseVisualStyleBackColor = false;
            b.Text = "ROBAR";
            b.ForeColor = Color.White;
            b.Font = new Font("Segoe UI Black", 11f, FontStyle.Bold);
            b.TextAlign = ContentAlignment.BottomCenter;
            b.Cursor = Cursors.Hand;
        }

        private void EstilizarUno(Button b)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(255, 70, 40);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(170, 10, 10);
            b.BackColor = Color.FromArgb(225, 25, 25);
            b.UseVisualStyleBackColor = false;
            b.Text = "¡UNO!";
            b.ForeColor = Color.FromArgb(255, 221, 0);
            b.Font = new Font("Segoe UI Black", 18f, FontStyle.Bold);
            b.Cursor = Cursors.Hand;
        }

        //aquí también (imagen de reverso de carta)
        private void AplicarReversoMazo()
        {
            Image reverso = ImagenesUNO.Reverso();
            if (reverso == null) return;

            foreach (Button boton in new Button[] { RobarJ1, RobarJ2, RobarJ3 })
            {
                boton.BackgroundImage = reverso;
                boton.BackgroundImageLayout = ImageLayout.Stretch;
            }
        }

        // falta implementar iconos aleatorios 
        private void AsignarIconos()
        {
            if (iconosJugadores == null)
                iconosJugadores = ImagenesUNO.IconosAleatorios(3);

            Control[] marcos = { panelJugador, panelJugador2, panelJugador3 };
            Label[] nombres = { lblNombreJ1, lblNombreJ2, lblNombreJ3 };

            for (int i = 0; i < 3; i++)
            {
                marcos[i].BackColor = Color.White;
                marcos[i].BackgroundImage = ImagenesUNO.Cargar(iconosJugadores[i]);
                marcos[i].BackgroundImageLayout = ImageLayout.Zoom;
                nombres[i].Text = juego.jugadores[i].Nombre;
            }
        }

        // AJUSTAR TODO AL TAMAÑO DE LA VENTANA
        private void UNO_Resize(object sender, EventArgs e)
        {
            CentrarCarta();
        }

        private void CentrarCarta()
        {
            AcomodarControles();
        }

        // Coloca todo en proporción al tamaño de la ventana
        private void AcomodarControles()
        {
            if (abanicos[0] == null || lblTurno == null) return;

            int W = this.ClientSize.Width;
            int H = this.ClientSize.Height;
            if (W < 400 || H < 300) return;

            int m = 16;
            int icono = Limitar((int)(H * 0.11), 56, 100);
            int altoNombre = Limitar((int)(H * 0.045), 28, 38);
            int anchoNombre = Limitar((int)(W * 0.12), 150, 210);

            // Barra superior: turno e historial
            int anchoTurno = Limitar((int)(W * 0.42), 360, 600);
            lblTurno.SetBounds((W - anchoTurno) / 2, m, anchoTurno, 44);
            Redondear(lblTurno, 22);

            int anchoHist = Limitar((int)(W * 0.5), 380, 780);
            lblMensaje.SetBounds((W - anchoHist) / 2, m + 50, anchoHist, 72);
            Redondear(lblMensaje, 14);

            int yTop = m + 50 + 72 + 10;

            int anchoLateral = Limitar((int)(W * 0.17), 130, 260);
            int altoBase = Limitar((int)(H * 0.26), 150, 300);
            int yNombreJ1 = H - m - altoNombre;
            int yIconoJ1 = yNombreJ1 - 4 - icono;

            // Jugador 2 (izquierda)
            panelJugador2.SetBounds(m + (anchoNombre - icono) / 2, yTop, icono, icono);
            Circular(panelJugador2);
            lblNombreJ2.SetBounds(m, yTop + icono + 4, anchoNombre, altoNombre);
            Redondear(lblNombreJ2, altoNombre / 2);

            int yMano = yTop + icono + 4 + altoNombre + 8;
            int altoLateral = Math.Max(140, yIconoJ1 - 10 - yMano);
            abanicos[1].SetBounds(m, yMano, anchoLateral, altoLateral);

            // Jugador 3 (derecha)
            int xNombre3 = W - m - anchoNombre;
            panelJugador3.SetBounds(xNombre3 + (anchoNombre - icono) / 2, yTop, icono, icono);
            Circular(panelJugador3);
            lblNombreJ3.SetBounds(xNombre3, yTop + icono + 4, anchoNombre, altoNombre);
            Redondear(lblNombreJ3, altoNombre / 2);
            abanicos[2].SetBounds(W - m - anchoLateral, yMano, anchoLateral, altoLateral);

            // Jugador 1 (abajo)
            panelJugador.SetBounds(m + (anchoNombre - icono) / 2, yIconoJ1, icono, icono);
            Circular(panelJugador);
            lblNombreJ1.SetBounds(m, yNombreJ1, anchoNombre, altoNombre);
            Redondear(lblNombreJ1, altoNombre / 2);

            int anchoBase = Limitar((int)(W * 0.5), 420, 1100);
            abanicos[0].SetBounds((W - anchoBase) / 2, H - m - altoBase, anchoBase, altoBase);

            // Centro: mazo, carta actual y botón UNO
            int topMano1 = H - m - altoBase;
            int cx = W / 2;
            int cy = (yTop + topMano1 - 10) / 2;
            int altoCarta = Limitar((int)(H * 0.2), 110, 220);
            int anchoCarta = (int)(altoCarta * 0.68);

            cartaCentro.SetBounds(cx - anchoCarta / 2, cy - altoCarta / 2, anchoCarta, altoCarta);
            Redondear(cartaCentro, Math.Max(8, anchoCarta / 10));

            foreach (Button b in new Button[] { RobarJ1, RobarJ2, RobarJ3 })
            {
                b.SetBounds(cartaCentro.Left - 28 - anchoCarta, cartaCentro.Top, anchoCarta, altoCarta);
                Redondear(b, Math.Max(8, anchoCarta / 10));
            }

            int anchoUno = Math.Max(anchoCarta, 110);
            int altoUno = Limitar((int)(altoCarta * 0.38), 44, 64);
            foreach (Button b in new Button[] { UnoJ1, UnoJ2, UnoJ3 })
            {
                b.SetBounds(cartaCentro.Right + 28, cy - altoUno / 2, anchoUno, altoUno);
                Redondear(b, altoUno / 2);
            }

            lblAvisoUno.SetBounds(cx - 260, cartaCentro.Top - 56, 520, 46);
        }

        // ACTUALIZAR LA INTERFAZ
        private void ActualizarInterfaz()
        {
            if (juego.jugadores.Count < 3)
                return;

            Label[] nombres = { lblNombreJ1, lblNombreJ2, lblNombreJ3 };
            Button[] robar = { RobarJ1, RobarJ2, RobarJ3 };
            Button[] uno = { UnoJ1, UnoJ2, UnoJ3 };

            for (int i = 0; i < 3; i++)
            {
                Jugador jugador = juego.jugadores[i];
                bool esActual = juego.jugadorActual == i;

                MostrarCartas(abanicos[i], jugador.Cartas, i);

                // nombre + cantidad de cartas, resaltado si es su turno
                nombres[i].Text = jugador.Nombre + "  ·  " + jugador.Cartas.Count;
                nombres[i].Font = esActual ? fuenteNombreActivo : fuenteNombre;
                nombres[i].BackColor = esActual ? Color.Gold : Color.FromArgb(200, 35, 10, 10);
                nombres[i].ForeColor = esActual ? Color.FromArgb(60, 20, 0) : Color.White;

                // botones solo para el jugador con turno
                robar[i].Visible = esActual && !juego.partidaTerminada;
                uno[i].Visible = esActual && jugador.Cartas.Count == 2 && !juego.partidaTerminada;
            }

            MostrarCartaCentro();
            ActualizarTurno();
        }

        private void ActualizarTurno()
        {
            if (lblTurno == null) return;

            if (juego.partidaTerminada && juego.Ganador != null)
            {
                lblTurno.Text = "¡Ganó " + juego.Ganador.Nombre + "!";
                lblTurno.BackColor = Color.Gold;
                lblTurno.ForeColor = Color.FromArgb(60, 20, 0);
                return;
            }

            string texto = "Turno de " + juego.obtenerJugadorActual().Nombre + "  ·  " +
                           (juego.direccion == 1 ? "sentido horario" : "sentido antihorario");

            Carta superior = juego.cartaActual;
            if (superior != null && (superior.Tipo == "Comodin" || superior.Tipo == "+4") && superior.Color != "Negro")
                texto += "  ·  Color: " + superior.Color;

            lblTurno.Text = texto;
            lblTurno.BackColor = coloresJugador[juego.jugadorActual];
            lblTurno.ForeColor = Color.White;
        }

        // Muestra los últimos 3 movimientos en el historial
        private void AgregarHistorial(string texto)
        {
            historial.Insert(0, texto);
            if (historial.Count > 3)
                historial.RemoveAt(historial.Count - 1);

            lblMensaje.Text = string.Join("\n", historial);
        }

        // MOSTRAR CARTAS DE UN JUGADOR (en abanico)
        private void MostrarCartas(AbanicoCartas abanico, List<Carta> cartas, int indiceJugador)
        {
            if (cartas == null)
            {
                abanico.PonerCartas(new List<Carta>(), false);
                return;
            }

            abanico.PonerCartas(cartas, indiceJugador == juego.jugadorActual && !juego.partidaTerminada);

            if (cartas.Count == 0 && !partidaFinalizada)
            {
                partidaFinalizada = true;
                FinalizarPartidaBD(indiceJugador);
                MessageBox.Show($"{nombresJugadores[indiceJugador]} se quedó sin cartas. ¡Ganó y sus datos fueron guardados!");
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
            Image imagen = ImagenesUNO.ImagenCarta(carta);

            cartaCentro.BackgroundImageLayout = ImageLayout.Stretch;

            if (imagen != null)
            {
                cartaCentro.Text = "";
                cartaCentro.BackgroundImage = imagen;
                cartaCentro.BackColor = Color.Transparent;
            }
            else
            {
                cartaCentro.BackgroundImage = null;
                cartaCentro.Text = carta.Color + "\n" + carta.Valor;
                cartaCentro.BackColor = ColorSegunCarta(carta);
                cartaCentro.ForeColor = Color.Black;
                cartaCentro.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            }

            cartaCentro.Visible = true;
        }

        // ANIMACIONES
        private Point Centro(Control c)
        {
            return new Point(c.Left + c.Width / 2, c.Top + c.Height / 2);
        }

        private Point EnFormulario(Control c, Point p)
        {
            return new Point(c.Left + p.X, c.Top + p.Y);
        }

        private Panel NuevaCartaVuelo(Image imagen, Size tam)
        {
            Panel p = new Panel();
            p.Size = tam;
            p.BackgroundImage = imagen;
            p.BackgroundImageLayout = ImageLayout.Stretch;
            p.BackColor = imagen == null ? Color.DarkRed : Color.Transparent;
            this.Controls.Add(p);
            p.BringToFront();
            return p;
        }

        // Una carta que "viaja" de un punto a otro
        private async Task VolarCarta(Image img, Point desde, Size tamDesde, Point hasta, Size tamHasta, int ms)
        {
            Panel vuelo = NuevaCartaVuelo(img, tamDesde);
            vuelo.SetBounds(desde.X - tamDesde.Width / 2, desde.Y - tamDesde.Height / 2, tamDesde.Width, tamDesde.Height);

            Stopwatch reloj = Stopwatch.StartNew();
            while (reloj.ElapsedMilliseconds < ms)
            {
                if (this.IsDisposed) return;

                double t = reloj.ElapsedMilliseconds / (double)ms;
                t = 1 - Math.Pow(1 - t, 3);

                int w = (int)(tamDesde.Width + (tamHasta.Width - tamDesde.Width) * t);
                int h = (int)(tamDesde.Height + (tamHasta.Height - tamDesde.Height) * t);
                int x = (int)(desde.X + (hasta.X - desde.X) * t);
                int y = (int)(desde.Y + (hasta.Y - desde.Y) * t);

                vuelo.SetBounds(x - w / 2, y - h / 2, w, h);
                await Task.Delay(15);
            }

            this.Controls.Remove(vuelo);
            vuelo.Dispose();
        }

        private async Task VolarConEspera(Image img, Point desde, Size tamDesde, Point hasta, Size tamHasta, int ms, int espera)
        {
            if (espera > 0) await Task.Delay(espera);
            await VolarCarta(img, desde, tamDesde, hasta, tamHasta, ms);
        }

        // Cartas boca abajo que salen del mazo hacia la mano de un jugador (robar / penalizaciones)
        private async Task VolarReversos(int cantidad, int indiceDestino)
        {
            Image reverso = ImagenesUNO.Reverso();
            Point desde = Centro(RobarJ1);
            Point hasta = Centro(abanicos[indiceDestino]);
            Size tam = cartaCentro.Size;
            Size tamFinal = new Size(tam.Width * 2 / 3, tam.Height * 2 / 3);

            List<Task> vuelos = new List<Task>();
            for (int i = 0; i < cantidad; i++)
                vuelos.Add(VolarConEspera(reverso, desde, tam, hasta, tamFinal, 360, i * 140));

            await Task.WhenAll(vuelos);
        }

        // La carta jugada viaja de la mano al centro
        private async Task AnimarJugada(Carta carta, int indiceJugador)
        {
            AbanicoCartas mano = abanicos[indiceJugador];
            Point origen = EnFormulario(mano, mano.CentroCarta(carta));
            Size tamOrigen = mano.TamanoCarta();

            mano.CartaOculta = carta;
            mano.Invalidate();

            await VolarCarta(ImagenesUNO.ImagenCarta(carta), origen, tamOrigen, Centro(cartaCentro), cartaCentro.Size, 320);
        }

        private async Task RepartirCarta(Image img, Point desde, Size tam, Point hasta, int espera, int jugador, int[] recibidas)
        {
            await Task.Delay(espera);
            await VolarCarta(img, desde, tam, hasta, new Size(tam.Width * 2 / 3, tam.Height * 2 / 3), 320);

            if (this.IsDisposed) return;
            recibidas[jugador]++;
            abanicos[jugador].MaxVisibles = recibidas[jugador];
        }

        // Barajeo + reparto al iniciar la partida
        private async Task AnimacionInicio()
        {
            animando = true;
            List<Panel> pila = new List<Panel>();

            try
            {
                lblTurno.Text = "Barajando y repartiendo...";
                lblTurno.BackColor = Color.FromArgb(40, 40, 40);

                Image reverso = ImagenesUNO.Reverso();
                Size tam = cartaCentro.Size;
                Point centro = Centro(cartaCentro);

                // las manos existen pero aún no se ven
                for (int i = 0; i < 3; i++)
                {
                    abanicos[i].PonerCartas(juego.jugadores[i].Cartas, false);
                    abanicos[i].MaxVisibles = 0;
                }

                // 1) BARAJAR: dos montones se abren y se intercalan
                for (int i = 0; i < 10; i++)
                {
                    Panel p = NuevaCartaVuelo(reverso, tam);
                    p.Location = new Point(centro.X - tam.Width / 2, centro.Y - tam.Height / 2 - i * 2);
                    pila.Add(p);
                }

                for (int ronda = 0; ronda < 3; ronda++)
                {
                    for (int i = 0; i < pila.Count; i += 2) pila[i].BringToFront();
                    for (int i = 1; i < pila.Count; i += 2) pila[i].BringToFront();

                    Stopwatch reloj = Stopwatch.StartNew();
                    bool intercalado = false;

                    while (reloj.ElapsedMilliseconds < 500)
                    {
                        if (this.IsDisposed) return;

                        double t = reloj.ElapsedMilliseconds / 500.0;
                        double abrir = Math.Sin(Math.PI * t);

                        for (int i = 0; i < pila.Count; i++)
                        {
                            int lado = (i % 2 == 0) ? -1 : 1;
                            int dx = (int)(lado * abrir * tam.Width * 0.9);
                            pila[i].Location = new Point(centro.X - tam.Width / 2 + dx, centro.Y - tam.Height / 2 - i * 2);
                        }

                        if (!intercalado && t >= 0.5)
                        {
                            intercalado = true;
                            for (int i = 0; i < pila.Count; i++) pila[i].BringToFront();
                        }

                        await Task.Delay(15);
                    }
                }

                // 2) REPARTIR: una carta a cada jugador por ronda
                int[] recibidas = new int[3];
                int cartasPorJugador = juego.jugadores[0].Cartas.Count;
                List<Task> vuelos = new List<Task>();

                for (int ronda = 0; ronda < cartasPorJugador; ronda++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        int k = ronda * 3 + j;
                        vuelos.Add(RepartirCarta(reverso, centro, tam, Centro(abanicos[j]), k * 70, j, recibidas));
                    }
                }

                await Task.WhenAll(vuelos);
            }
            finally
            {
                foreach (Panel p in pila)
                {
                    this.Controls.Remove(p);
                    p.Dispose();
                }
                for (int i = 0; i < 3; i++) abanicos[i].MaxVisibles = -1;
                animando = false;
            }
        }

        // JUGAR CARTA
        private async Task JugarCarta(Carta cartaJugada, int indiceJugador)
        {
            if (animando || juego.partidaTerminada || indiceJugador != juego.jugadorActual)
                return;

            animando = true;
            try
            {
                await JugarCartaInterno(cartaJugada, indiceJugador);
            }
            finally
            {
                animando = false;
            }
        }

        private async Task JugarCartaInterno(Carta cartaJugada, int indiceJugador)
        {
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

            // La carta viaja de la mano al centro
            await AnimarJugada(cartaJugada, indiceJugador);

            // 1) Sacar la carta de la mano y ponerla en el centro (UNA sola vez)
            jugador.quitarCarta(cartaJugada);
            juego.agregarCartaDescarte(cartaJugada);

            string colorCarta = cartaJugada.Color ?? "SinColor";
            string valorCarta = cartaJugada.Valor ?? cartaJugada.Tipo;
            string tipoCarta = cartaJugada.Tipo;
            string descripcionCartaBD = (colorCarta + " " + valorCarta).Trim();

            // 2) Registrar la jugada en la BD y en la API
            logJuegoDAO.RegistrarCartaJugada(idPartidaActual, idJugador, jugador.Nombre, descripcionCartaBD);

            _ = APICliente.RegistrarJugadaAsync(
                idPartidaActual,
                idJugador,
                jugador.Nombre,
                colorCarta,
                valorCarta,
                tipoCarta
            );

            string mensaje = jugador.Nombre + " jugó: " + valorCarta;

            // 3) Penalización por no decir UNO
            if (jugador.Cartas.Count == 1 && !juego.declaroUNO)
            {
                juego.agregaCartaRobada(jugador);
                juego.agregaCartaRobada(jugador);
                logJuegoDAO.RegistrarMensaje(idPartidaActual, idJugador,
                    jugador.Nombre + " no declaró UNO y robó 2 cartas de penalización");
                mensaje += ". No dijo UNO y roba 2 cartas";
                await VolarReversos(2, indiceJugador);
            }

            // 4) Efecto de la carta (aquí se cambia el turno UNA sola vez)
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
                    await VolarReversos(4, juego.jugadores.IndexOf(afectado));
                    break;

                case "Reversa":
                    if (juego.jugadores.Count == 2)
                    {
                        juego.aplicarSalta();   // con 2 jugadores la reversa funciona como salto
                    }
                    else
                    {
                        juego.aplicarReversa();   // invierte el sentido
                        juego.cambiarTurno();     // y pasa al siguiente en el nuevo sentido
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
                    await VolarReversos(2, juego.jugadores.IndexOf(afectado));
                    break;

                default:
                    juego.cambiarTurno();
                    break;
            }

            AgregarHistorial(mensaje);

            // 5) Comprobar ganador
            if (juego.esGanador(jugador))
            {
                juego.comprobarGanador(jugador);
                logJuegoDAO.RegistrarMensaje(idPartidaActual, idJugador, jugador.Nombre + " ganó la partida");
                ActualizarInterfaz();   // aquí MostrarCartas guarda el resultado y muestra el aviso
                return;
            }

            FinalizarTurno();
        }

        // ROBAR Y PASAR TURNO
        private async void BtnRobar_Click(object sender, EventArgs e)
        {
            if (animando || juego.partidaTerminada)
                return;

            Jugador jugador = juego.obtenerJugadorActual();

            // Segundo clic en el mazo en el mismo turno = pasar turno
            if (yaRobo)
            {
                PasarTurno(jugador);
                return;
            }

            animando = true;
            try
            {
                Carta cartaNueva = juego.robarDuranteTurno(jugador);

                if (cartaNueva == null)
                {
                    MessageBox.Show("No quedan cartas en el mazo. Se pasa el turno.", "Mazo vacío",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    PasarTurno(jugador);
                    return;
                }

                string colorCarta = cartaNueva.Color ?? "SinColor";
                string valorCarta = cartaNueva.Valor ?? cartaNueva.Tipo;
                string tipoCarta = cartaNueva.Tipo;
                int idJugadorActual = idsJugadores[juego.jugadorActual];
                string nombreActual = nombresJugadores[juego.jugadorActual];

                // Registrar la carta robada en la BD y en la API (una sola vez)
                logJuegoDAO.RegistrarCartaRobada(idPartidaActual, idJugadorActual, nombreActual,
                    (colorCarta + " " + valorCarta).Trim());

                _ = APICliente.RegistrarCartaRobadaAsync(
                    idPartidaActual,
                    idJugadorActual,
                    nombreActual,
                    colorCarta,
                    valorCarta,
                    tipoCarta
                );

                yaRobo = true;
                cartaRobadaEnTurno = cartaNueva;

                // La carta viaja del mazo a la mano
                await VolarReversos(1, juego.jugadorActual);

                if (juego.puedeJugarCartaRobada(jugador, cartaNueva))
                {
                    AgregarHistorial(jugador.Nombre + " robó una carta que puede jugar. Juégala o haz clic en el mazo para pasar.");
                    ActualizarInterfaz();
                }
                else
                {
                    AgregarHistorial(jugador.Nombre + " robó una carta y no puede jugarla. Pasa el turno.");
                    PasarTurno(jugador);   // solo cambia el turno, NO el sentido
                }
            }
            finally
            {
                animando = false;
            }
        }

        private void BtnRobar_Click_1(object sender, EventArgs e) => BtnRobar_Click(sender, e);
        private void RobarJ2_Click(object sender, EventArgs e) => BtnRobar_Click(sender, e);
        private void RobarJ3_Click(object sender, EventArgs e) => BtnRobar_Click(sender, e);

        private void PasarTurno(Jugador jugador)
        {
            logJuegoDAO.RegistrarMensaje(idPartidaActual, idsJugadores[juego.jugadorActual], jugador.Nombre + " pasó su turno");
            juego.cambiarTurno();
            FinalizarTurno();
        }

        private void FinalizarTurno()
        {
            yaRobo = false;
            cartaRobadaEnTurno = null;
            juego.declaroUNO = false;

            logJuegoDAO.RegistrarTurno(idPartidaActual, idsJugadores[juego.jugadorActual], nombresJugadores[juego.jugadorActual]);
            ActualizarInterfaz();
        }

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

            logJuegoDAO.RegistrarMensaje(idPartidaActual, idsJugadores[indiceJugador], jugador.Nombre + " declaró UNO");

            timerMensaje.Stop();
            lblAvisoUno.Text = "¡" + jugador.Nombre.ToUpper() + " DIJO UNO!";
            lblAvisoUno.ForeColor = Color.Gold;
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

        // COLOR DE RESPALDO (si falta una imagen)
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
}
