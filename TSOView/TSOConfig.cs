using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace TSOView
{

public class TSOConfig
{
    public Size ClientSize { get; set; }
    public bool Windowed { get; set; }
    public Size DeviceSize { get; set; }
    Color screen_color;
    public Color ScreenColor { get { return screen_color; } }
    public string ScreenColorName
    {
        get { return ColorTranslator.ToHtml(screen_color); }
        set { screen_color = ColorTranslator.FromHtml(value); }
    }
    public Microsoft.DirectX.Vector4 Ambient { get; set; }
    public float HohoAlpha { get; set; }
    public bool XRGBDepth { get; set; }
    public bool MainGel { get; set; }
    public bool ScreenDof { get; set; }
    public string DepthMapFormat { get; set; }
    public string NormalMapFormat { get; set; }
    public string RenderMode { get; set;}
    public int RecordStep { get; set; }

    public TSOConfig()
    {
        this.ClientSize = new Size(800, 600);
        this.Windowed = true;
        this.DeviceSize = new Size(0, 0);
        screen_color = Color.LightGray;
        this.Ambient = new Microsoft.DirectX.Vector4(1, 1, 1, 1);
        this.HohoAlpha = 1.0f;
        this.XRGBDepth = true;
        this.DepthMapFormat = "X8R8G8B8";
        this.NormalMapFormat = "X8R8G8B8";
        this.RenderMode = "Main";
        this.MainGel = false;
        this.ScreenDof = false;
        this.RecordStep = 5;
    }

    public void Dump()
    {
        XmlSerializer serializer = new XmlSerializer(typeof(TSOConfig));
        XmlWriterSettings settings = new XmlWriterSettings();
        settings.Encoding = Encoding.GetEncoding("Shift_JIS");
        settings.Indent = true;
        XmlWriter writer = XmlWriter.Create(Console.Out, settings);
        serializer.Serialize(writer, this);
        writer.Close();
    }

    public static TSOConfig Load(string source_file)
    {
        XmlReader reader = XmlReader.Create(source_file);
        XmlSerializer serializer = new XmlSerializer(typeof(TSOConfig));
        TSOConfig config = serializer.Deserialize(reader) as TSOConfig;
        reader.Close();
        return config;
    }
}
}
