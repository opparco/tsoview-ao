﻿using System;
using System.Globalization;
using System.Windows.Forms;
using TDCG;

namespace TSOView
{
    public partial class ConfigForm : Form
    {
        public CameraConfig CameraConfig = null;
        public DepthMapConfig DepthMapConfig = null;
        public OcclusionConfig OcclusionConfig = null;
        public DiffusionConfig DiffusionConfig = null;

        public ConfigForm()
        {
            InitializeComponent();
        }

        private void tbFovy_ConfigChanged(object sender, EventArgs e)
        {
            tbFovy.ValueChanged -= new EventHandler(tbFovy_ValueChanged);
            int fovy = (int)CameraConfig.GetFovyDegree();
            tbFovy.Value = (fovy - 15) / 5;
            edFovy.Text = string.Format("{0}", fovy);
            tbFovy.ValueChanged += new EventHandler(tbFovy_ValueChanged);
        }

        private void tbRoll_ConfigChanged(object sender, EventArgs e)
        {
            tbRoll.ValueChanged -= new EventHandler(tbRoll_ValueChanged);
            int roll = (int)CameraConfig.GetRollDegree();
            tbRoll.Value = roll / 5;
            edRoll.Text = string.Format("{0}", roll);
            tbRoll.ValueChanged += new EventHandler(tbRoll_ValueChanged);
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
            CameraConfig.ChangeFovy += new EventHandler(tbFovy_ConfigChanged);
            CameraConfig.ChangeRoll += new EventHandler(tbRoll_ConfigChanged);

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

        Viewer viewer = null;

        public void SetViewer(Viewer viewer)
        {
            this.viewer = viewer;

            this.rbPersp.CheckedChanged -= new System.EventHandler(this.rbPersp_CheckedChanged);
            this.rbOrtho.CheckedChanged -= new System.EventHandler(this.rbOrtho_CheckedChanged);
            switch (viewer.ProjectionMode)
            {
                case ProjectionMode.Perspective:
                    rbPersp.Checked = true;
                    break;
                case ProjectionMode.Ortho:
                    rbOrtho.Checked = true;
                    break;
            }
            this.rbPersp.CheckedChanged += new System.EventHandler(this.rbPersp_CheckedChanged);
            this.rbOrtho.CheckedChanged += new System.EventHandler(this.rbOrtho_CheckedChanged);

            this.rbRenderOcc.CheckedChanged -= new System.EventHandler(this.rbRenderOcc_CheckedChanged);
            this.rbRenderAmb.CheckedChanged -= new System.EventHandler(this.rbRenderAmb_CheckedChanged);
            this.rbRenderNmap.CheckedChanged -= new System.EventHandler(this.rbRenderNmap_CheckedChanged);
            this.rbRenderDmap.CheckedChanged -= new System.EventHandler(this.rbRenderDmap_CheckedChanged);
            this.rbRenderDF.CheckedChanged -= new System.EventHandler(this.rbRenderDF_CheckedChanged);
            this.rbRenderAO.CheckedChanged -= new System.EventHandler(this.rbRenderAO_CheckedChanged);
            switch (viewer.RenderMode)
            {
                case RenderMode.Ambient:
                    rbRenderAmb.Checked = true;
                    break;
                case RenderMode.Occlusion:
                    rbRenderOcc.Checked = true;
                    break;
                case RenderMode.DepthMap:
                    rbRenderDmap.Checked = true;
                    break;
                case RenderMode.NormalMap:
                    rbRenderNmap.Checked = true;
                    break;
                case RenderMode.Main:
                    rbRenderAO.Checked = true;
                    break;
                case RenderMode.Diffusion:
                    rbRenderDF.Checked = true;
                    break;
            }
            this.rbRenderOcc.CheckedChanged += new System.EventHandler(this.rbRenderOcc_CheckedChanged);
            this.rbRenderAmb.CheckedChanged += new System.EventHandler(this.rbRenderAmb_CheckedChanged);
            this.rbRenderNmap.CheckedChanged += new System.EventHandler(this.rbRenderNmap_CheckedChanged);
            this.rbRenderDmap.CheckedChanged += new System.EventHandler(this.rbRenderDmap_CheckedChanged);
            this.rbRenderDF.CheckedChanged += new System.EventHandler(this.rbRenderDF_CheckedChanged);
            this.rbRenderAO.CheckedChanged += new System.EventHandler(this.rbRenderAO_CheckedChanged);
        }

        private void ConfigForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason != CloseReason.FormOwnerClosing)
            {
                this.Hide();
                e.Cancel = true;
            }
        }

