using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using TDCG;

namespace TSOView
{

    public partial class TSOForm : Form
    {
        internal Viewer viewer = null;
        internal FigureForm figureForm = null;
        internal ConfigForm configForm = null;

        string save_path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\TechArts3D\TDCG";

        bool initialized = false;

        public TSOForm(TSOConfig tso_config, string[] args)
        {
            InitializeComponent();
            this.ClientSize = tso_config.ClientSize;
            if (tso_config.PseudoFullScreen)
                this.FormBorderStyle = FormBorderStyle.None;
            if (tso_config.Keying)
                this.TransparencyKey = tso_config.ScreenColor;

            this.DragDrop += new DragEventHandler(form_OnDragDrop);
            this.DragOver += new DragEventHandler(form_OnDragOver);

            this.viewer = new Viewer();
            viewer.Windowed = tso_config.Windowed;
            viewer.DeviceSize = tso_config.DeviceSize;
            viewer.ScreenColor = tso_config.ScreenColor;
            viewer.Ambient = tso_config.Ambient;
            viewer.HohoAlpha = tso_config.HohoAlpha;
            viewer.XRGBDepth = tso_config.XRGBDepth;
            viewer.SetDepthMapFormat(tso_config.DepthMapFormat);
            viewer.SetNormalMapFormat(tso_config.NormalMapFormat);
            viewer.SetProjectionMode(tso_config.ProjectionMode);
            viewer.SetRenderMode(tso_config.RenderMode);
            viewer.MainGel = tso_config.MainGel;
            viewer.ScreenDof = tso_config.ScreenDof;

            this.figureForm = new FigureForm();
            this.configForm = new ConfigForm();

            CameraConfig camera_config = new CameraConfig();
            DepthMapConfig depthmap_config = new DepthMapConfig();
            OcclusionConfig occlusion_config = new OcclusionConfig();
            DiffusionConfig diffusion_config = new DiffusionConfig();

            viewer.CameraConfig = camera_config;
            viewer.DepthMapConfig = depthmap_config;
            viewer.OcclusionConfig = occlusion_config;
            viewer.DiffusionConfig = diffusion_config;

            configForm.CameraConfig = camera_config;
            configForm.DepthMapConfig = depthmap_config;
            configForm.OcclusionConfig = occlusion_config;
            configForm.DiffusionConfig = diffusion_config;
            configForm.ConfigConnect();

            if (viewer.InitializeApplication(this, true))
            {
                viewer.ConfigConnect();
                configForm.SetViewer(viewer);
                figureForm.SetViewer(viewer);
                viewer.ConfigFormEvent += delegate (object sender, EventArgs e)
                {
                    // stale KeyUp event
                    configForm.Show();
                    configForm.Activate();
                };
                viewer.FigureFormEvent += delegate (object sender, EventArgs e)
                {
                    // stale KeyUp event
                    figureForm.Show();
                    figureForm.Activate();
                };
                viewer.TSOSelectEvent += delegate (object sender, EventArgs e)
                {
                    TSOFile tso;
                    if (viewer.TryGetTSOFile(out tso))
                        figureForm.SetTSOFile(tso);
                };
                viewer.FigureSelectEvent += delegate (object sender, EventArgs e)
                {
                    Figure fig;
                    if (viewer.TryGetFigure(out fig))
                        figureForm.SetFigure(fig);
                    else
                        figureForm.Clear();
                };
                viewer.LoadCameraPreset(1);
                foreach (string arg in args)
                    viewer.LoadAnyFile(arg, true);
                if (viewer.FigureList.Count == 0)
                    viewer.LoadAnyFile(Path.Combine(save_path, "system.tdcgsav.png"), true);
                this.initialized = true;
            }
        }

        public void Render()
        {
            if (! initialized)
                return;

            viewer.Update();
            viewer.Render();
        }

        private void form_OnDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                if ((e.KeyState & 8) == 8)
                    e.Effect = DragDropEffects.Copy;
                else
                    e.Effect = DragDropEffects.Move;
            }
        }

        private void form_OnDragDrop(object sender, DragEventArgs e)
        {
            Debug.WriteLine("enter form_OnDragDrop");

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                foreach (string src in (string[])e.Data.GetData(DataFormats.FileDrop))
                    viewer.LoadAnyFile(src, (e.KeyState & 8) == 8);
            }

            Debug.WriteLine("leave form_OnDragDrop");
        }

        protected override void OnKeyPress(System.Windows.Forms.KeyPressEventArgs e)
        {
            if ((int)(byte)e.KeyChar == (int)System.Windows.Forms.Keys.Escape)
                this.Dispose(); // Esc was pressed
        }
    }
}
