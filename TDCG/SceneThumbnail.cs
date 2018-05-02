using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace TDCG
{
    public class SceneThumbnail
    {
        Device device;
        Texture texture;
        Surface surface;

        public void Dispose()
        {
            if (surface != null)
                surface.Dispose();
            if (texture != null)
                texture.Dispose();
        }

        public void Create(Device device)
        {
            this.device = device;
            texture = new Texture(device, 128, 128, 1, Usage.RenderTarget, Format.X8R8G8B8, Pool.Default);
            surface = texture.GetSurfaceLevel(0);
        }

        public void Snap(Surface dev_surface)
        {
            Debug.WriteLine("SceneThumbnail.Snap");

            int w = dev_surface.Description.Width;
            int h = dev_surface.Description.Height;
            Rectangle square_rect;
            if (h < w)
                square_rect = new Rectangle((w-h)/2, 0, h, h);
            else
                square_rect = new Rectangle(0, (h-w)/2, w, w);

            device.StretchRectangle(dev_surface, square_rect, this.surface, new Rectangle(0, 0, 128, 128), TextureFilter.Point);
        }

        public static string GetFileName()
        {
            return Path.Combine(Application.StartupPath, @"scene.tdcgpose\thumbnail.png");
        }

        public void SaveToFile()
        {
            SurfaceLoader.Save(GetFileName(), ImageFileFormat.Png, this.surface);
        }
    }
}
