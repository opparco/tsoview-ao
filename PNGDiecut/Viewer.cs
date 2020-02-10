using System;
using System.Diagnostics;
using System.Drawing;
using System.ComponentModel;
using System.Windows.Forms;
using System.IO;

using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace PNGDiecut
{
public class Viewer : IDisposable
{
    Device device;
    Effect effect_diecut;

    Surface dev_surface;
    Surface dev_zbuf;
    Surface tex_zbuf;

    /// config: BackBufferWidth BackBufferHeight
    public Size DeviceSize { get; set; }

    Texture amb_texture;
    Texture occ_texture;

    Surface amb_surface;
    Surface occ_surface;

    Screen screen;

    /// <summary>
    /// マウスポイントしているスクリーン座標
    /// </summary>
    protected Point lastScreenPoint = Point.Empty;

    /// マウスボタンを押したときに実行するハンドラ
    protected virtual void form_OnMouseDown(object sender, MouseEventArgs e)
    {
        lastScreenPoint.X = e.X;
        lastScreenPoint.Y = e.Y;
    }

    /// マウスを移動したときに実行するハンドラ
    protected virtual void form_OnMouseMove(object sender, MouseEventArgs e)
    {
        lastScreenPoint.X = e.X;
        lastScreenPoint.Y = e.Y;
    }

    public bool InitializeGraphices(Control control)
    {
        control.MouseDown += new MouseEventHandler(form_OnMouseDown);
        control.MouseMove += new MouseEventHandler(form_OnMouseMove);

        PresentParameters pp = new PresentParameters();
        try
        {
            pp.Windowed = true;
            pp.SwapEffect = SwapEffect.Discard;
            pp.BackBufferFormat = Format.A8R8G8B8;
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

            Caps caps = Manager.GetDeviceCaps(adapter_ordinal, DeviceType.Hardware);
            CreateFlags flags = CreateFlags.SoftwareVertexProcessing;
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

        if (!LoadEffect(@"diecut.fx", out effect_diecut))
            return false;

        screen = new Screen(device);

        OnDeviceReset(device, null);

        ChangeShadowFile(@"shadow.png");

        return true;
    }

    bool LoadEffect(string effect_filename, out Effect effect)
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
            effect = Effect.FromStream(device, effect_stream, null, ShaderFlags.None, null, out compile_error);
            if (compile_error != null)
            {
                Console.WriteLine(compile_error);
                return false;
            }
        }
        return true;
    }

    bool need_render = true;

    /// <summary>
    /// レンダリングは必要か
    /// </summary>
    public bool NeedRender { get { return need_render; } }

    public void Render()
    {
        if (!need_render)
            return;

        need_render = false;

        device.BeginScene();

        DrawMain();

        device.EndScene();

        device.Present();
    }

    void DrawMain()
    {
        Debug.WriteLine("DrawMain");

        device.SetRenderState(RenderStates.AlphaBlendEnable, false);

        device.SetRenderTarget(0, dev_surface); // out
        device.DepthStencilSurface = dev_zbuf;

        screen.Draw(effect_diecut);
    }

    void DrawScreen(Effect effect)
    {
        screen.Draw(effect);
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
    /// バックバッファをTGA形式でファイルに保存します。
    /// </summary>
    /// <param name="file">ファイル名</param>
    public void SaveToTga(string file)
    {
      using (Surface sf = device.GetBackBuffer(0, 0, BackBufferType.Mono))
      if (sf != null)
          SurfaceLoader.Save(file, ImageFileFormat.Tga, sf);
    }

    string sample_file = null;

    public void Save()
    {
        if (sample_file != null)
            SaveToTga(Path.ChangeExtension(sample_file, ".cut.tga"));
    }

    private void OnDeviceLost(object sender, EventArgs e)
    {
        Console.WriteLine("OnDeviceLost");

        if (screen != null)
            screen.Dispose();

        if (amb_surface != null)
            amb_surface.Dispose();
        if (occ_surface != null)
            occ_surface.Dispose();

        if (amb_texture != null)
            amb_texture.Dispose();
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

    public void LoadAnyFile(string source_file, bool append)
    {
        switch (Path.GetExtension(source_file).ToLower())
        {
        case ".bmp":
        case ".png":
            ChangeSampleFile(source_file);
            break;
        }
    }

    public void ChangeSampleFile(string file)
    {
        if (!File.Exists(file))
        {
            Console.WriteLine("File not found: " + file);
            return;
        }

        if (amb_surface != null)
            amb_surface.Dispose();
        if (amb_texture != null)
            amb_texture.Dispose();

        this.sample_file = file;

        amb_texture = TextureLoader.FromFile(device, file, dev_rect.Width, dev_rect.Height, 1, Usage.RenderTarget, Format.X8R8G8B8, Pool.Default, Filter.Linear, Filter.Linear, 0);
        amb_surface = amb_texture.GetSurfaceLevel(0);

        effect_diecut.SetValue("Ambient_texture", amb_texture); // in
        need_render = true;
    }

    public void ChangeShadowFile(string file)
    {
        if (!File.Exists(file))
        {
            Console.WriteLine("File not found: " + file);
            return;
        }

        if (occ_surface != null)
            occ_surface.Dispose();
        if (occ_texture != null)
            occ_texture.Dispose();

        occ_texture = TextureLoader.FromFile(device, file, dev_rect.Width, dev_rect.Height, 1, Usage.RenderTarget, Format.X8R8G8B8, Pool.Default, Filter.Linear, Filter.Linear, 0);
        occ_surface = occ_texture.GetSurfaceLevel(0);

        effect_diecut.SetValue("Occlusion_texture", occ_texture); // in
        need_render = true;
    }

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

        screen.Create(dev_rect);
        screen.AssignValue(effect_diecut);

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
    }

    private void OnDeviceResizing(object sender, CancelEventArgs e)
    {
        e.Cancel = true;
    }

    public void Dispose()
    {
        if (screen != null)
            screen.Dispose();

        if (amb_surface != null)
            amb_surface.Dispose();
        if (occ_surface != null)
            occ_surface.Dispose();

        if (amb_texture != null)
            amb_texture.Dispose();
        if (occ_texture != null)
            occ_texture.Dispose();

        if (tex_zbuf != null)
            tex_zbuf.Dispose();
        if (dev_zbuf != null)
            dev_zbuf.Dispose();
        if (dev_surface != null)
            dev_surface.Dispose();

        if (effect_diecut != null)
            effect_diecut.Dispose();
        if (device != null)
            device.Dispose();
    }
}
}