        private void tbFovy_ValueChanged(object sender, EventArgs e)
        {
            CameraConfig.ChangeFovy -= new EventHandler(tbFovy_ConfigChanged);
            int fovy = tbFovy.Value * 5 + 15;
            edFovy.Text = string.Format("{0}", fovy);
            CameraConfig.SetFovyDegree(fovy);
            CameraConfig.ChangeFovy += new EventHandler(tbFovy_ConfigChanged);
        }

        private void tbRoll_ValueChanged(object sender, EventArgs e)
        {
            CameraConfig.ChangeRoll -= new EventHandler(tbRoll_ConfigChanged);
            int roll = tbRoll.Value * 5;
            edRoll.Text = string.Format("{0}", roll);
            CameraConfig.SetRollDegree(roll);
            CameraConfig.ChangeRoll += new EventHandler(tbRoll_ConfigChanged);
        }

        private void rbPersp_CheckedChanged(object sender, EventArgs e)
        {
            viewer.ProjectionMode = ProjectionMode.Perspective;
        }

        private void rbOrtho_CheckedChanged(object sender, EventArgs e)
        {
            viewer.ProjectionMode = ProjectionMode.Ortho;
        }

        private void rbRenderAO_CheckedChanged(object sender, EventArgs e)
        {
            viewer.RenderMode = RenderMode.Main;
        }

        private void rbRenderDF_CheckedChanged(object sender, EventArgs e)
        {
            viewer.RenderMode = RenderMode.Diffusion;
        }

        private void rbRenderDmap_CheckedChanged(object sender, EventArgs e)
        {
            viewer.RenderMode = RenderMode.DepthMap;
        }

        private void rbRenderNmap_CheckedChanged(object sender, EventArgs e)
        {
            viewer.RenderMode = RenderMode.NormalMap;
        }

        private void rbRenderAmb_CheckedChanged(object sender, EventArgs e)
        {
            viewer.RenderMode = RenderMode.Ambient;
        }

        private void rbRenderOcc_CheckedChanged(object sender, EventArgs e)
        {
            viewer.RenderMode = RenderMode.Occlusion;
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

        string GetSaveFileName(string type)
        {
            DateTime ti = DateTime.Now;
            CultureInfo ci = CultureInfo.InvariantCulture;
            string ti_string = ti.ToString("yyyyMMdd-hhmmss-fff", ci);
            return string.Format("{0}-{1}.png", ti_string, type);
        }

        void SaveToPng()
        {
            string type = "none";
            switch (viewer.RenderMode)
            {
                case RenderMode.Main:
                    type = "ao";
                    break;
                case RenderMode.Ambient:
                    type = "amb";
                    break;
                case RenderMode.DepthMap:
                    type = "d";
                    break;
                case RenderMode.NormalMap:
                    type = "n";
                    break;
                case RenderMode.Occlusion:
                    type = "o";
                    break;
                case RenderMode.Diffusion:
                    type = "df";
                    break;
                case RenderMode.Shadow:
                    type = "shadow";
                    break;
            }
            viewer.SaveToPng(GetSaveFileName(type));
        }

        private void btnCapture_Click(object sender, EventArgs e)
        {
            SaveToPng();
        }
    }
}
