using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.ComponentModel;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using Direct3D = Microsoft.DirectX.Direct3D;

namespace TDCG
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

    /// カメラ設定
    public class CameraConfig
    {
        /// 視野角を変更すると呼ばれる
        public event EventHandler ChangeFovy;
        /// 回転角を変更すると呼ばれる
        public event EventHandler ChangeRoll;

        float fovy = (float)(Math.PI / 6.0);
        float roll = 0;

        /// カメラのY軸（垂直）方向の視野角を得ます。単位は radian です。
        public float Fovy
        {
            get { return fovy; }
        }

        /// カメラのZ軸回転角を得ます。単位は radian です。
        public float Roll
        {
            get { return roll; }
        }

        /// 視野角を得ます。単位は degree です。
        public float GetFovyDegree()
        {
            return (float)(this.fovy * 180.0 / Math.PI);
        }

        /// 視野角を設定します。単位は degree です。
        public void SetFovyDegree(float fovy)
        {
            this.fovy = (float)(Math.PI * fovy / 180.0);
            //Console.WriteLine("update fovy {0} rad", this.fovy);
            if (ChangeFovy != null)
                ChangeFovy(this, EventArgs.Empty);
        }

        /// 回転角を得ます。単位は degree です。
        public float GetRollDegree()
        {
            return (float)(this.roll * 180.0 / Math.PI);
        }

        /// 回転角を設定します。単位は degree です。
        public void SetRollDegree(float roll)
        {
            this.roll = (float)(Math.PI * roll / 180.0);
            //Console.WriteLine("update roll {0} rad", this.roll);
            if (ChangeRoll != null)
                ChangeRoll(this, EventArgs.Empty);
        }
    }

    /// 深度マップ設定
    public class DepthMapConfig
    {
        /// zn を変更すると呼ばれる
        public event EventHandler ChangeZnearPlane;
        /// zf を変更すると呼ばれる
        public event EventHandler ChangeZfarPlane;

        float zn = 15.0f;
        float zf = 50.0f;

        /// 近クリップ面までの距離
        public float ZnearPlane
        {
            get { return zn; }
            set
            {
                zn = value;
                if (ChangeZnearPlane != null)
                    ChangeZnearPlane(this, EventArgs.Empty);
            }
        }

        /// 遠クリップ面までの距離
        public float ZfarPlane
        {
            get { return zf; }
            set
            {
                zf = value;
                if (ChangeZfarPlane != null)
                    ChangeZfarPlane(this, EventArgs.Empty);
            }
        }

        public float Zdistance
        {
            get { return zf - zn; }
        }
    }

    /// occlusion 設定
    public class OcclusionConfig
    {
        /// 強度を変更すると呼ばれる
        public event EventHandler ChangeIntensity;
        /// 半径を変更すると呼ばれる
        public event EventHandler ChangeRadius;

        float intensity = 0.5f;
        float radius = 2.5f;

        /// 強度
        public float Intensity
        {
            get { return intensity; }
            set
            {
                intensity = value;
                if (ChangeIntensity != null)
                    ChangeIntensity(this, EventArgs.Empty);
            }
        }

        /// 半径
        public float Radius
        {
            get { return radius; }
            set
            {
                radius = value;
                if (ChangeRadius != null)
                    ChangeRadius(this, EventArgs.Empty);
            }
        }
    }

    /// diffusion 設定
    public class DiffusionConfig
    {
        /// 強度を変更すると呼ばれる
        public event EventHandler ChangeIntensity;
        /// 範囲を変更すると呼ばれる
        public event EventHandler ChangeExtent;

        float intensity = 0.5f;
        float extent = 2.0f;

        /// 強度
        public float Intensity
        {
            get { return intensity; }
            set
            {
                intensity = value;
                if (ChangeIntensity != null)
                    ChangeIntensity(this, EventArgs.Empty);
            }
        }

        /// 範囲
        public float Extent
        {
            get { return extent; }
            set
            {
                extent = value;
                if (ChangeExtent != null)
                    ChangeExtent(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// セーブファイルの内容を保持します。
    /// </summary>
    public class PNGSaveFile
    {
        /// <summary>
        /// タイプ
        /// </summary>
        public string type = null;
        /// <summary>
        /// 最後に読み込んだライト方向
        /// </summary>
        public Vector3 LightDirection;
        /// <summary>
        /// 最後に読み込んだtmo
        /// </summary>
        public TMOFile Tmo;
        /// <summary>
        /// フィギュアリスト
        /// </summary>
        public List<Figure> FigureList = new List<Figure>();
    }

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
        Effect effect_dnclear;
        Effect effect_dnmap;
        Effect effect_depth;
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

        bool shadow_map_enabled;
        /// <summary>
        /// シャドウマップを作成するか
        /// </summary>
        public bool ShadowMapEnabled { get { return shadow_map_enabled; } }

        Screen screen = null;
        Sprite sprite = null;
        NodeRenderer node_renderer = null;
        SpriteRenderer sprite_renderer = null;

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

        Texture amb_texture;
        Texture dmap_texture;
        Texture nmap_texture;
        Texture randommap_texture;
        Texture occ_texture;
        Texture tmp_texture;

        Surface amb_surface;
        Surface dmap_surface;
        Surface nmap_surface;
        Surface occ_surface;
        Surface tmp_surface;

        /// zbuffer of render target
        Surface tex_zbuf;

        //snap:
        Texture snap_texture;
        Surface snap_surface;

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
            Ambient = new Vector4(1, 1, 1, 1);
            HohoAlpha = 1.0f;
            XRGBDepth = true;
            MainGel = false;
            ScreenDof = false;

            techmap = new Dictionary<string, bool>();
            LoadTechMap();

            manipulator = new Manipulator(camera);
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

                //TODO: selected_node should not be null
                if (node == null)
                    node = fig.Tmo.nodes[0]; // W_Hips

                Vector3 position = Vector3.TransformCoordinate(node.GetWorldPosition(), world);
                camera.SetCenter(position);
            }
        }

        // 指定位置の近傍に selected_node があるか
        // location 座標系: device 生成時の screen 座標系
        bool CloseToSelectedNode(Point location)
        {
            if (selected_node == null)
                return false;

            int screen_center_y = dev_rect.Height / 2;

            Figure fig;
            if (TryGetFigure(out fig))
            {
                Matrix world;
                fig.GetWorldMatrix(out world);

                Vector3 position = Vector3.TransformCoordinate(selected_node.GetWorldPosition(), world);
                Vector3 screen_position = WorldToScreen(position);

                int dx = location.X - (int)screen_position.X;
                int dy = location.Y - (int)screen_position.Y;

                Vector3 view_position = Vector3.TransformCoordinate(position, Transform_View);
                Vector3 view_p = new Vector3(0, 0.25f, view_position.Z);
                Vector3 screen_p = ViewToScreen(view_p);
                float radius = screen_center_y - screen_p.Y;

                return (dx * dx + dy * dy < radius * radius);
            }
            return false;
        }

        // 指定位置の node を selected_node に設定する
        // なければ false を返す
        // location 座標系: device 生成時の screen 座標系
        bool SelectNode(Point location)
        {
            int screen_center_y = dev_rect.Height / 2;

            Figure fig;
            if (TryGetFigure(out fig))
            {
                Matrix world;
                fig.GetWorldMatrix(out world);

                Dictionary<TMONode, float> close_nodes = new Dictionary<TMONode, float>();

                foreach (TMONode node in GetDrawableNodes(fig.Tmo))
                {
                    Vector3 position = Vector3.TransformCoordinate(node.GetWorldPosition(), world);
                    Vector3 screen_position = WorldToScreen(position);

                    int dx = location.X - (int)screen_position.X;
                    int dy = location.Y - (int)screen_position.Y;

                    Vector3 view_position = Vector3.TransformCoordinate(position, Transform_View);
                    Vector3 view_p = new Vector3(0, 0.125f, view_position.Z);
                    Vector3 screen_p = ViewToScreen(view_p);
                    float radius = screen_center_y - screen_p.Y;

                    if (dx * dx + dy * dy < radius * radius)
                    {
                        //近傍なら候補に入れる
                        close_nodes[node] = screen_position.Z;
                    }
                }

                if (close_nodes.Count == 0)
                {
                    return false;
                }

                //近傍のうち最小z値を持つnodeを選択する
                float min_z = 1.0f;

                foreach (var pair in close_nodes)
                {
                    if (pair.Value < min_z)
                    {
                        min_z = pair.Value;
                        selected_node = pair.Key;
                    }
                }
                return true;
            }
            return false;
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

        void SwapTSOFileRow(int arow, int brow)
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

                    fig.TsoList.Sort();
                }
                else
                {
                    if (a != null)
                        a.Row = (byte)brow;
                    else
                        b.Row = (byte)arow;

                    fig.TsoList.Sort();
                }
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
            else
            {
                if (a != null)
                {
                    FigureList.RemoveAt(aidx); // Remove(a)
                    FigureList.Add(a);
                }
                else
                {
                    FigureList.RemoveAt(bidx); // Remove(b)
                    FigureList.Add(b);
                }
            }
        }

        /// マウスボタンを押したときに実行するハンドラ
        protected virtual void form_OnMouseDown(object sender, MouseEventArgs e)
        {
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

                    if (sprite_renderer.Update(sprite_p))
                    {
                        string modename = sprite_renderer.CurrentModeName;
                        if (modename == "MODEL")
                        {
                            if (swap_row != -1)
                            {
                                int row = sprite_renderer.model_mode.SelectedIdx;
                                if (row != swap_row)
                                    SwapTSOFileRow(swap_row, row);
                                swap_row = -1;
                            }
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
                        }
                        need_render = true;
                    }
                    else if (CloseToSelectedNode(screen_p))
                        manipulator.BeginRotateNode(selected_node);
                    else if (SelectNode(screen_p))
                    {
                        sprite_renderer.pose_mode.SelectedNodeName = selected_node.Name;
                        need_render = true;
                    }
                    else
                        manipulator.BeginRotateCamera();
                    break;
                case MouseButtons.Right:
                    Debug.WriteLine("form_OnMouseDown RMB");

                    if (sprite_renderer.Update(sprite_p))
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
                        need_render = true;
                    }
                    else if (CloseToSelectedNode(screen_p))
                        manipulator.BeginGrabNode(selected_node);
                    else
                        manipulator.BeginGrabCamera();
                    break;
            }

            lastScreenPoint = screen_p;
        }

        /// マウスを移動したときに実行するハンドラ
        protected virtual void form_OnMouseMove(object sender, MouseEventArgs e)
        {
            const float delta_scale = 0.125f;

            //device 生成時の screen 座標系に変換する
            Point screen_p = new Point(e.X, e.Y);
            ScaleToScreen(ref screen_p);

            int dx = screen_p.X - lastScreenPoint.X;
            int dy = screen_p.Y - lastScreenPoint.Y;

            switch (e.Button)
            {
                case MouseButtons.Left:
                    if (manipulator.WhileRotateNode(dx, dy))
                    {
                        //TODO: UpdateSelectedBoneMatrices
                        GetSelectedFigure().UpdateBoneMatrices(true);
                    }
                    manipulator.WhileRotateCamera(dx, dy);
                    break;
                case MouseButtons.Middle:
                    Camera.MoveView(-dx * delta_scale, dy * delta_scale);
                    break;
                case MouseButtons.Right:
                    if (manipulator.WhileGrabNode(dx, dy))
                    {
                        //TODO: UpdateSelectedBoneMatrices
                        GetSelectedFigure().UpdateBoneMatrices(true);
                    }
                    manipulator.WhileGrabCamera(dx, dy);
                    break;
            }

            lastScreenPoint = screen_p;
        }

        protected virtual void form_OnMouseUp(object sender, MouseEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButtons.Left:
                    manipulator.EndRotateNode();
                    manipulator.EndRotateCamera();
                    break;
                case MouseButtons.Right:
                    manipulator.EndGrabNode();
                    manipulator.EndGrabCamera();
                    break;
            }
        }

        // コントロールのサイズを変更したときに実行するハンドラ
        protected virtual void form_OnResize(object sender, EventArgs e)
        {
            need_render = true;
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
                    if (!append)
                        ClearFigureList();
                    LoadTSOFile(source_file);
                    break;
                case ".tmo":
                    LoadTMOFile(source_file);
                    break;
                case ".png":
                    AddFigureFromPNGFile(source_file, append);
                    break;
                default:
                    if (!append)
                        ClearFigureList();
                    if (Directory.Exists(source_file))
                        AddFigureFromTSODirectory(source_file);
                    break;
            }
        }

        List<float> ReadFloats(string file)
        {
            List<float> floats = new List<float>();
            string line;
            using (StreamReader source = new StreamReader(File.OpenRead(file)))
            while ((line = source.ReadLine()) != null)
            {
                floats.Add(Single.Parse(line));
            }
            return floats;
        }

        int GetFiguresCount(string path)
        {
            int num = 0;
            while ( true )
            {
                string file = Path.Combine(path, string.Format("Figure{0}.txt", num + 1));
                if (! File.Exists(file))
                    break;
                num++;
            }
            return num;
        }

        public void LoadScene(string path, bool append)
        {
            PNGSaveFile sav = new PNGSaveFile();
            {
                Figure fig = null;

                //png.Cami
                {
                    List<float> factor = ReadFloats(Path.Combine(path, "Camera.txt"));

                    camera.Reset();
                    camera.Translation = new Vector3(-factor[0], -factor[1], -factor[2]);
                    camera.Angle = new Vector3(-factor[5], -factor[4], -factor[6]);
                }

                int figures_count = GetFiguresCount(path);
                for (int i = 0; i < figures_count; i++)
                {
                    //png.Lgta
                    {
                        List<float> factor = ReadFloats(Path.Combine(path, string.Format("LightA{0}.txt", i + 1)));

                        Matrix m;
                        m.M11 = factor[0];
                        m.M12 = factor[1];
                        m.M13 = factor[2];
                        m.M14 = factor[3];

                        m.M21 = factor[4];
                        m.M22 = factor[5];
                        m.M23 = factor[6];
                        m.M24 = factor[7];

                        m.M31 = factor[8];
                        m.M32 = factor[9];
                        m.M33 = factor[10];
                        m.M34 = factor[11];

                        m.M41 = factor[12];
                        m.M42 = factor[13];
                        m.M43 = factor[14];
                        m.M44 = factor[15];

                        sav.LightDirection = Vector3.TransformCoordinate(new Vector3(0.0f, 0.0f, -1.0f), m);
                    }
                    //png.Ftmo
                    {
                        sav.Tmo = new TMOFile();
                        sav.Tmo.Load(Path.Combine(path, string.Format("{0}.tmo", i + 1)));
                    }
                    //png.Figu
                    {
                        fig = new Figure();
                        fig.LightDirection = sav.LightDirection;
                        fig.Tmo = sav.Tmo;
                        sav.FigureList.Add(fig);

                        List<float> ratios = ReadFloats(Path.Combine(path, string.Format("Figure{0}.txt", i + 1)));
                        /*
                        ◆FIGU
                        スライダの位置。値は float型で 0.0 .. 1.0
                            0: 姉妹
                            1: うで
                            2: あし
                            3: 胴まわり
                            4: おっぱい
                            5: つり目たれ目
                            6: やわらか
                         */
                        if (fig.slider_matrix != null)
                        {
                            fig.slider_matrix.AgeRatio = ratios[0];
                            fig.slider_matrix.ArmRatio = ratios[1];
                            fig.slider_matrix.LegRatio = ratios[2];
                            fig.slider_matrix.WaistRatio = ratios[3];
                            fig.slider_matrix.OppaiRatio = ratios[4];
                            fig.slider_matrix.EyeRatio = ratios[5];
                        }
                    }
                    //png.Ftso
                    foreach (string file in Directory.GetFiles(Path.Combine(path, string.Format("{0}", i + 1)), "*.tso"))
                    {
                        TSOFile tso = new TSOFile();
                        tso.Load(file);
                        string code = Path.GetFileNameWithoutExtension(file);
                        tso.Row = Convert.ToByte(code, 16);
                        fig.TsoList.Add(tso);
                    }
                }
            }

            {
                if (!append)
                    ClearFigureList();

                int idx = FigureList.Count;
                foreach (Figure fig in sav.FigureList)
                {
                    fig.OpenTSOFile(device, effect);
                    //todo: override List.Add
                    fig.ComputeClothed();
                    fig.UpdateNodeMapAndBoneMatrices();
                    //todo: override List#Add
                    FigureList.Add(fig);
                    fig.UpdateBoneMatricesEvent += delegate (object sender, EventArgs e)
                    {
                        if (GetSelectedFigure() == sender)
                            need_render = true;
                    };
                }
                SetFigureIndex(idx);
                if (FigureUpdateEvent != null)
                    FigureUpdateEvent(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// フィギュア選択時に呼び出されるハンドラ
        /// </summary>
        public event EventHandler FigureSelectEvent;

        /// <summary>
        /// フィギュア更新時に呼び出されるハンドラ
        /// </summary>
        public event EventHandler FigureUpdateEvent;

        /// <summary>
        /// フィギュアを選択します。
        /// </summary>
        /// <param name="idx">フィギュア番号</param>
        public void SetFigureIndex(int idx)
        {
            if (idx < 0)
                idx = 0;
            if (idx > FigureList.Count - 1)
                idx = FigureList.Count - 1;
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
            {
                tso.Open(device, effect);
                fig.TsoList.Add(tso);
            }
            //todo: override List.Add
            fig.ComputeClothed();
            fig.UpdateNodeMapAndBoneMatrices();
            int idx = FigureList.Count;
            //todo: override List#Add
            FigureList.Add(fig);
            fig.UpdateBoneMatricesEvent += delegate (object sender, EventArgs e)
            {
                if (GetSelectedFigure() == sender)
                    need_render = true;
            };
            SetFigureIndex(idx);
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
        /// 選択フィギュアを得ます。なければ作成します。
        /// </summary>
        public Figure GetSelectedOrCreateFigure()
        {
            Figure fig = GetSelectedFigure();

            if (fig == null)
            {
                fig = new Figure();
                int idx = FigureList.Count;
                //todo: override List#Add
                FigureList.Add(fig);
                fig.UpdateBoneMatricesEvent += delegate (object sender, EventArgs e)
                {
                    if (GetSelectedFigure() == sender)
                        need_render = true;
                };
                SetFigureIndex(idx);
            }
            return fig;
        }

        /// <summary>
        /// 指定パスからTSOFileを読み込みます。
        /// </summary>
        /// <param name="source_file">パス</param>
        public void LoadTSOFile(string source_file)
        {
            Debug.WriteLine("loading " + source_file);
            using (Stream source_stream = File.OpenRead(source_file))
                LoadTSOFile(source_stream, source_file);
        }

        /// <summary>
        /// 指定ストリームからTSOFileを読み込みます。
        /// </summary>
        /// <param name="source_stream">ストリーム</param>
        public void LoadTSOFile(Stream source_stream)
        {
            LoadTSOFile(source_stream, null);
        }

        /// <summary>
        /// 指定ストリームからTSOFileを読み込みます。
        /// </summary>
        /// <param name="source_stream">ストリーム</param>
        /// <param name="file">ファイル名</param>
        public void LoadTSOFile(Stream source_stream, string file)
        {
            List<TSOFile> tso_list = new List<TSOFile>();
            try
            {
                TSOFile tso = new TSOFile();
                tso.Load(source_stream);
                tso.FileName = file != null ? Path.GetFileNameWithoutExtension(file) : null;
                tso_list.Add(tso);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex);
            }
            Figure fig = GetSelectedOrCreateFigure();
            foreach (TSOFile tso in tso_list)
            {
                tso.Open(device, effect);
                fig.TsoList.Add(tso);
            }
            //todo: override List.Add
            fig.ComputeClothed();
            fig.UpdateNodeMapAndBoneMatrices();
            if (FigureUpdateEvent != null)
                FigureUpdateEvent(this, EventArgs.Empty);
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
            if (idx > FigureList.Count - 1)
                idx = 0;
            SetFigureIndex(idx);
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
                if (FigureUpdateEvent != null)
                    FigureUpdateEvent(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// 指定パスからPNGFileを読み込みフィギュアを作成して追加します。
        /// </summary>
        /// <param name="source_file">PNGFile のパス</param>
        /// <param name="append">FigureListを消去せずに追加するか</param>
        public void AddFigureFromPNGFile(string source_file, bool append)
        {
            PNGSaveFile sav = LoadPNGFile(source_file);
            if (sav.FigureList.Count == 0) //POSE png
            {
                Debug.Assert(sav.Tmo != null, "save.Tmo should not be null");
                Figure fig;
                if (TryGetFigure(out fig))
                {
                    if (sav.LightDirection != Vector3.Empty)
                        fig.LightDirection = sav.LightDirection;
                    fig.Tmo = sav.Tmo;
                    fig.UpdateNodeMapAndBoneMatrices();
                    if (FigureUpdateEvent != null)
                        FigureUpdateEvent(this, EventArgs.Empty);
                }
            }
            else
            {
                if (!append)
                    ClearFigureList();

                int idx = FigureList.Count;
                foreach (Figure fig in sav.FigureList)
                {
                    fig.OpenTSOFile(device, effect);
                    //todo: override List.Add
                    fig.ComputeClothed();
                    fig.UpdateNodeMapAndBoneMatrices();
                    //todo: override List#Add
                    FigureList.Add(fig);
                    fig.UpdateBoneMatricesEvent += delegate (object sender, EventArgs e)
                    {
                        if (GetSelectedFigure() == sender)
                            need_render = true;
                    };
                }
                SetFigureIndex(idx);
                if (FigureUpdateEvent != null)
                    FigureUpdateEvent(this, EventArgs.Empty);
            }
        }

        SimpleCamera camera = new SimpleCamera();
        Manipulator manipulator;

        /// <summary>
        /// カメラ
        /// </summary>
        public SimpleCamera Camera { get { return camera; } set { camera = value; } }

        /// <summary>
        /// world行列
        /// </summary>
        protected Matrix world_matrix = Matrix.Identity;
        /// <summary>
        /// view変換行列
        /// </summary>
        protected Matrix Transform_View = Matrix.Identity;
        /// <summary>
        /// projection変換行列
        /// </summary>
        protected Matrix Transform_Projection = Matrix.Identity;

        /// <summary>
        /// deviceを作成します。
        /// </summary>
        /// <param name="control">レンダリング先となるcontrol</param>
        /// <param name="shadow_map_enabled">シャドウマップを作成するか</param>
        /// <returns>deviceの作成に成功したか</returns>
        public bool InitializeApplication(Control control, bool shadow_map_enabled = false)
        {
            this.shadow_map_enabled = shadow_map_enabled;
            SetControl(control);

            control.MouseDown += new MouseEventHandler(form_OnMouseDown);
            control.MouseMove += new MouseEventHandler(form_OnMouseMove);
            control.MouseUp += new MouseEventHandler(form_OnMouseUp);
            control.Resize += new EventHandler(form_OnResize);

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

            if (!LoadEffect(@"clear.fx", out effect_clear))
                return false;

            if (!LoadEffect(@"dnclear.fx", out effect_dnclear))
                return false;

            if (!LoadEffect(@"dnmap.fx", out effect_dnmap, macros, effect_pool))
                return false;

            if (!LoadEffect(@"depth.fx", out effect_depth, macros))
                return false;

            if (!LoadEffect(@"ao.fx", out effect_ao, macros))
                return false;

            if (!LoadEffect(@"gb.fx", out effect_gb))
                return false;

            if (!LoadEffect(@"main.fx", out effect_main, macros))
                return false;

            if (!LoadEffect(@"screen.fx", out effect_screen, macros))
                return false;

            node_renderer = new NodeRenderer(device, sprite);

            if (!LoadEffect(@"circle.fx", out node_renderer.effect_circle, macros))
                return false;

            if (!LoadEffect(@"pole.fx", out node_renderer.effect_pole, macros))
                return false;

            handle_LocalBoneMats = effect.GetParameter(null, "LocalBoneMats");
            handle_LightDirForced = effect.GetParameter(null, "LightDirForced");
            handle_Ambient = effect.GetParameter(null, "ColorRate");
            handle_HohoAlpha = effect.GetParameter(null, "HohoAlpha");
            handle_UVSCR = effect.GetParameter(null, "UVSCR");

            screen = new Screen(device);
            sprite = new Sprite(device);
            sprite_renderer = new SpriteRenderer(device, sprite);
            camera.Update();
            OnDeviceReset(device, null);

            FigureSelectEvent += delegate (object sender, EventArgs e)
            {
                Figure fig;
                if (TryGetFigure(out fig))
                {
                    selected_node = fig.Tmo.nodes[0]; // W_Hips
                    sprite_renderer.pose_mode.SelectedNodeName = selected_node.Name;
                }
                need_render = true;
            };
            FigureUpdateEvent += delegate (object sender, EventArgs e)
            {
                need_render = true;
            };
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

        bool LoadEffect(string effect_filename, out Effect effect, Macro[] macros = null, EffectPool effect_pool = null)
        {
            effect = null;

            string effect_file = Path.Combine(Application.StartupPath, effect_filename);
            if (!File.Exists(effect_file))
            {
                Console.WriteLine("File not found: " + effect_file);
                return false;
            }
            using (FileStream effect_stream = File.OpenRead(effect_file))
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

            effect_dnmap.SetValue("zp", zp); // in
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

            node_renderer.Dispose();
            sprite_renderer.Dispose();

            if (sprite != null)
                sprite.Dispose();
            if (screen != null)
                screen.Dispose();

            //snap:
            if (snap_surface != null)
                snap_surface.Dispose();
            if (snap_texture != null)
                snap_texture.Dispose();

            if (tex_zbuf != null)
                tex_zbuf.Dispose();

            if (amb_surface != null)
                amb_surface.Dispose();
            if (dmap_surface != null)
                dmap_surface.Dispose();
            if (nmap_surface != null)
                nmap_surface.Dispose();
            if (occ_surface != null)
                occ_surface.Dispose();
            if (tmp_surface != null)
                tmp_surface.Dispose();

            if (amb_texture != null)
                amb_texture.Dispose();
            if (dmap_texture != null)
                dmap_texture.Dispose();
            if (nmap_texture != null)
                nmap_texture.Dispose();
            if (randommap_texture != null)
                randommap_texture.Dispose();
            if (occ_texture != null)
                occ_texture.Dispose();
            if (tmp_texture != null)
                tmp_texture.Dispose();

            if (dev_zbuf != null)
                dev_zbuf.Dispose();
            if (dev_surface != null)
                dev_surface.Dispose();
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

            amb_texture = new Texture(device, devw, devh, 1, Usage.RenderTarget, Format.X8R8G8B8, Pool.Default);
            amb_surface = amb_texture.GetSurfaceLevel(0);

            dmap_texture = new Texture(device, devw, devh, 1, Usage.RenderTarget, dmap_format, Pool.Default);
            dmap_surface = dmap_texture.GetSurfaceLevel(0);

            nmap_texture = new Texture(device, devw, devh, 1, Usage.RenderTarget, nmap_format, Pool.Default);
            nmap_surface = nmap_texture.GetSurfaceLevel(0);

            randommap_texture = TextureLoader.FromFile(device, GetRandomTexturePath());

            occ_texture = new Texture(device, devw, devh, 1, Usage.RenderTarget, Format.X8R8G8B8, Pool.Default);
            occ_surface = occ_texture.GetSurfaceLevel(0);

            tmp_texture = new Texture(device, devw, devh, 1, Usage.RenderTarget, Format.X8R8G8B8, Pool.Default);
            tmp_surface = tmp_texture.GetSurfaceLevel(0);

            tex_zbuf = device.CreateDepthStencilSurface(devw, devh, DepthFormat.D16, MultiSampleType.None, 0, false);

            //snap:
            snap_texture = new Texture(device, 1024, 1024, 1, Usage.RenderTarget, Format.X8R8G8B8, Pool.Default);
            snap_surface = snap_texture.GetSurfaceLevel(0);

            node_renderer.Create(dev_rect);
            sprite_renderer.Create(dev_rect);

            effect_depth.SetValue("DepthMap_texture", dmap_texture); // in

            effect_ao.SetValue("DepthMap_texture", dmap_texture); // in
            effect_ao.SetValue("NormalMap_texture", nmap_texture); // in
            effect_ao.SetValue("RandomMap_texture", randommap_texture); // in

            effect_main.SetValue("Ambient_texture", amb_texture); // in
            effect_main.SetValue("Occlusion_texture", occ_texture); // in

            effect_screen.SetValue("DepthMap_texture", dmap_texture); // in
            effect_screen.SetValue("Ambient_texture", amb_texture); // in
            effect_screen.SetValue("Occlusion_texture", occ_texture); // in

            screen.Create(dev_rect);

            AssignProjection();

            screen.AssignWorldViewProjection(effect_clear);
            screen.AssignWorldViewProjection(effect_dnclear);
            screen.AssignWorldViewProjection(effect_depth);
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
            device.SetRenderState(RenderStates.ReferenceAlpha, 0x08);
            device.SetRenderState(RenderStates.AlphaFunction, (int)Compare.GreaterEqual);

            vd = new VertexDeclaration(device, TSOSubMesh.ve);

            //device.RenderState.IndexedVertexBlendEnable = true;
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
            SetFigureIndex(0);
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
                SetFigureIndex(idx - 1);
            }
            fig = null;
            // free meshes and textures.
            Console.WriteLine("Total Memory: {0}", GC.GetTotalMemory(true));
        }

        /// <summary>
        /// スプライトの有無
        /// </summary>
        [Obsolete("use RenderMode")]
        public bool SpriteShown = false;

        public void Update()
        {
            if (camera.NeedUpdate)
            {
                camera.Update();

                Transform_View = camera.ViewMatrix;
                // xxx: for w-buffering
                device.Transform.View = Transform_View;
                effect.SetValue("view", Transform_View);

                AssignProjection();
                AssignDepthProjection();

                node_renderer.Rotation_Camera = camera.RotationMatrix;
                node_renderer.Transform_View = Transform_View;
                node_renderer.Transform_Projection = Transform_Projection;

                need_render = true;
            }
        }

        bool need_render = true;

        /// <summary>
        /// レンダリングは必要か
        /// </summary>
        public bool NeedRender { get { return need_render; } }

        /// <summary>
        /// レンダリングするのに用いるデリゲート型
        /// </summary>
        public delegate void RenderingHandler();

        /// <summary>
        /// レンダリングするハンドラ
        /// </summary>
        public RenderingHandler Rendering;

        public void AssignWorldViewProjection()
        {
            Matrix world_view_matrix = world_matrix * Transform_View;
            Matrix world_view_projection_matrix = world_view_matrix * Transform_Projection;

            effect.SetValue("wld", world_matrix);
            effect.SetValue("wv", world_view_matrix);
            effect.SetValue("wvp", world_view_projection_matrix);
        }

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

            AssignWorldViewProjection();

            switch (RenderMode)
            {
                case RenderMode.Ambient:
                    string modename = sprite_renderer.CurrentModeName;
                    if (modename == "MODEL")
                    {
                        device.SetRenderState(RenderStates.AlphaBlendEnable, true);

                        device.SetRenderTarget(0, dev_surface);
                        device.DepthStencilSurface = dev_zbuf;
                        //device.Clear(ClearFlags.Target | ClearFlags.ZBuffer | ClearFlags.Stencil, ScreenColor, 1.0f, 0);

                        device.VertexDeclaration = vd;
                        effect.SetValue(handle_Ambient, Ambient);
                        effect.SetValue(handle_HohoAlpha, HohoAlpha);
                        effect.SetValue(handle_UVSCR, UVSCR());

                        Figure fig;
                        if (TryGetFigure(out fig))
                        {
                            foreach (TSOFile tso in fig.TsoList)
                            {
                                int idx = tso.Row;

                                device.Clear(ClearFlags.Target | ClearFlags.ZBuffer | ClearFlags.Stencil, ScreenColor, 1.0f, 0);

                                DrawTSO(fig, tso);
                                SnapTSO(idx);
                            }
                        }
                    }

                    if (modename == "SCENE")
                    {
                        device.SetRenderState(RenderStates.AlphaBlendEnable, true);

                        device.SetRenderTarget(0, dev_surface);
                        device.DepthStencilSurface = dev_zbuf;
                        //device.Clear(ClearFlags.Target | ClearFlags.ZBuffer | ClearFlags.Stencil, ScreenColor, 1.0f, 0);

                        device.VertexDeclaration = vd;
                        effect.SetValue(handle_Ambient, Ambient);
                        effect.SetValue(handle_HohoAlpha, HohoAlpha);
                        effect.SetValue(handle_UVSCR, UVSCR());

                        int idx = 0;
                        foreach (Figure fig in FigureList)
                        {
                            device.Clear(ClearFlags.Target | ClearFlags.ZBuffer | ClearFlags.Stencil, ScreenColor, 1.0f, 0);

                            DrawFigure(fig);
                            SnapFigure(idx);

                            idx++;
                        }
                    }

                    DrawFigure();

                    {
                        device.SetRenderState(RenderStates.AlphaBlendEnable, true);

                        device.SetRenderTarget(0, dev_surface);
                        device.DepthStencilSurface = dev_zbuf;
                        //device.Clear(ClearFlags.Target, Color.Black, 1.0f, 0);

                        sprite_renderer.Render();
                    }

                    if (modename == "MODEL")
                    {
                        DrawSpriteSnapTSO();
                        if (swap_row != -1)
                            sprite_renderer.model_mode.DrawDottedSprite(sprite_renderer.model_mode.SelectedIdx);
                        else
                            sprite_renderer.model_mode.DrawCursorSprite(sprite_renderer.model_mode.SelectedIdx);
                    }
                    if (modename == "POSE")
                    {
                        Figure fig;
                        if (TryGetFigure(out fig))
                        {
                            device.SetRenderState(RenderStates.AlphaBlendEnable, true);

                            device.SetRenderTarget(0, dev_surface);
                            device.DepthStencilSurface = dev_zbuf;
                            //device.Clear(ClearFlags.Target, Color.Black, 1.0f, 0);

                            node_renderer.Render(fig, selected_node, GetDrawableNodes(fig.Tmo));
                        }
                    }
                    if (modename == "SCENE")
                    {
                        DrawSpriteSnapFigure();
                        if (swap_idx != -1)
                            sprite_renderer.scene_mode.DrawDottedSprite(sprite_renderer.scene_mode.SelectedIdx);
                        else
                            sprite_renderer.scene_mode.DrawCursorSprite(sprite_renderer.scene_mode.SelectedIdx);
                    }
                    break;
                case RenderMode.DepthMap:
                    DrawDepthNormalMap();
                    DrawDepth();
                    break;
                case RenderMode.NormalMap:
                    DrawDepthNormalMap();
                    DrawSprite(nmap_texture);
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

                    DrawFigure();
                    Blit(dev_surface, amb_surface); // from:dev to:amb
                    DrawMain(); // main in:amb occ out:dev

                    Blit(dev_surface, amb_surface); // from:dev to:amb
                    Blit(dev_surface, occ_surface); // from:dev to:occ
                    DrawGaussianBlur(DiffusionConfig.Extent); // gb in:occ out:occ
                    DrawScreen(); // screen in:amb occ out:dev
                    break;
                case RenderMode.Shadow:
                    DrawFigure();
                    DrawShadow();
                    break;
                default:
                    DrawDepthNormalMap();
                    DrawOcclusion();
                    DrawGaussianBlur(1.0f);

                    DrawFigure();
                    Blit(dev_surface, amb_surface); // from:dev to:amb
                    DrawMain(); // main in:amb occ out:dev
                    break;
            }

            if (Rendering != null)
                Rendering();

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
            //Thread.Sleep(30);
        }

        /// config: スクリーン塗りつぶし色
        public Color ScreenColor { get; set; }

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

        /// <summary>
        /// UVSCR値を得ます。
        /// </summary>
        /// <returns></returns>
        public Vector4 UVSCR()
        {
            float x = Environment.TickCount * 0.000002f;
            return new Vector4(x, 0.0f, 0.0f, 0.0f);
        }

        protected virtual void DrawTSO(Figure fig, TSOFile tso)
        {
            tso.BeginRender();

            foreach (TSOMesh mesh in tso.meshes)
                foreach (TSOSubMesh sub_mesh in mesh.sub_meshes)
                {
                    //device.RenderState.VertexBlend = (VertexBlend)(4 - 1);
                    device.SetStreamSource(0, sub_mesh.vb, 0, 52);

                    tso.SwitchShader(sub_mesh);
                    effect.SetValue(handle_LocalBoneMats, fig.ClipBoneMatrices(sub_mesh));

                    int npass = effect.Begin(0);
                    for (int ipass = 0; ipass < npass; ipass++)
                    {
                        effect.BeginPass(ipass);
                        device.DrawPrimitives(PrimitiveType.TriangleStrip, 0, sub_mesh.vertices.Length - 2);
                        effect.EndPass();
                    }
                    effect.End();
                }
            tso.EndRender();
        }

        protected virtual void DrawFigure(Figure fig)
        {
            {
                Matrix world;
                fig.GetWorldMatrix(out world);

                Matrix world_view_matrix = world * Transform_View;
                Matrix world_view_projection_matrix = world_view_matrix * Transform_Projection;
                effect.SetValue("wld", world);
                effect.SetValue("wv", world_view_matrix);
                effect.SetValue("wvp", world_view_projection_matrix);
            }
            effect.SetValue(handle_LightDirForced, fig.LightDirForced());

            foreach (TSOFile tso in fig.TsoList)
                DrawTSO(fig, tso);
        }

        /// <summary>
        /// フィギュアを描画します。
        /// </summary>
        protected virtual void DrawFigure()
        {
            Debug.WriteLine("DrawFigure");

            device.SetRenderState(RenderStates.AlphaBlendEnable, true);

            device.SetRenderTarget(0, dev_surface);
            device.DepthStencilSurface = dev_zbuf;
            device.Clear(ClearFlags.Target | ClearFlags.ZBuffer | ClearFlags.Stencil, ScreenColor, 1.0f, 0);

            device.VertexDeclaration = vd;
            effect.SetValue(handle_Ambient, Ambient);
            effect.SetValue(handle_HohoAlpha, HohoAlpha);
            effect.SetValue(handle_UVSCR, UVSCR());

            foreach (Figure fig in FigureList)
                DrawFigure(fig);
        }

        void DrawShadow()
        {
            effect_clear.Technique = "Clear";
            screen.Draw(effect_clear);
            effect_clear.Technique = "White";
            screen.Draw(effect_clear);
        }

        static string GetHideTechsPath()
        {
            return Path.Combine(Application.StartupPath, @"hidetechs.txt");
        }

        static string GetRandomTexturePath()
        {
            return Path.Combine(Application.StartupPath, @"rand.png");
        }

        Dictionary<string, bool> techmap;

        void LoadTechMap()
        {
            char[] delim = { ' ' };
            using (StreamReader source = new StreamReader(File.OpenRead(GetHideTechsPath())))
            {
                string line;
                while ((line = source.ReadLine()) != null)
                {
                    string[] tokens = line.Split(delim);
                    string op = tokens[0];
                    if (op == "hide")
                    {
                        Debug.Assert(tokens.Length == 2, "tokens length should be 2");
                        string techname = tokens[1];
                        techmap[techname] = true;
                    }
                }
            }
        }

        bool HiddenTechnique(string technique)
        {
            return techmap.ContainsKey(technique);
        }

        // draw depthmap and normalmap
        // out dmap_surface
        // out nmap_surface
        void DrawDepthNormalMap()
        {
            Debug.WriteLine("DrawDepthNormalMap");

            device.SetRenderState(RenderStates.AlphaBlendEnable, false);

            device.SetRenderTarget(0, dmap_surface);
            device.SetRenderTarget(1, nmap_surface);

            device.DepthStencilSurface = tex_zbuf;
            device.Clear(ClearFlags.ZBuffer, Color.White, 1.0f, 0);

            screen.Draw(effect_dnclear);

            device.VertexDeclaration = vd;

            foreach (Figure fig in FigureList)
            {
                {
                    Matrix world;
                    fig.GetWorldMatrix(out world);

                    Matrix world_view_matrix = world * Transform_View;
                    Matrix world_view_projection_matrix = world_view_matrix * Transform_Projection;
                    effect.SetValue("wld", world);
                    effect.SetValue("wv", world_view_matrix);
                    effect.SetValue("wvp", world_view_projection_matrix);
                }
                foreach (TSOFile tso in fig.TsoList)
                {
                    tso.BeginRender();

                    foreach (TSOMesh mesh in tso.meshes)
                        foreach (TSOSubMesh sub_mesh in mesh.sub_meshes)
                        {
                            Shader shader = tso.sub_scripts[sub_mesh.spec].shader;

                            if (HiddenTechnique(shader.technique))
                                continue;

                            //device.RenderState.VertexBlend = (VertexBlend)(4 - 1);
                            device.SetStreamSource(0, sub_mesh.vb, 0, 52);

                            tso.SwitchShaderColorTex(shader);
                            effect.SetValue(handle_LocalBoneMats, fig.ClipBoneMatrices(sub_mesh)); // shared

                            int npass = effect_dnmap.Begin(0);
                            for (int ipass = 0; ipass < npass; ipass++)
                            {
                                effect_dnmap.BeginPass(ipass);
                                device.DrawPrimitives(PrimitiveType.TriangleStrip, 0, sub_mesh.vertices.Length - 2);
                                effect_dnmap.EndPass();
                            }
                            effect_dnmap.End();
                        }
                    tso.EndRender();
                }
            }
            device.SetRenderTarget(1, null); // attention!
        }

        void DrawSprite(Texture src_texture)
        {
            Debug.WriteLine("DrawSprite");

            device.SetRenderState(RenderStates.AlphaBlendEnable, false);

            device.SetRenderTarget(0, dev_surface);
            device.DepthStencilSurface = dev_zbuf;
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

        void DrawSpriteSnapTSO()
        {
            Debug.WriteLine("DrawSpriteSnapTSO");

            device.SetRenderState(RenderStates.AlphaBlendEnable, false);

            device.SetRenderTarget(0, dev_surface);
            device.DepthStencilSurface = dev_zbuf;
            //device.Clear(ClearFlags.Target, Color.Black, 1.0f, 0);

            sprite.Transform = Matrix.Scaling(dev_rect.Width / 1024.0f * 0.75f, dev_rect.Height / 768.0f * 0.75f, 1.0f);

            Figure fig;
            if (TryGetFigure(out fig))
            {
                sprite.Begin(0);

                foreach (TSOFile tso in fig.TsoList)
                {
                    int idx = tso.Row;
                    int x16 = (idx%8)*7 + 4;
                    int y16 = (idx/8)*9 + 5;

                    sprite.Draw(snap_texture, new Rectangle((idx%8)*128, (idx/8)*128, 128, 128), new Vector3(0, 0, 0), new Vector3(x16 * 16 / 0.75f, y16 * 16 / 0.75f, 0), Color.White);
                }
                sprite.End();
            }
        }

        void DrawSpriteSnapFigure()
        {
            Debug.WriteLine("DrawSpriteSnapFigure");

            device.SetRenderState(RenderStates.AlphaBlendEnable, false);

            device.SetRenderTarget(0, dev_surface);
            device.DepthStencilSurface = dev_zbuf;
            //device.Clear(ClearFlags.Target, Color.Black, 1.0f, 0);

            sprite.Transform = Matrix.Scaling(dev_rect.Width / 2048.0f, dev_rect.Height / 1536.0f, 1.0f);

            sprite.Begin(0);
            int idx = 0;
            foreach (Figure fig in FigureList)
            {
                int x16 = (idx%6)*9 + 5;
                int y16 = (idx/6)*9 + 5;

                sprite.Draw(snap_texture, new Rectangle((idx%4)*256, (idx/4)*192, 256, 192), new Vector3(0, 0, 0), new Vector3(x16 * 32, y16 * 32, 0), Color.White);

                idx++;
            }
            sprite.End();
        }

        void SnapTSO(int idx)
        {
            Debug.WriteLine("SnapTSO");

            int w = dev_rect.Width;
            int h = dev_rect.Height;
            Rectangle square_rect;
            if (h < w)
                square_rect = new Rectangle((w-h)/2, 0, h, h);
            else
                square_rect = new Rectangle(0, (h-w)/2, w, w);

            device.StretchRectangle(dev_surface, square_rect, snap_surface, new Rectangle((idx%8)*128, (idx/8)*128, 128, 128), TextureFilter.Point);
        }

        void SnapFigure(int idx)
        {
            Debug.WriteLine("SnapFigure");

            device.StretchRectangle(dev_surface, dev_rect, snap_surface, new Rectangle((idx%4)*256, (idx/4)*192, 256, 192), TextureFilter.Point);
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
        }

        // draw depth
        // in dmap_texture
        // out dev_surface
        void DrawDepth()
        {
            Debug.WriteLine("DrawDepth");

            device.SetRenderState(RenderStates.AlphaBlendEnable, false);

            device.SetRenderTarget(0, dev_surface); // out
            device.DepthStencilSurface = dev_zbuf;

            screen.Draw(effect_depth);
        }

        // draw main
        // in amb_texture
        // in occ_texture
        // out dev_surface
        void DrawMain()
        {
            Debug.WriteLine("DrawMain");

            device.SetRenderState(RenderStates.AlphaBlendEnable, false);

            device.SetRenderTarget(0, dev_surface); // out
            device.DepthStencilSurface = dev_zbuf;

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

            device.SetRenderTarget(0, dev_surface); // out
            device.DepthStencilSurface = dev_zbuf;

            screen.Draw(effect_screen);
        }

        /// <summary>
        /// Direct3Dメッシュを描画します。
        /// </summary>
        /// <param name="mesh">メッシュ</param>
        /// <param name="wld">ワールド変換行列</param>
        /// <param name="color">描画色</param>
        public void DrawMesh(Mesh mesh, Matrix wld, Vector4 color)
        {
            effect.Technique = "BONE";

            Matrix wv = wld * Transform_View;
            Matrix wvp = wv * Transform_Projection;

            effect.SetValue("wld", wld);
            effect.SetValue("wv", wv);
            effect.SetValue("wvp", wvp);

            effect.SetValue("ManColor", color);

            int npass = effect.Begin(0);
            for (int ipass = 0; ipass < npass; ipass++)
            {
                effect.BeginPass(ipass);
                mesh.DrawSubset(0);
                effect.EndPass();
            }
            effect.End();
        }

        /// <summary>
        /// 内部objectを破棄します。
        /// </summary>
        public void Dispose()
        {
            OnDeviceLost(device, null);

            foreach (Figure fig in FigureList)
                fig.Dispose();

            if (vd != null)
                vd.Dispose();

            if (effect_screen != null)
                effect_screen.Dispose();
            if (effect_main != null)
                effect_main.Dispose();
            if (effect_gb != null)
                effect_gb.Dispose();
            if (effect_ao != null)
                effect_ao.Dispose();
            if (effect_depth != null)
                effect_depth.Dispose();
            if (effect_dnmap != null)
                effect_dnmap.Dispose();
            if (effect_dnclear != null)
                effect_dnclear.Dispose();
            if (effect_clear != null)
                effect_clear.Dispose();
            if (effect != null)
                effect.Dispose();
            if (device != null)
                device.Dispose();
        }

        /// <summary>
        /// 指定パスからPNGFileを読み込みフィギュアを作成します。
        /// </summary>
        /// <param name="source_file">PNGFileのパス</param>
        public PNGSaveFile LoadPNGFile(string source_file)
        {
            PNGSaveFile sav = new PNGSaveFile();

            if (File.Exists(source_file))
                try
                {
                    PNGFile png = new PNGFile();
                    Figure fig = null;

                    png.Hsav += delegate (string type)
                    {
                        sav.type = type;
                        fig = new Figure();
                        sav.FigureList.Add(fig);
                    };
                    png.Pose += delegate (string type)
                    {
                        sav.type = type;
                    };
                    png.Scne += delegate (string type)
                    {
                        sav.type = type;
                    };
                    png.Cami += delegate (Stream dest, int extract_length)
                    {
                        byte[] buf = new byte[extract_length];
                        dest.Read(buf, 0, extract_length);

                        List<float> factor = new List<float>();
                        for (int offset = 0; offset < extract_length; offset += sizeof(float))
                        {
                            float flo = BitConverter.ToSingle(buf, offset);
                            factor.Add(flo);
                        }

                        camera.Reset();
                        camera.Translation = new Vector3(-factor[0], -factor[1], -factor[2]);
                        camera.Angle = new Vector3(-factor[5], -factor[4], -factor[6]);
                    };
                    png.Lgta += delegate (Stream dest, int extract_length)
                    {
                        byte[] buf = new byte[extract_length];
                        dest.Read(buf, 0, extract_length);

                        List<float> factor = new List<float>();
                        for (int offset = 0; offset < extract_length; offset += sizeof(float))
                        {
                            float flo = BitConverter.ToSingle(buf, offset);
                            factor.Add(flo);
                        }

                        Matrix m;
                        m.M11 = factor[0];
                        m.M12 = factor[1];
                        m.M13 = factor[2];
                        m.M14 = factor[3];

                        m.M21 = factor[4];
                        m.M22 = factor[5];
                        m.M23 = factor[6];
                        m.M24 = factor[7];

                        m.M31 = factor[8];
                        m.M32 = factor[9];
                        m.M33 = factor[10];
                        m.M34 = factor[11];

                        m.M41 = factor[12];
                        m.M42 = factor[13];
                        m.M43 = factor[14];
                        m.M44 = factor[15];

                        sav.LightDirection = Vector3.TransformCoordinate(new Vector3(0.0f, 0.0f, -1.0f), m);
                    };
                    png.Ftmo += delegate (Stream dest, int extract_length)
                    {
                        sav.Tmo = new TMOFile();
                        sav.Tmo.Load(dest);
                    };
                    png.Figu += delegate (Stream dest, int extract_length)
                    {
                        fig = new Figure();
                        fig.LightDirection = sav.LightDirection;
                        fig.Tmo = sav.Tmo;
                        sav.FigureList.Add(fig);

                        byte[] buf = new byte[extract_length];
                        dest.Read(buf, 0, extract_length);

                        List<float> ratios = new List<float>();
                        for (int offset = 0; offset < extract_length; offset += sizeof(float))
                        {
                            float flo = BitConverter.ToSingle(buf, offset);
                            ratios.Add(flo);
                        }
                        /*
                        ◆FIGU
                        スライダの位置。値は float型で 0.0 .. 1.0
                            0: 姉妹
                            1: うで
                            2: あし
                            3: 胴まわり
                            4: おっぱい
                            5: つり目たれ目
                            6: やわらか
                         */
                        if (fig.slider_matrix != null)
                        {
                            fig.slider_matrix.AgeRatio = ratios[0];
                            fig.slider_matrix.ArmRatio = ratios[1];
                            fig.slider_matrix.LegRatio = ratios[2];
                            fig.slider_matrix.WaistRatio = ratios[3];
                            fig.slider_matrix.OppaiRatio = ratios[4];
                            fig.slider_matrix.EyeRatio = ratios[5];
                        }
                    };
                    png.Ftso += delegate (Stream dest, int extract_length, byte[] opt1)
                    {
                        TSOFile tso = new TSOFile();
                        tso.Load(dest);
                        tso.Row = opt1[0];
                        fig.TsoList.Add(tso);
                    };
                    Debug.WriteLine("loading " + source_file);
                    png.Load(source_file);

                    if (sav.type == "HSAV")
                    {
                        BMPSaveData data = new BMPSaveData();

                        using (Stream stream = File.OpenRead(source_file))
                            data.Read(stream);

                        if (fig.slider_matrix != null && data.bitmap.Size == new Size(128, 256))
                        {
                            fig.slider_matrix.AgeRatio = data.GetSliderValue(4);
                            fig.slider_matrix.ArmRatio = data.GetSliderValue(5);
                            fig.slider_matrix.LegRatio = data.GetSliderValue(6);
                            fig.slider_matrix.WaistRatio = data.GetSliderValue(7);
                            fig.slider_matrix.OppaiRatio = data.GetSliderValue(0);
                            fig.slider_matrix.EyeRatio = data.GetSliderValue(8);
                        }

                        foreach (TSOFile tso in fig.TsoList)
                        {
                            string file = data.GetFileName(tso.Row);
                            if (file != "")
                                tso.FileName = Path.GetFileName(file);
                            else
                                tso.FileName = string.Format("{0:X2}", tso.Row);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex);
                }
            return sav;
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

        public static Vector3 ViewToScreen(Vector3 v, Viewport viewport, Matrix proj)
        {
            return Vector3.TransformCoordinate(v, proj * CreateViewportMatrix(viewport));
        }

        public Vector3 ViewToScreen(Vector3 v)
        {
            return ViewToScreen(v, device.Viewport, Transform_Projection);
        }
    }
}
