namespace TSOView
{
    partial class ConfigForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.gbDepthMap = new System.Windows.Forms.GroupBox();
            this.tbzfarPlane = new System.Windows.Forms.TextBox();
            this.lbzfarPlane = new System.Windows.Forms.Label();
            this.tbznearPlane = new System.Windows.Forms.TextBox();
            this.lbznearPlane = new System.Windows.Forms.Label();
            this.gbAmbientOcclusion = new System.Windows.Forms.GroupBox();
            this.edAORadius = new System.Windows.Forms.TextBox();
            this.edAOIntensity = new System.Windows.Forms.TextBox();
            this.tbAORadius = new System.Windows.Forms.TrackBar();
            this.lbAORadius = new System.Windows.Forms.Label();
            this.tbAOIntensity = new System.Windows.Forms.TrackBar();
            this.lbAOIntensity = new System.Windows.Forms.Label();
            this.gbDiffusion = new System.Windows.Forms.GroupBox();
            this.edDFExtent = new System.Windows.Forms.TextBox();
            this.edDFIntensity = new System.Windows.Forms.TextBox();
            this.tbDFExtent = new System.Windows.Forms.TrackBar();
            this.lbDFExtent = new System.Windows.Forms.Label();
            this.tbDFIntensity = new System.Windows.Forms.TrackBar();
            this.lbDFIntensity = new System.Windows.Forms.Label();
            this.gbDepthMap.SuspendLayout();
            this.gbAmbientOcclusion.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.tbAORadius)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbAOIntensity)).BeginInit();
            this.gbDiffusion.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.tbDFExtent)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbDFIntensity)).BeginInit();
            this.SuspendLayout();
            // 
            // gbDepthMap
            // 
            this.gbDepthMap.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gbDepthMap.Controls.Add(this.tbzfarPlane);
            this.gbDepthMap.Controls.Add(this.lbzfarPlane);
            this.gbDepthMap.Controls.Add(this.tbznearPlane);
            this.gbDepthMap.Controls.Add(this.lbznearPlane);
            this.gbDepthMap.Location = new System.Drawing.Point(12, 12);
            this.gbDepthMap.Name = "gbDepthMap";
            this.gbDepthMap.Size = new System.Drawing.Size(260, 65);
            this.gbDepthMap.TabIndex = 10;
            this.gbDepthMap.TabStop = false;
            this.gbDepthMap.Text = "Depth map";
            // 
            // tbzfarPlane
            // 
            this.tbzfarPlane.Location = new System.Drawing.Point(113, 37);
            this.tbzfarPlane.Name = "tbzfarPlane";
            this.tbzfarPlane.Size = new System.Drawing.Size(50, 19);
            this.tbzfarPlane.TabIndex = 11;
            this.tbzfarPlane.Text = "50";
            this.tbzfarPlane.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.tbzfarPlane.TextChanged += new System.EventHandler(this.tbzfarPlane_TextChanged);
            // 
            // lbzfarPlane
            // 
            this.lbzfarPlane.AutoSize = true;
            this.lbzfarPlane.Location = new System.Drawing.Point(44, 40);
            this.lbzfarPlane.Name = "lbzfarPlane";
            this.lbzfarPlane.Size = new System.Drawing.Size(55, 12);
            this.lbzfarPlane.TabIndex = 10;
            this.lbzfarPlane.Text = "zfar plane";
            // 
            // tbznearPlane
            // 
            this.tbznearPlane.Location = new System.Drawing.Point(113, 12);
            this.tbznearPlane.Name = "tbznearPlane";
            this.tbznearPlane.Size = new System.Drawing.Size(50, 19);
            this.tbznearPlane.TabIndex = 9;
            this.tbznearPlane.Text = "15";
            this.tbznearPlane.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.tbznearPlane.TextChanged += new System.EventHandler(this.tbznearPlane_TextChanged);
            // 
            // lbznearPlane
            // 
            this.lbznearPlane.AutoSize = true;
            this.lbznearPlane.Location = new System.Drawing.Point(44, 15);
            this.lbznearPlane.Name = "lbznearPlane";
            this.lbznearPlane.Size = new System.Drawing.Size(63, 12);
            this.lbznearPlane.TabIndex = 8;
            this.lbznearPlane.Text = "znear plane";
            // 
            // gbAmbientOcclusion
            // 
            this.gbAmbientOcclusion.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gbAmbientOcclusion.Controls.Add(this.edAORadius);
            this.gbAmbientOcclusion.Controls.Add(this.edAOIntensity);
            this.gbAmbientOcclusion.Controls.Add(this.tbAORadius);
            this.gbAmbientOcclusion.Controls.Add(this.lbAORadius);
            this.gbAmbientOcclusion.Controls.Add(this.tbAOIntensity);
            this.gbAmbientOcclusion.Controls.Add(this.lbAOIntensity);
            this.gbAmbientOcclusion.Location = new System.Drawing.Point(12, 83);
            this.gbAmbientOcclusion.Name = "gbAmbientOcclusion";
            this.gbAmbientOcclusion.Size = new System.Drawing.Size(260, 95);
            this.gbAmbientOcclusion.TabIndex = 11;
            this.gbAmbientOcclusion.TabStop = false;
            this.gbAmbientOcclusion.Text = "Ambient Occlusion";
            // 
            // edAORadius
            // 
            this.edAORadius.Location = new System.Drawing.Point(219, 54);
            this.edAORadius.Name = "edAORadius";
            this.edAORadius.ReadOnly = true;
            this.edAORadius.Size = new System.Drawing.Size(35, 19);
            this.edAORadius.TabIndex = 19;
            this.edAORadius.Text = "2.50";
            this.edAORadius.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // edAOIntensity
            // 
            this.edAOIntensity.Location = new System.Drawing.Point(219, 18);
            this.edAOIntensity.Name = "edAOIntensity";
            this.edAOIntensity.ReadOnly = true;
            this.edAOIntensity.Size = new System.Drawing.Size(35, 19);
            this.edAOIntensity.TabIndex = 18;
            this.edAOIntensity.Text = "0.50";
            this.edAOIntensity.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // tbAORadius
            // 
            this.tbAORadius.AutoSize = false;
            this.tbAORadius.Location = new System.Drawing.Point(61, 54);
            this.tbAORadius.Maximum = 20;
            this.tbAORadius.Name = "tbAORadius";
            this.tbAORadius.Size = new System.Drawing.Size(152, 30);
            this.tbAORadius.TabIndex = 17;
            this.tbAORadius.Value = 10;
            this.tbAORadius.ValueChanged += new System.EventHandler(this.tbAORadius_ValueChanged);
            // 
            // lbAORadius
            // 
            this.lbAORadius.AutoSize = true;
            this.lbAORadius.Location = new System.Drawing.Point(6, 54);
            this.lbAORadius.Name = "lbAORadius";
            this.lbAORadius.Size = new System.Drawing.Size(36, 12);
            this.lbAORadius.TabIndex = 15;
            this.lbAORadius.Text = "radius";
            // 
            // tbAOIntensity
            // 
            this.tbAOIntensity.AutoSize = false;
            this.tbAOIntensity.Location = new System.Drawing.Point(61, 18);
            this.tbAOIntensity.Maximum = 20;
            this.tbAOIntensity.Name = "tbAOIntensity";
            this.tbAOIntensity.Size = new System.Drawing.Size(152, 30);
            this.tbAOIntensity.TabIndex = 16;
            this.tbAOIntensity.Value = 10;
            this.tbAOIntensity.ValueChanged += new System.EventHandler(this.tbAOIntensity_ValueChanged);
            // 
            // lbAOIntensity
            // 
            this.lbAOIntensity.AutoSize = true;
            this.lbAOIntensity.Location = new System.Drawing.Point(6, 18);
            this.lbAOIntensity.Name = "lbAOIntensity";
            this.lbAOIntensity.Size = new System.Drawing.Size(49, 12);
            this.lbAOIntensity.TabIndex = 14;
            this.lbAOIntensity.Text = "intensity";
            // 
            // gbDiffusion
            // 
            this.gbDiffusion.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gbDiffusion.Controls.Add(this.edDFExtent);
            this.gbDiffusion.Controls.Add(this.edDFIntensity);
            this.gbDiffusion.Controls.Add(this.tbDFExtent);
            this.gbDiffusion.Controls.Add(this.lbDFExtent);
            this.gbDiffusion.Controls.Add(this.tbDFIntensity);
            this.gbDiffusion.Controls.Add(this.lbDFIntensity);
            this.gbDiffusion.Location = new System.Drawing.Point(12, 184);
            this.gbDiffusion.Name = "gbDiffusion";
            this.gbDiffusion.Size = new System.Drawing.Size(260, 95);
            this.gbDiffusion.TabIndex = 18;
            this.gbDiffusion.TabStop = false;
            this.gbDiffusion.Text = "Diffusion";
            // 
            // edDFExtent
            // 
            this.edDFExtent.Location = new System.Drawing.Point(219, 54);
            this.edDFExtent.Name = "edDFExtent";
            this.edDFExtent.ReadOnly = true;
            this.edDFExtent.Size = new System.Drawing.Size(35, 19);
            this.edDFExtent.TabIndex = 21;
            this.edDFExtent.Text = "2.00";
            this.edDFExtent.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // edDFIntensity
            // 
            this.edDFIntensity.Location = new System.Drawing.Point(219, 18);
            this.edDFIntensity.Name = "edDFIntensity";
            this.edDFIntensity.ReadOnly = true;
            this.edDFIntensity.Size = new System.Drawing.Size(35, 19);
            this.edDFIntensity.TabIndex = 20;
            this.edDFIntensity.Text = "0.50";
            this.edDFIntensity.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // tbDFExtent
            // 
            this.tbDFExtent.AutoSize = false;
            this.tbDFExtent.Location = new System.Drawing.Point(61, 54);
            this.tbDFExtent.Maximum = 30;
            this.tbDFExtent.Minimum = 10;
            this.tbDFExtent.Name = "tbDFExtent";
            this.tbDFExtent.Size = new System.Drawing.Size(152, 30);
            this.tbDFExtent.TabIndex = 19;
            this.tbDFExtent.Value = 20;
            this.tbDFExtent.ValueChanged += new System.EventHandler(this.tbDFExtent_ValueChanged);
            // 
            // lbDFExtent
            // 
            this.lbDFExtent.AutoSize = true;
            this.lbDFExtent.Location = new System.Drawing.Point(6, 54);
            this.lbDFExtent.Name = "lbDFExtent";
            this.lbDFExtent.Size = new System.Drawing.Size(37, 12);
            this.lbDFExtent.TabIndex = 18;
            this.lbDFExtent.Text = "extent";
            // 
            // tbDFIntensity
            // 
            this.tbDFIntensity.AutoSize = false;
            this.tbDFIntensity.Location = new System.Drawing.Point(61, 18);
            this.tbDFIntensity.Maximum = 20;
            this.tbDFIntensity.Name = "tbDFIntensity";
            this.tbDFIntensity.Size = new System.Drawing.Size(152, 30);
            this.tbDFIntensity.TabIndex = 16;
            this.tbDFIntensity.Value = 10;
            this.tbDFIntensity.ValueChanged += new System.EventHandler(this.tbDFIntensity_ValueChanged);
            // 
            // lbDFIntensity
            // 
            this.lbDFIntensity.AutoSize = true;
            this.lbDFIntensity.Location = new System.Drawing.Point(6, 18);
            this.lbDFIntensity.Name = "lbDFIntensity";
            this.lbDFIntensity.Size = new System.Drawing.Size(49, 12);
            this.lbDFIntensity.TabIndex = 14;
            this.lbDFIntensity.Text = "intensity";
            // 
            // ConfigForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 291);
            this.Controls.Add(this.gbDiffusion);
            this.Controls.Add(this.gbAmbientOcclusion);
            this.Controls.Add(this.gbDepthMap);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "ConfigForm";
            this.Text = "AO Config";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ConfigForm_FormClosing);
            this.gbDepthMap.ResumeLayout(false);
            this.gbDepthMap.PerformLayout();
            this.gbAmbientOcclusion.ResumeLayout(false);
            this.gbAmbientOcclusion.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.tbAORadius)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbAOIntensity)).EndInit();
            this.gbDiffusion.ResumeLayout(false);
            this.gbDiffusion.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.tbDFExtent)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbDFIntensity)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox gbDepthMap;
        private System.Windows.Forms.GroupBox gbAmbientOcclusion;
        private System.Windows.Forms.TextBox tbzfarPlane;
        private System.Windows.Forms.Label lbzfarPlane;
        private System.Windows.Forms.TextBox tbznearPlane;
        private System.Windows.Forms.Label lbznearPlane;
        private System.Windows.Forms.TrackBar tbAORadius;
        private System.Windows.Forms.Label lbAORadius;
        private System.Windows.Forms.TrackBar tbAOIntensity;
        private System.Windows.Forms.Label lbAOIntensity;
        private System.Windows.Forms.GroupBox gbDiffusion;
        private System.Windows.Forms.TrackBar tbDFIntensity;
        private System.Windows.Forms.Label lbDFIntensity;
        private System.Windows.Forms.TrackBar tbDFExtent;
        private System.Windows.Forms.Label lbDFExtent;
        private System.Windows.Forms.TextBox edAORadius;
        private System.Windows.Forms.TextBox edAOIntensity;
        private System.Windows.Forms.TextBox edDFExtent;
        private System.Windows.Forms.TextBox edDFIntensity;
    }
}
