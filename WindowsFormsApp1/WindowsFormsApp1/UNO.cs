using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using proyectoUNO;
using static WindowsFormsApp1.EstiloUI;

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

        private int idPartidaActual = 0;
        private bool partidaFinalizada = false;

        private string[] nombresJugadores = new string[] { "Jugador 1", "Jugador 2", "Jugador 3" };
        private int[] idsJugadores = new int[3];

        // Interfaz visual (Abanicos)
        private AbanicoCartas[] abanicos = new AbanicoCartas[3];
        private Label lblTurno;
        private bool animando = false;
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

        // Constructor que recibe nombres, iconos y la instancia del socket ya conectada
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

        // Inicializa la ventana cuando se carga y configura los controles visuales
        private void UNO_Load(object sender, EventArgs e)
        {
            AcomodarControles();
            AsignarIconos();

            // NOTA: Ya no se ejecuta juego.iniciarPartida() ni juego.colocarCartaInicial() localmente,
            // pues el servidor es el que manda el estado inicial por sockets.

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

            // Aquí deserializarás o interpretarás el mensaje ESTADO_PARTIDA que envía el servidor
            // Ejemplo básico de lectura de trama:
            // if (mensajeServidor.StartsWith("ESTADO_PARTIDA|")) { ... ActualizarInterfazDesdeServidor(...); }
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
                // Al hacer clic en una carta, ahora enviamos la orden al servidor por socket en lugar de validar localmente
                abanico.CartaClick += async (carta) => await EnviarJugadaAlServidor(carta, indice);
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

        // Envía la acción de jugar una carta al servidor mediante sockets en lugar de resolverlo localmente
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

        // Manejador del botón de robar carta que envía la solicitud al servidor por socket
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