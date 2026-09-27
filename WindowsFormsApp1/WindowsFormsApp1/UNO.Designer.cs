namespace WindowsFormsApp1
{
    partial class UNO
    {
        /// <summary>
        /// Variable del diseñador necesaria.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Limpiar los recursos que se estén usando.
        /// </summary>
        /// <param name="disposing">true si los recursos administrados se deben desechar; false en caso contrario.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Código generado por el Diseñador de Windows Forms

        /// <summary>
        /// Método necesario para admitir el Diseñador. No se puede modificar
        /// el contenido de este método con el editor de código.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(UNO));
            this.panelMano = new System.Windows.Forms.FlowLayoutPanel();
            this.panelJugador = new System.Windows.Forms.Panel();
            this.nombreJugador = new System.Windows.Forms.Label();
            this.PanelJugador2 = new System.Windows.Forms.FlowLayoutPanel();
            this.panelJugador3 = new System.Windows.Forms.FlowLayoutPanel();
            this.SuspendLayout();
            // 
            // panelMano
            // 
            this.panelMano.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panelMano.Location = new System.Drawing.Point(186, 291);
            this.panelMano.Name = "panelMano";
            this.panelMano.Size = new System.Drawing.Size(499, 98);
            this.panelMano.TabIndex = 0;
            // 
            // panelJugador
            // 
            this.panelJugador.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panelJugador.BackColor = System.Drawing.Color.Cyan;
            this.panelJugador.Location = new System.Drawing.Point(35, 289);
            this.panelJugador.Name = "panelJugador";
            this.panelJugador.Size = new System.Drawing.Size(85, 100);
            this.panelJugador.TabIndex = 1;
            // 
            // nombreJugador
            // 
            this.nombreJugador.Location = new System.Drawing.Point(32, 264);
            this.nombreJugador.Name = "nombreJugador";
            this.nombreJugador.Size = new System.Drawing.Size(90, 24);
            this.nombreJugador.TabIndex = 2;
            this.nombreJugador.Text = "label1";
            this.nombreJugador.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // PanelJugador2
            // 
            this.PanelJugador2.Location = new System.Drawing.Point(33, 12);
            this.PanelJugador2.Name = "PanelJugador2";
            this.PanelJugador2.Size = new System.Drawing.Size(87, 217);
            this.PanelJugador2.TabIndex = 3;
            this.PanelJugador2.WrapContents = false;
            // 
            // panelJugador3
            // 
            this.panelJugador3.Location = new System.Drawing.Point(686, 12);
            this.panelJugador3.Name = "panelJugador3";
            this.panelJugador3.Size = new System.Drawing.Size(87, 217);
            this.panelJugador3.TabIndex = 4;
            this.panelJugador3.WrapContents = false;
            // 
            // UNO
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("$this.BackgroundImage")));
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.ClientSize = new System.Drawing.Size(800, 389);
            this.Controls.Add(this.panelJugador3);
            this.Controls.Add(this.PanelJugador2);
            this.Controls.Add(this.nombreJugador);
            this.Controls.Add(this.panelJugador);
            this.Controls.Add(this.panelMano);
            this.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.Name = "UNO";
            this.Text = "UNO";
            this.Load += new System.EventHandler(this.UNO_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.FlowLayoutPanel panelMano;
        private System.Windows.Forms.Panel panelJugador;
        private System.Windows.Forms.Label nombreJugador;
        private System.Windows.Forms.FlowLayoutPanel PanelJugador2;
        private System.Windows.Forms.FlowLayoutPanel panelJugador3;
    }
}

