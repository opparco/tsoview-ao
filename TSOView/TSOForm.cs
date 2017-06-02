using System;
using System.Collections.Generic;
using System.Diagnostics;
//using System.Drawing;
//using System.Threading;
//using System.ComponentModel;
using System.Windows.Forms;
using System.IO;
using CSScriptLibrary;
using TDCG;

namespace TSOView
{

public partial class TSOForm : Form
{
    // キー入力を保持
    internal bool[] keys = new bool[256];
    internal bool[] keysEnabled = new bool[256];

    internal int keySave        = (int)Keys.Return;
    internal int keyMotion      = (int)Keys.Space;
    internal int keyMain        = (int)Keys.C;
    internal int keyAmbient     = (int)Keys.C;
    internal int keyDepthMap    = (int)Keys.Z;
    internal int keyNormalMap   = (int)Keys.X;
    internal int keyOcclusion   = (int)Keys.O;
    internal int keyDiffusion   = (int)Keys.Home;
    internal int keyShadow      = (int)Keys.S;
    internal int keyFigure      = (int)Keys.Tab;
    internal int keyDelete      = (int)Keys.Delete;
    internal int keyCameraReset = (int)Keys.D0;
    internal int keyCenter      = (int)Keys.F;
    internal int keyFigureForm = (int)Keys.G;
    internal int keyConfigForm = (int)Keys.H;

    bool record_enabled = false;
    string dest_path = @"snapshots";
    int orig_frame_idx;
    int frame_len;
    int frame_idx;

    public int RecordStep { get; set; }

    internal Viewer viewer = null;
    internal FigureForm figureForm = null;
    internal ConfigForm configForm = null;

    string save_path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\TechArts3D\TDCG";

