namespace TSOView
{

    partial class TSOForm
    {
        /// <summary>
        /// 必要なデザイナ変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージ リソースが破棄される場合 true、破棄されない場合は false です。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                viewer.Dispose();
            }
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナで生成されたコード

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // TSOForm
            // 
            this.AllowDrop = true;
            this.ClientSize = new System.Drawing.Size(284, 263);
            this.Name = "TSOForm";
            this.Text = "TSOViewAO";
            this.ResumeLayout(false);

        }

        #endregion
    }
}
