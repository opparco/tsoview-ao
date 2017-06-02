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
        public DepthMapConfig DepthMapConfig = null;
        public OcclusionConfig OcclusionConfig = null;
        public DiffusionConfig DiffusionConfig = null;

        public ConfigForm()
        {
            InitializeComponent();
        }

        private void tbznearPlane_ConfigChanged(object sender, EventArgs e)
        {
            tbznearPlane.TextChanged -= new EventHandler(tbznearPlane_TextChanged);
            tbznearPlane.Text = string.Format("{0:F2}", DepthMapConfig.ZnearPlane);
            tbznearPlane.TextChanged += new EventHandler(tbznearPlane_TextChanged);
        }

        private void tbzfarPlane_ConfigChanged(object sender, EventArgs e)
        {
            tbzfarPlane.TextChanged -= new EventHandler(tbzfarPlane_TextChanged);
            tbzfarPlane.Text = string.Format("{0:F2}", DepthMapConfig.ZfarPlane);
            tbzfarPlane.TextChanged += new EventHandler(tbzfarPlane_TextChanged);
        }

        private void tbAOIntensity_ConfigChanged(object sender, EventArgs e)
        {
            tbAOIntensity.ValueChanged -= new EventHandler(tbAOIntensity_ValueChanged);
            tbAOIntensity.Value = (int)(OcclusionConfig.Intensity * 20.0f);
            edAOIntensity.Text = string.Format("{0:F2}", OcclusionConfig.Intensity);
            tbAOIntensity.ValueChanged += new EventHandler(tbAOIntensity_ValueChanged);
        }

        private void tbAORadius_ConfigChanged(object sender, EventArgs e)
        {
            tbAORadius.ValueChanged -= new EventHandler(tbAORadius_ValueChanged);
            tbAORadius.Value = (int)(OcclusionConfig.Radius * 4.0f);
            edAORadius.Text = string.Format("{0:F2}", OcclusionConfig.Radius);
            tbAORadius.ValueChanged += new EventHandler(tbAORadius_ValueChanged);
        }

        private void tbDFIntensity_ConfigChanged(object sender, EventArgs e)
        {
            tbDFIntensity.ValueChanged -= new EventHandler(tbDFIntensity_ValueChanged);
            tbDFIntensity.Value = (int)(DiffusionConfig.Intensity * 20.0f);
            edDFIntensity.Text = string.Format("{0:F2}", DiffusionConfig.Intensity);
            tbDFIntensity.ValueChanged += new EventHandler(tbDFIntensity_ValueChanged);
        }

        private void tbDFExtent_ConfigChanged(object sender, EventArgs e)
        {
            tbDFExtent.ValueChanged -= new EventHandler(tbDFExtent_ValueChanged);
            tbDFExtent.Value = (int)(DiffusionConfig.Extent * 10.0f);
            edDFExtent.Text = string.Format("{0:F2}", DiffusionConfig.Extent);
            tbDFExtent.ValueChanged += new EventHandler(tbDFExtent_ValueChanged);
        }

        public void ConfigConnect()
        {
            DepthMapConfig.ChangeZnearPlane += new EventHandler(tbznearPlane_ConfigChanged);
            DepthMapConfig.ChangeZfarPlane += new EventHandler(tbzfarPlane_ConfigChanged);

            OcclusionConfig.ChangeIntensity += new EventHandler(tbAOIntensity_ConfigChanged);
            OcclusionConfig.ChangeRadius += new EventHandler(tbAORadius_ConfigChanged);

            DiffusionConfig.ChangeIntensity += new EventHandler(tbDFIntensity_ConfigChanged);
            DiffusionConfig.ChangeExtent += new EventHandler(tbDFExtent_ConfigChanged);
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
            {
                DepthMapConfig.ChangeZnearPlane -= new EventHandler(tbznearPlane_ConfigChanged);
                DepthMapConfig.ZnearPlane = zn;
                DepthMapConfig.ChangeZnearPlane += new EventHandler(tbznearPlane_ConfigChanged);
            }
        }

        private void tbzfarPlane_TextChanged(object sender, EventArgs e)
        {
            float zf;
            if (float.TryParse(tbzfarPlane.Text, out zf))
            {
                DepthMapConfig.ChangeZfarPlane -= new EventHandler(tbzfarPlane_ConfigChanged);
                DepthMapConfig.ZfarPlane = zf;
                DepthMapConfig.ChangeZfarPlane += new EventHandler(tbzfarPlane_ConfigChanged);
            }
        }

        private void tbAOIntensity_ValueChanged(object sender, EventArgs e)
        {
            OcclusionConfig.ChangeIntensity -= new EventHandler(tbAOIntensity_ConfigChanged);
            OcclusionConfig.Intensity = tbAOIntensity.Value / 20.0f;
            edAOIntensity.Text = string.Format("{0:F2}", OcclusionConfig.Intensity);
            OcclusionConfig.ChangeIntensity += new EventHandler(tbAOIntensity_ConfigChanged);
        }

        private void tbAORadius_ValueChanged(object sender, EventArgs e)
        {
            OcclusionConfig.ChangeRadius -= new EventHandler(tbAORadius_ConfigChanged);
            OcclusionConfig.Radius = tbAORadius.Value / 4.0f;
            edAORadius.Text = string.Format("{0:F2}", OcclusionConfig.Radius);
            OcclusionConfig.ChangeRadius += new EventHandler(tbAORadius_ConfigChanged);
        }

        private void tbDFIntensity_ValueChanged(object sender, EventArgs e)
        {
            DiffusionConfig.ChangeIntensity -= new EventHandler(tbDFIntensity_ConfigChanged);
            DiffusionConfig.Intensity = tbDFIntensity.Value / 20.0f;
            edDFIntensity.Text = string.Format("{0:F2}", DiffusionConfig.Intensity);
            DiffusionConfig.ChangeIntensity += new EventHandler(tbDFIntensity_ConfigChanged);
        }

        private void tbDFExtent_ValueChanged(object sender, EventArgs e)
        {
            DiffusionConfig.ChangeExtent -= new EventHandler(tbDFExtent_ConfigChanged);
            DiffusionConfig.Extent = tbDFExtent.Value / 10.0f;
            edDFExtent.Text = string.Format("{0:F2}", DiffusionConfig.Extent);
            DiffusionConfig.ChangeExtent += new EventHandler(tbDFExtent_ConfigChanged);
        }
    }
}
