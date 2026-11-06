using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using proyectoUNO;
using System.Threading.Tasks;
using static WindowsFormsApp1.EstiloUI;
using System.Text;
using System.Text.Json;

namespace WindowsFormsApp1
{
    /* Formulario de inicio del juego UNO adaptado para multijugador cliente-servidor,
     encargado de recopilar el nombre del jugador, asignar un icono aleatorio, validar la IP
     y dar inicio inmediato a la partida principal al establecer la conexión TCP*/
    public partial class InicioForm : Form
    {
        // Control de texto donde el jugador ingresará su nombre.
        private readonly TextBox txtNombre = new TextBox();

        // Contenedor visual del icono asignado al jugador.
        private readonly PictureBox picIcono = new PictureBox();

        // Campo de texto para la IP del servidor.
        private readonly TextBox txtIpServidor = new TextBox();

        // Botón principal para iniciar la conexión con el servidor.
        private readonly Button btnJugar = new Button();

        // Botón para consultar el historial general de partidas.
        private readonly Button btnHistorial = new Button();

        // Control decorativo que muestra un abanico de cartas en la interfaz de bienvenida.
        private readonly AbanicoCartas decoracion = new AbanicoCartas();

        // Rectángulo que delimita la zona donde se ubica el panel del jugador.
        private Rectangle panelJugadores = Rectangle.Empty;

        // Instancia del cliente socket para la red TCP.
        private readonly ClienteSocket _clienteSocket = new ClienteSocket();

        // Constructor de la clase InicioForm. Inicializa las propiedades de la ventana,
        // configura los controles (IP, icono aleatorio, nombre, botón) y los elementos decorativos.
        public InicioForm()
        {
            this.Text = "UNO";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ClientSize = new Size(1000, 720);
            this.MinimumSize = new Size(780, 620);
            this.BackColor = Color.FromArgb(120, 0, 0);
            this.DoubleBuffered = true;
            this.ResizeRedraw = true;

            // Evento de red para avisar si el servidor se desconecta.
            _clienteSocket.OnDisconnected += ManejarDesconexionServidor;

            // Configuración del campo de texto para ingresar la IP del servidor
            txtIpServidor.Font = new Font("Segoe UI", 14f);
            txtIpServidor.Text = "127.0.0.1";
            txtIpServidor.BorderStyle = BorderStyle.FixedSingle;
            this.Controls.Add(txtIpServidor);

            // Configuración del PictureBox para el icono del jugador
            picIcono.SizeMode = PictureBoxSizeMode.Zoom;
            picIcono.BackColor = Color.White;
            this.Controls.Add(picIcono);

            // Configuración de la caja de texto para el nombre del jugador
            txtNombre.Font = new Font("Segoe UI", 15f);
            txtNombre.MaxLength = 18;
            txtNombre.Text = "Jugador 1";
            txtNombre.BorderStyle = BorderStyle.FixedSingle;
            this.Controls.Add(txtNombre);

            // El jugador recibe un icono aleatorio visible correctamente mediante los recursos gráficos.
            AsignarIcono();

            // Configuración visual y de eventos del botón para conectar y comenzar la partida.
            btnJugar.Text = "¡CONECTAR!";
            btnJugar.FlatStyle = FlatStyle.Flat;
            btnJugar.FlatAppearance.BorderSize = 0;
            btnJugar.FlatAppearance.MouseOverBackColor = Color.FromArgb(255, 235, 90);
            btnJugar.FlatAppearance.MouseDownBackColor = Color.FromArgb(230, 170, 0);
            btnJugar.BackColor = Color.FromArgb(255, 205, 0);
            btnJugar.ForeColor = Color.FromArgb(150, 10, 10);
            btnJugar.Font = new Font("Segoe UI Black", 17f, FontStyle.Bold);
            btnJugar.Cursor = Cursors.Hand;
            btnJugar.UseVisualStyleBackColor = false;
            btnJugar.Click += (s, e) => Jugar();
            this.Controls.Add(btnJugar);
            this.AcceptButton = btnJugar;

            // Configuración del botón para consultar el historial.
            btnHistorial.Text = "VER HISTORIAL";
            btnHistorial.FlatStyle = FlatStyle.Flat;
            btnHistorial.FlatAppearance.BorderSize = 0;
            btnHistorial.BackColor = Color.FromArgb(255, 221, 0);
            btnHistorial.ForeColor = Color.FromArgb(150, 10, 10);
            btnHistorial.Font = new Font("Segoe UI Black", 11f, FontStyle.Bold);
            btnHistorial.Cursor = Cursors.Hand;
            btnHistorial.Click += async (s, e) => await VerHistorialAsync();

            this.Controls.Add(btnHistorial);

            // Configuración del componente visual decorativo del abanico de cartas.
            decoracion.Orientacion = 0f;
            decoracion.Atenuar = false;
            decoracion.PonerCartas(MuestraDecorativa(), false);
            this.Controls.Add(decoracion);

            // Suscripción al evento de cambio de tamaño para reorganizar los elementos de la interfaz.
            this.Resize += (s, e) => Acomodar();
            this.FormClosing += (s, e) => _clienteSocket.Desconectar();

            // El foco inicial va al botón para que la IP no aparezca resaltada en azul al abrir.
            this.ActiveControl = btnJugar;
            Acomodar();
        }

