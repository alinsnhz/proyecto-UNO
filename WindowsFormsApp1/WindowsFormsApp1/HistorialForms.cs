using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;

namespace proyectoUNO
{
    public class HistorialForm : Form
    {
        private readonly List<JsonElement> _partidas;
        private DataGridView dgvHistorial;
        private Label lblResumenTop;

        public HistorialForm(List<JsonElement> partidas)
        {
            _partidas = partidas;
            InitializeComponentCustom();
            CalcularEstadisticas();
        }

        private void InitializeComponentCustom()
        {
            this.Text = "Historial y Estadísticas - UNO";
            this.StartPosition = FormStartPosition.CenterParent;
            this.ClientSize = new Size(720, 520);
            this.MinimumSize = new Size(620, 460);
            this.BackColor = Color.FromArgb(120, 0, 0);
            this.DoubleBuffered = true;

            // Título superior estilizado
            Label lblTitulo = new Label
            {
                Text = "HISTORIAL DE PARTIDAS",
                Font = new Font("Segoe UI Black", 15f, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 221, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 45,
                BackColor = Color.Transparent
            };

            // Etiqueta de resumen (Jugador con más victorias y conteo)
            lblResumenTop = new Label
            {
                Font = new Font("Segoe UI Semibold", 11f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(100, 0, 0, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 60
            };

            // DataGridView estilizado con los colores del juego
            dgvHistorial = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.FromArgb(50, 0, 0),
                BorderStyle = BorderStyle.None,
                ForeColor = Color.White,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                GridColor = Color.FromArgb(90, 10, 10)
            };

            // Estilos de cabecera de la tabla
            dgvHistorial.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(150, 10, 10);
            dgvHistorial.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(255, 221, 0);
            dgvHistorial.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Black", 10f, FontStyle.Bold);
            dgvHistorial.EnableHeadersVisualStyles = false;

            // Estilos de filas
            dgvHistorial.DefaultCellStyle.BackColor = Color.FromArgb(40, 0, 0);
            dgvHistorial.DefaultCellStyle.ForeColor = Color.White;
            dgvHistorial.DefaultCellStyle.SelectionBackColor = Color.FromArgb(220, 30, 30);
            dgvHistorial.DefaultCellStyle.SelectionForeColor = Color.White;
            dgvHistorial.DefaultCellStyle.Font = new Font("Segoe UI", 10f);

            // Contenedor principal con márgenes
            Panel panelPrincipal = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20)
            };

            Panel panelContenedorGrid = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 10, 0, 0)
            };
            panelContenedorGrid.Controls.Add(dgvHistorial);

            panelPrincipal.Controls.Add(panelContenedorGrid);
            panelPrincipal.Controls.Add(lblResumenTop);
            panelPrincipal.Controls.Add(lblTitulo);

            this.Controls.Add(panelPrincipal);

            // Definición de columnas
            dgvHistorial.Columns.Add("Partida", "ID Partida");
            dgvHistorial.Columns.Add("Fecha", "Fecha y Hora");
            dgvHistorial.Columns.Add("Ganador", "Ganador");

            // Población de datos
            foreach (var p in _partidas)
            {
                string id = ObtenerValor(p, "Partida");
                string fecha = ObtenerValor(p, "Fecha");
                string ganador = ObtenerValor(p, "Ganador");

                if (DateTime.TryParse(fecha, out DateTime dt))
                    fecha = dt.ToString("dd/MM/yyyy HH:mm");

                dgvHistorial.Rows.Add(
                    id,
                    fecha,
                    string.IsNullOrEmpty(ganador) || ganador == "No disponible" ? "Sin definir" : ganador
                );
            }
        }

        private void CalcularEstadisticas()
        {
            var victoriasPorJugador = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var p in _partidas)
            {
                string ganador = ObtenerValor(p, "Ganador");
                if (!string.IsNullOrEmpty(ganador) && ganador != "No disponible" && ganador != "null")
                {
                    if (victoriasPorJugador.ContainsKey(ganador))
                        victoriasPorJugador[ganador]++;
                    else
                        victoriasPorJugador[ganador] = 1;
                }
            }

            if (victoriasPorJugador.Count > 0)
            {
                var topJugador = victoriasPorJugador.OrderByDescending(x => x.Value).First();
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"👑 ¡Jugador con más victorias: {topJugador.Key} ({topJugador.Value} ganadas)!");
                sb.Append("Conteo: ");
                foreach (var kv in victoriasPorJugador)
                {
                    sb.Append($"[{kv.Key}: {kv.Value}]  ");
                }
                lblResumenTop.Text = sb.ToString();
            }
            else
            {
                lblResumenTop.Text = "Aún no hay registros suficientes de ganadores.";
            }
        }

        private string ObtenerValor(JsonElement elemento, string propiedad)
        {
            if (elemento.TryGetProperty(propiedad, out JsonElement valor))
                return valor.ToString();
            return "No disponible";
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            int W = ClientSize.Width, H = ClientSize.Height;
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Degradado lineal idéntico al fondo de InicioForm
            using (LinearGradientBrush fondo = new LinearGradientBrush(
                new Rectangle(0, 0, W, H), Color.FromArgb(170, 15, 15), Color.FromArgb(30, 0, 5), 65f))
            {
                g.FillRectangle(fondo, 0, 0, W, H);
            }
        }
    }
}