using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Drawing;

using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

public class EnvironmentMapContainer
{
    Device device;
    string root_path;

    /// shader設定上の texture name と d3d texture を関連付ける辞書
    Dictionary<string, Texture> d3d_texturemap;

    public EnvironmentMapContainer(Device device, string root_path)
    {
        this.device = device;
        this.root_path = root_path;

        d3d_texturemap = new Dictionary<string, Texture>();
    }

    string GetTexturePath(string name)
    {
        return Path.Combine(root_path, name + ".png");
    }

    public Texture GetDirect3DTexture(string name)
    {
        Texture d3d_tex;
        if (d3d_texturemap.TryGetValue(name, out d3d_tex))
        {
            return d3d_tex;
        }
        else
        {
            string source_file = GetTexturePath(name);

            if (!File.Exists(source_file))
            {
                if (name != "emap")
                    return GetDirect3DTexture("emap");
                else
                    return null;
            }

            using (Bitmap bitmap = new Bitmap(source_file))
            using (MemoryStream ms = new MemoryStream())
            {
                bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
                bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                ms.Seek(0, SeekOrigin.Begin);
                d3d_tex = TextureLoader.FromStream(device, ms);
            }
            d3d_texturemap[name] = d3d_tex;
            return d3d_tex;
        }
    }

    public void Dispose()
    {
        foreach (Texture d3d_tex in d3d_texturemap.Values)
        {
            d3d_tex.Dispose();
        }
        d3d_texturemap.Clear();
    }
}
