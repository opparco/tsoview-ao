using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.ComponentModel;
using System.Windows.Forms;
using System.IO;
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

    /// surface of device
    protected Surface dev_surface = null;
    /// zbuffer of device
    protected Surface dev_zbuf = null;
    /// zbuffer of render target
    protected Surface tex_zbuf = null;

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
    Texture depthmap_texture;
    Texture normalmap_texture;
    Texture randommap_texture;
    Texture occ_texture;
    Texture tmp_texture;

    Surface amb_surface;
    Surface depthmap_surface;
    Surface normalmap_surface;
    Surface occ_surface;
    Surface tmp_surface;

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
    }

    /// <summary>
    /// 選択フィギュアの光源方向を設定します。
    /// </summary>
    /// <param name="dir">選択フィギュアの光源方向</param>
    public void SetLightDirection(Vector3 dir)
    {
        foreach (Figure fig in FigureList)
            fig.LightDirection = dir;
        need_render = true;
    }

    /// マウスボタンを押したときに実行するハンドラ
    protected virtual void form_OnMouseDown(object sender, MouseEventArgs e)
    {
        switch (e.Button)
        {
        case MouseButtons.Left:
            if (Control.ModifierKeys == Keys.Control)
                SetLightDirection(ScreenToOrientation(e.X, e.Y));
            break;
        }

        lastScreenPoint.X = e.X;
        lastScreenPoint.Y = e.Y;
    }

    /// マウスを移動したときに実行するハンドラ
    protected virtual void form_OnMouseMove(object sender, MouseEventArgs e)
    {
        int dx = e.X - lastScreenPoint.X;
        int dy = e.Y - lastScreenPoint.Y;

        switch (e.Button)
        {
        case MouseButtons.Left:
            if (Control.ModifierKeys == Keys.Control)
                SetLightDirection(ScreenToOrientation(e.X, e.Y));
            else
                Camera.Move(dx, -dy, 0.0f);
            break;
        case MouseButtons.Middle:
            Camera.MoveView(-dx*0.125f, dy*0.125f);
            break;
        case MouseButtons.Right:
            Camera.Move(0.0f, 0.0f, -dy*0.125f);
            break;
        }

        lastScreenPoint.X = e.X;
        lastScreenPoint.Y = e.Y;
    }

    // 選択フィギュアindex
    int fig_idx = 0;

    // スクリーンの中心座標
    private float screenCenterX = 800 / 2.0f;
    private float screenCenterY = 600 / 2.0f;

    /// <summary>
    /// controlを保持します。スクリーンの中心座標を更新します。
    /// </summary>
    /// <param name="control">control</param>
    protected void SetControl(Control control)
    {
        this.control = control;
        screenCenterX = control.ClientSize.Width / 2.0f;
        screenCenterY = control.ClientSize.Height / 2.0f;
    }

    /// <summary>
    /// 指定スクリーン座標からスクリーン中心へ向かうベクトルを得ます。
    /// </summary>
    /// <param name="screenPointX">スクリーンX座標</param>
    /// <param name="screenPointY">スクリーンY座標</param>
    /// <returns>方向ベクトル</returns>
    public Vector3 ScreenToOrientation(float screenPointX, float screenPointY)
    {
        float radius = 1.0f;
        float x = -(screenPointX - screenCenterX) / (radius * screenCenterX);
        float y = +(screenPointY - screenCenterY) / (radius * screenCenterY);
        float z = 0.0f;
        float mag = (x*x) + (y*y);

        if (mag > 1.0f)
        {
            float scale = 1.0f / (float)Math.Sqrt(mag);
            x *= scale;
            y *= scale;
        }
        else
            z = (float)-Math.Sqrt(1.0f - mag);

        return new Vector3(x, y, z);
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
            if (! append)
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
            if (! append)
                ClearFigureList();
            if (Directory.Exists(source_file))
                AddFigureFromTSODirectory(source_file);
            break;
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
    /// <param name="fig_idx">フィギュア番号</param>
    public void SetFigureIndex(int fig_idx)
    {
        if (fig_idx < 0)
            fig_idx = 0;
        if (fig_idx > FigureList.Count - 1)
            fig_idx = 0;
        this.fig_idx = fig_idx;
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
            string[] files = Directory.GetFiles(source_file, "*.TSO");
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
        fig.UpdateBoneMatricesEvent += delegate(object sender, EventArgs e)
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
        Figure fig;
        if (FigureList.Count == 0)
            fig = null;
        else
            fig = FigureList[fig_idx];
        return fig;
    }

    /// <summary>
    /// 選択フィギュアを得ます。なければ作成します。
    /// </summary>
    public Figure GetSelectedOrCreateFigure()
    {
        Figure fig;
        if (FigureList.Count == 0)
            fig = new Figure();
        else
            fig = FigureList[fig_idx];
        if (FigureList.Count == 0)
        {
            int idx = FigureList.Count;
            //todo: override List#Add
            FigureList.Add(fig);
            fig.UpdateBoneMatricesEvent += delegate(object sender, EventArgs e)
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
        if (fig_idx < FigureList.Count)
            fig = FigureList[fig_idx];
        return fig != null;
    }

    /// 次のフィギュアを選択します。
    public void NextFigure()
    {
        SetFigureIndex(fig_idx+1);
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
            if (! append)
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
                fig.UpdateBoneMatricesEvent += delegate(object sender, EventArgs e)
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
    /// 指定テクスチャを開き直します。
    /// </summary>
    /// <param name="tex">テクスチャ</param>
    public void OpenTexture(TSOTex tex)
    {
        tex.Open(device);
    }

    private SimpleCamera camera = new SimpleCamera();

    /// <summary>
    /// カメラ
    /// </summary>
    public SimpleCamera Camera
    {
        get {
            return camera;
        }
        set {
            camera = value;
        }
    }

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

        handle_LocalBoneMats = effect.GetParameter(null, "LocalBoneMats");
        handle_LightDirForced = effect.GetParameter(null, "LightDirForced");
        handle_Ambient = effect.GetParameter(null, "ColorRate");
        handle_HohoAlpha = effect.GetParameter(null, "HohoAlpha");
        handle_UVSCR = effect.GetParameter(null, "UVSCR");

        screen = new Screen(device);
        sprite = new Sprite(device);
        camera.Update();
        OnDeviceReset(device, null);

        FigureSelectEvent += delegate(object sender, EventArgs e)
        {
            need_render = true;
        };
        FigureUpdateEvent += delegate(object sender, EventArgs e)
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
        OcclusionConfig.ChangeIntensity += delegate(object sender, EventArgs e)
        {
            effect_ao.SetValue("_Intensity", OcclusionConfig.Intensity); // in
            need_render = true;
        };
        OcclusionConfig.ChangeRadius += delegate (object sender, EventArgs e)
        {
            effect_ao.SetValue("_Radius", OcclusionConfig.Radius); // in
            need_render = true;
        };
        DiffusionConfig.ChangeIntensity += delegate(object sender, EventArgs e)
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
        float aspect = (float)device.Viewport.Width / (float)device.Viewport.Height;
        float d;
        if (projection_mode == ProjectionMode.Ortho)
            d = camera.Translation.Z;
        else
            d = DepthMapConfig.ZnearPlane;
        float h = d * (float)Math.Tan(fieldOfViewY / 2.0f);
        float w = h * aspect;
        Matrix depth_projection;
        if (projection_mode == ProjectionMode.Ortho)
            depth_projection = Matrix.OrthoRH(w * 2.0f, h * 2.0f, DepthMapConfig.ZnearPlane, DepthMapConfig.ZfarPlane);
        else
            depth_projection = Matrix.PerspectiveRH(w * 2.0f, h * 2.0f, DepthMapConfig.ZnearPlane, DepthMapConfig.ZfarPlane);
        Vector4 vp = new Vector4(device.Viewport.Width, device.Viewport.Height, 0, 0);

        effect_dnmap.SetValue("depthproj", depth_projection); // in
        effect_ao.SetValue("depthproj", depth_projection); // in
        effect_ao.SetValue("vp", vp); // in

        if (projection_mode == ProjectionMode.Ortho)
            effect_ao.Technique = "AO_ortho";
        else
            effect_ao.Technique = "AO";
    }

    private void OnDeviceLost(object sender, EventArgs e)
    {
        Console.WriteLine("OnDeviceLost");

        if (screen != null)
            screen.Dispose();

        if (amb_surface != null)
            amb_surface.Dispose();
        if (depthmap_surface != null)
            depthmap_surface.Dispose();
        if (normalmap_surface != null)
            normalmap_surface.Dispose();
        if (occ_surface != null)
            occ_surface.Dispose();
        if (tmp_surface != null)
            tmp_surface.Dispose();

        if (amb_texture != null)
            amb_texture.Dispose();
        if (depthmap_texture != null)
            depthmap_texture.Dispose();
        if (normalmap_texture != null)
            normalmap_texture.Dispose();
        if (randommap_texture != null)
            randommap_texture.Dispose();
        if (occ_texture != null)
            occ_texture.Dispose();
        if (tmp_texture != null)
            tmp_texture.Dispose();

        if (tex_zbuf != null)
            tex_zbuf.Dispose();
        if (dev_zbuf != null)
            dev_zbuf.Dispose();
        if (dev_surface != null)
            dev_surface.Dispose();
    }

    Rectangle dev_rect;

    private void OnDeviceReset(object sender, EventArgs e)
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
        {
            int dev_zbufw = 0;
            int dev_zbufh = 0;
            dev_zbufw = dev_surface.Description.Width;
            dev_zbufh = dev_surface.Description.Height;
            Console.WriteLine("dev_zbuf {0}x{1}", dev_zbufw, dev_zbufh);
        }

        tex_zbuf = device.CreateDepthStencilSurface(devw, devh, DepthFormat.D16, MultiSampleType.None, 0, false);

        amb_texture = new Texture(device, devw, devh, 1, Usage.RenderTarget, Format.X8R8G8B8, Pool.Default);
        amb_surface = amb_texture.GetSurfaceLevel(0);

        depthmap_texture = new Texture(device, devw, devh, 1, Usage.RenderTarget, depthmap_format, Pool.Default);
        depthmap_surface = depthmap_texture.GetSurfaceLevel(0);

        normalmap_texture = new Texture(device, devw, devh, 1, Usage.RenderTarget, normalmap_format, Pool.Default);
        normalmap_surface = normalmap_texture.GetSurfaceLevel(0);

        randommap_texture = TextureLoader.FromFile(device, GetRandomTexturePath());

        occ_texture = new Texture(device, devw, devh, 1, Usage.RenderTarget, Format.X8R8G8B8, Pool.Default);
        occ_surface = occ_texture.GetSurfaceLevel(0);

        tmp_texture = new Texture(device, devw, devh, 1, Usage.RenderTarget, Format.X8R8G8B8, Pool.Default);
        tmp_surface = tmp_texture.GetSurfaceLevel(0);

        effect_depth.SetValue("DepthMap_texture", depthmap_texture); // in

        effect_ao.SetValue("DepthMap_texture", depthmap_texture); // in
        effect_ao.SetValue("NormalMap_texture", normalmap_texture); // in
        effect_ao.SetValue("RandomMap_texture", randommap_texture); // in

        effect_main.SetValue("Ambient_texture", amb_texture); // in
        effect_main.SetValue("Occlusion_texture", occ_texture); // in

        effect_screen.SetValue("DepthMap_texture", depthmap_texture); // in
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
            SetFigureIndex(fig_idx-1);
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

    long start_ticks = 0;
    int start_frame_idx = 0;

    bool motionEnabled = false;

    /// <summary>
    /// モーションの有無
    /// </summary>
    public bool MotionEnabled
    {
        get { return motionEnabled; }
        set
        {
            motionEnabled = value;

            if (motionEnabled)
            {
                start_ticks = DateTime.Now.Ticks;
                start_frame_idx = frame_idx;
            }
        }
    }

    int frame_idx = 0;

    /// <summary>
    /// フレーム番号
    /// </summary>
    public int FrameIndex { get { return frame_idx; } set { frame_idx = value; } }

    /// <summary>
    /// フレームを進めるのに用いるデリゲート型
    /// </summary>
    public delegate void FrameMovingHandler();

    /// <summary>
    /// フレームを進めるハンドラ
    /// </summary>
    public FrameMovingHandler FrameMoving;

    /// <summary>
    /// 次のシーンフレームに進みます。
    /// </summary>
    public void FrameMove()
    {
        if (motionEnabled)
        {
            int frame_len = GetMaxFrameLength();
            if (frame_len > 0)
            {
                long ticks = DateTime.Now.Ticks - start_ticks;
                long current_frame_idx = (long)(start_frame_idx + ticks * 0.000006);
                frame_idx = (int)(current_frame_idx % frame_len);
                Debug.Assert(frame_idx >= 0);
                Debug.Assert(frame_idx < frame_len);
            }
        }
        FrameMove(frame_idx);

        if (FrameMoving != null)
            FrameMoving();
    }

    /// <summary>
    /// 指定シーンフレームに進みます。
    /// </summary>
    /// <param name="frame_idx">フレーム番号</param>
    public void FrameMove(int frame_idx)
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

            need_render = true;
        }

        if (motionEnabled)
        {
            //フレーム番号を通知する。
            foreach (Figure fig in FigureList)
                fig.SetFrameIndex(frame_idx);

            //device.Transform.World = world_matrix;
            foreach (Figure fig in FigureList)
                fig.UpdateBoneMatrices();

            need_render = true;
        }
    }

    /// <summary>
    /// tmo file中で最大のフレーム長さを得ます。
    /// </summary>
    /// <returns>フレーム長さ</returns>
    public int GetMaxFrameLength()
    {
        int max = 0;
        foreach (Figure fig in FigureList)
            if (fig.Tmo.frames != null && max < fig.Tmo.frames.Length)
                max = fig.Tmo.frames.Length;
        return max;
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

        {
            Matrix world_view_matrix = world_matrix * Transform_View;
            Matrix world_view_projection_matrix = world_view_matrix * Transform_Projection;
            effect.SetValue("wld", world_matrix);
            effect.SetValue("wv", world_view_matrix);
            effect.SetValue("wvp", world_view_projection_matrix);
        }

        switch (RenderMode)
        {
        case RenderMode.Ambient:
            DrawFigure();
            break;
        case RenderMode.DepthMap:
            DrawDepthNormalMap();
            DrawDepth();
            break;
        case RenderMode.NormalMap:
            DrawDepthNormalMap();
            DrawSprite(normalmap_texture);
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
            if (! device.CheckCooperativeLevel(out ret))
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

    Format depthmap_format = Format.X8R8G8B8;
    Format normalmap_format = Format.X8R8G8B8;

    /// config: depthmap format name
    public void SetDepthMapFormat(string name)
    {
        depthmap_format = (Format)Enum.Parse(typeof(Format), name);
    }

    /// config: normalmap format name
    public void SetNormalMapFormat(string name)
    {
        normalmap_format = (Format)Enum.Parse(typeof(Format), name);
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
        {
            {
                Matrix world = Matrix.Identity;

                if (fig.slider_matrix != null)
                {
                    //姉妹スライダによる変形
                    world = Matrix.Scaling(fig.slider_matrix.Local);
                }

                //移動変位を設定
                world.M41 = fig.Translation.X;
                world.M42 = fig.Translation.Y;
                world.M43 = fig.Translation.Z;

                Matrix world_view_matrix = world * Transform_View;
                Matrix world_view_projection_matrix = world_view_matrix * Transform_Projection;
                effect.SetValue("wld", world);
                effect.SetValue("wv", world_view_matrix);
                effect.SetValue("wvp", world_view_projection_matrix);
            }
            effect.SetValue(handle_LightDirForced, fig.LightDirForced());
            foreach (TSOFile tso in fig.TsoList)
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
        }
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
    // out depthmap_surface
    // out normalmap_surface
    void DrawDepthNormalMap()
    {
        Debug.WriteLine("DrawDepthNormalMap");

        device.SetRenderState(RenderStates.AlphaBlendEnable, false);

        device.SetRenderTarget(0, depthmap_surface);
        device.SetRenderTarget(1, normalmap_surface);

        device.DepthStencilSurface = tex_zbuf;
        device.Clear(ClearFlags.ZBuffer, Color.White, 1.0f, 0);

        screen.Draw(effect_dnclear);

        device.VertexDeclaration = vd;

        foreach (Figure fig in FigureList)
        {
            {
                Matrix world = Matrix.Identity;

                if (fig.slider_matrix != null)
                {
                    //姉妹スライダによる変形
                    world = Matrix.Scaling(fig.slider_matrix.Local);
                }

                //移動変位を設定
                world.M41 = fig.Translation.X;
                world.M42 = fig.Translation.Y;
                world.M43 = fig.Translation.Z;

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
        sprite.Draw(src_texture, dev_rect, new Vector3(0, 0, 0), new Vector3(0, 0, 0), Color.White);
        sprite.End();
    }

    // blit
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

        effect_gb.SetValue("dir", new Vector4(extent/(float)dev_rect.Width, 0, 0, 0));
        screen.Draw(effect_gb);

        effect_gb.SetValue("Ambient_texture", tmp_texture); // in
        device.SetRenderTarget(0, occ_surface); // out
        device.DepthStencilSurface = tex_zbuf;

        effect_gb.SetValue("dir", new Vector4(0, extent/(float)dev_rect.Height, 0, 0));
        screen.Draw(effect_gb);
    }

    // draw Ambient Occlusion
    // in depthmap_texture
    // in normalmap_texture
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
    // in depthmap_texture
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
        foreach (Figure fig in FigureList)
            fig.Dispose();
        if (sprite != null)
            sprite.Dispose();

        if (screen != null)
            screen.Dispose();

        if (amb_surface != null)
            amb_surface.Dispose();
        if (depthmap_surface != null)
            depthmap_surface.Dispose();
        if (normalmap_surface != null)
            normalmap_surface.Dispose();
        if (occ_surface != null)
            occ_surface.Dispose();
        if (tmp_surface != null)
            tmp_surface.Dispose();

        if (amb_texture != null)
            amb_texture.Dispose();
        if (depthmap_texture != null)
            depthmap_texture.Dispose();
        if (normalmap_texture != null)
            normalmap_texture.Dispose();
        if (occ_texture != null)
            occ_texture.Dispose();
        if (tmp_texture != null)
            tmp_texture.Dispose();

        if (tex_zbuf != null)
            tex_zbuf.Dispose();
        if (dev_zbuf != null)
            dev_zbuf.Dispose();
        if (dev_surface != null)
            dev_surface.Dispose();

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

            png.Hsav += delegate(string type)
            {
                sav.type = type;
                fig = new Figure();
                sav.FigureList.Add(fig);
            };
            png.Pose += delegate(string type)
            {
                sav.type = type;
            };
            png.Scne += delegate(string type)
            {
                sav.type = type;
            };
            png.Cami += delegate(Stream dest, int extract_length)
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
            png.Lgta += delegate(Stream dest, int extract_length)
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
            png.Ftmo += delegate(Stream dest, int extract_length)
            {
                sav.Tmo = new TMOFile();
                sav.Tmo.Load(dest);
            };
            png.Figu += delegate(Stream dest, int extract_length)
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
            png.Ftso += delegate(Stream dest, int extract_length, byte[] opt1)
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

                for (int i = 0; i < fig.TsoList.Count; i++)
                {
                    TSOFile tso = fig.TsoList[i];
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
    /// 球とレイの衝突を見つけます。
    /// </summary>
    /// <param name="sphereRadius">球の半径</param>
    /// <param name="sphereCenter">球の中心位置</param>
    /// <param name="rayStart">光線の発射位置</param>
    /// <param name="rayOrientation">光線の方向</param>
    /// <param name="collisionPoint">衝突位置</param>
    /// <param name="collisionTime">衝突時刻</param>
    /// <returns>衝突したか</returns>
    public static bool DetectSphereRayCollision(float sphereRadius, ref Vector3 sphereCenter, ref Vector3 rayStart, ref Vector3 rayOrientation, out Vector3 collisionPoint, out float collisionTime)
    {
        collisionTime = 0.0f;
        collisionPoint = Vector3.Empty;

        Vector3 u = rayStart - sphereCenter;
        float a = Vector3.Dot(rayOrientation, rayOrientation);
        float b = Vector3.Dot(rayOrientation, u);
        float c = Vector3.Dot(u, u) - sphereRadius * sphereRadius;
        if (a <= float.Epsilon)
            //誤差
            return false;
        float d = b * b - a * c;
        if (d < 0.0f)
            //衝突しない
            return false;
        collisionTime = (-b - (float)Math.Sqrt(d)) / a;
        collisionPoint = rayStart + rayOrientation * collisionTime;
        return true;
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
        //スクリーン位置
        Vector3 v = new Vector3(screenX, screenY, z);

        Matrix inv_m = Matrix.Invert(CreateViewportMatrix(viewport));
        Matrix inv_proj = Matrix.Invert(proj);
        Matrix inv_view = Matrix.Invert(view);

        //スクリーン位置をワールド座標へ変換
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