    public TSOForm(TSOConfig tso_config, string[] args)
    {
        InitializeComponent();
        this.ClientSize = tso_config.ClientSize;
        this.RecordStep = tso_config.RecordStep;

        for (int i = 0; i < keysEnabled.Length; i++)
        {
            keysEnabled[i] = true;
        }
        this.KeyDown += new KeyEventHandler(form_OnKeyDown);
        this.KeyUp += new KeyEventHandler(form_OnKeyUp);

        this.DragDrop += new DragEventHandler(form_OnDragDrop);
        this.DragOver += new DragEventHandler(form_OnDragOver);

        this.viewer = new Viewer();
        viewer.DeviceSize = tso_config.DeviceSize;
        viewer.SetFieldOfViewY(tso_config.FieldOfViewY);
        viewer.ScreenColor = tso_config.ScreenColor;
        viewer.HohoAlpha = tso_config.HohoAlpha;
        viewer.XRGBDepth = tso_config.XRGBDepth;
        viewer.SetDepthMapFormat(tso_config.DepthMapFormat);
        viewer.SetNormalMapFormat(tso_config.NormalMapFormat);
        viewer.SetRenderMode(tso_config.RenderMode);

        this.figureForm = new FigureForm();
        this.configForm = new ConfigForm();

        DepthMapConfig depthmap_config = new DepthMapConfig();
        OcclusionConfig occlusion_config = new OcclusionConfig();
        DiffusionConfig diffusion_config = new DiffusionConfig();

        viewer.DepthMapConfig = depthmap_config;
        viewer.OcclusionConfig = occlusion_config;
        viewer.DiffusionConfig = diffusion_config;

        configForm.DepthConfig = depthmap_config;
        configForm.OcclusionConfig = occlusion_config;
        configForm.DiffusionConfig = diffusion_config;

        if (viewer.InitializeApplication(this, true))
        {
            viewer.FigureSelectEvent += delegate(object sender, EventArgs e)
            {
                Figure fig;
                if (viewer.TryGetFigure(out fig))
                    figureForm.SetFigure(fig);
                else
                    figureForm.Clear();
            };
            viewer.Camera.SetTranslation(0.0f, +10.0f, +44.0f);
            foreach (string arg in args)
                viewer.LoadAnyFile(arg, true);
            if (viewer.FigureList.Count == 0)
                viewer.LoadAnyFile(Path.Combine(save_path, "system.tdcgsav.png"), true);

            string script_file = Path.Combine(Application.StartupPath, "Script.cs");
            if (File.Exists(script_file))
            {
                string assembly_file = Path.GetTempFileName();
                var script = CSScript.Load(script_file, assembly_file, true, null).CreateInstance("TDCG.Script").AlignToInterface<IScript>();
                script.Hello(viewer);
            }

            this.timer1.Enabled = true;
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

    static float DegreeToRadian(float angle)
    {
        return (float)(Math.PI * angle / 180.0);
    }

    void SaveToPng()
    {
        switch (viewer.RenderMode)
        {
        case RenderMode.Main:
            viewer.SaveToPng("sample-ao.png");
            break;
        case RenderMode.Ambient:
            viewer.SaveToPng("sample-amb.png");
            break;
        case RenderMode.DepthMap:
            viewer.SaveToPng("sample-d.png");
            break;
        case RenderMode.NormalMap:
            viewer.SaveToPng("sample-n.png");
            break;
        case RenderMode.Occlusion:
            viewer.SaveToPng("sample-o.png");
            break;
        case RenderMode.Diffusion:
            viewer.SaveToPng("sample-df.png");
            break;
        case RenderMode.Shadow:
            viewer.SaveToPng("shadow.png");
            break;
        }
    }

    void SwitchRenderMode(RenderMode mode)
    {
        if (viewer.RenderMode == mode)
            viewer.RenderMode = RenderMode.Main;
        else
            viewer.RenderMode = mode;
    }

    public void FrameMove()
    {
        if (keysEnabled[keySave] && keys[keySave])
        {
            keysEnabled[keySave] = false;
            if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
                RecordStart();
            else
                SaveToPng();
        }
        if (keysEnabled[keyMotion] && keys[keyMotion])
        {
            keysEnabled[keyMotion] = false;
            viewer.MotionEnabled = !viewer.MotionEnabled;
        }
        if (keysEnabled[keyAmbient] && keys[keyAmbient])
        {
            keysEnabled[keyAmbient] = false;
            SwitchRenderMode(RenderMode.Ambient);
        }
        if (keysEnabled[keyDepthMap] && keys[keyDepthMap])
        {
            keysEnabled[keyDepthMap] = false;
            SwitchRenderMode(RenderMode.DepthMap);
        }
        if (keysEnabled[keyNormalMap] && keys[keyNormalMap])
        {
            keysEnabled[keyNormalMap] = false;
            SwitchRenderMode(RenderMode.NormalMap);
        }
        if (keysEnabled[keyOcclusion] && keys[keyOcclusion])
        {
            keysEnabled[keyOcclusion] = false;
            SwitchRenderMode(RenderMode.Occlusion);
        }
        if (keysEnabled[keyDiffusion] && keys[keyDiffusion])
        {
            keysEnabled[keyDiffusion] = false;
            SwitchRenderMode(RenderMode.Diffusion);
        }
        if (keysEnabled[keyShadow] && keys[keyShadow])
        {
            keysEnabled[keyShadow] = false;
            SwitchRenderMode(RenderMode.Shadow);
        }
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
        if (keysEnabled[keyCameraReset] && keys[keyCameraReset])
        {
            keysEnabled[keyCameraReset] = false;
            viewer.Camera.Reset();
            viewer.Camera.SetTranslation(0.0f, +10.0f, +44.0f);
        }
        if (keysEnabled[keyCenter] && keys[keyCenter])
        {
            keysEnabled[keyCenter] = false;
            viewer.Camera.ResetTranslation();
            Figure fig;
            if (viewer.TryGetFigure(out fig))
                viewer.Camera.SetCenter(fig.Center + fig.Translation);
        }
        if (keysEnabled[keyFigureForm] && keys[keyFigureForm])
        {
            keys[keyFigureForm] = false;
            keysEnabled[keyFigureForm] = true;
            // stale KeyUp event
            figureForm.Show();
            figureForm.Activate();
        }

        float keyL = 0.0f;
        float keyR = 0.0f;
        float keyU = 0.0f;
        float keyD = 0.0f;
        float keyPush = 0.0f;
        float keyPull = 0.0f;
        float keyZRol = 0.0f;

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
        if (keys[(int)Keys.A])
            keyZRol = -2.0f;
        if (keys[(int)Keys.D])
            keyZRol = +2.0f;

        if (Control.ModifierKeys == Keys.Shift)
        {
            Figure fig;
            if (viewer.TryGetFigure(out fig))
                fig.Move(keyR - keyL, keyU - keyD, keyPull - keyPush);
        }
        else
        {
            viewer.Camera.Move(keyR - keyL, keyU - keyD, keyPull - keyPush);
            viewer.Camera.RotZ(DegreeToRadian(keyZRol));
        }
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
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            foreach (string src in (string[])e.Data.GetData(DataFormats.FileDrop))
                viewer.LoadAnyFile(src, (e.KeyState & 8) == 8);
        }
    }

    void RecordStart()
    {
        Directory.CreateDirectory(dest_path);

        orig_frame_idx = viewer.FrameIndex;
        viewer.MotionEnabled = true;
        frame_len = viewer.GetMaxFrameLength();
        frame_idx = 0;
        record_enabled = true;
    }

    void RecordEnd()
    {
        record_enabled = false;
        viewer.MotionEnabled = false;
        viewer.FrameIndex = orig_frame_idx;
    }

    void RecordNext()
    {
        if (frame_idx < frame_len)
        {
            viewer.FrameMove(frame_idx);
            viewer.Render();
            viewer.SaveToPng(Path.Combine(dest_path, String.Format("{0:D3}.png", frame_idx)));

            frame_idx += RecordStep;
        }
        else
            RecordEnd();
    }

    private void timer1_Tick(object sender, EventArgs e)
    {
        if (record_enabled)
        {
            RecordNext();
        }
        else
        {
            this.FrameMove();
            viewer.FrameMove();
            viewer.Render();
        }
    }

    protected override void OnKeyPress(System.Windows.Forms.KeyPressEventArgs e)
    {
        if ((int)(byte)e.KeyChar == (int)System.Windows.Forms.Keys.Escape)
            this.Dispose(); // Esc was pressed
    }
}
}
