namespace TSOView
{

    partial class FigureForm
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
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナで生成されたコード

        private void InitializeComponent()
        {
            this.btnDump = new System.Windows.Forms.Button();
            this.lvTSOFiles = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.lvSubScripts = new System.Windows.Forms.ListView();
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.gvShaderParams = new System.Windows.Forms.DataGridView();
            this.tbSlideEye = new System.Windows.Forms.TrackBar();
            this.tbSlideLeg = new System.Windows.Forms.TrackBar();
            this.tbSlideArm = new System.Windows.Forms.TrackBar();
            this.tbSlideWaist = new System.Windows.Forms.TrackBar();
            this.tbSlideOppai = new System.Windows.Forms.TrackBar();
            this.lbSlideEye = new System.Windows.Forms.Label();
            this.lbSlideLeg = new System.Windows.Forms.Label();
            this.lbSlideArm = new System.Windows.Forms.Label();
            this.lbSlideWaist = new System.Windows.Forms.Label();
            this.lbSlideOppai = new System.Windows.Forms.Label();
            this.lbSlideAge = new System.Windows.Forms.Label();
            this.tbSlideAge = new System.Windows.Forms.TrackBar();
            this.btnSave = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.gvShaderParams)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbSlideEye)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbSlideLeg)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbSlideArm)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbSlideWaist)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbSlideOppai)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbSlideAge)).BeginInit();
            this.SuspendLayout();
            // 
            // btnDump
            // 
            this.btnDump.Location = new System.Drawing.Point(442, 528);
            this.btnDump.Name = "btnDump";
            this.btnDump.Size = new System.Drawing.Size(75, 23);
            this.btnDump.TabIndex = 5;
            this.btnDump.Text = "&Dump";
            this.btnDump.UseVisualStyleBackColor = true;
            this.btnDump.Click += new System.EventHandler(this.btnDump_Click);
            // 
            // lvTSOFiles
            // 
            this.lvTSOFiles.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1});
            this.lvTSOFiles.FullRowSelect = true;
            this.lvTSOFiles.GridLines = true;
            this.lvTSOFiles.HideSelection = false;
            this.lvTSOFiles.Location = new System.Drawing.Point(12, 12);
            this.lvTSOFiles.MultiSelect = false;
            this.lvTSOFiles.Name = "lvTSOFiles";
            this.lvTSOFiles.Size = new System.Drawing.Size(180, 200);
            this.lvTSOFiles.TabIndex = 0;
            this.lvTSOFiles.UseCompatibleStateImageBehavior = false;
            this.lvTSOFiles.View = System.Windows.Forms.View.Details;
            this.lvTSOFiles.SelectedIndexChanged += new System.EventHandler(this.lvTSOFiles_SelectedIndexChanged);
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Name";
            // 
            // lvSubScripts
            // 
            this.lvSubScripts.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader2,
            this.columnHeader3});
            this.lvSubScripts.FullRowSelect = true;
            this.lvSubScripts.GridLines = true;
            this.lvSubScripts.HideSelection = false;
            this.lvSubScripts.Location = new System.Drawing.Point(12, 218);
            this.lvSubScripts.MultiSelect = false;
            this.lvSubScripts.Name = "lvSubScripts";
            this.lvSubScripts.Size = new System.Drawing.Size(180, 304);
            this.lvSubScripts.TabIndex = 3;
            this.lvSubScripts.UseCompatibleStateImageBehavior = false;
            this.lvSubScripts.View = System.Windows.Forms.View.Details;
            this.lvSubScripts.SelectedIndexChanged += new System.EventHandler(this.lvSubScripts_SelectedIndexChanged);
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Name";
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "File";
            // 
            // gvShaderParams
            // 
            this.gvShaderParams.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.gvShaderParams.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gvShaderParams.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
            this.gvShaderParams.Location = new System.Drawing.Point(198, 218);
            this.gvShaderParams.Name = "gvShaderParams";
            this.gvShaderParams.RowTemplate.Height = 21;
            this.gvShaderParams.Size = new System.Drawing.Size(400, 304);
            this.gvShaderParams.TabIndex = 4;
            // 
            // tbSlideEye
            // 
            this.tbSlideEye.Location = new System.Drawing.Point(604, 473);
            this.tbSlideEye.Maximum = 20;
            this.tbSlideEye.Name = "tbSlideEye";
            this.tbSlideEye.Size = new System.Drawing.Size(168, 45);
            this.tbSlideEye.TabIndex = 18;
            this.tbSlideEye.ValueChanged += new System.EventHandler(this.tbSlideEye_ValueChanged);
            // 
            // tbSlideLeg
            // 
            this.tbSlideLeg.Location = new System.Drawing.Point(604, 269);
            this.tbSlideLeg.Maximum = 20;
            this.tbSlideLeg.Name = "tbSlideLeg";
            this.tbSlideLeg.Size = new System.Drawing.Size(168, 45);
            this.tbSlideLeg.TabIndex = 10;
            this.tbSlideLeg.ValueChanged += new System.EventHandler(this.tbSlideLeg_ValueChanged);
            // 
            // tbSlideArm
            // 
            this.tbSlideArm.Location = new System.Drawing.Point(604, 218);
            this.tbSlideArm.Maximum = 20;
            this.tbSlideArm.Name = "tbSlideArm";
            this.tbSlideArm.Size = new System.Drawing.Size(168, 45);
            this.tbSlideArm.TabIndex = 8;
            this.tbSlideArm.ValueChanged += new System.EventHandler(this.tbSlideArm_ValueChanged);
            // 
            // tbSlideWaist
            // 
            this.tbSlideWaist.Location = new System.Drawing.Point(604, 320);
            this.tbSlideWaist.Maximum = 20;
            this.tbSlideWaist.Name = "tbSlideWaist";
            this.tbSlideWaist.Size = new System.Drawing.Size(168, 45);
            this.tbSlideWaist.TabIndex = 12;
            this.tbSlideWaist.ValueChanged += new System.EventHandler(this.tbSlideWaist_ValueChanged);
            // 
            // tbSlideOppai
            // 
            this.tbSlideOppai.Location = new System.Drawing.Point(604, 371);
            this.tbSlideOppai.Maximum = 20;
            this.tbSlideOppai.Name = "tbSlideOppai";
            this.tbSlideOppai.Size = new System.Drawing.Size(168, 45);
            this.tbSlideOppai.TabIndex = 14;
            this.tbSlideOppai.ValueChanged += new System.EventHandler(this.tbSlideOppai_ValueChanged);
            // 
            // lbSlideEye
            // 
            this.lbSlideEye.AutoSize = true;
            this.lbSlideEye.Location = new System.Drawing.Point(604, 458);
            this.lbSlideEye.Name = "lbSlideEye";
            this.lbSlideEye.Size = new System.Drawing.Size(24, 12);
            this.lbSlideEye.TabIndex = 17;
            this.lbSlideEye.Text = "Eye";
            // 
            // lbSlideLeg
            // 
            this.lbSlideLeg.AutoSize = true;
            this.lbSlideLeg.Location = new System.Drawing.Point(604, 254);
            this.lbSlideLeg.Name = "lbSlideLeg";
            this.lbSlideLeg.Size = new System.Drawing.Size(23, 12);
            this.lbSlideLeg.TabIndex = 9;
            this.lbSlideLeg.Text = "Leg";
            // 
            // lbSlideArm
            // 
            this.lbSlideArm.AutoSize = true;
            this.lbSlideArm.Location = new System.Drawing.Point(605, 206);
            this.lbSlideArm.Name = "lbSlideArm";
            this.lbSlideArm.Size = new System.Drawing.Size(26, 12);
            this.lbSlideArm.TabIndex = 7;
            this.lbSlideArm.Text = "Arm";
            // 
            // lbSlideWaist
            // 
            this.lbSlideWaist.AutoSize = true;
            this.lbSlideWaist.Location = new System.Drawing.Point(604, 305);
            this.lbSlideWaist.Name = "lbSlideWaist";
            this.lbSlideWaist.Size = new System.Drawing.Size(33, 12);
            this.lbSlideWaist.TabIndex = 11;
            this.lbSlideWaist.Text = "Waist";
            // 
            // lbSlideOppai
            // 
            this.lbSlideOppai.AutoSize = true;
            this.lbSlideOppai.Location = new System.Drawing.Point(604, 356);
            this.lbSlideOppai.Name = "lbSlideOppai";
            this.lbSlideOppai.Size = new System.Drawing.Size(34, 12);
            this.lbSlideOppai.TabIndex = 13;
            this.lbSlideOppai.Text = "Oppai";
            // 
            // lbSlideAge
            // 
            this.lbSlideAge.AutoSize = true;
            this.lbSlideAge.Location = new System.Drawing.Point(604, 407);
            this.lbSlideAge.Name = "lbSlideAge";
            this.lbSlideAge.Size = new System.Drawing.Size(25, 12);
            this.lbSlideAge.TabIndex = 15;
            this.lbSlideAge.Text = "Age";
            // 
            // tbSlideAge
            // 
            this.tbSlideAge.Location = new System.Drawing.Point(604, 422);
            this.tbSlideAge.Maximum = 20;
            this.tbSlideAge.Name = "tbSlideAge";
            this.tbSlideAge.Size = new System.Drawing.Size(168, 45);
            this.tbSlideAge.TabIndex = 16;
            this.tbSlideAge.ValueChanged += new System.EventHandler(this.tbSlideAge_ValueChanged);
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(523, 528);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(75, 23);
            this.btnSave.TabIndex = 6;
            this.btnSave.Text = "&Save";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // FigureForm
            // 
            this.AllowDrop = true;
            this.ClientSize = new System.Drawing.Size(784, 563);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.lbSlideEye);
            this.Controls.Add(this.tbSlideEye);
            this.Controls.Add(this.lbSlideAge);
            this.Controls.Add(this.tbSlideAge);
            this.Controls.Add(this.lbSlideOppai);
            this.Controls.Add(this.lbSlideWaist);
            this.Controls.Add(this.lbSlideArm);
            this.Controls.Add(this.lbSlideLeg);
            this.Controls.Add(this.tbSlideOppai);
            this.Controls.Add(this.tbSlideWaist);
            this.Controls.Add(this.tbSlideArm);
            this.Controls.Add(this.tbSlideLeg);
            this.Controls.Add(this.gvShaderParams);
            this.Controls.Add(this.lvSubScripts);
            this.Controls.Add(this.lvTSOFiles);
            this.Controls.Add(this.btnDump);
            this.Name = "FigureForm";
            this.Text = "TSOGrid";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FigureForm_FormClosing);
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.FigureForm_DragDrop);
            this.DragOver += new System.Windows.Forms.DragEventHandler(this.FigureForm_DragOver);
            ((System.ComponentModel.ISupportInitialize)(this.gvShaderParams)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbSlideEye)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbSlideLeg)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbSlideArm)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbSlideWaist)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbSlideOppai)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbSlideAge)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnDump;
        private System.Windows.Forms.ListView lvTSOFiles;
        private System.Windows.Forms.ListView lvSubScripts;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.TrackBar tbSlideEye;
        private System.Windows.Forms.TrackBar tbSlideLeg;
        private System.Windows.Forms.TrackBar tbSlideArm;
        private System.Windows.Forms.TrackBar tbSlideWaist;
        private System.Windows.Forms.TrackBar tbSlideOppai;
        private System.Windows.Forms.Label lbSlideEye;
        private System.Windows.Forms.Label lbSlideLeg;
        private System.Windows.Forms.Label lbSlideArm;
        private System.Windows.Forms.Label lbSlideWaist;
        private System.Windows.Forms.Label lbSlideOppai;
        private System.Windows.Forms.Label lbSlideAge;
        private System.Windows.Forms.TrackBar tbSlideAge;
        private System.Windows.Forms.DataGridView gvShaderParams;
        private System.Windows.Forms.Button btnSave;
    }
}
