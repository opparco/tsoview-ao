using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using TDCG;

namespace TSOView
{
    public partial class ConfigForm : Form
    {
        public DepthMapConfig DepthConfig = null;
        public OcclusionConfig OcclusionConfig = null;

        public ConfigForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Escを押すと抜けます。
        /// </summary>
        /// <param name="e">イベント引数</param>
        protected override void OnKeyPress(System.Windows.Forms.KeyPressEventArgs e)
        {
            if ((int)(byte)e.KeyChar == (int)System.Windows.Forms.Keys.Escape)
                this.Dispose(); // Esc was pressed
        }

        private void ConfigForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason != CloseReason.FormOwnerClosing)
            {
                this.Hide();
                e.Cancel = true;
            }
        }

        private void tbznearPlane_TextChanged(object sender, EventArgs e)
        {
            float zn;
            if (float.TryParse(tbznearPlane.Text, out zn))
                DepthConfig.ZnearPlane = zn;
        }

        private void tbzfarPlane_TextChanged(object sender, EventArgs e)
        {
            float zf;
            if (float.TryParse(tbzfarPlane.Text, out zf))
                DepthConfig.ZfarPlane = zf;
        }

        private void tbIntensity_ValueChanged(object sender, EventArgs e)
        {
            OcclusionConfig.Intensity = tbIntensity.Value / 20.0f;
        }

        private void tbRadius_ValueChanged(object sender, EventArgs e)
        {
            OcclusionConfig.Radius = tbRadius.Value / 4.0f;
        }
    }
}
