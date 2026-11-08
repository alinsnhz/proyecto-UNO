using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using proyectoUNO;
using ProyectoUno.Protocolo;
using static WindowsFormsApp1.EstiloUI;

namespace WindowsFormsApp1
{
    /*
- Es la ventana principal de la partida. 
- Coordina la interfaz, la lógica del juego, las animaciones, los botones y el registro de eventos
    */
    public partial class UNO : Form
    {
        private juegoUNO juego;
        private Timer timerMensaje;

        // Instancias de BD y control de sesión/partida
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

        // Conexión y socket para el protocolo de servidor
        private ClienteSocket clienteSocket;
        private string nombreJugadorActual;
        private int idJugadorServidor = 0;
        private bool puedePasarTurnoServidor = false;

        // Indica qué jugador quedó con una carta y todavía no declaró UNO.
        private bool[] jugadoresPendientesUNO = new bool[3];

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

            juego = new juegoUNO();

            juego.jugadores.Add(new Jugador(1, nombresJugadores[0]));
            juego.jugadores.Add(new Jugador(2, nombresJugadores[1]));
            juego.jugadores.Add(new Jugador(3, nombresJugadores[2]));

            this.Resize += UNO_Resize;

            lblAvisoUno.Visible = false;

            timerMensaje = new Timer();
            timerMensaje.Interval = 2000;
            timerMensaje.Tick += timerMensaje_Tick;

            // UnoJ1 ya tiene su evento conectado desde el Designer.
            if (UnoJ2 != null) UnoJ2.Click += UnoJ2_Click;
            if (UnoJ3 != null) UnoJ3.Click += UnoJ3_Click;

            ConfigurarInterfaz();
        }

        private void ProcesarEstadoServidor(string json)
        {
            try
            {
                var respuesta = JsonConvert.DeserializeObject<Mensajes>(json);

                if (respuesta == null)
                    return;

                if (respuesta.EstadoPartida == null)
                {
                    if (InvokeRequired)
                    {
                        BeginInvoke(new Action(() => ProcesarRespuestaAccion(respuesta)));
                    }
                    else
                    {
                        ProcesarRespuestaAccion(respuesta);
                    }

                    return;
                }

                EstadoPartidaDto estado = respuesta.EstadoPartida;

                if (InvokeRequired)
                {
                    BeginInvoke(new Action(() => AplicarEstadoServidor(estado)));
                    return;
                }

                AplicarEstadoServidor(estado);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error al procesar estado del servidor: " + ex.Message);
            }
        }

