using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using proyectoUNO;

namespace WindowsFormsApp1
{
    public class AbanicoCartas : Control
    {
        private const float Proporcion = 0.68f;   // ancho / alto de una carta

        private List<Carta> cartas = new List<Carta>();
        private int indiceHover = -1;
        private int maxVisibles = -1;

        public float Orientacion { get; set; }    // 0 = abajo, 90 = izquierda, 270 = derecha
        public bool ManoActiva { get; set; }      // solo la mano con turno responde al mouse
        public bool Atenuar { get; set; }         // oscurece la mano si no es su turno
        public Carta CartaOculta { get; set; }    // carta que está "volando" (no se dibuja)
        public event Action<Carta> CartaClick;

        public int MaxVisibles
        {
            get { return maxVisibles; }
            set { maxVisibles = value; Invalidate(); }
        }

        public AbanicoCartas()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingFlags |
                     ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            SetStyle(ControlStyles.Selectable, false);
            BackColor = Color.Transparent;
            TabStop = false;
        }

        public void PonerCartas(List<Carta> lista, bool activa)
        {
            cartas = lista == null ? new List<Carta>() : new List<Carta>(lista);
            ManoActiva = activa;
            CartaOculta = null;
            indiceHover = -1;
            Cursor = Cursors.Default;
            Invalidate();
        }

        // ---------- geometría ----------
        private float VW { get { return (Orientacion == 90f || Orientacion == 270f) ? Height : Width; } }
        private float VH { get { return (Orientacion == 90f || Orientacion == 270f) ? Width : Height; } }

        private int CantidadVisible()
        {
            return maxVisibles < 0 ? cartas.Count : Math.Min(maxVisibles, cartas.Count);
        }

        private void Medidas(out float cw, out float ch)
        {
            float vw = VW, vh = VH;
            ch = vh * 0.85f;
            cw = ch * Proporcion;
            if (cw > vw * 0.6f)
            {
                cw = vw * 0.6f;
                ch = cw / Proporcion;
            }
        }

        // Centro y ángulo de la carta i (de n) en el espacio "virtual" (mano abajo)
        private void Calcular(int n, int i, out float cx, out float cy, out float grados)
        {
            float cw, ch;
            Medidas(out cw, out ch);

            float vw = VW, vh = VH;
            float radio = ch * 2.2f;
            float t = i - (n - 1) / 2f;

            float separacion = 0f;
            if (n > 1)
            {
                float maxSep = cw * 0.55f;
                float disponible = (vw - cw * 1.35f) / (n - 1);
                separacion = Math.Min(maxSep, Math.Max(disponible, 6f));
            }

            float ang = t * (separacion / radio);
            cx = vw / 2f + radio * (float)Math.Sin(ang);
            cy = vh - ch / 2f + ch * 0.08f + radio * (1f - (float)Math.Cos(ang));
            grados = ang * 180f / (float)Math.PI;
        }

        public Size TamanoCarta()
        {
            float cw, ch;
            Medidas(out cw, out ch);
            return new Size((int)cw, (int)ch);
        }

        // Centro de una carta (o de la mano si no está) en coordenadas del control
        public Point CentroCarta(Carta carta)
        {
            int n = CantidadVisible();
            int idx = carta == null ? -1 : cartas.IndexOf(carta);
            if (idx < 0 || idx >= n)
                return new Point(Width / 2, Height / 2);

            float cx, cy, g;
            Calcular(n, idx, out cx, out cy, out g);

            double rad = Orientacion * Math.PI / 180.0;
            double dx = cx - VW / 2.0, dy = cy - VH / 2.0;
            double x = dx * Math.Cos(rad) - dy * Math.Sin(rad) + Width / 2.0;
            double y = dx * Math.Sin(rad) + dy * Math.Cos(rad) + Height / 2.0;
            return new Point((int)x, (int)y);
        }

        // ---------- dibujo ----------
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            int n = CantidadVisible();
            if (n == 0 || Width < 10 || Height < 10) return;

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            float cw, ch;
            Medidas(out cw, out ch);

            GraphicsState estado = g.Save();
            g.TranslateTransform(Width / 2f, Height / 2f);
            g.RotateTransform(Orientacion);
            g.TranslateTransform(-VW / 2f, -VH / 2f);

