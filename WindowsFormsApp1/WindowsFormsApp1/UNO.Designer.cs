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
            this.lblNombreJ1 = new System.Windows.Forms.Label();
            this.panelManoJugador2 = new System.Windows.Forms.FlowLayoutPanel();
            this.panelManoJugador3 = new System.Windows.Forms.FlowLayoutPanel();
            this.lblNombreJ2 = new System.Windows.Forms.Label();
            this.lblNombreJ3 = new System.Windows.Forms.Label();
            this.lblMensaje = new System.Windows.Forms.Label();
            this.BtnRobar = new System.Windows.Forms.Button();
            this.panelJugador2 = new System.Windows.Forms.Panel();
            this.panelJugador3 = new System.Windows.Forms.Panel();
            this.SuspendLayout();
            // 
            // panelMano
            // 
            this.panelMano.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panelMano.Location = new System.Drawing.Point(186, 343);
            this.panelMano.Name = "panelMano";
            this.panelMano.Size = new System.Drawing.Size(664, 98);
            this.panelMano.TabIndex = 0;
            // 
            // panelJugador
            // 
            this.panelJugador.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.panelJugador.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.panelJugador.Location = new System.Drawing.Point(35, 341);
            this.panelJugador.Name = "panelJugador";
            this.panelJugador.Size = new System.Drawing.Size(85, 100);
            this.panelJugador.TabIndex = 1;
            // 
            // lblNombreJ1
            // 
            this.lblNombreJ1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblNombreJ1.Location = new System.Drawing.Point(32, 314);
            this.lblNombreJ1.Name = "lblNombreJ1";
            this.lblNombreJ1.Size = new System.Drawing.Size(90, 24);
            this.lblNombreJ1.TabIndex = 2;
            this.lblNombreJ1.Text = "Jugador 1";
            this.lblNombreJ1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // panelManoJugador2
            // 
            this.panelManoJugador2.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.panelManoJugador2.Location = new System.Drawing.Point(127, 23);
            this.panelManoJugador2.Name = "panelManoJugador2";
            this.panelManoJugador2.Size = new System.Drawing.Size(87, 283);
            this.panelManoJugador2.TabIndex = 3;
            this.panelManoJugador2.WrapContents = false;
            // 
            // panelManoJugador3
            // 
            this.panelManoJugador3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.panelManoJugador3.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.panelManoJugador3.Location = new System.Drawing.Point(763, 12);
            this.panelManoJugador3.Name = "panelManoJugador3";
            this.panelManoJugador3.Size = new System.Drawing.Size(87, 294);
            this.panelManoJugador3.TabIndex = 4;
            this.panelManoJugador3.WrapContents = false;
            // 
            // lblNombreJ2
            // 
            this.lblNombreJ2.Location = new System.Drawing.Point(12, 23);
            this.lblNombreJ2.Name = "lblNombreJ2";
            this.lblNombreJ2.Size = new System.Drawing.Size(90, 24);
            this.lblNombreJ2.TabIndex = 5;
            this.lblNombreJ2.Text = "Jugador 2";
            this.lblNombreJ2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblNombreJ3
            // 
            this.lblNombreJ3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblNombreJ3.Location = new System.Drawing.Point(863, 23);
            this.lblNombreJ3.Name = "lblNombreJ3";
            this.lblNombreJ3.Size = new System.Drawing.Size(90, 24);
            this.lblNombreJ3.TabIndex = 6;
            this.lblNombreJ3.Text = "Jugador 3";
            this.lblNombreJ3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblMensaje
            // 
            this.lblMensaje.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.lblMensaje.AutoSize = true;
            this.lblMensaje.Location = new System.Drawing.Point(452, 23);
            this.lblMensaje.Name = "lblMensaje";
            this.lblMensaje.Size = new System.Drawing.Size(44, 13);
            this.lblMensaje.TabIndex = 7;
            this.lblMensaje.Text = "Historial";
            // 
            // BtnRobar
            // 
            this.BtnRobar.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnRobar.Location = new System.Drawing.Point(856, 343);
            this.BtnRobar.Name = "BtnRobar";
            this.BtnRobar.Size = new System.Drawing.Size(75, 23);
            this.BtnRobar.TabIndex = 8;
            this.BtnRobar.Text = "Robar";
            this.BtnRobar.UseVisualStyleBackColor = true;
            // 
            // panelJugador2
            // 
            this.panelJugador2.BackColor = System.Drawing.Color.Cyan;
            this.panelJugador2.Location = new System.Drawing.Point(15, 50);
            this.panelJugador2.Name = "panelJugador2";
            this.panelJugador2.Size = new System.Drawing.Size(85, 100);
            this.panelJugador2.TabIndex = 2;
            // 
            // panelJugador3
            // 
            this.panelJugador3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.panelJugador3.BackColor = System.Drawing.Color.Lime;
            this.panelJugador3.Location = new System.Drawing.Point(868, 50);
            this.panelJugador3.Name = "panelJugador3";
            this.panelJugador3.Size = new System.Drawing.Size(85, 100);
            this.panelJugador3.TabIndex = 2;
            // 
            // UNO
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("$this.BackgroundImage")));
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.ClientSize = new System.Drawing.Size(965, 441);
            this.Controls.Add(this.panelJugador3);
            this.Controls.Add(this.panelManoJugador3);
            this.Controls.Add(this.panelJugador2);
            this.Controls.Add(this.BtnRobar);
            this.Controls.Add(this.lblMensaje);
            this.Controls.Add(this.lblNombreJ3);
            this.Controls.Add(this.lblNombreJ2);
            this.Controls.Add(this.panelManoJugador2);
            this.Controls.Add(this.lblNombreJ1);
            this.Controls.Add(this.panelJugador);
            this.Controls.Add(this.panelMano);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "UNO";
            this.Text = "UNO";
            this.Load += new System.EventHandler(this.UNO_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.FlowLayoutPanel panelMano;
        private System.Windows.Forms.Panel panelJugador;
        private System.Windows.Forms.Label lblNombreJ1;
        private System.Windows.Forms.FlowLayoutPanel panelManoJugador2;
        private System.Windows.Forms.FlowLayoutPanel panelManoJugador3;
        private System.Windows.Forms.Label lblNombreJ2;
        private System.Windows.Forms.Label lblNombreJ3;
        private System.Windows.Forms.Label lblMensaje;
        private System.Windows.Forms.Button BtnRobar;
        private System.Windows.Forms.Panel panelJugador2;
        private System.Windows.Forms.Panel panelJugador3;
    }
}

