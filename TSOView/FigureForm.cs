using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
//using System.ComponentModel;
using System.Windows.Forms;
using System.IO;
using TDCG;

namespace TSOView
{
    /// <summary>
    /// フィギュア情報を扱うフォーム
    /// </summary>
    public partial class FigureForm : Form
    {
        /// <summary>
        /// フィギュア情報フォームを生成します。
        /// </summary>
        public FigureForm()
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

        Viewer viewer = null;
        Figure fig = null;
        TSOFile tso = null;
        TSOSubScript sub_script = null;

        public void SetViewer(Viewer viewer)
        {
            this.viewer = viewer;
        }

        /// <summary>
        /// フィギュア情報を削除します。
        /// </summary>
        public void Clear()
        {
            gvShaderParams.DataSource = null;
            this.sub_script = null;
            lvSubScripts.Items.Clear();
            this.tso = null;
            lvTSOFiles.Items.Clear();
            this.fig = null;
        }

        /// <summary>
        /// フィギュアをUIに設定します。
        /// </summary>
        /// <param name="fig">フィギュア</param>
        public void SetFigure(Figure fig)
        {
            this.fig = fig;
            SliderMatrix slider_matrix = fig.SliderMatrix;
            if (slider_matrix != null)
            {
                this.tbSlideArm.Value = (int)(slider_matrix.ArmRatio * (float)tbSlideArm.Maximum);
                this.tbSlideLeg.Value = (int)(slider_matrix.LegRatio * (float)tbSlideLeg.Maximum);
                this.tbSlideWaist.Value = (int)(slider_matrix.WaistRatio * (float)tbSlideWaist.Maximum);
                this.tbSlideOppai.Value = (int)(slider_matrix.OppaiRatio * (float)tbSlideOppai.Maximum);
                this.tbSlideAge.Value = (int)(slider_matrix.AgeRatio * (float)tbSlideAge.Maximum);
                this.tbSlideEye.Value = (int)(slider_matrix.EyeRatio * (float)tbSlideEye.Maximum);
            }
            lvTSOFiles.Items.Clear();
            for (int i = 0; i < fig.TsoList.Count; i++)
            {
                TSOFile tso = fig.TsoList[i];
                ListViewItem li = new ListViewItem(tso.FileName ?? string.Format("{0:X2}.tso", tso.Row));
                li.Tag = tso;
                lvTSOFiles.Items.Add(li);
            }
            lvTSOFiles.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
        }

        /// <summary>
        /// tsoをUIに設定します。
        /// </summary>
        /// <param name="tso">tso</param>
        public void SetTSOFile(TSOFile tso)
        {
            this.tso = tso;
            lvSubScripts.Items.Clear();
            foreach (TSOSubScript sub_script in tso.sub_scripts)
            {
                ListViewItem li = new ListViewItem(sub_script.Name);
                li.SubItems.Add(sub_script.FileName);
                li.Tag = sub_script;
                lvSubScripts.Items.Add(li);
            }
            lvSubScripts.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
        }

        public void SetSubScript(TSOSubScript sub_script)
        {
            this.sub_script = sub_script;
            gvShaderParams.DataSource = sub_script.shader.shader_parameters;
        }

        private void btnDump_Click(object sender, EventArgs e)
        {
            if (sub_script == null)
                return;
            Console.WriteLine("-- dump shader parameters --");
            foreach (string str in sub_script.shader.GetLines())
                Console.WriteLine(str);
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (tso == null)
                return;
            if (sub_script != null)
                sub_script.SaveShader();

            SaveFileDialog dialog = new SaveFileDialog();
            dialog.FileName = tso.FileName;
            dialog.Filter = "tso files|*.tso";
            dialog.FilterIndex = 0;
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string dest_file = dialog.FileName;
                string extension = Path.GetExtension(dest_file);
                if (extension == ".tso")
                {
                    tso.Save(dest_file);
                }
            }
        }

        private void lvTSOFiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lvTSOFiles.SelectedItems.Count == 0)
                return;
            ListViewItem li = lvTSOFiles.SelectedItems[0];
            TSOFile tso = li.Tag as TSOFile;
            SetTSOFile(tso);
        }

        private void lvSubScripts_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lvSubScripts.SelectedItems.Count == 0)
                return;
            ListViewItem li = lvSubScripts.SelectedItems[0];
            TSOSubScript sub_script = li.Tag as TSOSubScript;
            SetSubScript(sub_script);
        }

        private void FigureForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason != CloseReason.FormOwnerClosing)
            {
                this.Hide();
                e.Cancel = true;
            }
        }

        private void tbSlideArm_ValueChanged(object sender, EventArgs e)
        {
            if (fig == null)
                return;

            fig.SliderMatrix.ArmRatio = tbSlideArm.Value / (float)tbSlideArm.Maximum;
            fig.UpdateBoneMatrices(true);
        }

        private void tbSlideLeg_ValueChanged(object sender, EventArgs e)
        {
            if (fig == null)
                return;

            fig.SliderMatrix.LegRatio = tbSlideLeg.Value / (float)tbSlideLeg.Maximum;
            fig.UpdateBoneMatrices(true);
        }

        private void tbSlideWaist_ValueChanged(object sender, EventArgs e)
        {
            if (fig == null)
                return;

            fig.SliderMatrix.WaistRatio = tbSlideWaist.Value / (float)tbSlideWaist.Maximum;
            fig.UpdateBoneMatrices(true);
        }

        private void tbSlideOppai_ValueChanged(object sender, EventArgs e)
        {
            if (fig == null)
                return;

            fig.SliderMatrix.OppaiRatio = tbSlideOppai.Value / (float)tbSlideOppai.Maximum;
            fig.UpdateBoneMatrices(true);
        }

        private void tbSlideAge_ValueChanged(object sender, EventArgs e)
        {
            if (fig == null)
                return;

            fig.SliderMatrix.AgeRatio = tbSlideAge.Value / (float)tbSlideAge.Maximum;
            fig.UpdateBoneMatrices(true);
        }

        private void tbSlideEye_ValueChanged(object sender, EventArgs e)
        {
            if (fig == null)
                return;

            fig.SliderMatrix.EyeRatio = tbSlideEye.Value / (float)tbSlideEye.Maximum;
            fig.UpdateBoneMatrices(true);
        }

        private void FigureForm_DragDrop(object sender, DragEventArgs e)
        {
            if (viewer == null)
                return;

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                foreach (string src in (string[])e.Data.GetData(DataFormats.FileDrop))
                    viewer.LoadAnyFile(src, (e.KeyState & 8) == 8);
            }
        }

        private void FigureForm_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                if ((e.KeyState & 8) == 8)
                    e.Effect = DragDropEffects.Copy;
                else
                    e.Effect = DragDropEffects.Move;
            }
        }
    }
}
