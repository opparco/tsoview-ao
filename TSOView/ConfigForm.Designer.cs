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
            this.tbRadius = new System.Windows.Forms.TrackBar();
            this.lbRadius = new System.Windows.Forms.Label();
            this.tbIntensity = new System.Windows.Forms.TrackBar();
            this.lbIntensity = new System.Windows.Forms.Label();
            this.gbDepthMap.SuspendLayout();
            this.gbAmbientOcclusion.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.tbRadius)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbIntensity)).BeginInit();
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
            this.gbDepthMap.Size = new System.Drawing.Size(260, 96);
            this.gbDepthMap.TabIndex = 10;
            this.gbDepthMap.TabStop = false;
            this.gbDepthMap.Text = "Depth map";
            // 
            // tbzfarPlane
            // 
            this.tbzfarPlane.Location = new System.Drawing.Point(73, 67);
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
            this.lbzfarPlane.Location = new System.Drawing.Point(44, 52);
            this.lbzfarPlane.Name = "lbzfarPlane";
            this.lbzfarPlane.Size = new System.Drawing.Size(55, 12);
            this.lbzfarPlane.TabIndex = 10;
            this.lbzfarPlane.Text = "zfar plane";
            // 
            // tbznearPlane
            // 
            this.tbznearPlane.Location = new System.Drawing.Point(73, 30);
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
            this.gbAmbientOcclusion.Controls.Add(this.tbRadius);
            this.gbAmbientOcclusion.Controls.Add(this.lbRadius);
            this.gbAmbientOcclusion.Controls.Add(this.tbIntensity);
            this.gbAmbientOcclusion.Controls.Add(this.lbIntensity);
            this.gbAmbientOcclusion.Location = new System.Drawing.Point(12, 114);
            this.gbAmbientOcclusion.Name = "gbAmbientOcclusion";
            this.gbAmbientOcclusion.Size = new System.Drawing.Size(260, 135);
            this.gbAmbientOcclusion.TabIndex = 11;
            this.gbAmbientOcclusion.TabStop = false;
            this.gbAmbientOcclusion.Text = "Ambient Occlusion";
            // 
            // tbRadius
            // 
            this.tbRadius.Location = new System.Drawing.Point(46, 81);
            this.tbRadius.Maximum = 20;
            this.tbRadius.Name = "tbRadius";
            this.tbRadius.Size = new System.Drawing.Size(168, 45);
            this.tbRadius.TabIndex = 17;
            this.tbRadius.Value = 10;
            this.tbRadius.ValueChanged += new System.EventHandler(this.tbRadius_ValueChanged);
            // 
            // lbRadius
            // 
            this.lbRadius.AutoSize = true;
            this.lbRadius.Location = new System.Drawing.Point(44, 63);
            this.lbRadius.Name = "lbRadius";
            this.lbRadius.Size = new System.Drawing.Size(36, 12);
            this.lbRadius.TabIndex = 15;
            this.lbRadius.Text = "radius";
            // 
            // tbIntensity
            // 
            this.tbIntensity.Location = new System.Drawing.Point(46, 30);
            this.tbIntensity.Maximum = 20;
            this.tbIntensity.Name = "tbIntensity";
            this.tbIntensity.Size = new System.Drawing.Size(168, 45);
            this.tbIntensity.TabIndex = 16;
            this.tbIntensity.Value = 10;
            this.tbIntensity.ValueChanged += new System.EventHandler(this.tbIntensity_ValueChanged);
            // 
            // lbIntensity
            // 
            this.lbIntensity.AutoSize = true;
            this.lbIntensity.Location = new System.Drawing.Point(44, 15);
            this.lbIntensity.Name = "lbIntensity";
            this.lbIntensity.Size = new System.Drawing.Size(49, 12);
            this.lbIntensity.TabIndex = 14;
            this.lbIntensity.Text = "intensity";
            // 
            // ConfigForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 261);
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
            ((System.ComponentModel.ISupportInitialize)(this.tbRadius)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbIntensity)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox gbDepthMap;
        private System.Windows.Forms.GroupBox gbAmbientOcclusion;
        private System.Windows.Forms.TextBox tbzfarPlane;
        private System.Windows.Forms.Label lbzfarPlane;
        private System.Windows.Forms.TextBox tbznearPlane;
        private System.Windows.Forms.Label lbznearPlane;
        private System.Windows.Forms.TrackBar tbRadius;
        private System.Windows.Forms.Label lbRadius;
        private System.Windows.Forms.TrackBar tbIntensity;
        private System.Windows.Forms.Label lbIntensity;
    }
}