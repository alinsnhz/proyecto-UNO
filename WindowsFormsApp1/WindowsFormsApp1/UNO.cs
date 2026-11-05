using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using static WindowsFormsApp1.EstiloUI;
using proyectoUNO;

namespace WindowsFormsApp1
{
    /*
     * Ventana principal de la partida del cliente UNO.
     * Coordina la interfaz gráfica, dibuja las cartas y se comunica con el servidor vía sockets.
    */
    public partial class UNO : Form
    {
        // Instancia opcional para referencias visuales locales pero sin ejecutar reglas
        private JuegoUNO juego;
        private Timer timerMensaje;

        // Instancias de BD y control local (si se mantienen en cliente para estadísticas)
        private HistorialDAO historialDAO = new HistorialDAO();
        private LogJuegoDAO logJuegoDAO = new LogJuegoDAO();
        private JugadorDAO jugadorDAO = new JugadorDAO();

        private string[] nombresJugadores = new string[] { "Jugador 1", "Jugador 2", "Jugador 3" };
        private int[] idsJugadores = new int[3];

        // Interfaz visual (Abanicos)
        private AbanicoCartas[] abanicos = new AbanicoCartas[3];
        private Label lblTurno;
        private string[] iconosJugadores = null;
        private List<string> historial = new List<string>();

        // Conexión por socket con el servidor externo
        private ClienteSocket clienteSocket;

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

            // Se inicializa el objeto base únicamente para mantener estructuras de jugadores visuales
            juego = new JuegoUNO();
            juego.jugadores.Add(new Jugador(1, nombresJugadores[0]));
            juego.jugadores.Add(new Jugador(2, nombresJugadores[1]));
            juego.jugadores.Add(new Jugador(3, nombresJugadores[2]));

            this.Resize += UNO_Resize;

            if (lblAvisoUno != null)
                lblAvisoUno.Visible = false;

            timerMensaje = new Timer();
            timerMensaje.Interval = 2000;
            timerMensaje.Tick += timerMensaje_Tick;

            if (UnoJ1 != null) UnoJ1.Click += UnoJ1_Click;
            if (UnoJ2 != null) UnoJ2.Click += UnoJ2_Click;
            if (UnoJ3 != null) UnoJ3.Click += UnoJ3_Click;

            ConfigurarInterfaz();
        }

        // Constructor que recibe el socket y el nombre del jugador desde InicioForm
        public UNO(ClienteSocket socketConectado, string nombreJugador) : this()
        {
            clienteSocket = socketConectado;

            if (clienteSocket != null)
            {
                clienteSocket.OnMessageReceived += ProcesarMensajeServidor;
                clienteSocket.OnDisconnected += ManejarDesconexion;
            }

            if (juego.jugadores.Count > 0)
            {
                juego.jugadores[0].Nombre = nombreJugador;
                nombresJugadores[0] = nombreJugador;
            }

            this.WindowState = FormWindowState.Maximized;
        }

        // Constructor alternativo si se requiere pasar nombres e iconos directamente
        public UNO(string[] nombres, string[] rutasIconos, ClienteSocket socketConectado) : this(socketConectado, nombres.Length > 0 ? nombres[0] : "Jugador 1")
        {
            for (int i = 0; i < 3 && i < nombres.Length; i++)
            {
                nombresJugadores[i] = nombres[i];
                juego.jugadores[i].Nombre = nombres[i];
            }
            iconosJugadores = rutasIconos;
        }

        // Inicializa la ventana cuando se carga y configura los controles visuales
        private void UNO_Load(object sender, EventArgs e)
        {
            AcomodarControles();
            AsignarIconos();

            AgregarHistorial("Conectado a la partida. Esperando estado del servidor...");
        }

