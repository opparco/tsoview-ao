using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Threading;
using System.ComponentModel;
using System.Windows.Forms;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using Direct3D = Microsoft.DirectX.Direct3D;

namespace TDCG.Editor
{
    /// 射影 mode
    public enum ProjectionMode
    {
        /// 正射影
        Ortho,
        /// 透視射影
        Perspective
    };

    /// 描画 mode
    public enum RenderMode
    {
        /// main
        Main,
        /// 環境光
        Ambient,
        /// 深度マップ
        DepthMap,
        /// 法線マップ
        NormalMap,
        /// occlusion
        Occlusion,
        /// diffusion
        Diffusion,
        /// 影絵
        Shadow
    };

    /// <summary>
    /// TSOFileをDirect3D上でレンダリングします。
    /// </summary>
    public class Viewer : IDisposable
    {
        /// <summary>
        /// control
        /// </summary>
        protected Control control;

        /// <summary>
        /// device
        /// </summary>
        protected Device device;

        /// <summary>
        /// effect
        /// </summary>
        protected Effect effect;
        Effect effect_clear;
        Effect effect_ao;
        Effect effect_gb;
        Effect effect_main;
        Effect effect_screen;

        /// <summary>
        /// toonshader.cgfx に渡す頂点宣言
        /// </summary>
        protected VertexDeclaration vd;

        /// <summary>
        /// effect handle for LocalBoneMats
        /// since v0.90
        /// </summary>
        protected EffectHandle handle_LocalBoneMats;

        /// <summary>
        /// effect handle for LightDirForced
        /// since v0.90
        /// </summary>
        protected EffectHandle handle_LightDirForced;

        /// <summary>
        /// effect handle for Ambient
        /// since v0.90
        /// </summary>
        protected EffectHandle handle_Ambient;

        /// <summary>
        /// effect handle for HohoAlpha
        /// since v0.90
        /// </summary>
        protected EffectHandle handle_HohoAlpha;

        /// <summary>
        /// effect handle for UVSCR
        /// since v0.91
        /// </summary>
        protected EffectHandle handle_UVSCR;

        ToonShader toon_shader = null;
        Screen screen = null;
        Sprite sprite = null;
        DepthNormalMapRenderer dnmap_renderer = null;
        LampRenderer lamp_renderer = null;
        NodeRenderer node_renderer = null;
        SpriteCellRenderer sprite_cell_renderer = null;
        SpriteRenderer sprite_renderer = null;
        NodeFilter node_filter = null;

        protected Direct3D.Font font = null;

        /// surface of device
        protected Surface dev_surface = null;
        /// zbuffer of device
        protected Surface dev_zbuf = null;

        /// config:
        public bool Windowed { get; set; }

        /// config: BackBufferWidth BackBufferHeight
        public Size DeviceSize { get; set; }

        float fieldOfViewY;

        /// config: 視野角を設定します。単位は degree です。
        public void SetFieldOfViewY(float fieldOfViewY)
        {
            this.fieldOfViewY = Geometry.DegreeToRadian(fieldOfViewY);
        }

        RenderMode render_mode = RenderMode.Main;

        /// 描画 mode
        public RenderMode RenderMode
        {
            get { return render_mode; }
            set
            {
                render_mode = value;
                need_render = true;
            }
        }

        /// カメラ設定を保持する
        public CameraConfig CameraConfig = null;
        /// 深度マップ設定を保持する
        public DepthMapConfig DepthMapConfig = null;
        /// occlusion 設定を保持する
        public OcclusionConfig OcclusionConfig = null;
        /// diffusion 設定を保持する
        public DiffusionConfig DiffusionConfig = null;

        NormalMapContainer nmap_container;
        EnvironmentMapContainer emap_container;

        Texture amb_texture;
        Texture randommap_texture;
        Texture occ_texture;
        Texture tmp_texture;

        Surface amb_surface;
        Surface occ_surface;
        Surface tmp_surface;

        /// zbuffer of render target
        Surface tex_zbuf;

        ModelThumbnail model_thumbnail = new ModelThumbnail();
        SceneThumbnail scene_thumbnail = new SceneThumbnail();

        SimpleCamera camera = new SimpleCamera();

        /// <summary>
        /// カメラ
        /// </summary>
        public SimpleCamera Camera { get { return camera; } set { camera = value; } }

        /// <summary>
        /// viewerが保持しているフィギュアリスト
        /// </summary>
        public List<Figure> FigureList = new List<Figure>();

        /// <summary>
        /// マウスポイントしているスクリーン座標
        /// </summary>
        protected Point lastScreenPoint = Point.Empty;

        /// <summary>
        /// viewerを生成します。
        /// </summary>
        public Viewer()
        {
            Windowed = true;
            DeviceSize = new Size(0, 0);
            SetFieldOfViewY(30.0f);
            ScreenColor = Color.LightGray;
            FontColor = Color.Black;
            Ambient = new Vector4(1, 1, 1, 1);
            HohoAlpha = 1.0f;
            XRGBDepth = true;
            MainGel = false;
            ScreenDof = false;
            KeyGrabNodeDelta = 2;
            KeyRotateNodeDelta = 2;
            GrabNodeSpeed = 0.0125f;
            RotateNodeSpeed = 0.0125f;
            GrabCameraSpeed = 0.125f;
            RotateCameraSpeed = 0.01f;
            LampCenter = new Point(944, 96);
            LampRadius = 18;
            NodeRadius = 6;
            SelectedNodeRadius = 18;

            manipulator = new Manipulator(camera);
            command_man = new CommandManager();
        }

        int swap_row = -1;
        int swap_idx = -1;

        TMONode selected_node = null;

        public void SetCenterToSelectedNode()
        {
            camera.ResetTranslation();

            Figure fig;
            if (TryGetFigure(out fig))
            {
                Matrix world;
                fig.GetWorldMatrix(out world);

                TMONode node = selected_node;

                if (node == null)
                    node = fig.Tmo.nodes[0]; // W_Hips

                camera.Center = Vector3.TransformCoordinate(node.GetWorldPosition(), world);
            }
        }

        bool CloseToLamp(Point location)
        {
            Figure fig;
            if (TryGetFigure(out fig))
            {
                int dx = location.X - lamp_center_on_device.X;
                int dy = location.Y - lamp_center_on_device.Y;

                float radius = lamp_radius_on_device;

                return (dx * dx + dy * dy < radius * radius);
            }
            return false;
        }

        // 指定位置の近傍に selected_node があるか
        // location 座標系: device 生成時の screen 座標系
        bool CloseToSelectedNode(Point location)
        {
            if (selected_node == null)
                return false;

            Figure fig;
            if (TryGetFigure(out fig))
            {
                Matrix world;
                fig.GetWorldMatrix(out world);

                Vector3 position = Vector3.TransformCoordinate(selected_node.GetWorldPosition(), world);
                Vector3 screen_position = WorldToScreen(position);

                int dx = location.X - (int)screen_position.X;
                int dy = location.Y - (int)screen_position.Y;

                float radius = selected_node_radius_on_device;

                return (dx * dx + dy * dy < radius * radius);
            }
            return false;
        }

        // 指定位置の node を selected_node に設定する
        // なければ false を返す
        // location 座標系: device 生成時の screen 座標系
        bool SelectNode(Point location)
        {
            Figure fig;
            if (TryGetFigure(out fig))
            {
                Matrix world;
                fig.GetWorldMatrix(out world);

                Dictionary<TMONode, float> close_nodes = new Dictionary<TMONode, float>();

                foreach (TMONode node in GetDrawableNodes(fig.Tmo))
                {
                    Vector3 position = Vector3.TransformCoordinate(node.GetWorldPosition(), world);

                    if (Vector3.TransformCoordinate(position, Transform_View).Z >-1.0f)
                        continue;

                    Vector3 screen_position = WorldToScreen(position);

                    int dx = location.X - (int)screen_position.X;
                    int dy = location.Y - (int)screen_position.Y;

                    float radius = node_radius_on_device;

                    if (dx * dx + dy * dy < radius * radius)
                    {
                        // 近傍なら候補に入れる
                        close_nodes[node] = screen_position.Z;
                    }
                }

                if (close_nodes.Count == 0)
                {
                    return false;
                }

                // 近傍のうち最小z値を持つnodeを選択する
                float min_z = 1.0f;

                foreach (var pair in close_nodes)
                {
                    TMONode node = pair.Key;
                    float z = pair.Value;

                    if (z < min_z)
                    {
                        min_z = z;
                        selected_node = node;
                    }
                }
                return true;
            }
            return false;
        }

        float ScaleToScreen(int x)
        {
            Size client_size = control.ClientSize;
            return x * dev_rect.Width / client_size.Width;
        }

        void ScaleToScreen(ref Point location)
        {
            Size client_size = control.ClientSize;
            location.X = location.X * dev_rect.Width / client_size.Width;
            location.Y = location.Y * dev_rect.Height / client_size.Height;
        }

        void ScaleToSprite(ref Point location)
        {
            Size client_size = control.ClientSize;
            location.X = location.X * 1024 / client_size.Width;
            location.Y = location.Y * 768 / client_size.Height;
        }

        void SwapTSORow(int arow, int brow)
        {
            Figure fig;
            if (TryGetFigure(out fig))
            {
                TSOFile a = null;
                TSOFile b = null;
                foreach (TSOFile tso in fig.TsoList)
                {
                    if (arow == tso.Row)
                        a = tso;
                    if (brow == tso.Row)
                        b = tso;
                }
                if (a != null && b != null)
                {
                    // swap tso.Row
                    byte tmp = a.Row;
                    a.Row = b.Row;
                    b.Row = tmp;
                }
                else if (a != null)
                {
                    a.Row = (byte)brow;
                }
                else if (b != null)
                {
                    b.Row = (byte)arow;
                }
                fig.TsoList.Sort();
            }
        }

        void SwapFigureIdx(int aidx, int bidx)
        {
            Figure a = null;
            Figure b = null;
            int idx = 0;
            foreach (Figure fig in FigureList)
            {
                if (aidx == idx)
                    a = fig;
                if (bidx == idx)
                    b = fig;
                idx++;
            }
            if (a != null && b != null)
            {
                FigureList[aidx] = b;
                FigureList[bidx] = a;
            }
            else if (a != null)
            {
                FigureList.RemoveAt(aidx); // Remove(a)
                FigureList.Add(a);
            }
            else if (b != null)
            {
                FigureList.RemoveAt(bidx); // Remove(b)
                FigureList.Add(b);
            }
        }

        void DoSpriteOnLMB()
        {
            string modename = sprite_renderer.CurrentModeName;
            if (modename == "MODEL")
            {
                if (swap_row != -1)
                {
                    int row = sprite_renderer.model_mode.SelectedIdx;
                    if (row != swap_row)
                        SwapTSORow(swap_row, row);
                    swap_row = -1;
                }
                if (TSOSelectEvent != null)
                    TSOSelectEvent(this, EventArgs.Empty);
            }
            if (modename == "POSE")
            {
                string nodename = sprite_renderer.pose_mode.SelectedNodeName;
                if (nodename != null)
                {
                    Figure fig;
                    if (TryGetFigure(out fig))
                        selected_node = fig.Tmo.FindNodeByName(nodename);
                }
            }
            if (modename == "SCENE")
            {
                if (swap_idx != -1)
                {
                    int idx = sprite_renderer.scene_mode.SelectedIdx;
                    if (idx != swap_idx)
                        SwapFigureIdx(swap_idx, idx);
                    swap_idx = -1;
                }
                if (FigureSelectEvent != null)
                    FigureSelectEvent(this, EventArgs.Empty);
            }
        }

        void DoSpriteOnRMB()
        {
            string modename = sprite_renderer.CurrentModeName;
            if (modename == "MODEL")
            {
                swap_row = sprite_renderer.model_mode.SelectedIdx;
            }
            if (modename == "SCENE")
            {
                swap_idx = sprite_renderer.scene_mode.SelectedIdx;
            }
        }

        bool camera_manipulated;

