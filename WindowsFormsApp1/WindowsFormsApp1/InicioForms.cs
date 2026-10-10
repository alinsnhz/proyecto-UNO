using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using proyectoUNO;
using static WindowsFormsApp1.EstiloUI;

namespace WindowsFormsApp1
{
    // Formulario de inicio del juego UNO, encargado de recopilar los nombres de los 3 jugadores,
    // asignarles iconos aleatorios, validar los datos y dar inicio a la partida principal.
    public partial class InicioForm : Form
    {
        // Arreglo para almacenar los controles de texto donde cada jugador ingresará su nombre.
        private readonly TextBox[] txtNombres = new TextBox[3];

        // Arreglo para almacenar los contenedores visuales de los iconos asignados a cada jugador.
        private readonly PictureBox[] picIconos = new PictureBox[3];

        // Botón principal para iniciar el juego.
        private readonly Button btnJugar = new Button();

        // Control decorativo que muestra un abanico de cartas en la interfaz de bienvenida.
        private readonly AbanicoCartas decoracion = new AbanicoCartas();

        // Arreglo para almacenar los identificadores de los iconos seleccionados.
        private string[] iconos;

        // Rectángulo que delimita la zona donde se ubica el panel de jugadores.
        private Rectangle panelJugadores = Rectangle.Empty;

