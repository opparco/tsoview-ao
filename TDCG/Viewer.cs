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
    public enum RenderMode
    {
        Figure,
        Ambient,
        DepthMap,
        NormalMap,
        Occlusion
    };
    public class DepthMapConfig
    {
        public event EventHandler ChangeZnearPlane;
        float zn = 15.0f;
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
        public event EventHandler ChangeZfarPlane;
        float zf = 50.0f;
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
    public class OcclusionConfig
    {
        public event EventHandler ChangeIntensity;
        public event EventHandler ChangeRadius;
        float intensity = 0.5f;
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
        float radius = 2.5f;
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
    protected Effect effect_dnclear;
    protected Effect effect_dnmap;
    protected Effect effect_depth;
    protected Effect effect_ao;
    protected Effect effect_gb;
    protected Effect effect_main;

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
    /// effect handle for HohoAlpha
    /// since v0.90
    /// </summary>
    protected EffectHandle handle_HohoAlpha;

    /// <summary>
    /// effect handle for UVSCR
    /// since v0.91
    /// </summary>
    protected EffectHandle handle_UVSCR;
    protected EffectHandle handle_ao_UVSCR;

    bool shadow_map_enabled;
    /// <summary>
    /// シャドウマップを作成するか
    /// </summary>
    public bool ShadowMapEnabled { get { return shadow_map_enabled; } }

    /// sprite
    public Sprite sprite = null;
    internal Line line = null;

    /// <summary>
    /// surface of device
    /// </summary>
    protected Surface dev_surface = null;
    /// <summary>
    /// zbuffer of device
    /// </summary>
    protected Surface dev_zbuf = null;
    protected Surface tex_zbuf = null;

    /// config: BackBufferWidth BackBufferHeight
    public Size DeviceSize { get; set; }

    RenderMode render_mode = RenderMode.Figure;
    public RenderMode RenderMode
    {
        get { return render_mode; }
        set
        {
            render_mode = value;
            need_render = true;
        }
    }

    public DepthMapConfig DepthMapConfig = null;
    public OcclusionConfig OcclusionConfig = null;

    protected Texture amb_texture;
    protected Texture depthmap_texture;
    protected Texture normalmap_texture;
    protected Texture occ_texture;

    protected Surface amb_surface;
    protected Surface depthmap_surface;
    protected Surface normalmap_surface;
    protected Surface occ_surface;

    VertexBuffer vb;

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
        DeviceSize = new Size(0, 0);
        ScreenColor = Color.LightGray;
        HohoAlpha = 1.0f;

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
            fig.TSOList.Add(tso);
        }
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
            fig.TSOList.Add(tso);
        }
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
                //fig.TransformTpo();
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
            pp.Windowed = true;
            pp.SwapEffect = SwapEffect.Discard;
            pp.BackBufferFormat = Format.X8R8G8B8;
            pp.BackBufferWidth = DeviceSize.Width;
            pp.BackBufferHeight = DeviceSize.Height;
            pp.BackBufferCount = 1;
            pp.EnableAutoDepthStencil = true;

            int adapter_ordinal = Manager.Adapters.Default.Adapter;
            DisplayMode display_mode = Manager.Adapters.Default.CurrentDisplayMode;

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

            CreateFlags flags = CreateFlags.SoftwareVertexProcessing;
            Caps caps = Manager.GetDeviceCaps(adapter_ordinal, DeviceType.Hardware);
            if (caps.DeviceCaps.SupportsHardwareTransformAndLight)
                flags = CreateFlags.HardwareVertexProcessing;
            if (caps.DeviceCaps.SupportsPureDevice)
                flags |= CreateFlags.PureDevice;
            device = new Device(adapter_ordinal, DeviceType.Hardware, control, flags, pp);
        }
        catch (DirectXException ex)
        {
            Console.WriteLine("Error: " + ex);
            return false;
        }

        device.DeviceLost += new EventHandler(OnDeviceLost);
        device.DeviceReset += new EventHandler(OnDeviceReset);
        device.DeviceResizing += new CancelEventHandler(OnDeviceResizing);

        Macro[] macros = new Macro[1];
        macros[0].Name = "XRGB_DEPTH";
        macros[0].Definition = XRGBDepth ? "1" : "0";

        EffectPool effect_pool = new EffectPool();

        Stopwatch sw = new Stopwatch();
        sw.Start();
        string toonshader_filename = @"toonshader-shared.cgfx";
        if (!LoadEffect(toonshader_filename, out effect, null, effect_pool))
            return false;
        sw.Stop();
        Console.WriteLine(toonshader_filename + " read time: " + sw.Elapsed);

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

        if (!LoadEffect(@"main.fx", out effect_main))
            return false;

        handle_LocalBoneMats = effect.GetParameter(null, "LocalBoneMats");
        handle_LightDirForced = effect.GetParameter(null, "LightDirForced");
        handle_HohoAlpha = effect.GetParameter(null, "HohoAlpha");
        handle_UVSCR = effect.GetParameter(null, "UVSCR");
        handle_ao_UVSCR = effect_ao.GetParameter(null, "UVSCR");

        sprite = new Sprite(device);
        line = new Line(device);
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
        return true;
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

    void AssignDepthProjection()
    {
        Matrix depth_projection = Matrix.PerspectiveFovRH(
                Geometry.DegreeToRadian(30.0f),
                (float)device.Viewport.Width / (float)device.Viewport.Height,
                DepthMapConfig.ZnearPlane,
                DepthMapConfig.ZfarPlane);
        effect_dnmap.SetValue("depthproj", depth_projection); // in
        effect_ao.SetValue("depthproj", depth_projection); // in
    }

    CustomVertex.PositionTextured[] vertices;

    void CreateVertices(Rectangle rect)
    {
        vertices = new CustomVertex.PositionTextured[6];

        const float z = 0.5f;

        vertices[0] = new CustomVertex.PositionTextured(rect.Left, rect.Bottom, z, 0.0f, 1.0f);
        vertices[1] = new CustomVertex.PositionTextured(rect.Left, rect.Top, z, 0.0f, 0.0f);
        vertices[2] = new CustomVertex.PositionTextured(rect.Right, rect.Bottom, z, 1.0f, 1.0f);
        vertices[3] = new CustomVertex.PositionTextured(rect.Right, rect.Top, z, 1.0f, 0.0f);

        for (int i = 0; i < 4; i++)
        {
            vertices[i].X -= 0.5f;
            vertices[i].Y -= 0.5f;
        }
    }

    void vb_Created(object sender, EventArgs e)
    {
        VertexBuffer vb = (VertexBuffer)sender;

        {
            GraphicsStream gs = vb.Lock(0, 0, LockFlags.None);
            {
                for (int i = 0; i < vertices.Length; i++)
                {
                    gs.Write(vertices[i]);
                }
            }
            vb.Unlock();
        }
    }

    private void OnDeviceLost(object sender, EventArgs e)
    {
        Console.WriteLine("OnDeviceLost");

        if (vb != null)
            vb.Dispose();

        if (amb_surface != null)
            amb_surface.Dispose();
        if (depthmap_surface != null)
            depthmap_surface.Dispose();
        if (normalmap_surface != null)
            normalmap_surface.Dispose();
        if (occ_surface != null)
            occ_surface.Dispose();

        if (amb_texture != null)
            amb_texture.Dispose();
        if (depthmap_texture != null)
            depthmap_texture.Dispose();
        if (normalmap_texture != null)
            normalmap_texture.Dispose();
        if (occ_texture != null)
            occ_texture.Dispose();

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

        occ_texture = new Texture(device, devw, devh, 1, Usage.RenderTarget, Format.X8R8G8B8, Pool.Default);
        occ_surface = occ_texture.GetSurfaceLevel(0);

        effect_depth.SetValue("DepthMap_texture", depthmap_texture); // in

        effect_ao.SetValue("DepthMap_texture", depthmap_texture); // in
        effect_ao.SetValue("NormalMap_texture", normalmap_texture); // in

        effect_main.SetValue("Ambient_texture", amb_texture); // in
        effect_main.SetValue("Occlusion_texture", occ_texture); // in

        CreateVertices(dev_rect);
        vb = new VertexBuffer(typeof(CustomVertex.PositionTextured), vertices.Length, device, Usage.Dynamic | Usage.WriteOnly, CustomVertex.PositionTextured.Format, Pool.Default);
        vb_Created(vb, null);

        Transform_Projection = Matrix.PerspectiveFovRH(
                Geometry.DegreeToRadian(30.0f),
                (float)device.Viewport.Width / (float)device.Viewport.Height,
                1.0f,
                1000.0f );
        // xxx: for w-buffering
        device.Transform.Projection = Transform_Projection;
        effect.SetValue("proj", Transform_Projection);

        {
            Matrix world = Matrix.Translation(-devw/2, -devh/2, 0);
            Matrix view = Matrix.RotationX((float)Math.PI);
            Matrix projection = Matrix.OrthoRH(devw, devh, 0.0f, 1.0f);

            Matrix world_view = world * view;
            Matrix world_view_projection = world_view * projection;

            //todo: shared
            effect_dnclear.SetValue("wvp", world_view_projection);
            effect_depth.SetValue("wvp", world_view_projection);
            effect_ao.SetValue("wvp", world_view_projection);
            effect_gb.SetValue("wvp", world_view_projection);
            effect_main.SetValue("wvp", world_view_projection);

            AssignDepthProjection();
        }

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
            DrawAmbientOcclusion();
            //DrawGaussianBlur();
            DrawSprite(occ_texture);
            break;
        default:
            DrawDepthNormalMap();
            DrawAmbientOcclusion();
            DrawGaussianBlur();

            DrawFigure();
            Blit();
            DrawMain();
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
        Thread.Sleep(30);
    }

    /// config: スクリーン塗りつぶし色
    public Color ScreenColor { get; set; }

    /// config: ほほ赤みの濃さ
    public float HohoAlpha { get; set; }

    /// config: enhance depth precision on Format.X8A8G8B8
    public bool XRGBDepth { get; set; }

    Format depthmap_format;
    Format normalmap_format;

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
        effect.SetValue(handle_HohoAlpha, HohoAlpha);
        effect.SetValue(handle_UVSCR, UVSCR());
        foreach (Figure fig in FigureList)
        {
            effect.SetValue(handle_LightDirForced, fig.LightDirForced());
            foreach (TSOFile tso in fig.TSOList)
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

    void DrawPlane(Effect effect)
    {
        device.VertexFormat = CustomVertex.PositionTextured.Format;
        device.SetStreamSource(0, vb, 0);

        int npass = effect.Begin(0);
        for (int ipass = 0; ipass < npass; ipass++)
        {
            effect.BeginPass(ipass);
            device.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
            effect.EndPass();
        }
        effect.End();
    }

    public static string GetHideTechsPath()
    {
        return Path.Combine(Application.StartupPath, @"hidetechs.txt");
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

        DrawPlane(effect_dnclear);

        device.VertexDeclaration = vd;

        foreach (Figure fig in FigureList)
        foreach (TSOFile tso in fig.TSOList)
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
    // from dev_surface
    // to amb_texture
    void Blit()
    {
        Debug.WriteLine("Blit");

        device.StretchRectangle(dev_surface, dev_rect, amb_surface, dev_rect, TextureFilter.Point);
    }

    // draw Gaussian Blur
    // x direction
    //   in occ_texture
    //   out amb_texture
    // y direction
    //   in amb_texture
    //   out occ_surface
    void DrawGaussianBlur()
    {
        Debug.WriteLine("DrawGaussianBlur");

        device.SetRenderState(RenderStates.AlphaBlendEnable, false);

        effect_gb.SetValue("Ambient_texture", occ_texture); // in
        device.SetRenderTarget(0, amb_surface); // out
        device.DepthStencilSurface = tex_zbuf;

        effect_gb.SetValue("dir", new Vector4(1.0f/(float)dev_rect.Width, 0, 0, 0));
        DrawPlane(effect_gb);

        effect_gb.SetValue("Ambient_texture", amb_texture); // in
        device.SetRenderTarget(0, occ_surface); // out
        device.DepthStencilSurface = tex_zbuf;

        effect_gb.SetValue("dir", new Vector4(0, 1.0f/(float)dev_rect.Height, 0, 0));
        DrawPlane(effect_gb);
    }

    // draw Ambient Occlusion
    // in depthmap_texture
    // in normalmap_texture
    // out occ_surface
    public void DrawAmbientOcclusion()
    {
        Debug.WriteLine("DrawAmbientOcclusion");

        device.SetRenderState(RenderStates.AlphaBlendEnable, false);

        device.SetRenderTarget(0, occ_surface); // out
        device.DepthStencilSurface = tex_zbuf;

        if (MotionEnabled)
            effect_ao.SetValue(handle_ao_UVSCR, UVSCR());

        DrawPlane(effect_ao);
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

        DrawPlane(effect_depth);
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

        DrawPlane(effect_main);
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
        if (line != null)
            line.Dispose();
        if (sprite != null)
            sprite.Dispose();

        if (vb != null)
            vb.Dispose();

        if (amb_surface != null)
            amb_surface.Dispose();
        if (depthmap_surface != null)
            depthmap_surface.Dispose();
        if (normalmap_surface != null)
            normalmap_surface.Dispose();
        if (occ_surface != null)
            occ_surface.Dispose();

        if (amb_texture != null)
            amb_texture.Dispose();
        if (depthmap_texture != null)
            depthmap_texture.Dispose();
        if (normalmap_texture != null)
            normalmap_texture.Dispose();
        if (occ_texture != null)
            occ_texture.Dispose();

        if (tex_zbuf != null)
            tex_zbuf.Dispose();
        if (dev_zbuf != null)
            dev_zbuf.Dispose();
        if (dev_surface != null)
            dev_surface.Dispose();

        if (vd != null)
            vd.Dispose();

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
                    fig.slider_matrix.TallRatio = ratios[0];
                    fig.slider_matrix.ArmRatio = ratios[1];
                    fig.slider_matrix.LegRatio = ratios[2];
                    fig.slider_matrix.WaistRatio = ratios[3];
                    fig.slider_matrix.BustRatio = ratios[4];
                    fig.slider_matrix.EyeRatio = ratios[5];
                }

                //fig.TransformTpo();
            };
            png.Ftso += delegate(Stream dest, int extract_length, byte[] opt1)
            {
                TSOFile tso = new TSOFile();
                tso.Load(dest);
                tso.Row = opt1[0];
                fig.TSOList.Add(tso);
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
                    fig.slider_matrix.TallRatio = data.GetSliderValue(4);
                    fig.slider_matrix.ArmRatio = data.GetSliderValue(5);
                    fig.slider_matrix.LegRatio = data.GetSliderValue(6);
                    fig.slider_matrix.WaistRatio = data.GetSliderValue(7);
                    fig.slider_matrix.BustRatio = data.GetSliderValue(0);
                    fig.slider_matrix.EyeRatio = data.GetSliderValue(8);
                }

                for (int i = 0; i < fig.TSOList.Count; i++)
                {
                    TSOFile tso = fig.TSOList[i];
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