        /// マウスボタンを押したときに実行するハンドラ
        protected virtual void form_OnMouseDown(object sender, MouseEventArgs e)
        {
            Debug.WriteLine("enter form_OnMouseDown");

            //device 生成時の screen 座標系に変換する
            Point screen_p = new Point(e.X, e.Y);
            ScaleToScreen(ref screen_p);

            //sprite 座標系に変換する
            Point sprite_p = new Point(e.X, e.Y);
            ScaleToSprite(ref sprite_p);

            switch (e.Button)
            {
                case MouseButtons.Left:
                    Debug.WriteLine("form_OnMouseDown LMB");

                    if (sprite_enabled && sprite_renderer.Update(sprite_p))
                    {
                        DoSpriteOnLMB();
                        need_render = true;
                    }
                    else if (CloseToLamp(screen_p))
                    {
                        Figure fig;
                        if (TryGetFigure(out fig))
                            manipulator.BeginRotateLamp(fig);
                    }
                    else if (CloseToSelectedNode(screen_p))
                    {
                        if (selected_node != null)
                        {
                            BeginSelectedNodeCommand();
                            manipulator.BeginRotateNode(ManipulatorDeviceType.Mouse, selected_node);
                        }
                    }
                    else if (SelectNode(screen_p))
                    {
                        sprite_renderer.pose_mode.SelectedNodeName = selected_node.Name;
                        need_render = true;
                    }
                    else
                        manipulator.BeginRotateCamera();
                    break;
                case MouseButtons.Middle:
                    manipulator.BeginGrabCamera();
                    camera_manipulated = false;
                    break;
                case MouseButtons.Right:
                    Debug.WriteLine("form_OnMouseDown RMB");

                    if (sprite_enabled && sprite_renderer.Update(sprite_p))
                    {
                        DoSpriteOnRMB();
                        need_render = true;
                    }
                    else if (CloseToSelectedNode(screen_p))
                    {
                        if (selected_node != null)
                        {
                            BeginSelectedNodeCommand();
                            manipulator.BeginGrabNode(ManipulatorDeviceType.Mouse, selected_node);
                        }
                    }
                    else
                        manipulator.BeginGrabCamera();
                    break;
            }

            lastScreenPoint = screen_p;

            Debug.WriteLine("leave form_OnMouseDown");
        }

        /// マウスを移動したときに実行するハンドラ
        protected virtual void form_OnMouseMove(object sender, MouseEventArgs e)
        {
            //device 生成時の screen 座標系に変換する
            Point screen_p = new Point(e.X, e.Y);
            ScaleToScreen(ref screen_p);

            int dx = screen_p.X - lastScreenPoint.X;
            int dy = screen_p.Y - lastScreenPoint.Y;

            switch (e.Button)
            {
                case MouseButtons.Left:
                    if (manipulator.WhileRotateLamp(dx, dy))
                        need_render = true;
                    if (manipulator.WhileRotateNode(dx, dy))
                    {
                        //TODO: UpdateSelectedBoneMatrices
                        GetSelectedFigure().UpdateBoneMatrices();
                    }
                    manipulator.WhileRotateCamera(dx, dy);
                    break;
                case MouseButtons.Middle:
                    camera_manipulated = manipulator.WhileGrabCamera(dx, dy);
                    break;
                case MouseButtons.Right:
                    if (manipulator.WhileGrabNode(dx, dy))
                    {
                        //TODO: UpdateSelectedBoneMatrices
                        GetSelectedFigure().UpdateBoneMatrices();
                    }
                    manipulator.WhileGrabCameraDepth(dx, dy);
                    break;
            }

            lastScreenPoint = screen_p;
        }

        protected virtual void form_OnMouseUp(object sender, MouseEventArgs e)
        {
            Debug.WriteLine("enter form_OnMouseUp");

            switch (e.Button)
            {
                case MouseButtons.Left:
                    manipulator.EndRotateLamp();
                    if (manipulator.EndRotateNode(ManipulatorDeviceType.Mouse))
                        EndSelectedNodeCommand();
                    manipulator.EndRotateCamera();
                    break;
                case MouseButtons.Middle:
                    manipulator.EndGrabCamera();
                    if (!camera_manipulated)
                    {
                        manipulator.NodePower = 0.0f;
                        need_render = true; // draw manipulator.NodePower
                    }
                    break;
                case MouseButtons.Right:
                    if (manipulator.EndGrabNode(ManipulatorDeviceType.Mouse))
                        EndSelectedNodeCommand();
                    manipulator.EndGrabCamera();
                    break;
            }

            Debug.WriteLine("leave form_OnMouseUp");
        }

        protected virtual void form_OnMouseWheel(object sender, MouseEventArgs e)
        {
            Debug.WriteLine("enter form_OnMouseWheel");

            const float delta_scale = 1.0f / (120.0f * 4.0f);
            manipulator.NodePower += e.Delta * delta_scale;
            need_render = true; // draw manipulator.NodePower

            Debug.WriteLine("leave form_OnMouseWheel");
        }

