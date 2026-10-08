using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using proyectoUNO;
using System.Drawing.Imaging;

namespace WindowsFormsApp1
{
    public static class ImagenesUNO
    {
        private static string carpeta = null;
        private static bool avisado = false;
        private static readonly Dictionary<string, Image> cache = new Dictionary<string, Image>();
        public static readonly Random Azar = new Random();

        public static string CarpetaImagenes()
        {
            if (carpeta != null) return carpeta;

            string[] bases = { Application.StartupPath, AppDomain.CurrentDomain.BaseDirectory };
            foreach (string inicio in bases)
            {
                DirectoryInfo dir = new DirectoryInfo(inicio);
                for (int i = 0; i < 6 && dir != null; i++)
                {
                    string candidata = Path.Combine(dir.FullName, "Imagenes");
                    if (Directory.Exists(candidata))
                    {
                        carpeta = candidata;
                        return carpeta;
                    }
                    dir = dir.Parent;
                }
            }

            if (!avisado)
            {
                avisado = true;
                MessageBox.Show("No se encontró la carpeta 'Imagenes'. Se buscó desde:\n" + Application.StartupPath,
                    "Imágenes no encontradas", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            return null;
        }

        public static Image Cargar(string ruta)
        {
            if (string.IsNullOrEmpty(ruta) || !File.Exists(ruta)) return null;

            Image img;
            if (!cache.TryGetValue(ruta, out img))
            {
                using (Image temporal = Image.FromFile(ruta))
                {
                    img = new Bitmap(temporal);
                }
                cache[ruta] = img;
            }
            return img;
        }

        public static string RutaCarta(Carta carta)
        {
            if (carta == null) 
                return "";

            string baseDir = CarpetaImagenes();
            if (baseDir == null) 
                return "";

            string nombreColor = carta.Color != null ? carta.Color.ToLower() : "";
            string subcarpeta;
            string archivo;

            if (carta.Tipo == "Comodin")
            {
                subcarpeta = "Comodines";
                archivo = "comodin_cambioColor.png";
            }
            else if (carta.Tipo == "+4")
            {
                subcarpeta = "Comodines";
                archivo = "comodin_+4.png";
            }
            else
            {
                subcarpeta = carta.Color;
                if (carta.Tipo == "Número") 
                    archivo = nombreColor + "_" + carta.Valor + ".png";
                else if (carta.Tipo == "+2") 
                    archivo = nombreColor + "_+2.png";
                else if (carta.Tipo == "Reversa") 
                    archivo = nombreColor + "_reversa.png";
                else if (carta.Tipo == "Salta") 
                    archivo = nombreColor + "_cancelar.png";
                else 
                    return "";
            }

            return Path.Combine(baseDir, subcarpeta, archivo);
        }

        public static Image ImagenCarta(Carta carta)
        {
            return Cargar(RutaCarta(carta));
        }

        private static Image reversoRecortado = null;

        public static Image Reverso()
        {
            if (reversoRecortado != null) 
                return reversoRecortado;

            string baseDir = CarpetaImagenes();
            if (baseDir == null) 
                return null;

            Image original = Cargar(Path.Combine(baseDir, "card_reverse.png"));
            if (original == null) 
                original = Cargar(Path.Combine(baseDir, "reverso.png"));
            if (original == null) 
                return null;

            // La imagen trae un margen blanco: se recorta y se redondean las esquinas
            int margenX = (int)(original.Width * 0.04);
            int margenY = (int)(original.Height * 0.021);
            int w = original.Width - margenX * 2;
            int h = original.Height - margenY * 2;

            Bitmap resultado = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(resultado))
            using (GraphicsPath clip = EstiloUI.RectRedondeado(new RectangleF(0, 0, w, h), w * 0.05f))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SetClip(clip);
                g.DrawImage(original, new Rectangle(0, 0, w, h),
                    new Rectangle(margenX, margenY, w, h), GraphicsUnit.Pixel);
            }

            reversoRecortado = resultado;
            return reversoRecortado;
        }

        // Busca Imagenes\Iconos junto al .exe y, si no está, sube por las carpetas padre
        private static string BuscarCarpetaIconos()
        {
            string[] bases = { Application.StartupPath, AppDomain.CurrentDomain.BaseDirectory };
            foreach (string inicio in bases)
            {
                DirectoryInfo dir = new DirectoryInfo(inicio);
                for (int i = 0; i < 6 && dir != null; i++)
                {
                    string candidata = Path.Combine(dir.FullName, "Imagenes", "Iconos");
                    if (Directory.Exists(candidata)) return candidata;
                    dir = dir.Parent;
                }
            }
            return null;
        }

        public static List<string> RutasIconos()
        {
            List<string> lista = new List<string>();

            string dirIconos = BuscarCarpetaIconos();
            if (dirIconos == null) return lista;

            foreach (string patron in new string[] { "*.png", "*.jpg", "*.jpeg", "*.bmp" })
            {
                foreach (string f in Directory.GetFiles(dirIconos, patron))
                {
                    if (!lista.Contains(f)) 
                        lista.Add(f);
                }
            }
            return lista;
        }

        // Devuelve 'cantidad' iconos al azar, sin repetir mientras alcancen
        public static string[] IconosAleatorios(int cantidad)
        {
            List<string> todas = RutasIconos();
            string[] resultado = new string[cantidad];
            if (todas.Count == 0) return resultado;

            for (int i = todas.Count - 1; i > 0; i--)
            {
                int j = Azar.Next(i + 1);
                string temp = todas[i];
                todas[i] = todas[j];
                todas[j] = temp;
            }

            for (int i = 0; i < cantidad; i++)
                resultado[i] = todas[i % todas.Count];

            return resultado;
        }
    }

    public static class EstiloUI
    {
        public static int Limitar(int valor, int min, int max)
        {
            return Math.Max(min, Math.Min(max, valor));
        }

        public static GraphicsPath RectRedondeado(RectangleF r, float radio)
        {
            GraphicsPath path = new GraphicsPath();
            float d = Math.Max(2f, Math.Min(radio * 2f, Math.Min(r.Width, r.Height)));
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static void Redondear(Control c, int radio)
        {
            if (c.Width < 4 || c.Height < 4) return;
            using (GraphicsPath p = RectRedondeado(new RectangleF(0, 0, c.Width, c.Height), radio))
            {
                c.Region = new Region(p);
            }
        }

        public static void Circular(Control c)
        {
            if (c.Width < 4 || c.Height < 4) return;
            using (GraphicsPath p = new GraphicsPath())
            {
                p.AddEllipse(0, 0, c.Width, c.Height);
                c.Region = new Region(p);
            }
        }
    }
}