        private void ProcesarRespuestaAccion(Mensajes respuesta)
        {
            if (respuesta == null)
                return;

            if (respuesta.Aceptado != true)
            {
                AgregarHistorial(respuesta.Mensaje);
                return;
            }

            if (string.Equals(
                respuesta.Tipo, "RobarCarta",
                StringComparison.OrdinalIgnoreCase))
            {
                yaRobo = true;
            }
            else if (
                string.Equals(respuesta.Tipo, "PasarTurno",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(respuesta.Tipo, "JugarCarta",
                    StringComparison.OrdinalIgnoreCase))
            {
                yaRobo = false;
                cartaRobadaEnTurno = null;
            }

            AgregarHistorial(respuesta.Mensaje);
        }

        private void AplicarEstadoServidor(EstadoPartidaDto estado)
        {
            if (estado == null || estado.Jugadores == null || juego == null)
                return;

            // Espera os três jogadores estarem registrados.
            if (!estado.PartidaIniciada || estado.Jugadores.Count != 3)
                return;

            List<Jugador> jugadoresServidor = new List<Jugador>();

            foreach (var jugadorServidor in estado.Jugadores)
            {
                Jugador jugadorLocal = new Jugador(
                    jugadorServidor.Id,
                    jugadorServidor.Nombre
                );

                bool esMiJugador = string.Equals(
                    jugadorServidor.Nombre?.Trim(),
                    nombreJugadorActual?.Trim(),
                    StringComparison.OrdinalIgnoreCase
                );

                if (esMiJugador)
                {
                    jugadorLocal.Cartas = estado.MisCartas == null ? new List<Carta>() : estado.MisCartas.ConvertAll(c => new Carta
                    {
                        Id = c.Id,
                        Color = c.Color,
                        Valor = c.Valor,
                        Tipo = c.Tipo
                    });
                }
                else
                {
                    for (int j = 0; j < jugadorServidor.CantidadCartas; j++)
                    {
                        jugadorLocal.Cartas.Add(new Carta
                        {
                            Color = "Negro",
                            Valor = "Reverso",
                            Tipo = "Especial"
                        });
                    }
                }

                jugadoresServidor.Add(jugadorLocal);
            }

            // Mantém exatamente a ordem enviada pelo servidor.
            juego.jugadores = jugadoresServidor;

            if (estado.CartaCentro != null)
            {
                juego.cartaActual = new Carta
                {
                    Id = estado.CartaCentro.Id,
                    Color = estado.CartaCentro.Color,
                    Valor = estado.CartaCentro.Valor,
                    Tipo = estado.CartaCentro.Tipo
                };
            }

            juego.direccion = estado.Sentido;

            int indiceActual = estado.Jugadores.FindIndex(
                j => j.EsTurnoActual
            );

            if (indiceActual >= 0)
                juego.jugadorActual = indiceActual;

            juego.partidaTerminada = estado.PartidaTerminada;
            juego.Ganador = null;

            if (estado.PartidaTerminada)
            {
                var ganador = estado.Jugadores.Find(
                    j => j.Nombre == estado.Ganador
                );

                if (ganador != null)
                {
                    juego.Ganador = juego.jugadores.Find(
                        j => j.ID == ganador.Id
                    );
                }
            }

            ActualizarInterfaz();
        }

        // Constructor que usa la pantalla de inicio (nombres e iconos de cada jugador)
        public UNO(ClienteSocket clienteSocket, string nombreJugador) : this()
        {
            this.clienteSocket = clienteSocket;
            this.nombreJugadorActual = string.IsNullOrEmpty(nombreJugador)
                ? "Jugador 1"
                : nombreJugador;

            this.WindowState = FormWindowState.Maximized;

            if (this.clienteSocket != null)
            {
                this.clienteSocket.OnMessageReceived += ProcesarEstadoServidor;
                this.clienteSocket.OnDisconnected += ClienteSocket_OnDisconnected;

                // Fuerza la creación del handle para poder recibir eventos del socket.
                IntPtr handleFormulario = this.Handle;
            }

            nombresJugadores[0] = this.nombreJugadorActual;

            if (juego != null && juego.jugadores.Count > 0)
            {
                juego.jugadores[0].Nombre = this.nombreJugadorActual;
            }
        }

        // Inicializa la ventana cuando se carga.
        private async void UNO_Load(object sender, EventArgs e)
        {
            AcomodarControles();
            AsignarIconos();

            if (clienteSocket != null)
            {
                // En una partida en red, el servidor es el único que reparte cartas,
                // decide turnos y aplica efectos. La ventana espera su primer estado.
                lblTurno.Text = "Conectado. Esperando a los otros jugadores...";
                lblMensaje.Text = "La partida comenzará cuando se conecten los 3 jugadores.";
                return;
            }

            // Modo local: se conserva el funcionamiento original.
            juego.iniciarPartida();
            juego.colocarCartaInicial();

            // Animación visual inicial
            await AnimacionInicio();

            AgregarHistorial("Conectando con la partida...");
            ActualizarInterfaz();
        }

        // Recibe respuestas y estados JSON del servidor.
        // El evento llega desde un hilo secundario.
        private void ClienteSocket_OnMessageReceived(string json)
        {
            if (IsDisposed || !IsHandleCreated) return;

            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(ClienteSocket_OnMessageReceived), json);
                return;
            }

            try
            {
                JObject mensaje = JObject.Parse(json);
                string tipo = (string)mensaje["tipo"] ?? "";
                string texto = (string)mensaje["mensaje"] ?? "";

                if (tipo == "Conectado")
                {
                    idJugadorServidor = (int?)mensaje["jugadorId"] ?? 0;

                    if (!string.IsNullOrWhiteSpace(texto))
                        AgregarHistorial(texto);

                    return;
                }

                if (tipo == "Error" ||
                    (mensaje["aceptado"] != null && !(bool)mensaje["aceptado"]))
                {
                    if (!string.IsNullOrWhiteSpace(texto))
                    {
                        MessageBox.Show(
                            this,
                            texto,
                            "UNO",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }

                    return;
                }

                if (tipo == "Respuesta")
                {
                    if (!string.IsNullOrWhiteSpace(texto))
                        AgregarHistorial(texto);

                    return;
                }

                if (tipo == "EsperandoJugadores" ||
                    tipo == "PartidaIniciada" ||
                    tipo == "ActualizacionPartida")
                {
                    JObject estado = mensaje["estadoPartida"] as JObject;

                    if (estado != null)
                        AplicarEstadoServidor(estado);

                    if (!string.IsNullOrWhiteSpace(texto))
                        AgregarHistorial(texto);
                }
            }
            catch (Exception ex)
            {
                AgregarHistorial("No se pudo leer la actualización del servidor: " + ex.Message);
            }
        }

