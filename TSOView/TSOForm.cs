using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using TDCG;

namespace TSOView
{

    public partial class TSOForm : Form
    {
        // キー入力を保持
        internal bool[] keys = new bool[256];
        internal bool[] keysEnabled = new bool[256];

        internal int keyFigure = (int)Keys.Tab;
        internal int keyDelete = (int)Keys.Delete;
        internal int keyCamera1 = (int)Keys.D1;
        internal int keyCamera2 = (int)Keys.D2;
        internal int keyCamera3 = (int)Keys.D3;
        internal int keyCamera4 = (int)Keys.D4;
        internal int keyCamera5 = (int)Keys.D5;
        internal int keyCenter = (int)Keys.F;
        internal int keyFigureForm = (int)Keys.G;
        internal int keyConfigForm = (int)Keys.H;
        internal int keyUndo = (int)Keys.Z;
        internal int keySave = (int)Keys.Enter;
        internal int keySprite = (int)Keys.Home;

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

            for (int i = 0; i < keysEnabled.Length; i++)
            {
                keysEnabled[i] = true;
            }
            this.KeyDown += new KeyEventHandler(form_OnKeyDown);
            this.KeyUp += new KeyEventHandler(form_OnKeyUp);

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
                viewer.TSOFileSelectEvent += delegate (object sender, EventArgs e)
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

        private void form_OnKeyDown(object sender, KeyEventArgs e)
        {
            if ((int)e.KeyCode < keys.Length)
            {
                keys[(int)e.KeyCode] = true;
            }
        }

        private void form_OnKeyUp(object sender, KeyEventArgs e)
        {
            if ((int)e.KeyCode < keys.Length)
            {
                keys[(int)e.KeyCode] = false;
                keysEnabled[(int)e.KeyCode] = true;
            }
        }

        public void Render()
        {
            if (! initialized)
                return;

            if (keysEnabled[keyConfigForm] && keys[keyConfigForm])
            {
                keys[keyConfigForm] = false;
                keysEnabled[keyConfigForm] = true;
                // stale KeyUp event
                configForm.Show();
                configForm.Activate();
            }
            if (keysEnabled[keyFigure] && keys[keyFigure])
            {
                keysEnabled[keyFigure] = false;
                viewer.NextFigure();
            }
            if (keysEnabled[keyDelete] && keys[keyDelete])
            {
                keysEnabled[keyDelete] = false;

                if (keys[(int)Keys.ControlKey])
                    viewer.ClearFigureList();
                else
                    viewer.RemoveSelectedFigure();
            }
            if (keysEnabled[keyCamera1] && keys[keyCamera1])
            {
                keysEnabled[keyCamera1] = false;
                viewer.LoadCameraPreset(1);
            }
            if (keysEnabled[keyCamera2] && keys[keyCamera2])
            {
                keysEnabled[keyCamera2] = false;
                viewer.LoadCameraPreset(2);
            }
            if (keysEnabled[keyCamera3] && keys[keyCamera3])
            {
                keysEnabled[keyCamera3] = false;
                viewer.LoadCameraPreset(3);
            }
            if (keysEnabled[keyCamera4] && keys[keyCamera4])
            {
                keysEnabled[keyCamera4] = false;
                viewer.LoadCameraPreset(4);
            }
            if (keysEnabled[keyCamera5] && keys[keyCamera5])
            {
                keysEnabled[keyCamera5] = false;
                viewer.LoadCameraPreset(5);
            }
            if (keysEnabled[keyCenter] && keys[keyCenter])
            {
                keysEnabled[keyCenter] = false;
                viewer.SetCenterToSelectedNode();
            }
            if (keysEnabled[keyFigureForm] && keys[keyFigureForm])
            {
                keys[keyFigureForm] = false;
                keysEnabled[keyFigureForm] = true;
                // stale KeyUp event
                figureForm.Show();
                figureForm.Activate();
            }
            if (keysEnabled[keyUndo] && keys[keyUndo])
            {
                keysEnabled[keyUndo] = false;
                viewer.Undo();
            }
            if (keysEnabled[keySave] && keys[keySave])
            {
                keysEnabled[keySave] = false;
                viewer.SaveSceneToFile();
            }
            if (keysEnabled[keySprite] && keys[keySprite])
            {
                keysEnabled[keySprite] = false;
                viewer.SwitchSpriteEnabled();
            }

            float keyL = 0.0f;
            float keyR = 0.0f;
            float keyU = 0.0f;
            float keyD = 0.0f;
            float keyPush = 0.0f;
            float keyPull = 0.0f;

            if (keys[(int)Keys.Left])
                keyL = 2.0f;
            if (keys[(int)Keys.Right])
                keyR = 2.0f;
            if (keys[(int)Keys.PageUp])
                keyU = 2.0f;
            if (keys[(int)Keys.PageDown])
                keyD = 2.0f;
            if (keys[(int)Keys.Up])
                keyPush = 1.0f;
            if (keys[(int)Keys.Down])
                keyPull = 1.0f;

            viewer.Camera.Move(keyR - keyL, keyU - keyD, keyPull - keyPush);
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