        // Procesa los comandos y tramas recibidas desde el servidor de sockets en tiempo real
        private void ProcesarMensajeServidor(string mensajeServidor)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<string>(ProcesarMensajeServidor), mensajeServidor);
                return;
            }

            try
            {
                if (mensajeServidor.StartsWith("ESTADO_PARTIDA|"))
                {
                    string payload = mensajeServidor.Substring("ESTADO_PARTIDA|".Length);

                    // Nota: Deserializa aquí usando tu DTO recibido del servidor (ej. Newtonsoft.Json)
                    // var estado = Newtonsoft.Json.JsonConvert.DeserializeObject<EstadoPartidaDto>(payload);

                    // 1. Actualizar cartas propias y habilitar clics solo si es su turno:
                    // bool esMiTurno = (estado.TurnoActualId == miIdJugador);
                    // abanicos[0].PonerCartas(estado.ManoJugadorLocal, esMiTurno);

                    // 2. Mostrar únicamente el conteo de cartas de los rivales mediante MaxVisibles (reversos ocultos):
                    // abanicos[1].MaxVisibles = estado.CantidadCartasJugador2;
                    // abanicos[2].MaxVisibles = estado.CantidadCartasJugador3;

                    // 3. Actualizar carta del centro y turno en la interfaz:
                    // ActualizarCartaCentro(estado.CartaActual);
                    // lblTurno.Text = "Turno de: " + estado.NombreTurnoActual;
                }
                else if (mensajeServidor.StartsWith("MENSAJE|"))
                {
                    AgregarHistorial(mensajeServidor.Substring("MENSAJE|".Length));
                }
            }
            catch (Exception)
            {
                AgregarHistorial("Error al procesar el estado recibido del servidor.");
            }
        }

        // Maneja la pérdida de conexión informada por el socket
        private void ManejarDesconexion(string motivo)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<string>(ManejarDesconexion), motivo);
                return;
            }

            MessageBox.Show(motivo, "Desconectado", MessageBoxButtons.OK, MessageBoxIcon.Error);
            this.Close();
        }

        // Aplica la configuración visual inicial a los controles de la interfaz
        private void ConfigurarInterfaz()
        {
            this.DoubleBuffered = true;
            this.MinimumSize = new Size(900, 620);
            this.BackgroundImageLayout = ImageLayout.Stretch;

            FlowLayoutPanel[] paneles = { panelMano, panelManoJugador2, panelManoJugador3 };
            float[] orientaciones = { 0f, 90f, 270f };

            for (int i = 0; i < 3; i++)
            {
                int indice = i;
                paneles[i].Visible = false;

                AbanicoCartas abanico = new AbanicoCartas();
                abanico.Orientacion = orientaciones[i];
                abanico.Atenuar = true;

                if (i == 0)
                {
                    // La mano propia permite interacción y envío de jugadas al servidor
                    abanico.CartaClick += async (carta) => await EnviarJugadaAlServidor(carta, indice);
                }
                else
                {
                    // Manos ajenas: configuradas inactivas y con relleno de cartas ficticias para mostrar solo el reverso y conteo
                    abanico.ManoActiva = false;
                    abanico.PonerCartas(GenerarCartasFicticias(20), false);
                }

                this.Controls.Add(abanico);
                abanicos[i] = abanico;
            }

            lblTurno = new Label();
            lblTurno.AutoSize = false;
            lblTurno.AutoEllipsis = true;
            lblTurno.TextAlign = ContentAlignment.MiddleCenter;
            lblTurno.ForeColor = Color.White;
            lblTurno.BackColor = Color.FromArgb(40, 40, 40);
            lblTurno.Font = new Font("Segoe UI", 13f, FontStyle.Bold);
            this.Controls.Add(lblTurno);

            lblMensaje.AutoSize = false;
            lblMensaje.AutoEllipsis = true;
            lblMensaje.TextAlign = ContentAlignment.MiddleLeft;
            lblMensaje.Padding = new Padding(14, 4, 14, 4);
            lblMensaje.BackColor = Color.FromArgb(170, 30, 10, 10);
            lblMensaje.ForeColor = Color.White;
            lblMensaje.Font = new Font("Segoe UI", 10.5f);
            lblMensaje.Text = "";

            foreach (Label l in new Label[] { lblNombreJ1, lblNombreJ2, lblNombreJ3 })
            {
                l.AutoSize = false;
                l.AutoEllipsis = true;
                l.TextAlign = ContentAlignment.MiddleCenter;
                l.Font = fuenteNombre;
            }

            lblAvisoUno.AutoSize = false;
            lblAvisoUno.TextAlign = ContentAlignment.MiddleCenter;
            lblAvisoUno.BackColor = Color.Transparent;
            lblAvisoUno.Font = new Font("Segoe UI Black", 20f, FontStyle.Bold);

            foreach (Button b in new Button[] { RobarJ1, RobarJ2, RobarJ3 }) EstilizarRobar(b);
            foreach (Button b in new Button[] { UnoJ1, UnoJ2, UnoJ3 }) EstilizarUno(b);
            AplicarReversoMazo();

            cartaCentro.Visible = false;
            foreach (Button b in new Button[] { RobarJ1, RobarJ2, RobarJ3, UnoJ1, UnoJ2, UnoJ3 })
                b.Visible = false;
        }

        // Genera una lista de cartas de relleno para representar visualmente los reversos de los rivales
        private List<Carta> GenerarCartasFicticias(int cantidad)
        {
            List<Carta> lista = new List<Carta>();
            for (int i = 0; i < cantidad; i++)
            {
                lista.Add(new Carta("Negro", "Reverso", "Especial"));
            }
            return lista;
        }

        // Da estilo visual al botón de robar
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

        // Da estilo visual al botón de declarar UNO
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

        // Coloca o actualiza la imagen del reverso del mazo
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

        // Asigna iconos visuales a los jugadores en la interfaz
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

        // Responde al cambio de tamaño de la ventana reorganizando los elementos
        private void UNO_Resize(object sender, EventArgs e)
        {
            AcomodarControles();
        }

        // Calcula y actualiza la posición y tamaño de los controles de la ventana
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

            panelJugador2.SetBounds(m + (anchoNombre - icono) / 2, yTop, icono, icono);
            Circular(panelJugador2);
            lblNombreJ2.SetBounds(m, yTop + icono + 4, anchoNombre, altoNombre);
            Redondear(lblNombreJ2, altoNombre / 2);

            int yMano = yTop + icono + 4 + altoNombre + 8;
            int altoLateral = Math.Max(140, yIconoJ1 - 10 - yMano);
            abanicos[1].SetBounds(m, yMano, anchoLateral, altoLateral);

            int xNombre3 = W - m - anchoNombre;
            panelJugador3.SetBounds(xNombre3 + (anchoNombre - icono) / 2, yTop, icono, icono);
            Circular(panelJugador3);
            lblNombreJ3.SetBounds(xNombre3, yTop + icono + 4, anchoNombre, altoNombre);
            Redondear(lblNombreJ3, altoNombre / 2);
            abanicos[2].SetBounds(W - m - anchoLateral, yMano, anchoLateral, altoLateral);

            panelJugador.SetBounds(m + (anchoNombre - icono) / 2, yIconoJ1, icono, icono);
            Circular(panelJugador);
            lblNombreJ1.SetBounds(m, yNombreJ1, anchoNombre, altoNombre);
            Redondear(lblNombreJ1, altoNombre / 2);

            int anchoBase = Limitar((int)(W * 0.5), 420, 1100);
            abanicos[0].SetBounds((W - anchoBase) / 2, H - m - altoBase, anchoBase, altoBase);

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

        // Envía la acción de jugar una carta al servidor mediante sockets
        private async Task EnviarJugadaAlServidor(Carta cartaJugada, int indiceJugador)
        {
            if (clienteSocket != null && clienteSocket.IsConnected)
            {
                string trama = $"JUGAR|{cartaJugada.Id}|{cartaJugada.Color}|{cartaJugada.Tipo}";
                await clienteSocket.EnviarMensajeAsync(trama);
            }
        }

        // Añade un mensaje al historial visual de la partida
        private void AgregarHistorial(string texto)
        {
            historial.Insert(0, texto);
            if (historial.Count > 3)
                historial.RemoveAt(historial.Count - 1);

            lblMensaje.Text = string.Join("\n", historial);
        }

        // MOSTRAR CARTAS DE UN JUGADOR (en abanico)
        //entrega las cartas de un jugador al control gráfico que las dibuja
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

        //registra en la base de datos el resultado final usando el índice del ganador
        private void FinalizarPartidaBD(int indiceGanador)
        {
            string nombreGanador = nombresJugadores[indiceGanador];

            historialDAO.GuardarGanador(idPartidaActual, nombreGanador);
            historialDAO.RegistrarGanadasPerdidas(idPartidaActual, nombreGanador);

            for (int i = 0; i < nombresJugadores.Length; i++)
            {
                historialDAO.GuardarResultado(idPartidaActual, nombresJugadores[i], juego.jugadores[i].Cartas.Count);
            }
            /*if (juego.partidaTerminada)
            {
                MessageBox.Show($"{juego.Ganador.Nombre} ha ganado la partida!", "Fin del Juego",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }*/
        }

        // MOSTRAR CARTA CENTRAL
        //actualiza la carta que aparece en el centro de la mesa
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
        //calcula el punto central de un control
        private Point Centro(Control c)
        {
            return new Point(c.Left + c.Width / 2, c.Top + c.Height / 2);
        }

        //convierte un punto relativo a un control a coordenadas del formulario
        private Point EnFormulario(Control c, Point p)
        {
            return new Point(c.Left + p.X, c.Top + p.Y);
        }

        //crea el panel temporal usado para animar una carta en movimiento
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

        // anima el desplazamiento y cambio de tamaño de una imagen de carta
        // entre dos posiciones durante el tiempo indicado
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

        //espera el tiempo especificado y después ejecuta la animación de vuelo
        private async Task VolarConEspera(Image img, Point desde, Size tamDesde, Point hasta, Size tamHasta, int ms, int espera)
        {
            if (espera > 0) await Task.Delay(espera);
            await VolarCarta(img, desde, tamDesde, hasta, tamHasta, ms);
        }

        // Cartas boca abajo que salen del mazo hacia la mano de un jugador (robar / penalizaciones)
        //anima el movimiento de cierta cantidad de reversos hacia el jugador destino
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
        //anima visualmente una carta que se juega desde la mano de un jugador hacia el centro
        private async Task AnimarJugada(Carta carta, int indiceJugador)
        {
            AbanicoCartas mano = abanicos[indiceJugador];
            Point origen = EnFormulario(mano, mano.CentroCarta(carta));
            Size tamOrigen = mano.TamanoCarta();

            mano.CartaOculta = carta;
            mano.Invalidate();

            await VolarCarta(ImagenesUNO.ImagenCarta(carta), origen, tamOrigen, Centro(cartaCentro), cartaCentro.Size, 320);
        }

        //anima el reparto de una carta hacia el jugador indicado y controla la espera/contador de cartas recibidas
        private async Task RepartirCarta(Image img, Point desde, Size tam, Point hasta, int espera, int jugador, int[] recibidas)
        {
            await Task.Delay(espera);
            await VolarCarta(img, desde, tam, hasta, new Size(tam.Width * 2 / 3, tam.Height * 2 / 3), 320);

            if (this.IsDisposed) return;
            recibidas[jugador]++;
            abanicos[jugador].MaxVisibles = recibidas[jugador];
        }

        // Barajeo + reparto al iniciar la partida
        //ejecuta la animación de inicio o reparto inicial de cartas
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
        //coordina la jugada seleccionada, su animación y el procesamiento interno
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

        //aplica la jugada a la lógica, actualiza la mano/carta central y procesa efectos de la carta
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
            Jugador afectado = null;

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

            switch (cartaJugada.Tipo)
            {
                case "Comodin":
                    juego.aplicarComodin(colorElegido);
                    logJuegoDAO.RegistrarCambioColor(idPartidaActual, idJugador, jugador.Nombre, colorElegido);
                    mensaje += " (color: " + colorElegido + ")";
                    juego.cambiarTurno();
                    break;

                case "+4":
                    afectado = ObtenerSiguienteJugador();

                    juego.aplicarMasCuatro(colorElegido);

                    logJuegoDAO.RegistrarAccionEspecial(idPartidaActual, idJugador, jugador.Nombre, "+4");
                    logJuegoDAO.RegistrarCambioColor(idPartidaActual, idJugador, jugador.Nombre, colorElegido);

                    mensaje += " (color: " + colorElegido + "). " + afectado.Nombre + " roba 4 y pierde su turno";

                    await VolarReversos(4, juego.jugadores.IndexOf(afectado));
                    break;

                case "Reversa":
                    if (juego.jugadores.Count == 2)
                    {
                        juego.aplicarSalta();
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
                    afectado = ObtenerSiguienteJugador();

                    juego.aplicarMasDos();

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
                this.Close();//cierra la ventana del juego
                return;
            }

            FinalizarTurno();
        }

        // Obtiene al siguiente jugador, respetando el sentido del juego.
        private Jugador ObtenerSiguienteJugador()
        {
            int indice = juego.jugadorActual + juego.direccion;

            if (indice >= juego.jugadores.Count)
                indice = 0;

            if (indice < 0)
                indice = juego.jugadores.Count - 1;

            return juego.jugadores[indice];
        }

        // ROBAR Y PASAR TURNO
        //procesa el clic en robar, añade la carta a la mano y actualiza la interfaz/turno según la lógica
        private async void BtnRobar_Click(object sender, EventArgs e)
        {
            if (clienteSocket != null && clienteSocket.IsConnected)
            {
                await clienteSocket.EnviarMensajeAsync("ROBAR");
            }
        }

        private void BtnRobar_Click_1(object sender, EventArgs e) => BtnRobar_Click(sender, e);
        private void RobarJ2_Click(object sender, EventArgs e) => BtnRobar_Click(sender, e);
        private void RobarJ3_Click(object sender, EventArgs e) => BtnRobar_Click(sender, e);

        // Envía la declaración de UNO al servidor mediante socket
        private async void MostrarAvisoUno(int indiceJugador)
        {
            if (clienteSocket != null && clienteSocket.IsConnected)
            {
                await clienteSocket.EnviarMensajeAsync("UNO");
            }
        }

        private void UnoJ1_Click(object sender, EventArgs e) => MostrarAvisoUno(0);
        private void UnoJ2_Click(object sender, EventArgs e) => MostrarAvisoUno(1);
        private void UnoJ3_Click(object sender, EventArgs e) => MostrarAvisoUno(2);

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
}