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
    public class InicioForm : Form
    {
        private readonly TextBox[] txtNombres = new TextBox[3];
        private readonly PictureBox[] picIconos = new PictureBox[3];
        private readonly Button btnJugar = new Button();
        private readonly AbanicoCartas decoracion = new AbanicoCartas();
        private string[] iconos;
        private Rectangle panelJugadores = Rectangle.Empty;

        public InicioForm()
        {
            this.Text = "UNO";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ClientSize = new Size(1000, 720);
            this.MinimumSize = new Size(780, 620);
            this.BackColor = Color.FromArgb(120, 0, 0);
            this.DoubleBuffered = true;
            this.ResizeRedraw = true;

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

            // Cada jugador recibe un icono aleatorio
            RepartirIconos();

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

            decoracion.Orientacion = 0f;
            decoracion.Atenuar = false;
            decoracion.PonerCartas(MuestraDecorativa(), false);
            this.Controls.Add(decoracion);

            this.Resize += (s, e) => Acomodar();
            Acomodar();
        }

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

        private void RepartirIconos()
        {
            iconos = ImagenesUNO.IconosAleatorios(3);
            for (int i = 0; i < 3; i++)
                picIconos[i].Image = ImagenesUNO.Cargar(iconos[i]);
        }

        private void Jugar()
        {
            string[] nombres = new string[3];
            for (int i = 0; i < 3; i++)
            {
                nombres[i] = txtNombres[i].Text.Trim();

                if (nombres[i].Length == 0)
                {
                    MessageBox.Show("Escribe el nombre de los 3 jugadores.", "Faltan nombres",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    txtNombres[i].Focus();
                    return;
                }

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

            string error;
            if (!conexionBD.ProbarConexion(out error))
            {
                MessageBox.Show(error, "No se pudo conectar a la base de datos",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            UNO partida = new UNO(nombres, (string[])iconos.Clone());
            partida.FormClosed += (s, e) =>
            {
                RepartirIconos();     // iconos nuevos para la siguiente partida
                this.Show();
            };
            this.Hide();
            partida.Show();
        }

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

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            int W = ClientSize.Width, H = ClientSize.Height;
            if (W < 50 || H < 50) return;

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAlias;

            using (LinearGradientBrush fondo = new LinearGradientBrush(
                new Rectangle(0, 0, W, H), Color.FromArgb(170, 15, 15), Color.FromArgb(30, 0, 5), 65f))
            {
                g.FillRectangle(fondo, 0, 0, W, H);
            }

            // círculos decorativos
            using (SolidBrush b1 = new SolidBrush(Color.FromArgb(28, 255, 200, 0)))
                g.FillEllipse(b1, -W * 0.15f, -H * 0.25f, W * 0.55f, W * 0.55f);
            using (SolidBrush b2 = new SolidBrush(Color.FromArgb(24, 30, 120, 255)))
                g.FillEllipse(b2, W * 0.65f, H * 0.35f, W * 0.6f, W * 0.6f);

            // logo UNO (óvalo rojo inclinado)
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

            // subtítulo
            using (Font f = new Font("Segoe UI", 12f))
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Center;
                g.DrawString("Escribe los nombres de los jugadores y a barajar",
                    f, Brushes.WhiteSmoke, new RectangleF(0, H * 0.265f, W, 26), sf);
            }

            // panel de jugadores
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