        // Constructor de la clase InicioForm. Inicializa las propiedades de la ventana,
        // crea y configura dinámicamente los campos de texto, iconos, botón de juego y elementos decorativos.
        public InicioForm()
        {
            this.Text = "UNO";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ClientSize = new Size(1000, 720);
            this.MinimumSize = new Size(780, 620);
            this.BackColor = Color.FromArgb(120, 0, 0);
            this.DoubleBuffered = true;
            this.ResizeRedraw = true;

            // Bucle para instanciar, configurar y agregar los PictureBox (iconos) y TextBox (nombres) de los 3 jugadores.
            for (int i = 0; i < 3; i++)
            {
                PictureBox pic = new PictureBox();
                pic.SizeMode = PictureBoxSizeMode.Zoom;
                pic.BackColor = Color.White;
                picIconos[i] = pic;
                this.Controls.Add(pic);

                TextBox txt = new TextBox();
                txt.Font = new Font("Segoe UI", 15f);
                txt.MaxLength = 18;
                txt.Text = "Jugador " + (i + 1);
                txt.BorderStyle = BorderStyle.FixedSingle;
                txtNombres[i] = txt;
                this.Controls.Add(txt);
            }

            // Cada jugador recibe un icono aleatorio para su identificación visual.
            RepartirIconos();

            // Configuración visual y de eventos del botón para comenzar la partida.
            btnJugar.Text = "¡JUGAR!";
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

            // Configuración del componente visual decorativo del abanico de cartas.
            decoracion.Orientacion = 0f;
            decoracion.Atenuar = false;
            decoracion.PonerCartas(MuestraDecorativa(), false);
            this.Controls.Add(decoracion);

            // Suscripción al evento de cambio de tamaño para reorganizar los elementos de la interfaz.
            this.Resize += (s, e) => Acomodar();
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

        // Obtiene un conjunto de iconos aleatorios y los asigna visualmente a los PictureBox de cada jugador.
        private void RepartirIconos()
        {
            iconos = ImagenesUNO.IconosAleatorios(3);
            for (int i = 0; i < 3; i++)
                picIconos[i].Image = ImagenesUNO.Cargar(iconos[i]);
        }

        // Valida que los nombres ingresados no estén vacíos ni repetidos, comprueba la conexión a la base de datos
        // y procede a inicializar y abrir el formulario principal de la partida de UNO.
        private void Jugar()
        {
            string[] nombres = new string[3];
            for (int i = 0; i < 3; i++)
            {
                nombres[i] = txtNombres[i].Text.Trim();

                // Validación de campos vacíos.
                if (nombres[i].Length == 0)
                {
                    MessageBox.Show("Escribe el nombre de los 3 jugadores.", "Faltan nombres",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    txtNombres[i].Focus();
                    return;
                }

                // Validación de nombres duplicados entre los jugadores.
                for (int j = 0; j < i; j++)
                {
                    if (string.Equals(nombres[i], nombres[j], StringComparison.OrdinalIgnoreCase))
                    {
                        MessageBox.Show("Los nombres no se pueden repetir.", "Nombres repetidos",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        txtNombres[i].Focus();
                        return;
                    }
                }
            }

            // Verificación del estado de la conexión a la base de datos antes de arrancar.
            string error;
            if (!conexionBD.ProbarConexion(out error))
            {
                MessageBox.Show(error, "No se pudo conectar a la base de datos",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Instanciación del formulario de juego, pasando los nombres y los iconos elegidos.
            UNO partida = new UNO(nombres, (string[])iconos.Clone());
            partida.FormClosed += (s, e) =>
            {
                RepartirIconos();     // Genera nuevos iconos para una futura partida.
                this.Show();          // Vuelve a mostrar la pantalla de inicio al cerrar el juego.
            };
            this.Hide();
            partida.Show();
        }

        // Calcula y reubica dinámicamente las posiciones y tamaños de todos los controles visuales 
        // (cajas de texto, iconos, botón de jugar y abanico) en función de las dimensiones actuales de la ventana.
        private void Acomodar()
        {
            int W = ClientSize.Width, H = ClientSize.Height;
            if (W < 100 || H < 100) return;

            int filaAlto = Limitar((int)(H * 0.095), 56, 78);
            int panelAncho = Limitar((int)(W * 0.5), 420, 640);
            int panelAlto = filaAlto * 3 + 28;
            int panelX = (W - panelAncho) / 2;
            int panelY = (int)(H * 0.31);
            panelJugadores = new Rectangle(panelX, panelY, panelAncho, panelAlto);

            int icono = filaAlto - 12;
            for (int i = 0; i < 3; i++)
            {
                int y = panelY + 14 + i * filaAlto;
                picIconos[i].SetBounds(panelX + 22, y + 6, icono, icono);
                Circular(picIconos[i]);

                int xTxt = panelX + 22 + icono + 16;
                txtNombres[i].SetBounds(xTxt, y + (filaAlto - txtNombres[i].Height) / 2,
                    panelX + panelAncho - 22 - xTxt, txtNombres[i].Height);
            }

            int btnAncho = Limitar((int)(W * 0.3), 240, 380);
            btnJugar.SetBounds((W - btnAncho) / 2, panelY + panelAlto + 18, btnAncho, 58);
            Redondear(btnJugar, 29);

            int abY = btnJugar.Bottom + 8;
            int abAncho = Limitar((int)(W * 0.7), 520, 1000);
            decoracion.SetBounds((W - abAncho) / 2, abY, abAncho, Math.Max(110, H - abY));

            Invalidate();
        }

        // Sobrescribe el evento de renderizado (OnPaint) para dibujar manualmente el fondo con degradado,
        // círculos estéticos decorativos, el logotipo inclinado del juego "UNO", el subtítulo y el contenedor semitransparente de los jugadores.
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

            // Dibujo del subtítulo informativo en la interfaz.
            using (Font f = new Font("Segoe UI", 12f))
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Center;
                g.DrawString("Escribe los nombres de los jugadores y a barajar",
                    f, Brushes.WhiteSmoke, new RectangleF(0, H * 0.265f, W, 26), sf);
            }

            // Dibujo del panel semitransparente que contiene los campos de los jugadores.
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

        // Método requerido por Windows Forms para inicializar los componentes visuales
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(InicioForm));
            this.SuspendLayout();
      
            // InicioForm
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "InicioForm";
            this.ResumeLayout(false);

        }
    }
}