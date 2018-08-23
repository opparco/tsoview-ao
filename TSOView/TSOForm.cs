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

            this.viewer = new Viewer();
            viewer.Windowed = tso_config.Windowed;
            viewer.DeviceSize = tso_config.DeviceSize;
            viewer.ScreenColor = tso_config.ScreenColor;
            viewer.FontColor = tso_config.FontColor;
            viewer.Ambient = tso_config.Ambient;
            viewer.HohoAlpha = tso_config.HohoAlpha;
            viewer.XRGBDepth = tso_config.XRGBDepth;
            viewer.SetDepthMapFormat(tso_config.DepthMapFormat);
            viewer.SetNormalMapFormat(tso_config.NormalMapFormat);
            viewer.SetProjectionMode(tso_config.ProjectionMode);
            viewer.SetRenderMode(tso_config.RenderMode);
            viewer.MainGel = tso_config.MainGel;
            viewer.ScreenDof = tso_config.ScreenDof;
            viewer.KeyGrabNodeDelta = tso_config.KeyGrabNodeDelta;
            viewer.KeyRotateNodeDelta = tso_config.KeyRotateNodeDelta;
            viewer.GrabNodeSpeed = tso_config.GrabNodeSpeed;
            viewer.RotateNodeSpeed = tso_config.RotateNodeSpeed;
            viewer.GrabCameraSpeed = tso_config.GrabCameraSpeed;
            viewer.RotateCameraSpeed = tso_config.RotateCameraSpeed;
            viewer.LampCenter = tso_config.LampCenter;
            viewer.LampRadius = tso_config.LampRadius;
            viewer.NodeRadius = tso_config.NodeRadius;
            viewer.SelectedNodeRadius = tso_config.SelectedNodeRadius;

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

            if (viewer.InitializeApplication(this))
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

        /// <summary>
        /// Escを押すと抜けます。
        /// </summary>
        /// <param name="e">イベント引数</param>
        protected override void OnKeyPress(System.Windows.Forms.KeyPressEventArgs e)
        {
            if ((int)(byte)e.KeyChar == (int)System.Windows.Forms.Keys.Escape)
            {
                viewer.SaveSceneToFile();
                this.Dispose(); // Esc was pressed
            }
        }
    }
}