        protected virtual void form_OnDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                if ((e.KeyState & 8) == 8)
                    e.Effect = DragDropEffects.Copy;
                else
                    e.Effect = DragDropEffects.Move;
            }
        }

        protected virtual void form_OnDragDrop(object sender, DragEventArgs e)
        {
            Debug.WriteLine("enter form_OnDragDrop");

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                foreach (string src in (string[])e.Data.GetData(DataFormats.FileDrop))
                    this.LoadAnyFile(src, (e.KeyState & 8) == 8);
            }

            Debug.WriteLine("leave form_OnDragDrop");
        }

        // コントロールのサイズを変更したときに実行するハンドラ
        protected virtual void form_OnResize(object sender, EventArgs e)
        {
            need_render = true;
        }

        NodeCommand node_command = null;

        public void BeginSelectedNodeCommand()
        {
            node_command = new NodeCommand(GetSelectedFigure(), selected_node.Name);
        }

        public void EndSelectedNodeCommand()
        {
            if (node_command != null)
                command_man.Execute(node_command);

            node_command = null;
        }

        PoseCommand pose_command = null;

        public void BeginSelectedFigurePoseCommand()
        {
            pose_command = new PoseCommand(GetSelectedFigure());
        }

        public void EndSelectedFigurePoseCommand()
        {
            if (pose_command != null)
                command_man.Execute(pose_command);

            pose_command = null;
        }

        public static string GetModelThumbnailFileName()
        {
            return CombineStartupPath(@"model.tdcgsav\thumbnail.png");
        }

        public static string GetSceneThumbnailFileName()
        {
            return CombineStartupPath(@"scene.tdcgpose\thumbnail.png");
        }

        public static string GetModelFileName()
        {
            DateTime ti = DateTime.Now;
            CultureInfo ci = CultureInfo.InvariantCulture;
            string ti_string = ti.ToString("yyyyMMdd-HHmmss-fff", ci);
            return ti_string + ".tdcgsav.png";
        }

        public static string GetSceneFileName()
        {
            DateTime ti = DateTime.Now;
            CultureInfo ci = CultureInfo.InvariantCulture;
            string ti_string = ti.ToString("yyyyMMdd-HHmmss-fff", ci);
            return ti_string + ".tdcgpose.png";
        }

        public void SaveModelToFile()
        {
            Figure fig = GetSelectedFigure();
            if (fig == null)
                return;

            PNGSaveData savedata = new PNGSaveData();

            savedata.figures = new Figure[1];
            savedata.figures[0] = fig;

            string thumbnail_file = GetModelThumbnailFileName();
            string dest_file = GetModelFileName();


            Blit(dev_surface, tmp_surface); // from:dev to:tmp
            //TODO: fig.Tmo = SnapShotPose.tmo

            Color bg_col = Color.FromArgb(136, 255, 156); // MODEL
            DrawFigure(fig, bg_col);
            model_thumbnail.Snap(dev_surface);
            model_thumbnail.SaveToFile(thumbnail_file);
            {
                BMPSaveData data = new BMPSaveData();

                data.Load(thumbnail_file);
                for (int i = 0; i < 32; i++)
                {
                    data.SetFileName(i, "(none)");
                }
                SliderMatrix slider_matrix = fig.SliderMatrix;
                data.SetSliderValue(0, slider_matrix.OppaiRatio);
                data.SetBytes(1, BitConverter.GetBytes((uint)3));
                data.SetBytes(2, BitConverter.GetBytes((uint)0x39AA5963));
                //3: key1
                data.SetSliderValue(4, slider_matrix.AgeRatio);
                data.SetSliderValue(5, slider_matrix.ArmRatio);
                data.SetSliderValue(6, slider_matrix.LegRatio);
                data.SetSliderValue(7, slider_matrix.WaistRatio);
                data.SetSliderValue(8, slider_matrix.EyeRatio);
                data.SetBytes(9, BitConverter.GetBytes((uint)0x31415926));
                //10: key2
                data.SetSliderValue(11, 0.5f); // やわらか
                data.SetBytes(12, BitConverter.GetBytes((uint)0x17320508));
                //13: key3
                data.Save(thumbnail_file);
            }

            //TODO: fig.Tmo = orig.tmo
            Blit(tmp_surface, dev_surface); // from:tmp to:dev

            PNGModelSaveWriter writer = new PNGModelSaveWriter();
            writer.Save(thumbnail_file, dest_file, savedata);
        }

        public void SavePoseToFile()
        {
            Figure fig = GetSelectedFigure();
            if (fig == null)
                return;

            PNGSaveData savedata = new PNGSaveData();

            PNGSaveCameraDescription camera_desc = new PNGSaveCameraDescription();
            camera_desc.Translation = camera.ViewTranslation;
            camera_desc.Angle = camera.Angle;
            savedata.CameraDescription = camera_desc;

            savedata.figures = new Figure[1];
            savedata.figures[0] = fig;

            string thumbnail_file = GetSceneThumbnailFileName();
            string dest_file = GetSceneFileName();

            Blit(dev_surface, tmp_surface); // from:dev to:tmp

            Color bg_col = Color.FromArgb(251, 198, 198); // POSE
            DrawFigure(fig, bg_col);
            scene_thumbnail.Snap(dev_surface);
            scene_thumbnail.SaveToFile(thumbnail_file);

            Blit(tmp_surface, dev_surface); // from:tmp to:dev

            PNGPoseSaveWriter writer = new PNGPoseSaveWriter();
            writer.Save(thumbnail_file, dest_file, savedata);
        }

        public void SaveSceneToFile()
        {
            PNGSaveData savedata = new PNGSaveData();

            PNGSaveCameraDescription camera_desc = new PNGSaveCameraDescription();
            camera_desc.Translation = camera.ViewTranslation;
            camera_desc.Angle = camera.Angle;
            savedata.CameraDescription = camera_desc;

            savedata.figures = new Figure[FigureList.Count];
            FigureList.CopyTo(savedata.figures);

            string thumbnail_file = GetSceneThumbnailFileName();
            string dest_file = GetSceneFileName();

            Blit(dev_surface, tmp_surface); // from:dev to:tmp

            Color bg_col = Color.FromArgb(253, 218, 112); // SCENE
            DrawFigureList(bg_col);
            scene_thumbnail.Snap(dev_surface);
            scene_thumbnail.SaveToFile(thumbnail_file);

            Blit(tmp_surface, dev_surface); // from:tmp to:dev

            PNGSceneSaveWriter writer = new PNGSceneSaveWriter();
            writer.Save(thumbnail_file, dest_file, savedata);
        }

        /// <summary>
        /// controlを保持します。
        /// </summary>
        /// <param name="control">control</param>
        protected void SetControl(Control control)
        {
            this.control = control;
        }

        /// <summary>
        /// 任意のファイルを読み込みます。
        /// </summary>
        /// <param name="source_file">任意のパス</param>
        public void LoadAnyFile(string source_file)
        {
            LoadAnyFile(source_file, false);
        }

        /// <summary>
        /// 任意のファイルを読み込みます。
        /// </summary>
        /// <param name="source_file">任意のパス</param>
        /// <param name="append">FigureListを消去せずに追加するか</param>
        public void LoadAnyFile(string source_file, bool append)
        {
            switch (Path.GetExtension(source_file).ToLower())
            {
                case ".tso":
                    LoadTSOFile(source_file, append);
                    break;
                case ".tmo":
                    LoadTMOFile(source_file);
                    break;
                case ".png":
                    LoadPNGFile(source_file, append);
                    break;
                default:
                    if (!append)
                        ClearFigureList();
                    if (Directory.Exists(source_file))
                        AddFigureFromTSODirectory(source_file);
                    break;
            }
        }

        /// <summary>
        /// tso 選択時に呼び出されるハンドラ
        /// </summary>
        public event EventHandler TSOSelectEvent;

        /// <summary>
        /// フィギュア選択時に呼び出されるハンドラ
        /// </summary>
        public event EventHandler FigureSelectEvent;

        /// <summary>
        /// ConfigForm を起動するために呼び出されるハンドラ
        /// </summary>
        public event EventHandler ConfigFormEvent;

        /// <summary>
        /// FigureForm を起動するために呼び出されるハンドラ
        /// </summary>
        public event EventHandler FigureFormEvent;

        /// <summary>
        /// フィギュアを選択します。
        /// @event: FigureSelectEvent
        /// </summary>
        /// <param name="idx">フィギュア番号</param>
        public void SetFigureIdx(int idx)
        {
            if (idx < 0)
                idx = 0;
            if (FigureList.Count != 0 && idx >= Math.Min(FigureList.Count, 24))
                idx = Math.Min(FigureList.Count, 24) - 1;

            sprite_renderer.scene_mode.SelectedIdx = idx;

            if (FigureSelectEvent != null)
                FigureSelectEvent(this, EventArgs.Empty);
        }

        /// <summary>
        /// 指定ディレクトリからフィギュアを作成して追加します。
        /// </summary>
        /// <param name="source_file">TSOFileを含むディレクトリ</param>
        public void AddFigureFromTSODirectory(string source_file)
        {
            List<TSOFile> tso_list = new List<TSOFile>();
            try
            {
                string[] files = Directory.GetFiles(source_file, "*.tso");
                foreach (string file in files)
                {
                    TSOFile tso = new TSOFile();
                    Debug.WriteLine("loading " + file);
                    tso.Load(file);
                    tso_list.Add(tso);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex);
            }
            Figure fig = new Figure();
            foreach (TSOFile tso in tso_list)
                fig.TsoList.Add(tso);

            int len = FigureList.Count;
            //todo: override List#Add
            FigureList.Add(fig);
            fig.UpdateBoneMatricesEvent += delegate (object sender, EventArgs e)
            {
                if (GetSelectedFigure() == sender)
                    need_render = true;
            };
            fig.UpdateClothed();
            fig.UpdateNodeMapAndBoneMatrices();

            // fire FigureSelectEvent
            SetFigureIdx(len);
        }

        /// <summary>
        /// 選択フィギュアを得ます。
        /// </summary>
        public Figure GetSelectedFigure()
        {
            int len = FigureList.Count;

            if (len == 0)
                return null;

            Figure fig;
            int idx = sprite_renderer.scene_mode.SelectedIdx;
            if (idx < len)
                fig = FigureList[idx];
            else
                fig = null;
            return fig;
        }

        /// <summary>
        /// 指定パスからTSOFileを読み込みます。
        /// </summary>
        /// <param name="source_file">パス</param>
        public void LoadTSOFile(string source_file, bool append)
        {
            Debug.WriteLine("loading " + source_file);
            using (Stream source_stream = File.OpenRead(source_file))
                LoadTSOFile(source_stream, source_file, append);
        }

        /// <summary>
        /// 指定ストリームからTSOFileを読み込みます。
        /// </summary>
        /// <param name="source_stream">ストリーム</param>
        public void LoadTSOFile(Stream source_stream)
        {
            LoadTSOFile(source_stream, null);
        }

        static int DetectRowFromFileName(string file)
        {
            int row = 0x19;
            string basename = Path.GetFileNameWithoutExtension(file);
            switch (basename.Length)
            {
                case 2:
                    //ex. filename = "00"
                    int num;
                    if (int.TryParse(basename, NumberStyles.AllowHexSpecifier, null, out num) && num < 0x1E)
                        row = num;
                    break;
                case 12:
                    //ex. filename = "N001BODY_A00"
                    //A: 0x00 .. Z: 0x19
                    //0: 0x1A .. 3: 0x1D
                    char c = basename[9];
                    byte b = (byte)c;
                    //Console.WriteLine("b: {0:X2}", b);
                    if (b >= 0x41 && b < 0x5B)
                        row = b - 0x41;
                    else if (b >= 0x30 && b < 0x34)
                        row = b - 0x30 + 0x1A;
                    break;
            }
            return row;
        }

        //同じrowを持つtsoを置き換える。
        //なければ追加する。
        void ChangeTsoSameRow(Figure fig, TSOFile tso)
        {
            if (fig.TsoList.Count == 0)
            {
                fig.TsoList.Add(tso);
            }
            else
            {
                byte row = tso.Row;
                int i = 0;
                foreach (TSOFile _tso in fig.TsoList)
                {
                    if (row == _tso.Row)
                    {
                        fig.TsoList[i] = tso;
                        fig.TsoList.Sort();
                        return;
                    }
                    i++;
                }
                fig.TsoList.Add(tso);
            }
        }

        /// <summary>
        /// 指定ストリームからTSOFileを読み込みます。
        /// </summary>
        /// <param name="source_stream">ストリーム</param>
        /// <param name="file">ファイル名</param>
        public void LoadTSOFile(Stream source_stream, string file, bool append = false)
        {
            TSOFile tso = new TSOFile();
            try
            {
                tso.Load(source_stream);
                tso.Row = (byte)DetectRowFromFileName(file);
                tso.FileName = file != null ? Path.GetFileNameWithoutExtension(file) : null;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex);
            }

            Figure fig = GetSelectedFigure();
            int idx = sprite_renderer.scene_mode.SelectedIdx;

            if (!append && fig != null)
            {
                ChangeTsoSameRow(fig, tso);
            }
            else
            {
                fig = new Figure();
                idx = FigureList.Count;
                //todo: override List#Add
                FigureList.Add(fig);
                fig.UpdateBoneMatricesEvent += delegate (object sender, EventArgs e)
                {
                    if (GetSelectedFigure() == sender)
                        need_render = true;
                };
                fig.TsoList.Add(tso);
            }

            fig.UpdateClothed();
            fig.UpdateNodeMapAndBoneMatrices();

            SetFigureIdx(idx);
        }

        /// <summary>
        /// 選択 tso を得ます。
        /// </summary>
        public bool TryGetTSOFile(out TSOFile tso)
        {
            tso = null;
            Figure fig;
            if (TryGetFigure(out fig))
            {
                int len = fig.TsoList.Count;

                if (len == 0)
                    return false;

                int row = sprite_renderer.model_mode.SelectedIdx;
                foreach (TSOFile _tso in fig.TsoList)
                {
                    if (row == _tso.Row)
                    {
                        tso = _tso;
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 選択フィギュアを得ます。
        /// </summary>
        public bool TryGetFigure(out Figure fig)
        {
            fig = null;
            int len = FigureList.Count;

            if (len == 0)
                return false;

            int idx = sprite_renderer.scene_mode.SelectedIdx;
            if (idx < len)
                fig = FigureList[idx];

            return fig != null;
        }

        /// 次のフィギュアを選択します。
        public void NextFigure()
        {
            int idx = sprite_renderer.scene_mode.SelectedIdx;
            idx++;
            if (idx >= Math.Min(FigureList.Count, 24))
                idx = 0;
            SetFigureIdx(idx);
        }

        /// <summary>
        /// 指定パスからTMOFileを読み込みます。
        /// </summary>
        /// <param name="source_file">パス</param>
        public void LoadTMOFile(string source_file)
        {
            using (Stream source_stream = File.OpenRead(source_file))
                LoadTMOFile(source_stream);
        }

        /// <summary>
        /// 指定ストリームからTMOFileを読み込みます。
        /// </summary>
        /// <param name="source_stream">ストリーム</param>
        public void LoadTMOFile(Stream source_stream)
        {
            Figure fig;
            if (TryGetFigure(out fig))
            {
                BeginSelectedFigurePoseCommand();
                try
                {
                    TMOFile tmo = new TMOFile();
                    tmo.Load(source_stream);
                    fig.Tmo = tmo;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex);
                }
                fig.UpdateNodeMapAndBoneMatrices();

                EndSelectedFigurePoseCommand();

                // fire FigureSelectEvent
                SetFigureIdx(sprite_renderer.scene_mode.SelectedIdx);
            }
        }

        static void Dump(ref Vector3 v)
        {
                    Console.WriteLine("{0:F4} {1:F4} {2:F4}", v.X, v.Y, v.Z);
        }

        /// <summary>
        /// 指定パスからPNGFileを読み込みます。
        /// </summary>
        /// <param name="source_file">PNGFile のパス</param>
        /// <param name="append">FigureListを消去せずに追加するか</param>
        public void LoadPNGFile(string source_file, bool append)
        {
            PNGSaveData savedata = PNGSaveLoader.FromFile(source_file);
            if (savedata.type == null)
            {
                //not save file
                return;
            }
            if (savedata.CameraDescription != null)
            {
                savedata.CameraDescription.Angle.Z = CameraConfig.Roll;

                    Console.WriteLine("-- translation");
                    Dump(ref savedata.CameraDescription.Translation);
                    Console.WriteLine("-- angle");
                    Dump(ref savedata.CameraDescription.Angle);

                camera.Reset();
                camera.Translation = savedata.CameraDescription.Translation;
                camera.Angle = savedata.CameraDescription.Angle;
            }
            if (savedata.type == "POSE")
            {
                Figure fig;
                if (TryGetFigure(out fig))
                {
                    BeginSelectedFigurePoseCommand();

                    fig.LampRotation = savedata.LampRotation;
                    fig.Tmo = savedata.Tmo;
                    fig.UpdateNodeMapAndBoneMatrices();

                    EndSelectedFigurePoseCommand();

                    // fire FigureSelectEvent
                    SetFigureIdx(sprite_renderer.scene_mode.SelectedIdx);
                }
            }
            if (savedata.type == "HSAV")
            {
                Figure fig = savedata.figures[0];

                int len = FigureList.Count;
                int idx = sprite_renderer.scene_mode.SelectedIdx;

                if (!append && idx < len)
                {
                    //更新する
                    //元のライトとポーズを維持する
                    Figure orig_fig = FigureList[idx];
                    FigureList[idx] = fig;  //置換
                    fig.LampRotation = orig_fig.LampRotation;
                    fig.Tmo = orig_fig.Tmo;
                    orig_fig.Dispose();
                    orig_fig = null;

                    fig.UpdateBoneMatricesEvent += delegate (object sender, EventArgs e)
                    {
                        if (GetSelectedFigure() == sender)
                            need_render = true;
                    };
                    fig.UpdateClothed();
                    fig.UpdateNodeMapAndBoneMatrices();

                    // fire FigureSelectEvent
                    SetFigureIdx(idx);
                }
                else
                {
                    //追加する
                    //todo: override List#Add
                    FigureList.Add(fig);
                    fig.UpdateBoneMatricesEvent += delegate (object sender, EventArgs e)
                    {
                        if (GetSelectedFigure() == sender)
                            need_render = true;
                    };
                    fig.UpdateClothed();
                    fig.UpdateNodeMapAndBoneMatrices();

                    // fire FigureSelectEvent
                    SetFigureIdx(len);
                }

                // free meshes and textures.
                Console.WriteLine("Total Memory: {0}", GC.GetTotalMemory(true));
            }
            if (savedata.type == "SCNE")
            {
                if (!append)
                    ClearFigureList();

                int len = FigureList.Count;
                foreach (Figure fig in savedata.figures)
                {
                    //todo: override List#Add
                    FigureList.Add(fig);
                    fig.UpdateBoneMatricesEvent += delegate (object sender, EventArgs e)
                    {
                        if (GetSelectedFigure() == sender)
                            need_render = true;
                    };
                    fig.UpdateClothed();
                    fig.UpdateNodeMapAndBoneMatrices();
                }
                // fire FigureSelectEvent
                SetFigureIdx(len);

                // free meshes and textures.
                Console.WriteLine("Total Memory: {0}", GC.GetTotalMemory(true));
            }
        }

        public static string CombineStartupPath(string path)
        {
            return Path.Combine(Application.StartupPath, path);
        }

        public void LoadCameraPreset(int i)
        {
            string file = CombineStartupPath(string.Format(@"resources\camera-presets\{0}.txt", i));
            PNGSaveCameraDescription cameraDescription = new PNGSaveCameraDescription();
            cameraDescription.ReadFromTextFile(file);

            camera.Reset();
            camera.Translation = cameraDescription.Translation;
            camera.Angle = cameraDescription.Angle;
        }

        Manipulator manipulator;
        CommandManager command_man;

        // キー入力を保持
        bool[] keys = new bool[256];
        bool[] keysEnabled = new bool[256];

        int keyFigure = (int)Keys.Tab;
        int keyDelete = (int)Keys.Delete;
        int keyHide = (int)Keys.E;
        int keyCamera1 = (int)Keys.D1;
        int keyCamera2 = (int)Keys.D2;
        int keyCamera3 = (int)Keys.D3;
        int keyCamera4 = (int)Keys.D4;
        int keyCamera5 = (int)Keys.D5;
        int keyCenter = (int)Keys.F;
        int keyFigureForm = (int)Keys.G;
        int keyConfigForm = (int)Keys.H;
        int keyUndo = (int)Keys.Z;
        int keyRedo = (int)Keys.Y;
        int keySave = (int)Keys.Enter;
        int keySprite = (int)Keys.Home;
        int keyResetLamp = (int)Keys.D9;
        int keyResetLamp2 = (int)Keys.Multiply; // alternative
        int keyResetNode = (int)Keys.D0;
        int keyResetNode2 = (int)Keys.Subtract; // alternative
        int keyResetNotFace = (int)Keys.F5;
        int keyResetFace = (int)Keys.F6;
        int keyResetPose = (int)Keys.F12;

        /// <summary>
        /// world行列
        /// </summary>
        Matrix world_matrix = Matrix.Identity;
        /// <summary>
        /// view変換行列
        /// </summary>
        Matrix Transform_View = Matrix.Identity;
        /// <summary>
        /// projection変換行列
        /// </summary>
        Matrix Transform_Projection = Matrix.Identity;

        /// <summary>
        /// deviceを作成します。
        /// </summary>
        /// <param name="control">レンダリング先となるcontrol</param>
        /// <returns>deviceの作成に成功したか</returns>
        public bool InitializeApplication(Control control)
        {
            SetControl(control);

            PresentParameters pp = new PresentParameters();
            try
            {
                int adapter_ordinal = Manager.Adapters.Default.Adapter;
                DisplayMode display_mode = Manager.Adapters.Default.CurrentDisplayMode;

                pp.Windowed = Windowed;
                pp.SwapEffect = SwapEffect.Discard;
                if (pp.Windowed)
                {
                    pp.BackBufferFormat = Format.X8R8G8B8;
                    pp.BackBufferWidth = DeviceSize.Width;
                    pp.BackBufferHeight = DeviceSize.Height;
                }
                else
                {
                    pp.BackBufferFormat = display_mode.Format;
                    pp.BackBufferWidth = display_mode.Width;
                    pp.BackBufferHeight = display_mode.Height;
                }
                pp.BackBufferCount = 1;
                pp.EnableAutoDepthStencil = true;

                int ret;
                if (Manager.CheckDepthStencilMatch(adapter_ordinal, DeviceType.Hardware, display_mode.Format, pp.BackBufferFormat, DepthFormat.D24S8, out ret))
                    pp.AutoDepthStencilFormat = DepthFormat.D24S8;
                else
                if (Manager.CheckDepthStencilMatch(adapter_ordinal, DeviceType.Hardware, display_mode.Format, pp.BackBufferFormat, DepthFormat.D24X8, out ret))
                    pp.AutoDepthStencilFormat = DepthFormat.D24X8;
                else
                    pp.AutoDepthStencilFormat = DepthFormat.D16;

                int quality;
                if (Manager.CheckDeviceMultiSampleType(adapter_ordinal, DeviceType.Hardware, pp.BackBufferFormat, pp.Windowed, MultiSampleType.FourSamples, out ret, out quality))
                {
                    pp.MultiSample = MultiSampleType.FourSamples;
                    pp.MultiSampleQuality = quality - 1;
                }
                //Console.WriteLine(pp);

                Caps caps = Manager.GetDeviceCaps(adapter_ordinal, DeviceType.Hardware);
                CreateFlags flags = CreateFlags.SoftwareVertexProcessing;
                if (caps.DeviceCaps.SupportsHardwareTransformAndLight)
                    flags = CreateFlags.HardwareVertexProcessing;
                if (caps.DeviceCaps.SupportsPureDevice)
                    flags |= CreateFlags.PureDevice;
                device = new Device(adapter_ordinal, DeviceType.Hardware, control.Handle, flags, pp);
            }
            catch (DirectXException ex)
            {
                Console.WriteLine("Error: " + ex);
                return false;
            }

            device.DeviceLost += new EventHandler(OnDeviceLost);
            device.DeviceReset += new EventHandler(OnDeviceReset);
            device.DeviceResizing += new CancelEventHandler(OnDeviceResizing);

            Macro[] macros = new Macro[3];
            macros[0].Name = "XRGB_DEPTH";
            macros[0].Definition = XRGBDepth ? "1" : "0";
            macros[1].Name = "MAIN_GEL";
            macros[1].Definition = MainGel ? "1" : "0";
            macros[2].Name = "SCREEN_DOF";
            macros[2].Definition = ScreenDof ? "1" : "0";

            EffectPool effect_pool = new EffectPool();

            Stopwatch sw = new Stopwatch();
            sw.Start();
            string toonshader_filename = @"toonshader-shared.cgfx";
            if (!LoadEffect(toonshader_filename, out effect, null, effect_pool))
                return false;
            sw.Stop();
            Console.WriteLine(toonshader_filename + " read time: " + sw.Elapsed);

            toon_shader = new ToonShader(effect);
            toon_shader.FetchNormalMap += delegate (string name)
            {
                return nmap_container.GetDirect3DTexture(name);
            };
            toon_shader.FetchEnvironmentMap += delegate (string name)
            {
                return emap_container.GetDirect3DTexture(name);
            };

            if (!LoadEffect(@"effects\clear.fx", out effect_clear))
                return false;

            if (!LoadEffect(@"effects\ao.fx", out effect_ao, macros))
                return false;

            if (!LoadEffect(@"effects\gb.fx", out effect_gb))
                return false;

            if (!LoadEffect(@"effects\main.fx", out effect_main, macros))
                return false;

            if (!LoadEffect(@"effects\screen.fx", out effect_screen, macros))
                return false;

            /// toonshader.cgfx に渡す頂点要素
            VertexElement[] ve = new VertexElement[]
            {
                new VertexElement(0,  0, DeclarationType.Float3, DeclarationMethod.Default, DeclarationUsage.Position, 0),
                new VertexElement(0, 12, DeclarationType.Float4, DeclarationMethod.Default, DeclarationUsage.TextureCoordinate, 3),
                new VertexElement(0, 28, DeclarationType.Ubyte4, DeclarationMethod.Default, DeclarationUsage.TextureCoordinate, 4),
                new VertexElement(0, 32, DeclarationType.Float3, DeclarationMethod.Default, DeclarationUsage.Normal, 0),
                new VertexElement(0, 44, DeclarationType.Float2, DeclarationMethod.Default, DeclarationUsage.TextureCoordinate, 0),
                    VertexElement.VertexDeclarationEnd
            };

            vd = new VertexDeclaration(device, ve);

            Shader.LoadTechMap(CombineStartupPath(@"resources\assumed-techs.txt"));
            Shader.LoadNameMap(CombineStartupPath(@"resources\parameters.txt"));

            screen = new Screen(device);
            sprite = new Sprite(device);

            string techmap_path = CombineStartupPath(@"resources\dnmap-hidetechs.txt");
            dnmap_renderer = new DepthNormalMapRenderer(device);
            dnmap_renderer.LoadTechMap(techmap_path);

            if (!LoadEffect(@"effects\dnclear.fx", out dnmap_renderer.effect_dnclear))
                return false;

            if (!LoadEffect(@"effects\dnmap.fx", out dnmap_renderer.effect_dnmap, macros, effect_pool))
                return false;

            if (!LoadEffect(@"effects\depth.fx", out dnmap_renderer.effect_depth, macros))
                return false;

            lamp_renderer = new LampRenderer(device, sprite);

            if (!LoadEffect(@"effects\circle.fx", out lamp_renderer.effect_circle, macros))
                return false;

            if (!LoadEffect(@"effects\pole.fx", out lamp_renderer.effect_pole, macros))
                return false;

            node_renderer = new NodeRenderer(device, sprite);

            if (!LoadEffect(@"effects\circle.fx", out node_renderer.effect_circle, macros))
                return false;

            if (!LoadEffect(@"effects\pole.fx", out node_renderer.effect_pole, macros))
                return false;

            handle_LocalBoneMats = effect.GetParameter(null, "LocalBoneMats");
            handle_LightDirForced = effect.GetParameter(null, "LightDirForced");
            handle_Ambient = effect.GetParameter(null, "ColorRate");
            handle_HohoAlpha = effect.GetParameter(null, "HohoAlpha");
            handle_UVSCR = effect.GetParameter(null, "UVSCR");

            sprite_cell_renderer = new SpriteCellRenderer(device, sprite);
            sprite_renderer = new SpriteRenderer(device, sprite, CombineStartupPath(@"resources"));

            node_filter = new NodeFilter();
            node_filter.LoadNotFaceNodes(CombineStartupPath(@"resources\not-facenodes.txt"));
            node_filter.LoadFaceNodes(CombineStartupPath(@"resources\facenodes.txt"));

            Direct3D.FontDescription fd = new Direct3D.FontDescription();
            fd.FaceName = "Consolas";
            font = new Direct3D.Font(device, fd);

            camera.Update();

            manipulator.GrabNodeSpeed = GrabNodeSpeed;
            manipulator.RotateNodeSpeed = RotateNodeSpeed;
            manipulator.GrabCameraSpeed = GrabCameraSpeed;
            manipulator.RotateCameraSpeed = RotateCameraSpeed;

            D3DTextureManager.instance.device = device;
            D3DVertexBufferManager.instance.device = device;

            OnDeviceReset(device, null);

            FigureSelectEvent += delegate (object sender, EventArgs e)
            {
                Figure fig;
                if (TryGetFigure(out fig))
                {
                    selected_node = fig.Tmo.nodes[0]; // W_Hips
                    sprite_renderer.pose_mode.SelectedNodeName = selected_node.Name;
                }
                UpdateFiguresHidden();
                UpdateTsosHidden();
                need_render = true;
            };

            for (int i = 0; i < keysEnabled.Length; i++)
            {
                keysEnabled[i] = true;
            }
            control.KeyDown += new KeyEventHandler(form_OnKeyDown);
            control.KeyUp += new KeyEventHandler(form_OnKeyUp);

            control.MouseDown += new MouseEventHandler(form_OnMouseDown);
            control.MouseMove += new MouseEventHandler(form_OnMouseMove);
            control.MouseUp += new MouseEventHandler(form_OnMouseUp);
            control.MouseWheel += new MouseEventHandler(form_OnMouseWheel);

            control.DragDrop += new DragEventHandler(form_OnDragDrop);
            control.DragOver += new DragEventHandler(form_OnDragOver);

            control.Resize += new EventHandler(form_OnResize);

            return true;
        }

        /// 設定を変更したら描画に反映する
        public void ConfigConnect()
        {
            CameraConfig.ChangeFovy += delegate (object sender, EventArgs e)
            {
                fieldOfViewY = CameraConfig.Fovy;
                AssignProjection();
                AssignDepthProjection();
                need_render = true;
            };
            CameraConfig.ChangeRoll += delegate (object sender, EventArgs e)
            {
                Vector3 angle = camera.Angle;
                angle.Z = CameraConfig.Roll;
                camera.SetAngle(angle);
                need_render = true;
            };
            DepthMapConfig.ChangeZnearPlane += delegate (object sender, EventArgs e)
            {
                AssignDepthProjection();
                need_render = true;
            };
            DepthMapConfig.ChangeZfarPlane += delegate (object sender, EventArgs e)
            {
                AssignDepthProjection();
                need_render = true;
            };
            OcclusionConfig.ChangeIntensity += delegate (object sender, EventArgs e)
            {
                effect_ao.SetValue("_Intensity", OcclusionConfig.Intensity); // in
                need_render = true;
            };
            OcclusionConfig.ChangeRadius += delegate (object sender, EventArgs e)
            {
                effect_ao.SetValue("_Radius", OcclusionConfig.Radius); // in
                need_render = true;
            };
            DiffusionConfig.ChangeIntensity += delegate (object sender, EventArgs e)
            {
                effect_screen.SetValue("_Intensity", DiffusionConfig.Intensity); // in
                need_render = true;
            };
            DiffusionConfig.ChangeExtent += delegate (object sender, EventArgs e)
            {
                //effect_gb.SetValue("_Extent", DiffusionConfig.Extent); // in
                need_render = true;
            };
        }

        bool LoadEffect(string effect_file, out Effect effect, Macro[] macros = null, EffectPool effect_pool = null)
        {
            effect = null;
            string effect_path = CombineStartupPath(effect_file);
            if (!File.Exists(effect_path))
            {
                Console.WriteLine("File not found: " + effect_file);
                return false;
            }
            using (FileStream effect_stream = File.OpenRead(effect_path))
            {
                string compile_error;
                effect = Effect.FromStream(device, effect_stream, macros, null, ShaderFlags.None, effect_pool, out compile_error);
                if (compile_error != null)
                {
                    Console.WriteLine(compile_error);
                    return false;
                }
            }
            return true;
        }

        ProjectionMode projection_mode = ProjectionMode.Perspective;

        /// 射影 mode
        public ProjectionMode ProjectionMode
        {
            get { return projection_mode; }
            set
            {
                projection_mode = value;

                AssignProjection();
                AssignDepthProjection();

                need_render = true;
            }
        }

        void AssignProjection()
        {
            //
            // update Transform_Projection
            //
            float aspect = (float)device.Viewport.Width / (float)device.Viewport.Height;
            float d;
            if (projection_mode == ProjectionMode.Ortho)
                d = camera.Translation.Z;
            else
                d = 1.0f; // zn
            float h = d * (float)Math.Tan(fieldOfViewY / 2.0f);
            float w = h * aspect;
            if (projection_mode == ProjectionMode.Ortho)
                Transform_Projection = Matrix.OrthoRH(w * 2.0f, h * 2.0f, 1.0f, 500.0f);
            else
                Transform_Projection = Matrix.PerspectiveRH(w * 2.0f, h * 2.0f, 1.0f, 500.0f);
            // xxx: for w-buffering
            device.Transform.Projection = Transform_Projection;
            effect.SetValue("proj", Transform_Projection);
        }

        void AssignDepthProjection()
        {
            float zn = DepthMapConfig.ZnearPlane;
            float zd = DepthMapConfig.Zdistance;
            float aspect = (float)device.Viewport.Width / (float)device.Viewport.Height;
            float d;
            if (projection_mode == ProjectionMode.Ortho)
                d = camera.Translation.Z;
            else
                d = zn;
            float h = d * (float)Math.Tan(fieldOfViewY / 2.0f);
            float w = h * aspect;
            Vector4 zp;
            if (projection_mode == ProjectionMode.Ortho)
                zp = new Vector4(1 / w, 1 / h, zn, zd);
            else
                zp = new Vector4(zn / w, zn / h, zn, zd);
            Vector4 vp = new Vector4(device.Viewport.Width, device.Viewport.Height, 0, 0);

            dnmap_renderer.effect_dnmap.SetValue("zp", zp); // in
            effect_ao.SetValue("zp", zp); // in
            effect_ao.SetValue("vp", vp); // in

            if (projection_mode == ProjectionMode.Ortho)
                effect_ao.Technique = "AO_ortho";
            else
                effect_ao.Technique = "AO";
        }

        void OnDeviceLost(object sender, EventArgs e)
        {
            Console.WriteLine("OnDeviceLost");

            if (sprite_renderer != null)
                sprite_renderer.Dispose();
            if (sprite_cell_renderer != null)
                sprite_cell_renderer.Dispose();
            if (node_renderer != null)
                node_renderer.Dispose();
            if (lamp_renderer != null)
                lamp_renderer.Dispose();
            if (dnmap_renderer != null)
                dnmap_renderer.Dispose();

            if (screen != null)
                screen.Dispose();

            if (scene_thumbnail != null)
                scene_thumbnail.Dispose();

            if (tex_zbuf != null)
                tex_zbuf.Dispose();

            if (amb_surface != null)
                amb_surface.Dispose();
            if (occ_surface != null)
                occ_surface.Dispose();
            if (tmp_surface != null)
                tmp_surface.Dispose();

            if (amb_texture != null)
                amb_texture.Dispose();
            if (randommap_texture != null)
                randommap_texture.Dispose();
            if (occ_texture != null)
                occ_texture.Dispose();
            if (tmp_texture != null)
                tmp_texture.Dispose();

            if (nmap_container != null)
                nmap_container.Dispose();

            if (dev_zbuf != null)
                dev_zbuf.Dispose();
            if (dev_surface != null)
                dev_surface.Dispose();

            foreach (Figure fig in FigureList)
                foreach (TSOFile tso in fig.TsoList)
                    tso.Dispose();

            D3DTextureManager.instance.Clear();
            D3DVertexBufferManager.instance.Clear();
        }

        Rectangle dev_rect;

        void OnDeviceReset(object sender, EventArgs e)
        {
            Console.WriteLine("OnDeviceReset");
            int devw = 0;
            int devh = 0;
            dev_surface = device.GetRenderTarget(0);
            {
                devw = dev_surface.Description.Width;
                devh = dev_surface.Description.Height;
            }
            Console.WriteLine("dev {0}x{1}", devw, devh);

            dev_rect = new Rectangle(0, 0, devw, devh);

            dev_zbuf = device.DepthStencilSurface;

            nmap_container = new NormalMapContainer(device, CombineStartupPath(@"resources"));
            emap_container = new EnvironmentMapContainer(device, CombineStartupPath(@"resources"));

            amb_texture = new Texture(device, devw, devh, 1, Usage.RenderTarget, Format.X8R8G8B8, Pool.Default);
            amb_surface = amb_texture.GetSurfaceLevel(0);

            string randommap_texture_path = CombineStartupPath(@"resources\rand.png");
            randommap_texture = TextureLoader.FromFile(device, randommap_texture_path);

            occ_texture = new Texture(device, devw, devh, 1, Usage.RenderTarget, Format.X8R8G8B8, Pool.Default);
            occ_surface = occ_texture.GetSurfaceLevel(0);

            tmp_texture = new Texture(device, devw, devh, 1, Usage.RenderTarget, Format.X8R8G8B8, Pool.Default);
            tmp_surface = tmp_texture.GetSurfaceLevel(0);

            tex_zbuf = device.CreateDepthStencilSurface(devw, devh, DepthFormat.D16, MultiSampleType.None, 0, false);

            model_thumbnail.Create(device);
            scene_thumbnail.Create(device);

            dnmap_renderer.Create(dev_rect, dmap_format, nmap_format, nmap_container);

            lamp_center_on_device = LampCenter;
            ScaleToScreen(ref lamp_center_on_device);

            lamp_radius_on_device = ScaleToScreen(LampRadius);
            node_radius_on_device = ScaleToScreen(NodeRadius);
            selected_node_radius_on_device = ScaleToScreen(SelectedNodeRadius);

            lamp_renderer.Create(dev_rect, lamp_radius_on_device, lamp_center_on_device);
            node_renderer.Create(dev_rect, node_radius_on_device, selected_node_radius_on_device);
            sprite_cell_renderer.Create(dev_rect);
            sprite_renderer.Create(dev_rect);

            effect_ao.SetValue("DepthMap_texture", dnmap_renderer.dmap_texture); // in
            effect_ao.SetValue("NormalMap_texture", dnmap_renderer.nmap_texture); // in
            effect_ao.SetValue("RandomMap_texture", randommap_texture); // in

            effect_main.SetValue("Ambient_texture", amb_texture); // in
            effect_main.SetValue("Occlusion_texture", occ_texture); // in

            effect_screen.SetValue("DepthMap_texture", dnmap_renderer.dmap_texture); // in
            effect_screen.SetValue("Ambient_texture", amb_texture); // in
            effect_screen.SetValue("Occlusion_texture", occ_texture); // in

            screen.Create(dev_rect);

            AssignProjection();

            screen.AssignWorldViewProjection(effect_clear);
            screen.AssignWorldViewProjection(dnmap_renderer.effect_dnclear);
            screen.AssignWorldViewProjection(dnmap_renderer.effect_depth);
            screen.AssignWorldViewProjection(effect_ao);
            screen.AssignWorldViewProjection(effect_gb);
            screen.AssignWorldViewProjection(effect_main);
            screen.AssignWorldViewProjection(effect_screen);

            AssignDepthProjection();

            device.SetRenderState(RenderStates.Lighting, false);
            device.SetRenderState(RenderStates.CullMode, (int)Cull.CounterClockwise);

            device.TextureState[0].AlphaOperation = TextureOperation.Modulate;
            device.TextureState[0].AlphaArgument1 = TextureArgument.TextureColor;
            device.TextureState[0].AlphaArgument2 = TextureArgument.Current;

            device.SetRenderState(RenderStates.AlphaBlendEnable, true);
            device.SetRenderState(RenderStates.SourceBlend, (int)Blend.SourceAlpha);
            device.SetRenderState(RenderStates.DestinationBlend, (int)Blend.InvSourceAlpha);
            device.SetRenderState(RenderStates.AlphaTestEnable, true);
            device.SetRenderState(RenderStates.ReferenceAlpha, 0x40);
            device.SetRenderState(RenderStates.AlphaFunction, (int)Compare.GreaterEqual);

            //device.RenderState.IndexedVertexBlendEnable = true;

            foreach (Figure fig in FigureList)
                foreach (TSOFile tso in fig.TsoList)
                    tso.ReadOnDeviceReset();
        }

        private void OnDeviceResizing(object sender, CancelEventArgs e)
        {
            //e.Cancel = true;
        }

        /// <summary>
        /// 全フィギュアを削除します。
        /// </summary>
        public void ClearFigureList()
        {
            foreach (Figure fig in FigureList)
                fig.Dispose();
            FigureList.Clear();
            SetFigureIdx(0);
            // free meshes and textures.
            Console.WriteLine("Total Memory: {0}", GC.GetTotalMemory(true));
        }

        /// <summary>
        /// 選択フィギュアを削除します。
        /// </summary>
        public void RemoveSelectedFigure()
        {
            Figure fig;
            if (TryGetFigure(out fig))
            {
                fig.Dispose();
                FigureList.Remove(fig);
                int idx = sprite_renderer.scene_mode.SelectedIdx;
                SetFigureIdx(idx - 1);
            }
            fig = null;
            // free meshes and textures.
            Console.WriteLine("Total Memory: {0}", GC.GetTotalMemory(true));
        }

        /// <summary>
        /// 選択フィギュアを隠します。
        /// </summary>
        public void HideSelectedFigure()
        {
            Figure fig;
            if (TryGetFigure(out fig))
            {
                fig.SwitchHidden();
                need_render = true;

                sprite_renderer.scene_mode.SetHidden(sprite_renderer.scene_mode.SelectedIdx, fig.Hidden);
            }
        }

        void UpdateFiguresHidden()
        {
            sprite_renderer.scene_mode.ClearHidden();
            int idx = 0;
            foreach (Figure fig in FigureList)
            {
                sprite_renderer.scene_mode.SetHidden(idx, fig.Hidden);
                idx++;
                if (idx == 24)
                    break;
            }
        }

        /// 選択 tso を削除します。
        public void RemoveSelectedTSO()
        {
            Figure fig;
            if (TryGetFigure(out fig))
            {
                TSOFile tso;
                if (TryGetTSOFile(out tso))
                {
                    tso.Dispose();
                    fig.TsoList.Remove(tso);
                }
                tso = null;

                fig.UpdateClothed();
                fig.UpdateNodeMapAndBoneMatrices();
            }
            fig = null;
            // free meshes and textures.
            Console.WriteLine("Total Memory: {0}", GC.GetTotalMemory(true));
        }

        /// 選択 tso を隠します。
        public void HideSelectedTSO()
        {
            Figure fig;
            if (TryGetFigure(out fig))
            {
                TSOFile tso;
                if (TryGetTSOFile(out tso))
                {
                    tso.SwitchHidden();
                    need_render = true;

                    sprite_renderer.model_mode.SetHidden(sprite_renderer.model_mode.SelectedIdx, tso.Hidden);
                }
            }
        }

        void UpdateTsosHidden()
        {
            Figure fig;
            if (TryGetFigure(out fig))
            {
                sprite_renderer.model_mode.ClearHidden();
                foreach (TSOFile tso in fig.TsoList)
                {
                    sprite_renderer.model_mode.SetHidden(tso.Row, tso.Hidden);
                }
            }
        }

        bool sprite_enabled = true;
        /// <summary>
        /// スプライトの有無
        /// </summary>
        public bool SpriteEnabled { get { return sprite_enabled; } }

        /// スプライトの有無を切り替えます。
        public void SwitchSpriteEnabled()
        {
            sprite_enabled = ! sprite_enabled;
            need_render = true;
        }

        /// ライト方向をリセットします。
        public void ResetLamp()
        {
            Figure fig;
            if (TryGetFigure(out fig))
            {
                fig.LampRotation = Quaternion.Identity;
                need_render = true;
            }
        }

        public bool CanResetSelectedNode()
        {
            if (selected_node == null)
                return false;

            Figure fig = GetSelectedFigure();
            if (fig == null)
                return false;

            if (fig.TsoList.Count == 0)
                return false;

            return true;
        }

        /// 選択ボーンをリセットします。
        public void ResetSelectedNode()
        {
            Figure fig = GetSelectedFigure();
            TSOFile tso = fig.TsoList[0];
            TSONode tso_node = tso.FindNodeByName(selected_node.Name);
            if (tso_node != null)
            {
                selected_node.TransformationMatrix = tso_node.TransformationMatrix;
            }
            fig.UpdateBoneMatrices();
        }

        public bool CanResetSelectedFigurePose()
        {
            Figure fig = GetSelectedFigure();
            if (fig == null)
                return false;

            if (fig.TsoList.Count == 0)
                return false;

            return true;
        }

        /// 選択フィギュアのポーズをリセットします。
        void ResetFigurePose(Figure fig, TMONode[] nodes)
        {
            //NOTE: W_Hipsの移動変位は維持される
            Vector3 w_hips_translation = Vector3.Empty;

            if (fig.Tmo.w_hips_node != null)
                w_hips_translation = fig.Tmo.w_hips_node.Translation;

            TSOFile tso = fig.TsoList[0];
            foreach (TMONode tmo_node in nodes)
            {
                TSONode tso_node = tso.FindNodeByName(tmo_node.Name);
                if (tso_node != null)
                {
                    tmo_node.TransformationMatrix = tso_node.TransformationMatrix;
                }
            }

            if (fig.Tmo.w_hips_node != null)
                fig.Tmo.w_hips_node.Translation = w_hips_translation;

            fig.UpdateBoneMatrices();
        }

        /// 選択フィギュアのポーズのうち顔以外をリセットします。
        void ResetSelectedFigureNotFace()
        {
            Figure fig = GetSelectedFigure();
            ResetFigurePose(fig, node_filter.GetNotFaceNodes(fig.Tmo));
        }

        /// 選択フィギュアのポーズのうち顔をリセットします。
        void ResetSelectedFigureFace()
        {
            Figure fig = GetSelectedFigure();
            ResetFigurePose(fig, node_filter.GetFaceNodes(fig.Tmo));
        }

        /// 選択フィギュアのポーズをリセットします。
        void ResetSelectedFigurePose()
        {
            Figure fig = GetSelectedFigure();
            ResetFigurePose(fig, fig.Tmo.nodes);
        }

        void form_OnKeyDown(object sender, KeyEventArgs e)
        {
            if ((int)e.KeyCode < keys.Length)
            {
                keys[(int)e.KeyCode] = true;
            }
        }

        void form_OnKeyUp(object sender, KeyEventArgs e)
        {
            if ((int)e.KeyCode < keys.Length)
            {
                keys[(int)e.KeyCode] = false;
                keysEnabled[(int)e.KeyCode] = true;
            }
        }

        void GrabNodeLocalByKey(int key, int dx, int dy, int dz)
        {
            if (keysEnabled[key])
            {
                keysEnabled[key] = false;

                if (selected_node != null)
                {
                    BeginSelectedNodeCommand();
                    manipulator.BeginGrabNode(ManipulatorDeviceType.Keyboard, selected_node);
                }
            }
            if (manipulator.WhileGrabNodeLocal(dx, dy, dz))
            {
                //TODO: UpdateSelectedBoneMatrices
                GetSelectedFigure().UpdateBoneMatrices();
            }
        }

        void RotateNodeLocalByKey(int key, int dx, int dy, int dz)
        {
            if (keysEnabled[key])
            {
                keysEnabled[key] = false;

                if (selected_node != null)
                {
                    BeginSelectedNodeCommand();
                    manipulator.BeginRotateNode(ManipulatorDeviceType.Keyboard, selected_node);
                }
            }
            if (manipulator.WhileRotateNodeLocal(dx, dy, dz))
            {
                //TODO: UpdateSelectedBoneMatrices
                GetSelectedFigure().UpdateBoneMatrices();
            }
        }

        public void Update()
        {
            if (keysEnabled[keyConfigForm] && keys[keyConfigForm])
            {
                keys[keyConfigForm] = false;
                keysEnabled[keyConfigForm] = true;
                if (ConfigFormEvent != null)
                    ConfigFormEvent(this, EventArgs.Empty);
            }
            if (keysEnabled[keyFigure] && keys[keyFigure])
            {
                keysEnabled[keyFigure] = false;
                this.NextFigure();
            }
            if (keysEnabled[keyDelete] && keys[keyDelete])
            {
                keysEnabled[keyDelete] = false;

                string modename = sprite_renderer.CurrentModeName;
                if (modename == "MODEL")
                    this.RemoveSelectedTSO();
                else
                    this.RemoveSelectedFigure();
            }
            if (keysEnabled[keyHide] && keys[keyHide])
            {
                keysEnabled[keyHide] = false;

                string modename = sprite_renderer.CurrentModeName;
                if (modename == "MODEL")
                    this.HideSelectedTSO();
                else
                    this.HideSelectedFigure();
            }
            if (keysEnabled[keyCamera1] && keys[keyCamera1])
            {
                keysEnabled[keyCamera1] = false;
                this.LoadCameraPreset(1);
            }
            if (keysEnabled[keyCamera2] && keys[keyCamera2])
            {
                keysEnabled[keyCamera2] = false;
                this.LoadCameraPreset(2);
            }
            if (keysEnabled[keyCamera3] && keys[keyCamera3])
            {
                keysEnabled[keyCamera3] = false;
                this.LoadCameraPreset(3);
            }
            if (keysEnabled[keyCamera4] && keys[keyCamera4])
            {
                keysEnabled[keyCamera4] = false;
                this.LoadCameraPreset(4);
            }
            if (keysEnabled[keyCamera5] && keys[keyCamera5])
            {
                keysEnabled[keyCamera5] = false;
                this.LoadCameraPreset(5);
            }
            if (keysEnabled[keyCenter] && keys[keyCenter])
            {
                keysEnabled[keyCenter] = false;
                this.SetCenterToSelectedNode();
            }
            if (keysEnabled[keyFigureForm] && keys[keyFigureForm])
            {
                keys[keyFigureForm] = false;
                keysEnabled[keyFigureForm] = true;
                if (FigureFormEvent != null)
                    FigureFormEvent(this, EventArgs.Empty);
            }
            if (keysEnabled[keyUndo] && keys[keyUndo])
            {
                keysEnabled[keyUndo] = false;
                command_man.Undo();
            }
            if (keysEnabled[keyRedo] && keys[keyRedo])
            {
                keysEnabled[keyRedo] = false;
                command_man.Redo();
            }
            if (keysEnabled[keySave] && keys[keySave])
            {
                keysEnabled[keySave] = false;
                string modename = sprite_renderer.CurrentModeName;
                if (modename == "MODEL")
                    this.SaveModelToFile();
                else
                {
                    // CRTL 同時押しでシーンを保存; なければポーズを保存
                    bool ctrl_p = (Control.ModifierKeys & Keys.Control) == Keys.Control;
                    if (ctrl_p)
                        this.SaveSceneToFile();
                    else
                        this.SavePoseToFile();
                }
            }
            if (keysEnabled[keySprite] && keys[keySprite])
            {
                keysEnabled[keySprite] = false;
                this.SwitchSpriteEnabled();
            }
            if (keysEnabled[keyResetLamp] && keys[keyResetLamp])
            {
                keysEnabled[keyResetLamp] = false;
                this.ResetLamp();
            }
            if (keysEnabled[keyResetLamp2] && keys[keyResetLamp2])
            {
                keysEnabled[keyResetLamp2] = false;
                this.ResetLamp();
            }
            if (keysEnabled[keyResetNode] && keys[keyResetNode])
            {
                keysEnabled[keyResetNode] = false;
                if (this.CanResetSelectedNode())
                {
                    BeginSelectedNodeCommand();
                    this.ResetSelectedNode();
                    EndSelectedNodeCommand();
                }
            }
            if (keysEnabled[keyResetNode2] && keys[keyResetNode2])
            {
                keysEnabled[keyResetNode2] = false;
                if (this.CanResetSelectedNode())
                {
                    BeginSelectedNodeCommand();
                    this.ResetSelectedNode();
                    EndSelectedNodeCommand();
                }
            }
            if (keysEnabled[keyResetPose] && keys[keyResetPose])
            {
                keysEnabled[keyResetPose] = false;
                if (this.CanResetSelectedFigurePose())
                {
                    BeginSelectedFigurePoseCommand();
                    this.ResetSelectedFigurePose();
                    EndSelectedFigurePoseCommand();
                }
            }
            if (keysEnabled[keyResetNotFace] && keys[keyResetNotFace])
            {
                keysEnabled[keyResetNotFace] = false;
                if (this.CanResetSelectedFigurePose())
                {
                    BeginSelectedFigurePoseCommand();
                    this.ResetSelectedFigureNotFace();
                    EndSelectedFigurePoseCommand();
                }
            }
            if (keysEnabled[keyResetFace] && keys[keyResetFace])
            {
                keysEnabled[keyResetFace] = false;
                if (this.CanResetSelectedFigurePose())
                {
                    BeginSelectedFigurePoseCommand();
                    this.ResetSelectedFigureFace();
                    EndSelectedFigurePoseCommand();
                }
            }

            // CRTL 同時押しで移動; なければ回転
            bool grab_mode = (Control.ModifierKeys & Keys.Control) == Keys.Control;

            // SHIFT 同時押しでZ軸操作
            bool z_mode = (Control.ModifierKeys & Keys.Shift) == Keys.Shift;

            if (grab_mode)
            {
                int d = KeyGrabNodeDelta;

                if (keys[(int)Keys.NumPad4])
                    GrabNodeLocalByKey((int)Keys.NumPad4, -d, 0, 0);

                else if (keys[(int)Keys.NumPad2])
                    GrabNodeLocalByKey((int)Keys.NumPad2, 0, +d, 0);

                else if (keys[(int)Keys.NumPad6])
                    GrabNodeLocalByKey((int)Keys.NumPad6, +d, 0, 0);

                else if (keys[(int)Keys.NumPad8])
                    GrabNodeLocalByKey((int)Keys.NumPad8, 0, -d, 0);

                else if (keys[(int)Keys.NumPad1])
                    GrabNodeLocalByKey((int)Keys.NumPad1, 0, 0, +d);

                else if (keys[(int)Keys.NumPad9])
                    GrabNodeLocalByKey((int)Keys.NumPad9, 0, 0, -d);

                else if (keys[(int)Keys.Left])
                    GrabNodeLocalByKey((int)Keys.Left, z_mode ? 0 : -d, 0, z_mode ? +d : 0);

                else if (keys[(int)Keys.Down])
                    GrabNodeLocalByKey((int)Keys.Down, 0, z_mode ? 0 : +d, z_mode ? +d : 0);

                else if (keys[(int)Keys.Right])
                    GrabNodeLocalByKey((int)Keys.Right, z_mode ? 0 : +d, 0, z_mode ? -d : 0);

                else if (keys[(int)Keys.Up])
                    GrabNodeLocalByKey((int)Keys.Up, 0, z_mode ? 0 : -d, z_mode ? -d : 0);

                else if (manipulator.EndGrabNode(ManipulatorDeviceType.Keyboard))
                    EndSelectedNodeCommand();
            }
            else
            {
                int d = KeyRotateNodeDelta;

                if (keys[(int)Keys.NumPad4])
                    RotateNodeLocalByKey((int)Keys.NumPad4, -d, 0, 0);

                else if (keys[(int)Keys.NumPad2])
                    RotateNodeLocalByKey((int)Keys.NumPad2, 0, +d, 0);

                else if (keys[(int)Keys.NumPad6])
                    RotateNodeLocalByKey((int)Keys.NumPad6, +d, 0, 0);

                else if (keys[(int)Keys.NumPad8])
                    RotateNodeLocalByKey((int)Keys.NumPad8, 0, -d, 0);

                else if (keys[(int)Keys.NumPad1])
                    RotateNodeLocalByKey((int)Keys.NumPad1, 0, 0, +d);

                else if (keys[(int)Keys.NumPad9])
                    RotateNodeLocalByKey((int)Keys.NumPad9, 0, 0, -d);

                else if (keys[(int)Keys.Left])
                    RotateNodeLocalByKey((int)Keys.Left, z_mode ? 0 : -d, 0, z_mode ? +d : 0);

                else if (keys[(int)Keys.Down])
                    RotateNodeLocalByKey((int)Keys.Down, 0, z_mode ? 0 : +d, z_mode ? +d : 0);

                else if (keys[(int)Keys.Right])
                    RotateNodeLocalByKey((int)Keys.Right, z_mode ? 0 : +d, 0, z_mode ? -d : 0);

                else if (keys[(int)Keys.Up])
                    RotateNodeLocalByKey((int)Keys.Up, 0, z_mode ? 0 : -d, z_mode ? -d : 0);

                else if (manipulator.EndRotateNode(ManipulatorDeviceType.Keyboard))
                    EndSelectedNodeCommand();
            }

            if (camera.NeedUpdate)
            {
                camera.Update();

                //
                // update Transform_View
                //
                Transform_View = camera.ViewMatrix;
                // xxx: for w-buffering
                device.Transform.View = Transform_View;
                effect.SetValue("view", Transform_View);

                Vector3 pos = camera.WorldPosition;
                effect.SetValue("campos", new Vector4(pos.X, pos.Y, pos.Z, 0));

                AssignProjection();
                AssignDepthProjection();

                need_render = true;
            }
        }

        bool need_render = true;

        /// <summary>
        /// レンダリングは必要か
        /// </summary>
        public bool NeedRender { get { return need_render; } }

        //選択中のパネル上にあるボーン配列を得る
        TMONode[] GetDrawableNodes(TMOFile tmo)
        {
            string[] nodenames = sprite_renderer.NodeNames;

            TMONode[] nodes = new TMONode[nodenames.Length];
            int idx = 0;
            for (int i = 0; i < nodenames.Length; i++)
            {
                TMONode node = tmo.FindNodeByName(nodenames[i]); // nullable
                if (node != null)
                    nodes[idx++] = node;
            }
            Array.Resize(ref nodes, idx);
            return nodes;
        }

        void SnapTsosOnModelMode()
        {
            device.SetRenderState(RenderStates.AlphaBlendEnable, true);

            device.VertexDeclaration = vd;
            effect.SetValue(handle_Ambient, Ambient);
            effect.SetValue(handle_HohoAlpha, HohoAlpha);
            effect.SetValue(handle_UVSCR, UVSCR());

            Figure fig;
            if (TryGetFigure(out fig))
            {
                {
                    Matrix world;
                    fig.GetWorldMatrix(out world);

                    AssignWorldViewProjection(ref world);
                }
                effect.SetValue(handle_LightDirForced, fig.LightDirForced);

                foreach (TSOFile tso in fig.TsoList)
                {
                    int idx = tso.Row;

                    device.Clear(ClearFlags.Target | ClearFlags.ZBuffer | ClearFlags.Stencil, ScreenColor, 1.0f, 0);

                    DrawTSO(fig, tso);
                    sprite_cell_renderer.SnapTSO(idx);
                }
            }
        }

        void SnapFiguresOnSceneMode()
        {
            device.SetRenderState(RenderStates.AlphaBlendEnable, true);

            device.VertexDeclaration = vd;
            effect.SetValue(handle_Ambient, Ambient);
            effect.SetValue(handle_HohoAlpha, HohoAlpha);
            effect.SetValue(handle_UVSCR, UVSCR());

            int idx = 0;
            foreach (Figure fig in FigureList)
            {
                device.Clear(ClearFlags.Target | ClearFlags.ZBuffer | ClearFlags.Stencil, ScreenColor, 1.0f, 0);

                DrawFigure(fig);
                sprite_cell_renderer.SnapFigure(idx);

                idx++;
                if (idx == 24)
                    break;
            }
        }

        void DrawSpritesOnModelMode()
        {
            Figure fig;
            if (TryGetFigure(out fig))
            {
                sprite_cell_renderer.DrawSpriteTSO(fig);
            }
            if (swap_row != -1)
                sprite_renderer.model_mode.DrawDottedSprite();
            else
                sprite_renderer.model_mode.DrawCursorSprite();
        }

        void DrawSpritesOnPoseMode()
        {
            Figure fig;
            if (TryGetFigure(out fig))
            {
                device.SetRenderState(RenderStates.AlphaBlendEnable, true);

                Matrix camera_rotation = camera.RotationMatrix;
                Matrix world;
                fig.GetWorldMatrix(out world);
                node_renderer.SetTransform(projection_mode, ref camera_rotation, ref world);
                node_renderer.Render(selected_node, GetDrawableNodes(fig.Tmo));

                Matrix lamp_rotation = Matrix.RotationQuaternion(fig.LampRotation);
                lamp_renderer.SetTransform(projection_mode, ref camera_rotation, ref lamp_rotation);
                lamp_renderer.Render();
            }
            // draw manipulator.NodePower
            sprite.Transform = Matrix.Scaling(dev_rect.Width / 1024.0f, dev_rect.Height / 768.0f, 1.0f);
            sprite.Begin(SpriteFlags.AlphaBlend);
            font.DrawText(sprite, string.Format("manipulator.NodePower:{0}", manipulator.NodePower), 48, 768-48, FontColor);
            sprite.End();
        }

        void DrawSpritesOnSceneMode()
        {
            sprite_cell_renderer.DrawSpriteFigure(FigureList);
            if (swap_idx != -1)
                sprite_renderer.scene_mode.DrawDottedSprite();
            else
                sprite_renderer.scene_mode.DrawCursorSprite();
        }

        public void Draw()
        {
            switch (RenderMode)
            {
                case RenderMode.Ambient:
                    DrawFigureList();
                    break;
                case RenderMode.DepthMap:
                    DrawDepthNormalMap();
                    DrawDepth();
                    break;
                case RenderMode.NormalMap:
                    DrawDepthNormalMap();
                    DrawSprite(dnmap_renderer.nmap_texture);
                    break;
                case RenderMode.Occlusion:
                    DrawDepthNormalMap();
                    DrawOcclusion();
                    //DrawGaussianBlur();
                    DrawSprite(occ_texture);
                    break;
                case RenderMode.Diffusion:
                    DrawDepthNormalMap();
                    DrawOcclusion();
                    DrawGaussianBlur(1.0f);

                    DrawFigureList();
                    Blit(dev_surface, amb_surface); // from:dev to:amb
                    DrawMain(); // main in:amb occ out:dev

                    Blit(dev_surface, amb_surface); // from:dev to:amb
                    Blit(dev_surface, occ_surface); // from:dev to:occ
                    DrawGaussianBlur(DiffusionConfig.Extent); // gb in:occ out:occ
                    DrawScreen(); // screen in:amb occ out:dev
                    break;
                case RenderMode.Shadow:
                    DrawFigureList();
                    DrawShadow();
                    break;
                default:
                    DrawDepthNormalMap();
                    DrawOcclusion();
                    DrawGaussianBlur(1.0f);

                    DrawFigureList();
                    Blit(dev_surface, amb_surface); // from:dev to:amb
                    DrawMain(); // main in:amb occ out:dev
                    break;
            }
        }

        /// <summary>
        /// シーンをレンダリングします。
        /// </summary>
        public void Render()
        {
            if (!need_render)
                return;

            need_render = false;

            Debug.WriteLine("-- device BeginScene --");
            device.BeginScene();

            if (sprite_enabled)
            {
                string modename = sprite_renderer.CurrentModeName;
                if (modename == "MODEL")
                    SnapTsosOnModelMode();
                if (modename == "SCENE")
                    SnapFiguresOnSceneMode();
            }
            Draw();
            if (sprite_enabled)
            {
                {
                    device.SetRenderState(RenderStates.AlphaBlendEnable, true);

                    sprite_renderer.Render();
                }

                string modename = sprite_renderer.CurrentModeName;
                if (modename == "MODEL")
                    DrawSpritesOnModelMode();
                if (modename == "POSE")
                    DrawSpritesOnPoseMode();
                if (modename == "SCENE")
                    DrawSpritesOnSceneMode();
            }

            Debug.WriteLine("-- device EndScene --");
            device.EndScene();
            {
                int ret;
                if (!device.CheckCooperativeLevel(out ret))
                {
                    switch ((ResultCode)ret)
                    {
                        case ResultCode.DeviceLost:
                            Thread.Sleep(30);
                            return;
                        case ResultCode.DeviceNotReset:
                            device.Reset(device.PresentationParameters);
                            break;
                        default:
                            Console.WriteLine((ResultCode)ret);
                            return;
                    }
                }
            }

            Debug.WriteLine("!! device Present !!");
            device.Present();
        }

        /// config: スクリーン塗りつぶし色
        public Color ScreenColor { get; set; }

        /// config: 文字色
        public Color FontColor { get; set; }

        /// config: 環境光
        public Vector4 Ambient { get; set; }

        /// config: ほほ赤みの濃さ
        public float HohoAlpha { get; set; }

        /// config: enhance depth precision on Format.X8A8G8B8
        public bool XRGBDepth { get; set; }

        Format dmap_format = Format.X8R8G8B8;
        Format nmap_format = Format.X8R8G8B8;

        /// config: depthmap format name
        public void SetDepthMapFormat(string name)
        {
            dmap_format = (Format)Enum.Parse(typeof(Format), name);
        }

        /// config: normalmap format name
        public void SetNormalMapFormat(string name)
        {
            nmap_format = (Format)Enum.Parse(typeof(Format), name);
        }

        /// config: projection mode name
        public void SetProjectionMode(string name)
        {
            projection_mode = (ProjectionMode)Enum.Parse(typeof(ProjectionMode), name);
        }

        /// config: render mode name
        public void SetRenderMode(string name)
        {
            render_mode = (RenderMode)Enum.Parse(typeof(RenderMode), name);
        }

        /// config: gel mode
        public bool MainGel { get; set; }

        /// config: dof mode
        public bool ScreenDof { get; set; }

        /// config: キーボード操作によるボーン移動の変位; マウス距離に換算
        public int KeyGrabNodeDelta { get; set; }

        /// config: キーボード操作によるボーン回転の変位; マウス距離に換算
        public int KeyRotateNodeDelta { get; set; }

        /// config: ボーン移動の速度
        public float GrabNodeSpeed { get; set; }

        /// config: ボーン回転の速度
        public float RotateNodeSpeed { get; set; }

        /// config: カメラ移動の速度
        public float GrabCameraSpeed { get; set; }

        /// config: カメラ回転の速度
        public float RotateCameraSpeed { get; set; }

        /// ライト操作円の中心位置
        public Point LampCenter { get; set; }
        Point lamp_center_on_device;

        /// ライト操作円の半径
        public int LampRadius { get; set; }
        float lamp_radius_on_device;

        /// ボーン選択円の半径
        public int NodeRadius { get; set; }
        float node_radius_on_device;

        /// ボーン操作円の半径
        public int SelectedNodeRadius { get; set; }
        float selected_node_radius_on_device;

        /// <summary>
        /// UVSCR値を得ます。
        /// </summary>
        /// <returns></returns>
        public Vector4 UVSCR()
        {
            float x = Environment.TickCount * 0.000002f;
            return new Vector4(x, 0.0f, 0.0f, 0.0f);
        }

        void DrawTSO(Figure fig, TSOFile tso)
        {
            toon_shader.RemoveShader();

            foreach (TSOMesh mesh in tso.meshes)
                foreach (TSOSubMesh sub_mesh in mesh.sub_meshes)
                {
                    Debug.Assert(sub_mesh.spec >= 0 && sub_mesh.spec < tso.sub_scripts.Length, string.Format("mesh.spec out of range: {0}", sub_mesh.spec));
                    Shader shader = tso.sub_scripts[sub_mesh.spec].shader;

                    //device.RenderState.VertexBlend = (VertexBlend)(4 - 1);
                    VertexBuffer vb = D3DVertexBufferManager.instance.GetDirect3dVertexBufferBySha1(sub_mesh.sha1);
                    device.SetStreamSource(0, vb, 0, 52);

                    toon_shader.SwitchShader(shader, tso.GetDirect3dTextureByName);
                    effect.SetValue(handle_LocalBoneMats, fig.ClipBoneMatrices(sub_mesh));

                    int npass = effect.Begin(0);
                    for (int ipass = 0; ipass < npass; ipass++)
                    {
                        effect.BeginPass(ipass);
                        device.DrawPrimitives(PrimitiveType.TriangleStrip, 0, sub_mesh.NumberVertices - 2);
                        effect.EndPass();
                    }
                    effect.End();
                }
            toon_shader.RemoveShader();
        }

        void AssignWorldViewProjection(ref Matrix world)
        {
            Matrix world_view_matrix = world * Transform_View;
            Matrix world_view_projection_matrix = world_view_matrix * Transform_Projection;

            effect.SetValue("wld", world);
            effect.SetValue("wv", world_view_matrix);
            effect.SetValue("wvp", world_view_projection_matrix);
        }

        void DrawFigure(Figure fig)
        {
            {
                Matrix world;
                fig.GetWorldMatrix(out world);

                AssignWorldViewProjection(ref world);
            }
            effect.SetValue(handle_LightDirForced, fig.LightDirForced);

            foreach (TSOFile tso in fig.TsoList)
            {
                if (tso.Hidden)
                    continue;
                DrawTSO(fig, tso);
            }
        }

        /// <summary>
        /// フィギュアを描画します。
        /// </summary>
        void DrawFigure(Figure fig, Color bg_col)
        {
            Debug.WriteLine("DrawFigure");

            device.SetRenderState(RenderStates.AlphaBlendEnable, true);

            device.Clear(ClearFlags.Target | ClearFlags.ZBuffer | ClearFlags.Stencil, bg_col, 1.0f, 0);

            device.VertexDeclaration = vd;
            effect.SetValue(handle_Ambient, Ambient);
            effect.SetValue(handle_HohoAlpha, HohoAlpha);
            effect.SetValue(handle_UVSCR, UVSCR());

            DrawFigure(fig);
        }

        /// <summary>
        /// フィギュアを描画します。
        /// </summary>
        void DrawFigureList(Color bg_col)
        {
            Debug.WriteLine("DrawFigureList");

            device.SetRenderState(RenderStates.AlphaBlendEnable, true);

            device.Clear(ClearFlags.Target | ClearFlags.ZBuffer | ClearFlags.Stencil, bg_col, 1.0f, 0);

            device.VertexDeclaration = vd;
            effect.SetValue(handle_Ambient, Ambient);
            effect.SetValue(handle_HohoAlpha, HohoAlpha);
            effect.SetValue(handle_UVSCR, UVSCR());

            foreach (Figure fig in FigureList)
            {
                if (fig.Hidden)
                    continue;
                DrawFigure(fig);
            }
        }

        void DrawFigureList()
        {
            DrawFigureList(ScreenColor);
        }

        void DrawShadow()
        {
            effect_clear.Technique = "Clear";
            screen.Draw(effect_clear);
            effect_clear.Technique = "White";
            screen.Draw(effect_clear);
        }

        void DrawDepthNormalMap()
        {
            Debug.WriteLine("DrawDepthNormalMap");

            device.SetRenderState(RenderStates.AlphaBlendEnable, false);

            device.SetRenderTarget(0, dnmap_renderer.dmap_surface);
            device.SetRenderTarget(1, dnmap_renderer.nmap_surface);
            device.DepthStencilSurface = tex_zbuf;
            device.Clear(ClearFlags.ZBuffer, Color.White, 1.0f, 0);

            screen.Draw(dnmap_renderer.effect_dnclear);

            device.VertexDeclaration = vd;

            dnmap_renderer.DrawFigureList(FigureList);

            // restore
            device.SetRenderTarget(0, dev_surface);
            device.SetRenderTarget(1, null); // attention!
            device.DepthStencilSurface = dev_zbuf;
        }

        void DrawSprite(Texture src_texture)
        {
            Debug.WriteLine("DrawSprite");

            device.SetRenderState(RenderStates.AlphaBlendEnable, false);

            device.Clear(ClearFlags.Target, Color.Black, 1.0f, 0);

            sprite.Transform = Matrix.Identity;

            sprite.Begin(0);
            sprite.Draw(src_texture, Rectangle.Empty, new Vector3(0, 0, 0), new Vector3(0, 0, 0), Color.White);
            sprite.End();
        }

        void Blit(Surface source, Surface dest)
        {
            Debug.WriteLine("Blit");

            device.StretchRectangle(source, dev_rect, dest, dev_rect, TextureFilter.Point);
        }

        // draw Gaussian Blur
        // x direction
        //   in occ_texture
        //   out tmp_surface
        // y direction
        //   in tmp_texture
        //   out occ_surface
        void DrawGaussianBlur(float extent)
        {
            Debug.WriteLine("DrawGaussianBlur");

            device.SetRenderState(RenderStates.AlphaBlendEnable, false);

            effect_gb.SetValue("Ambient_texture", occ_texture); // in
            device.SetRenderTarget(0, tmp_surface); // out
            device.DepthStencilSurface = tex_zbuf;

            effect_gb.SetValue("dir", new Vector4(extent / (float)dev_rect.Width, 0, 0, 0));
            screen.Draw(effect_gb);

            effect_gb.SetValue("Ambient_texture", tmp_texture); // in
            device.SetRenderTarget(0, occ_surface); // out
            device.DepthStencilSurface = tex_zbuf;

            effect_gb.SetValue("dir", new Vector4(0, extent / (float)dev_rect.Height, 0, 0));
            screen.Draw(effect_gb);

            // restore
            device.SetRenderTarget(0, dev_surface);
            device.DepthStencilSurface = dev_zbuf;
        }

        // draw Ambient Occlusion
        // in dmap_texture
        // in nmap_texture
        // out occ_surface
        void DrawOcclusion()
        {
            Debug.WriteLine("DrawOcclusion");

            device.SetRenderState(RenderStates.AlphaBlendEnable, false);

            device.SetRenderTarget(0, occ_surface); // out
            device.DepthStencilSurface = tex_zbuf;

            screen.Draw(effect_ao);

            // restore
            device.SetRenderTarget(0, dev_surface);
            device.DepthStencilSurface = dev_zbuf;
        }

        // draw depth
        // in dmap_texture
        // out dev_surface
        void DrawDepth()
        {
            Debug.WriteLine("DrawDepth");

            device.SetRenderState(RenderStates.AlphaBlendEnable, false);

            screen.Draw(dnmap_renderer.effect_depth);
        }

        // draw main
        // in amb_texture
        // in occ_texture
        // out dev_surface
        void DrawMain()
        {
            Debug.WriteLine("DrawMain");

            device.SetRenderState(RenderStates.AlphaBlendEnable, false);

            screen.Draw(effect_main);
        }

        // draw main
        // in amb_texture
        // in occ_texture
        // out dev_surface
        void DrawScreen()
        {
            Debug.WriteLine("DrawScreen");

            device.SetRenderState(RenderStates.AlphaBlendEnable, false);

            screen.Draw(effect_screen);
        }

        /// <summary>
        /// 内部objectを破棄します。
        /// </summary>
        public void Dispose()
        {
            Console.WriteLine("Viewer.Dispose");

            foreach (Figure fig in FigureList)
                fig.Dispose();
            FigureList.Clear();

            OnDeviceLost(device, null);

            if (device != null)
                device.Dispose();
        }

        /// <summary>
        /// バックバッファをBMP形式でファイルに保存します。
        /// </summary>
        /// <param name="file">ファイル名</param>
        public void SaveToBitmap(string file)
        {
            using (Surface sf = device.GetBackBuffer(0, 0, BackBufferType.Mono))
                if (sf != null)
                    SurfaceLoader.Save(file, ImageFileFormat.Bmp, sf);
        }

        /// <summary>
        /// バックバッファをPNG形式でファイルに保存します。
        /// </summary>
        /// <param name="file">ファイル名</param>
        public void SaveToPng(string file)
        {
            using (Surface sf = device.GetBackBuffer(0, 0, BackBufferType.Mono))
                if (sf != null)
                    SurfaceLoader.Save(file, ImageFileFormat.Png, sf);
        }

        string GetSaveFileName(string type)
        {
            DateTime ti = DateTime.Now;
            CultureInfo ci = CultureInfo.InvariantCulture;
            string ti_string = ti.ToString("yyyyMMdd-hhmmss-fff", ci);
            return string.Format("{0}-{1}.png", ti_string, type);
        }

        public void SaveToPng()
        {
            string type = "none";
            switch (RenderMode)
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
            SaveToPng(GetSaveFileName(type));
        }

        /// <summary>
        /// viewport行列を作成します。
        /// </summary>
        /// <param name="viewport">viewport</param>
        /// <returns>viewport行列</returns>
        public static Matrix CreateViewportMatrix(Viewport viewport)
        {
            Matrix m = Matrix.Identity;
            m.M11 = (float)viewport.Width / 2;
            m.M22 = -1.0f * (float)viewport.Height / 2;
            m.M33 = (float)viewport.MaxZ - (float)viewport.MinZ;
            m.M41 = (float)(viewport.X + viewport.Width / 2);
            m.M42 = (float)(viewport.Y + viewport.Height / 2);
            m.M43 = viewport.MinZ;
            return m;
        }

        /// スクリーン位置をワールド座標へ変換します。
        public static Vector3 ScreenToWorld(float screenX, float screenY, float z, Viewport viewport, Matrix view, Matrix proj)
        {
            Vector3 v = new Vector3(screenX, screenY, z);

            Matrix inv_m = Matrix.Invert(CreateViewportMatrix(viewport));
            Matrix inv_proj = Matrix.Invert(proj);
            Matrix inv_view = Matrix.Invert(view);

            return Vector3.TransformCoordinate(v, inv_m * inv_proj * inv_view);
        }

        /// スクリーン位置をワールド座標へ変換します。
        public Vector3 ScreenToWorld(float screenX, float screenY, float z)
        {
            return ScreenToWorld(screenX, screenY, z, device.Viewport, Transform_View, Transform_Projection);
        }

        /// ワールド座標をスクリーン位置へ変換します。
        public static Vector3 WorldToScreen(Vector3 v, Viewport viewport, Matrix view, Matrix proj)
        {
            return Vector3.TransformCoordinate(v, view * proj * CreateViewportMatrix(viewport));
        }

        /// ワールド座標をスクリーン位置へ変換します。
        public Vector3 WorldToScreen(Vector3 v)
        {
            return WorldToScreen(v, device.Viewport, Transform_View, Transform_Projection);
        }
    }
}