            for (int i = 0; i < n; i++)
            {
                Carta carta = cartas[i];
                if (carta == CartaOculta) continue;

                float cx, cy, grados;
                Calcular(n, i, out cx, out cy, out grados);
                float levantar = (i == indiceHover && ManoActiva) ? ch * 0.18f : 0f;

                GraphicsState s2 = g.Save();
                g.TranslateTransform(cx, cy);
                g.RotateTransform(grados);
                g.TranslateTransform(0f, -levantar);
                DibujarCarta(g, carta, new RectangleF(-cw / 2f, -ch / 2f, cw, ch));
                g.Restore(s2);
            }

            g.Restore(estado);
        }

        private void DibujarCarta(Graphics g, Carta carta, RectangleF r)
        {
            float radio = r.Width * 0.08f;

            // sombra suave para separar las cartas que se solapan
            using (GraphicsPath sombra = EstiloUI.RectRedondeado(new RectangleF(r.X + 2, r.Y + 2, r.Width, r.Height), radio))
            using (SolidBrush brochaSombra = new SolidBrush(Color.FromArgb(70, 0, 0, 0)))
            {
                g.FillPath(brochaSombra, sombra);
            }

            Image img = ImagenesUNO.ImagenCarta(carta);
            if (img != null)
            {
                g.DrawImage(img, r);
            }
            else
            {
                using (GraphicsPath p = EstiloUI.RectRedondeado(r, radio))
                using (SolidBrush fondo = new SolidBrush(ColorCarta(carta)))
                using (Pen borde = new Pen(Color.White, 3f))
                {
                    g.FillPath(fondo, p);
                    g.DrawPath(borde, p);
                }
                using (Font f = new Font("Segoe UI", Math.Max(8f, r.Width * 0.18f), FontStyle.Bold))
                using (StringFormat sf = new StringFormat())
                {
                    sf.Alignment = StringAlignment.Center;
                    sf.LineAlignment = StringAlignment.Center;
                    g.DrawString(carta.Valor, f, Brushes.Black, r, sf);
                }
            }

            if (Atenuar && !ManoActiva)
            {
                using (GraphicsPath p = EstiloUI.RectRedondeado(r, radio))
                using (SolidBrush velo = new SolidBrush(Color.FromArgb(90, 0, 0, 0)))
                {
                    g.FillPath(velo, p);
                }
            }
        }

        private static Color ColorCarta(Carta c)
        {
            switch (c.Color)
            {
                case "Rojo": return Color.FromArgb(225, 70, 70);
                case "Azul": return Color.FromArgb(70, 120, 225);
                case "Verde": return Color.FromArgb(70, 190, 100);
                case "Amarillo": return Color.FromArgb(245, 210, 60);
                default: return Color.FromArgb(70, 70, 70);
            }
        }

        // ---------- mouse ----------
        private int CartaEn(Point p)
        {
            int n = CantidadVisible();
            if (n == 0) return -1;

            float cw, ch;
            Medidas(out cw, out ch);

            // pasa el punto al espacio virtual (inverso de lo que hace OnPaint)
            double rad = -Orientacion * Math.PI / 180.0;
            double dx = p.X - Width / 2.0, dy = p.Y - Height / 2.0;
            double vx = dx * Math.Cos(rad) - dy * Math.Sin(rad) + VW / 2.0;
            double vy = dx * Math.Sin(rad) + dy * Math.Cos(rad) + VH / 2.0;

            for (int i = n - 1; i >= 0; i--)
            {
                if (cartas[i] == CartaOculta) continue;

                float cx, cy, grados;
                Calcular(n, i, out cx, out cy, out grados);

                double r2 = -grados * Math.PI / 180.0;
                double ex = vx - cx, ey = vy - cy;
                double lx = ex * Math.Cos(r2) - ey * Math.Sin(r2);
                double ly = ex * Math.Sin(r2) + ey * Math.Cos(r2);

                if (Math.Abs(lx) <= cw / 2.0 && Math.Abs(ly) <= ch / 2.0)
                    return i;
            }
            return -1;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int idx = ManoActiva ? CartaEn(e.Location) : -1;
            if (idx != indiceHover)
            {
                indiceHover = idx;
                Cursor = idx >= 0 ? Cursors.Hand : Cursors.Default;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (indiceHover != -1)
            {
                indiceHover = -1;
                Cursor = Cursors.Default;
                Invalidate();
            }
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (!ManoActiva || e.Button != MouseButtons.Left) return;

            int idx = CartaEn(e.Location);
            if (idx >= 0 && CartaClick != null)
                CartaClick(cartas[idx]);
        }
    }
}