        // Crea y retorna una lista predefinida de cartas utilizadas exclusivamente
        // para adornar estéticamente la pantalla de inicio mediante el componente de abanico.
        private List<Carta> MuestraDecorativa()
        {
            return new List<Carta>
            {
                new Carta("Rojo", "9", "Número"),
                new Carta("Azul", "+2", "+2"),
                new Carta("Verde", "Reversa", "Reversa"),
                new Carta("Negro", "+4", "+4"),
                new Carta("Amarillo", "Salta", "Salta"),
                new Carta("Azul", "5", "Número"),
                new Carta("Rojo", "Reversa", "Reversa")
            };
        }

        // Obtiene un icono aleatorio y lo asigna visualmente al PictureBox del jugador usando los recursos del sistema.
        private void AsignarIcono()
        {
            try
            {
                string[] iconos = ImagenesUNO.IconosAleatorios(1);
                if (iconos != null && iconos.Length > 0)
                {
                    picIcono.Image = ImagenesUNO.Cargar(iconos[0]);
                }
            }
            catch (Exception)
            {
                // Fallback visual en caso de excepción al cargar los recursos
            }
        }

        // Valida que los datos ingresados no estén vacíos, comprueba la conexión al servidor de forma asíncrona
        // y procede a abrir el formulario principal de la partida (`UNO.cs`) en cuanto se conecta con éxito.
        private async void Jugar()
        {
            // Forzar la actualización del foco para capturar el texto completo si quedó en el portapapeles o buffer
            txtNombre.Focus();
            string ipServidor = txtIpServidor.Text.Trim();
            string nombreJugador = txtNombre.Text.Trim();

            if (string.IsNullOrEmpty(ipServidor))
            {
                MessageBox.Show("Ingresa la IP del servidor.", "Falta IP", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtIpServidor.Focus();
                return;
            }

            if (string.IsNullOrEmpty(nombreJugador))
            {
                nombreJugador = "Jugador 1";
            }

            HabilitarControles(false);

            // Conectar pasando explícitamente el nombre limpio
            Task<bool> tareaConexion = _clienteSocket.ConectarAsync(ipServidor, nombreJugador);

            if (await Task.WhenAny(tareaConexion, Task.Delay(3000)) == tareaConexion)
            {
                bool conectado = await tareaConexion;

                if (conectado)
                {
                    _clienteSocket.OnDisconnected -= ManejarDesconexionServidor;

                    this.Hide();
                    UNO partidaForm = new UNO(_clienteSocket, nombreJugador);
                    partidaForm.FormClosed += (s, args) => this.Close();
                    partidaForm.Show();
                }
                else
                {
                    MessageBox.Show("No se pudo establecer conexión con el servidor.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    HabilitarControles(true);
                }
            }
            else
            {
                _clienteSocket.Desconectar();
                MessageBox.Show("Tiempo de espera agotado.", "Tiempo de espera", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                HabilitarControles(true);
            }
        }

        // Maneja el evento cuando ocurre una desconexión imprevista por parte del servidor.
        // Este evento llega desde otro hilo, por eso se usa Invoke.
        private void ManejarDesconexionServidor(string motivo)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<string>(ManejarDesconexionServidor), new object[] { motivo });
                return;
            }
            MessageBox.Show(motivo, "Desconectado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            HabilitarControles(true);
        }

        // Habilita o bloquea los controles de la interfaz. Con "false" se muestra el estado
        // "CONECTANDO..." y con "true" se restaura el estado original (fallo o desconexión).
        private void HabilitarControles(bool habilitado)
        {
            btnJugar.Enabled = habilitado;
            btnJugar.Text = habilitado ? "¡CONECTAR!" : "CONECTANDO...";
            txtIpServidor.Enabled = habilitado;
            txtNombre.Enabled = habilitado;
        }

        private async Task VerHistorialAsync()
        {
            try
            {
                btnHistorial.Enabled = false;
                btnHistorial.Text = "CARGANDO...";

                List<JsonElement> partidas =
                    await APICliente.ObtenerHistorialAsync();

                if (partidas == null || partidas.Count == 0)
                {
                    MessageBox.Show(
                        "No hay partidas registradas.",
                        "Historial",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );

                    return;
                }

                StringBuilder texto = new StringBuilder();

                foreach (JsonElement partida in partidas)
                {
                    string id = ObtenerValor(partida, "Partida");
                    string fecha = ObtenerValor(partida, "Fecha");
                    string ganador = ObtenerValor(partida, "Ganador");

                    texto.AppendLine("Partida: " + id);
                    texto.AppendLine("Fecha: " + fecha);
                    texto.AppendLine("Ganador: " + ganador);
                    texto.AppendLine("--------------------------------");
                }

                MessageBox.Show(
                    texto.ToString(),
                    "Historial de partidas",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "No se pudo consultar el historial: " + ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
            finally
            {
                btnHistorial.Enabled = true;
                btnHistorial.Text = "VER HISTORIAL";
            }
        }

        private string ObtenerValor(JsonElement elemento, string propiedad)
        {
            if (elemento.TryGetProperty(propiedad, out JsonElement valor))
                return valor.ToString();

            return "No disponible";
        }

        // Calcula y reubica dinámicamente las posiciones y tamaños de todos los controles visuales 
        // (caja de IP, icono, caja de texto del nombre, botón de jugar y abanico) en función de las dimensiones actuales de la ventana.
        private void Acomodar()
        {
            int W = ClientSize.Width, H = ClientSize.Height;
            if (W < 100 || H < 100) return;

            int ancho = Limitar((int)(W * 0.5), 420, 640);
            int x = (W - ancho) / 2;

            // Altura de cada fila dentro del panel (son dos: IP y jugador).
            int filaAlto = Limitar((int)(H * 0.095), 56, 78);
            int panelAlto = filaAlto * 2 + 28;
            int panelY = (int)(H * 0.37);   // más abajo, para dejar espacio al subtítulo
            panelJugadores = new Rectangle(x, panelY, ancho, panelAlto);

            // Fila 1: IP del servidor, dentro del panel.
            int y = panelY + 14;
            txtIpServidor.SetBounds(x + 22, y + (filaAlto - txtIpServidor.Height) / 2,
                ancho - 44, txtIpServidor.Height);

            // Fila 2: icono circular y nombre.
            y += filaAlto;
            int icono = filaAlto - 12;
            picIcono.SetBounds(x + 22, y + 6, icono, icono);
            Circular(picIcono);

            int xTxt = x + 22 + icono + 16;
            txtNombre.SetBounds(xTxt, y + (filaAlto - txtNombre.Height) / 2,
                x + ancho - 22 - xTxt, txtNombre.Height);

            // Botón de conectar.
            int btnAncho = Limitar((int)(W * 0.3), 240, 380);
            btnJugar.SetBounds((W - btnAncho) / 2, panelY + panelAlto + 18, btnAncho, 58);
            Redondear(btnJugar, 29);

            // Botón para consultar el historial.
            btnHistorial.SetBounds(
                (W - 220) / 2,
                btnJugar.Bottom + 10,
                220,
                42
            );

            Redondear(btnHistorial, 20);

            // Abanico de cartas decorativo en la parte inferior.
            int abY = btnHistorial.Bottom + 8;
            int abAncho = Limitar((int)(W * 0.7), 520, 1000);

            decoracion.SetBounds(
                (W - abAncho) / 2,
                abY,
                abAncho,
                Math.Max(110, H - abY)
            );

            Invalidate();
        }

        // Sobrescribe el evento de renderizado (OnPaint) para dibujar manualmente el fondo con degradado,
        // círculos estéticos decorativos, el logotipo inclinado del juego "UNO", el subtítulo y el contenedor semitransparente.
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            int W = ClientSize.Width, H = ClientSize.Height;
            if (W < 50 || H < 50) return;

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAlias;

            // Dibujo del fondo con degradado lineal.
            using (LinearGradientBrush fondo = new LinearGradientBrush(
                new Rectangle(0, 0, W, H), Color.FromArgb(170, 15, 15), Color.FromArgb(30, 0, 5), 65f))
            {
                g.FillRectangle(fondo, 0, 0, W, H);
            }

            // Dibujo de círculos decorativos de fondo.
            using (SolidBrush b1 = new SolidBrush(Color.FromArgb(28, 255, 200, 0)))
                g.FillEllipse(b1, -W * 0.15f, -H * 0.25f, W * 0.55f, W * 0.55f);
            using (SolidBrush b2 = new SolidBrush(Color.FromArgb(24, 30, 120, 255)))
                g.FillEllipse(b2, W * 0.65f, H * 0.35f, W * 0.6f, W * 0.6f);

            // Dibujo del logotipo principal "UNO" (óvalo rojo inclinado con borde blanco y texto).
            float lw = Math.Min(W * 0.46f, H * 0.46f);
            float lh = lw * 0.5f;
            GraphicsState st = g.Save();
            g.TranslateTransform(W / 2f, H * 0.15f);
            g.RotateTransform(-8f);

            RectangleF rect = new RectangleF(-lw / 2f, -lh / 2f, lw, lh);
            using (SolidBrush rojo = new SolidBrush(Color.FromArgb(225, 20, 20)))
            using (Pen borde = new Pen(Color.White, Math.Max(4f, lh * 0.06f)))
            {
                g.FillEllipse(rojo, rect);
                g.DrawEllipse(borde, rect);
            }

            using (Font f = new Font("Segoe UI Black", lh * 0.55f, FontStyle.Bold, GraphicsUnit.Pixel))
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;

                RectangleF sombra = new RectangleF(rect.X + lh * 0.04f, rect.Y + lh * 0.04f, rect.Width, rect.Height);
                g.DrawString("UNO", f, Brushes.Black, sombra, sf);
                using (SolidBrush amarillo = new SolidBrush(Color.FromArgb(255, 221, 0)))
                    g.DrawString("UNO", f, amarillo, rect, sf);
            }
            g.Restore(st);

            // Dibujo del subtítulo informativo, justo encima del panel (amarillo con sombra, como el logo).
            using (Font f = new Font("Segoe UI Semibold", 15f))
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;

                string texto = "Ingresa la IP del servidor y tu nombre para jugar";
                float yTexto = panelJugadores.Y - 48;

                RectangleF zona = new RectangleF(0, yTexto, W, 30);
                RectangleF sombra = new RectangleF(1.5f, yTexto + 1.5f, W, 30);

                g.DrawString(texto, f, Brushes.Black, sombra, sf);
                using (SolidBrush amarillo = new SolidBrush(Color.FromArgb(255, 221, 0)))
                    g.DrawString(texto, f, amarillo, zona, sf);
            }

            // Dibujo del panel semitransparente que contiene los campos de IP y jugador.
            if (panelJugadores.Width > 0)
            {
                using (GraphicsPath p = RectRedondeado(panelJugadores, 22))
                using (SolidBrush fondoPanel = new SolidBrush(Color.FromArgb(150, 0, 0, 0)))
                using (Pen borde = new Pen(Color.FromArgb(120, 255, 255, 255), 2f))
                {
                    g.FillPath(fondoPanel, p);
                    g.DrawPath(borde, p);
                }
            }
        }
    }
}