        private void ClienteSocket_OnDisconnected(string motivo)
        {
            if (IsDisposed || !IsHandleCreated) return;

            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(ClienteSocket_OnDisconnected), motivo);
                return;
            }

            MessageBox.Show(
                this,
                motivo,
                "Conexión perdida",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        // Reconstruye la vista local a partir del estado oficial.
        // Las manos rivales se representan con cartas ocultas.
        private void AplicarEstadoServidor(JObject estado)
        {
            JArray listaJugadores = estado["jugadores"] as JArray;

            if (listaJugadores == null) return;

            idJugadorServidor = (int?)estado["miJugadorId"] ?? idJugadorServidor;

            List<Jugador> jugadoresNuevos = new List<Jugador>();
            int indiceTurno = 0;
            int indicePropio = -1;

            JArray misCartas = estado["misCartas"] as JArray ?? new JArray();

            // El servidor informa si cada jugador tiene una oportunidad UNO pendiente.
            jugadoresPendientesUNO = new bool[3];

            foreach (JObject dato in listaJugadores)
            {
                int id = (int?)dato["id"] ?? 0;
                string nombre = (string)dato["nombre"] ?? "Jugador";
                int cantidad = (int?)dato["cantidadCartas"] ?? 0;
                bool esTurno = (bool?)dato["esTurnoActual"] ?? false;
                bool debeDeclararUNO = (bool?)dato["debeDeclararUNO"] ?? false;

                int indiceJugador = jugadoresNuevos.Count;

                if (indiceJugador < jugadoresPendientesUNO.Length)
                    jugadoresPendientesUNO[indiceJugador] = debeDeclararUNO;

                Jugador jugador = new Jugador(id, nombre);

                if (id == idJugadorServidor)
                {
                    indicePropio = jugadoresNuevos.Count;

                    foreach (JObject cartaDto in misCartas)
                    {
                        Carta carta = new Carta();

                        carta.Id = (string)cartaDto["id"] ?? Guid.NewGuid().ToString("N");
                        carta.Color = (string)cartaDto["color"] ?? "";
                        carta.Valor = (string)cartaDto["valor"] ?? "";
                        carta.Numero = carta.Valor;
                        carta.Simbolo = carta.Valor;
                        carta.Tipo = (string)cartaDto["tipo"] ?? "Numero";

                        jugador.agregarCarta(carta);
                    }
                }
                else
                {
                    // Objetos ficticios solo para conservar el contador y dibujar reversos.
                    for (int i = 0; i < cantidad; i++)
                        jugador.agregarCarta(new Carta("", "", "Oculta"));
                }

                if (esTurno)
                    indiceTurno = jugadoresNuevos.Count;

                jugadoresNuevos.Add(jugador);
            }

            if (jugadoresNuevos.Count != 3) return;

            juego.jugadores = jugadoresNuevos;
            juego.jugadorActual = indiceTurno;
            juego.direccion = (int?)estado["sentido"] ?? 1;
            puedePasarTurnoServidor = (bool?)estado["puedePasarTurno"] ?? false;
            juego.partidaTerminada = (bool?)estado["partidaTerminada"] ?? false;
            juego.Ganador = null;

            string ganador = (string)estado["ganador"];

            if (!string.IsNullOrWhiteSpace(ganador))
            {
                foreach (Jugador jugador in juego.jugadores)
                {
                    if (jugador.Nombre == ganador)
                        juego.Ganador = jugador;
                }
            }

            JObject centro = estado["cartaCentro"] as JObject;

            if (centro != null)
            {
                Carta cartaCentroServidor = new Carta();

                cartaCentroServidor.Id = (string)centro["id"] ?? Guid.NewGuid().ToString("N");
                cartaCentroServidor.Color = (string)centro["color"] ?? "";
                cartaCentroServidor.Valor = (string)centro["valor"] ?? "";
                cartaCentroServidor.Numero = cartaCentroServidor.Valor;
                cartaCentroServidor.Simbolo = cartaCentroServidor.Valor;
                cartaCentroServidor.Tipo = (string)centro["tipo"] ?? "Numero";

                juego.cartaActual = cartaCentroServidor;
                juego.pilaDescarte.Clear();
                juego.pilaDescarte.Add(cartaCentroServidor);
            }

            for (int i = 0; i < 3; i++)
                nombresJugadores[i] = juego.jugadores[i].Nombre;

            if (indicePropio >= 0)
                AgregarHistorial("Tu jugador es " + nombresJugadores[indicePropio] + ".");

            partidaFinalizada = juego.partidaTerminada;

            ActualizarInterfaz();

            if (juego.partidaTerminada && juego.Ganador != null)
            {
                MessageBox.Show(
                    this,
                    "¡Ganó " + juego.Ganador.Nombre + "!",
                    "Partida terminada",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        // CONFIGURACIÓN, ESTILO Y DISTRIBUCIÓN DE LA INTERFAZ
        private void ConfigurarInterfaz()
        {
            this.DoubleBuffered = true;
            this.MinimumSize = new Size(900, 620);
            this.BackgroundImageLayout = ImageLayout.Stretch;

            // Las manos se dibujan en abanico: los paneles originales quedan ocultos.
            FlowLayoutPanel[] paneles =
            {
                panelMano,
                panelManoJugador2,
                panelManoJugador3
            };

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
            foreach (Label l in new Label[]
            {
                lblNombreJ1,
                lblNombreJ2,
                lblNombreJ3
            })
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

            foreach (Button b in new Button[] { RobarJ1, RobarJ2, RobarJ3 })
                EstilitarRobar(b);

            foreach (Button b in new Button[] { UnoJ1, UnoJ2, UnoJ3 })
                EstilizarUno(b);

            AplicarReversoMazo();

            // Se muestran cuando termina la animación de inicio.
            cartaCentro.Visible = false;

            foreach (Button b in new Button[]
            {
                RobarJ1, RobarJ2, RobarJ3,
                UnoJ1, UnoJ2, UnoJ3
            })
            {
                b.Visible = false;
            }
        }

        // Da estilo visual al botón de robar.
        private void EstilitarRobar(Button b)
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

        // Da estilo visual al botón de declarar UNO.
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

        // Coloca o actualiza la imagen del reverso del mazo.
        private void AplicarReversoMazo()
        {
            Image reverso = ImagenesUNO.Reverso();

            if (reverso == null) return;

            foreach (Button boton in new Button[]
            {
                RobarJ1,
                RobarJ2,
                RobarJ3
            })
            {
                boton.BackgroundImage = reverso;
                boton.BackgroundImageLayout = ImageLayout.Stretch;
            }
        }

        // asigna iconos visuales a los jugadores
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

            panelJugador3.SetBounds(
                xNombre3 + (anchoNombre - icono) / 2,
                yTop,
                icono,
                icono);

            Circular(panelJugador3);

            lblNombreJ3.SetBounds(
                xNombre3,
                yTop + icono + 4,
                anchoNombre,
                altoNombre);

            Redondear(lblNombreJ3, altoNombre / 2);

            abanicos[2].SetBounds(
                W - m - anchoLateral,
                yMano,
                anchoLateral,
                altoLateral);

            // Jugador 1 (abajo)
            panelJugador.SetBounds(
                m + (anchoNombre - icono) / 2,
                yIconoJ1,
                icono,
                icono);

            Circular(panelJugador);

            lblNombreJ1.SetBounds(m, yNombreJ1, anchoNombre, altoNombre);
            Redondear(lblNombreJ1, altoNombre / 2);

            int anchoBase = Limitar((int)(W * 0.5), 420, 1100);

            abanicos[0].SetBounds(
                (W - anchoBase) / 2,
                H - m - altoBase,
                anchoBase,
                altoBase);

            // Centro: mazo, carta actual y botón UNO
            int topMano1 = H - m - altoBase;
            int cx = W / 2;
            int cy = (yTop + topMano1 - 10) / 2;
            int altoCarta = Limitar((int)(H * 0.2), 110, 220);
            int anchoCarta = (int)(altoCarta * 0.68);

            cartaCentro.SetBounds(
                cx - anchoCarta / 2,
                cy - altoCarta / 2,
                anchoCarta,
                altoCarta);

            Redondear(cartaCentro, Math.Max(8, anchoCarta / 10));

            foreach (Button b in new Button[] { RobarJ1, RobarJ2, RobarJ3 })
            {
                b.SetBounds(
                    cartaCentro.Left - 28 - anchoCarta,
                    cartaCentro.Top,
                    anchoCarta,
                    altoCarta);

                Redondear(b, Math.Max(8, anchoCarta / 10));
            }

            int anchoUno = Math.Max(anchoCarta, 110);
            int altoUno = Limitar((int)(altoCarta * 0.38), 44, 64);

            foreach (Button b in new Button[] { UnoJ1, UnoJ2, UnoJ3 })
            {
                b.SetBounds(
                    cartaCentro.Right + 28,
                    cy - altoUno / 2,
                    anchoUno,
                    altoUno);

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

                bool esMiJugador = string.Equals(
                    jugador.Nombre,
                    nombreJugadorActual,
                    StringComparison.OrdinalIgnoreCase
                );

                MostrarCartas(abanicos[i], jugador.Cartas, i);

                nombres[i].Text = jugador.Nombre + "  ·  " + jugador.Cartas.Count;
                nombres[i].Font = esActual ? fuenteNombreActivo : fuenteNombre;
                nombres[i].BackColor = esActual ? Color.Gold : Color.FromArgb(200, 35, 10, 10);

                nombres[i].ForeColor = esActual ? Color.FromArgb(60, 20, 0) : Color.White;

                // Robar solo está disponible para el jugador cuyo turno corresponde.
                robar[i].Visible = esActual && !juego.partidaTerminada;

                if (clienteSocket != null)
                {
                    // En red, el servidor decide quién tiene una oportunidad UNO pendiente.
                    bool esJugadorPropio = jugador.ID == idJugadorServidor;
                    bool oportunidadPendiente = jugadoresPendientesUNO[i];

                    uno[i].Text = esJugadorPropio ? "¡UNO!" : "ACUSAR";
                    uno[i].Visible = !juego.partidaTerminada && oportunidadPendiente;
                    uno[i].Enabled = uno[i].Visible;
                }
                else
                {
                    // Comportamiento del modo local.
                    uno[i].Text = "UNO";
                    uno[i].Visible = esActual &&
                                     jugador.Cartas.Count == 1 &&
                                     !juego.partidaTerminada;
                    uno[i].Enabled = uno[i].Visible;
                }
            }

            MostrarCartaCentro();
            ActualizarTurno();
        }

        // Actualiza la indicación visual del jugador que tiene el turno.
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
                           (juego.direccion == 1
                               ? "sentido horario"
                               : "sentido antihorario");

            Carta superior = juego.cartaActual;

            if (superior != null &&
                (superior.Tipo == "Comodin" || superior.Tipo == "+4") &&
                superior.Color != "Negro")
            {
                texto += "  ·  Color: " + superior.Color;
            }

            lblTurno.Text = texto;
            lblTurno.BackColor = coloresJugador[juego.jugadorActual];
            lblTurno.ForeColor = Color.White;
        }

        // Muestra los últimos 3 movimientos en el historial.
        private void AgregarHistorial(string texto)
        {
            historial.Insert(0, texto);

            if (historial.Count > 3)
                historial.RemoveAt(historial.Count - 1);

            lblMensaje.Text = string.Join("\n", historial);
        }

        // MOSTRAR CARTAS DE UN JUGADOR
        private void MostrarCartas(
            AbanicoCartas abanico,
            List<Carta> cartas,
            int indiceJugador)
        {
            if (cartas == null)
            {
                abanico.PonerCartas(new List<Carta>(), false);
                return;
            }

            bool mostrarFrente;
            bool puedeInteractuar;

            if (clienteSocket == null)
            {
                // Modo local: el jugador 1 ve sus cartas de frente.
                mostrarFrente = indiceJugador == 0;
                puedeInteractuar = indiceJugador == 0 && !juego.partidaTerminada;
            }
            else
            {
                // Modo en red: cada jugador ve sus propias cartas.
                bool esMiJugador =
                    juego.jugadores[indiceJugador].ID == idJugadorServidor;

                mostrarFrente = esMiJugador;

                puedeInteractuar = esMiJugador &&
                                juego.jugadorActual == indiceJugador &&
                                !juego.partidaTerminada;
            }

            abanico.PonerCartas(
                cartas,
                mostrarFrente && puedeInteractuar
            );
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

        private async Task VolarCarta(
            Image img,
            Point desde,
            Size tamDesde,
            Point hasta,
            Size tamHasta,
            int ms)
        {
            Panel vuelo = NuevaCartaVuelo(img, tamDesde);

            vuelo.SetBounds(
                desde.X - tamDesde.Width / 2,
                desde.Y - tamDesde.Height / 2,
                tamDesde.Width,
                tamDesde.Height);

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

        private async Task VolarConEspera(
            Image img,
            Point desde,
            Size tamDesde,
            Point hasta,
            Size tamHasta,
            int ms,
            int espera)
        {
            if (espera > 0)
                await Task.Delay(espera);

            await VolarCarta(img, desde, tamDesde, hasta, tamHasta, ms);
        }

        // Cartas boca abajo que salen del mazo hacia la mano de un jugador.
        private async Task VolarReversos(int cantidad, int indiceDestino)
        {
            Image reverso = ImagenesUNO.Reverso();

            Point desde = Centro(RobarJ1);
            Point hasta = Centro(abanicos[indiceDestino]);

            Size tam = cartaCentro.Size;
            Size tamFinal = new Size(tam.Width * 2 / 3, tam.Height * 2 / 3);

            List<Task> vuelos = new List<Task>();

            for (int i = 0; i < cantidad; i++)
            {
                vuelos.Add(
                    VolarConEspera(
                        reverso,
                        desde,
                        tam,
                        hasta,
                        tamFinal,
                        360,
                        i * 140));
            }

            await Task.WhenAll(vuelos);
        }

        // La carta jugada viaja de la mano al centro.
        private async Task AnimarJugada(Carta carta, int indiceJugador)
        {
            AbanicoCartas mano = abanicos[indiceJugador];

            Point origen = EnFormulario(mano, mano.CentroCarta(carta));
            Size tamOrigen = mano.TamanoCarta();

            mano.CartaOculta = carta;
            mano.Invalidate();

            await VolarCarta(
                ImagenesUNO.ImagenCarta(carta),
                origen,
                tamOrigen,
                Centro(cartaCentro),
                cartaCentro.Size,
                320);
        }

        // Anima el reparto de una carta hacia el jugador indicado.
        private async Task RepartirCarta(
            Image img,
            Point desde,
            Size tam,
            Point hasta,
            int espera,
            int jugador,
            int[] recibidas)
        {
            await Task.Delay(espera);

            await VolarCarta(
                img,
                desde,
                tam,
                hasta,
                new Size(tam.Width * 2 / 3, tam.Height * 2 / 3),
                320);

            if (this.IsDisposed) return;

            recibidas[jugador]++;
            abanicos[jugador].MaxVisibles = recibidas[jugador];
        }

        // Barajeo + reparto al iniciar la partida.
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

                // Las manos existen pero aún no se ven.
                for (int i = 0; i < 3; i++)
                {
                    abanicos[i].PonerCartas(juego.jugadores[i].Cartas, false);
                    abanicos[i].MaxVisibles = 0;
                }

                // 1) BARAJAR
                for (int i = 0; i < 10; i++)
                {
                    Panel p = NuevaCartaVuelo(reverso, tam);
                    p.Location = new Point(
                        centro.X - tam.Width / 2,
                        centro.Y - tam.Height / 2 - i * 2);

                    pila.Add(p);
                }

                for (int ronda = 0; ronda < 3; ronda++)
                {
                    for (int i = 0; i < pila.Count; i += 2)
                        pila[i].BringToFront();

                    for (int i = 1; i < pila.Count; i += 2)
                        pila[i].BringToFront();

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

                            pila[i].Location = new Point(
                                centro.X - tam.Width / 2 + dx,
                                centro.Y - tam.Height / 2 - i * 2);
                        }

                        if (!intercalado && t >= 0.5)
                        {
                            intercalado = true;

                            for (int i = 0; i < pila.Count; i++)
                                pila[i].BringToFront();
                        }

                        await Task.Delay(15);
                    }
                }

                // 2) REPARTIR
                int[] recibidas = new int[3];
                int cartasPorJugador = juego.jugadores[0].Cartas.Count;
                List<Task> vuelos = new List<Task>();

                for (int ronda = 0; ronda < cartasPorJugador; ronda++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        int k = ronda * 3 + j;

                        vuelos.Add(
                            RepartirCarta(
                                reverso,
                                centro,
                                tam,
                                Centro(abanicos[j]),
                                k * 70,
                                j,
                                recibidas));
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

                for (int i = 0; i < 3; i++)
                    abanicos[i].MaxVisibles = -1;

                animando = false;
            }
        }

        // JUGAR CARTA
        private async Task JugarCarta(Carta cartaJugada, int indiceJugador)
        {
            if (animando || juego.partidaTerminada || indiceJugador != juego.jugadorActual)
                return;

            if (clienteSocket != null &&
                juego.jugadores[indiceJugador].ID != idJugadorServidor)
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

        // Envía la acción JUGAR_CARTA al servidor.
        private async Task JugarCartaInterno(Carta cartaJugada, int indiceJugador)
        {
            Jugador jugador = juego.jugadores[indiceJugador];
            int idJugador = idsJugadores[indiceJugador];

            // En red, el servidor procesa y valida la jugada.
            if (clienteSocket != null)
            {
                if (jugador.ID != idJugadorServidor)
                    return;

                string colorRed = "";

                if (cartaJugada.Tipo == "Comodin" || cartaJugada.Tipo == "+4")
                {
                    colorRed = SeleccionarColor();

                    if (string.IsNullOrWhiteSpace(colorRed))
                        return;
                }

                await clienteSocket.EnviarAccionAsync(
                    "jugarcarta",
                    cartaJugada.Id,
                    colorRed);

                return;
            }

            // Validaciones del modo local.
            if (yaRobo && cartaJugada != cartaRobadaEnTurno)
            {
                MessageBox.Show(
                    "Después de robar solo puedes jugar la carta que robaste, o pasar el turno haciendo clic en el mazo.",
                    "Carta no válida",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            if (!juego.puedeJugar(jugador, cartaJugada))
            {
                MessageBox.Show(
                    "No puedes jugar esa carta",
                    "Carta no válida",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            // Modo local: selección de color para comodines.
            string colorElegido = "";

            if (cartaJugada.Tipo == "Comodin" || cartaJugada.Tipo == "+4")
            {
                colorElegido = SeleccionarColor();

                if (colorElegido == "")
                    return;
            }

            // Animación visual de la carta jugada hacia el centro.
            await AnimarJugada(cartaJugada, indiceJugador);

            string colorCarta = cartaJugada.Color ?? "SinColor";
            string valorCarta = cartaJugada.Valor ?? cartaJugada.Tipo;

            string descripcionCartaBD =
                (colorCarta + " " + valorCarta).Trim();

            // Registro directo permitido únicamente en modo local.
            logJuegoDAO.RegistrarCartaJugada(
                idPartidaActual,
                idJugador,
                jugador.Nombre,
                descripcionCartaBD);

            string mensaje = jugador.Nombre + " jugó: " + valorCarta;

            // Penalización por no decir UNO.
            if (jugador.Cartas.Count == 1 && !juego.declaroUNO)
            {
                juego.agregaCartaRobada(jugador);
                juego.agregaCartaRobada(jugador);

                logJuegoDAO.RegistrarMensaje(
                    idPartidaActual,
                    idJugador,
                    jugador.Nombre + " no declaró UNO y robó 2 cartas de penalización");

                mensaje += ". No dijo UNO y roba 2 cartas";
                await VolarReversos(2, indiceJugador);
            }

            // Efecto de la carta.
            switch (cartaJugada.Tipo)
            {
                case "Comodin":
                    juego.aplicarComodin(colorElegido);
                    mensaje += " (color: " + colorElegido + ").";
                    juego.cambiarTurno();
                    break;

                case "+4":
                    juego.aplicarMasCuatro(colorElegido);

                    mensaje += " (color: " + colorElegido +
                            "). El siguiente jugador roba 4 cartas y pierde su turno.";

                    await VolarReversos(4, (indiceJugador + 1) % 3);
                    break;

                case "Reversa":
                    juego.aplicarReversa();
                    mensaje += ". Se invirtió el sentido del juego.";
                    juego.cambiarTurno();
                    break;

                case "Salta":
                    juego.aplicarSalta();
                    mensaje += ". Se saltó el turno del siguiente jugador.";
                    break;

                case "+2":
                    juego.aplicarMasDos();

                    mensaje += ". El siguiente jugador roba 2 cartas y pierde su turno.";

                    await VolarReversos(2, (indiceJugador + 1) % 3);
                    break;
            }

            AgregarHistorial(mensaje);

            // Comprobar ganador.
            if (juego.esGanador(jugador))
            {
                juego.comprobarGanador(jugador);

                logJuegoDAO.RegistrarMensaje(
                    idPartidaActual,
                    idJugador,
                    jugador.Nombre + " ganó la partida");

                ActualizarInterfaz();
                this.Close();
                return;
            }

            FinalizarTurno();
        }

        private void FinalizarTurno()
        {
            if (clienteSocket == null)
            {
                logJuegoDAO.RegistrarTurno(
                    idPartidaActual,
                    idsJugadores[juego.jugadorActual],
                    nombresJugadores[juego.jugadorActual]
                );
            }

            ActualizarInterfaz();
        }

        // ROBAR Y PASAR TURNO
        private async void BtnRobar_Click(object sender, EventArgs e)
        {
            if (animando || juego.partidaTerminada)
                return;

            // Partida online: el servidor controla el robo y la pasada de turno.
            if (clienteSocket != null)
            {
                if (yaRobo)
                    await clienteSocket.EnviarAccionAsync("PasarTurno");
                else
                    await clienteSocket.EnviarAccionAsync("RobarCarta");

                return;
            }

            // Partida local.
            Jugador jugador = juego.obtenerJugadorActual();

            if (jugador == null)
                return;

            animando = true;

            try
            {
                Carta cartaNueva = juego.robarDuranteTurno(jugador);

                if (cartaNueva == null)
                {
                    AgregarHistorial("No hay cartas disponibles para robar.");
                    return;
                }

                yaRobo = true;
                cartaRobadaEnTurno = cartaNueva;

                await VolarReversos(1, juego.jugadorActual);

                if (juego.puedeJugarCartaRobada(jugador, cartaNueva))
                {
                    AgregarHistorial(
                        jugador.Nombre +
                        " robó una carta que puede jugar. Juégala o pasa el turno."
                    );

                    ActualizarInterfaz();
                }
                else
                {
                    AgregarHistorial(
                        jugador.Nombre +
                        " robó una carta que no puede jugar y pasó su turno."
                    );

                    PasarTurno(jugador);
                }
            }
            finally
            {
                animando = false;
            }
        }

        // Manejadores de botones que delegan la acción al método común de robar
        private void BtnRobar_Click_1(object sender, EventArgs e) => BtnRobar_Click(sender, e);
        private void RobarJ2_Click(object sender, EventArgs e) => BtnRobar_Click(sender, e);
        private void RobarJ3_Click(object sender, EventArgs e) => BtnRobar_Click(sender, e);

        // Avanza el turno a partir del jugador indicado según la lógica de la partida
        private async void PasarTurno(Jugador jugador)
        {
            if (clienteSocket != null)
            {
                await clienteSocket.EnviarAccionAsync("PasarTurno");
                return;
            }

            logJuegoDAO.RegistrarMensaje(
                idPartidaActual,
                idsJugadores[juego.jugadorActual],
                jugador.Nombre + " pasó su turno"
            );

            juego.PasarTurno(jugador);

            yaRobo = false;
            cartaRobadaEnTurno = null;

            FinalizarTurno();
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
            rojo.Click += (s, e) =>
            {
                colorSeleccionado = "Rojo";
                ventanaColor.Close();
            };

            Button azul = CrearBotonColor("Azul", Color.Blue);
            azul.Location = new Point(115, 55);
            azul.Click += (s, e) =>
            {
                colorSeleccionado = "Azul";
                ventanaColor.Close();
            };

            Button verde = CrearBotonColor("Verde", Color.Green);
            verde.Location = new Point(210, 55);
            verde.Click += (s, e) =>
            {
                colorSeleccionado = "Verde";
                ventanaColor.Close();
            };

            Button amarillo = CrearBotonColor("Amarillo", Color.Gold);
            amarillo.Location = new Point(305, 55);
            amarillo.Click += (s, e) =>
            {
                colorSeleccionado = "Amarillo";
                ventanaColor.Close();
            };

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

        // BOTONES UNO Y ACUSACIÓN
        // En red, el servidor valida la declaración y aplica la penalización.
        private async Task ProcesarBotonUNOAsync(int indiceJugador)
        {
            if (indiceJugador < 0 || indiceJugador >= juego.jugadores.Count)
                return;

            Jugador jugadorObjetivo = juego.jugadores[indiceJugador];

            if (clienteSocket != null)
            {
                if (!jugadoresPendientesUNO[indiceJugador])
                    return;

                if (jugadorObjetivo.ID == idJugadorServidor)
                {
                    // El jugador que quedó con una carta declara UNO.
                    await clienteSocket.EnviarAccionAsync("declararuno");
                }
                else
                {
                    // El cliente acusa al rival que no declaró UNO.
                    await clienteSocket.EnviarAccionAsync(
                        "acusaruno",
                        null,
                        null,
                        jugadorObjetivo.ID);
                }

                return;
            }

            // En modo local se mantiene la declaración del jugador correspondiente.
            if (juego.jugadorActual == indiceJugador)
            {
                if (juego.declararUNO(jugadorObjetivo))
                {
                    MostrarAvisoUno(indiceJugador);
                }
            }
        }

        private void MostrarAvisoUno(int indiceJugador)
        {
            if (lblAvisoUno == null ||
                indiceJugador < 0 ||
                indiceJugador >= juego.jugadores.Count)
                return;

            Jugador jugador = juego.jugadores[indiceJugador];

            logJuegoDAO.RegistrarMensaje(
                idPartidaActual,
                idsJugadores[indiceJugador],
                jugador.Nombre + " declaró UNO");

            timerMensaje.Stop();

            lblAvisoUno.Text = "¡" + jugador.Nombre.ToUpper() + " DIJO UNO!";
            lblAvisoUno.ForeColor = Color.Gold;
            lblAvisoUno.BringToFront();
            lblAvisoUno.Visible = true;

            timerMensaje.Start();
        }

        // Cada botón representa al jugador situado en esa posición.
        private async void UnoJ1_Click(object sender, EventArgs e)
        {
            await ProcesarBotonUNOAsync(0);
        }

        private async void UnoJ2_Click(object sender, EventArgs e)
        {
            await ProcesarBotonUNOAsync(1);
        }

        private async void UnoJ3_Click(object sender, EventArgs e)
        {
            await ProcesarBotonUNOAsync(2);
        }

        // Atiende el evento del temporizador.
        private void timerMensaje_Tick(object sender, EventArgs e)
        {
            timerMensaje.Stop();

            if (lblAvisoUno != null)
            {
                lblAvisoUno.Text = "";
                lblAvisoUno.Visible = false;
            }
        }

        // Convierte el color de la carta en un color visual para la interfaz.
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