using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

public class NormalMapContainer
{
    Device device;
    string root_path;

    /// shader設定上の texture name と d3d texture を関連付ける辞書
    Dictionary<string, Texture> d3d_texturemap;

    public NormalMapContainer(Device device, string root_path)
    {
        this.device = device;
        this.root_path = root_path;

        d3d_texturemap = new Dictionary<string, Texture>();
    }

    string GetNormalTexturePath(string name)
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
            string source_file = GetNormalTexturePath(name);

            if (!File.Exists(source_file))
            {
                if (name != "nmap")
                    return GetDirect3DTexture("nmap");
                else
                    return null;
            }

            d3d_tex = TextureLoader.FromFile(device, source_file);
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
