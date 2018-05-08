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
    public class ModelThumbnail
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
            texture = new Texture(device, 128, 256, 1, Usage.RenderTarget, Format.X8R8G8B8, Pool.Default);
            surface = texture.GetSurfaceLevel(0);
        }

        public void Snap(Surface dev_surface)
        {
            Debug.WriteLine("ModelThumbnail.Snap");

            int w = dev_surface.Description.Width;
            int h = dev_surface.Description.Height;
            Rectangle rect;
            if (h/2 < w)
                rect = new Rectangle((w-h/2)/2, 0, h/2, h);
            else
                rect = new Rectangle(0, (h/2-w), w, w*2);

            device.StretchRectangle(dev_surface, rect, this.surface, new Rectangle(0, 0, 128, 256), TextureFilter.Point);
        }

        public void SaveToFile(string thumbnail_file)
        {
            SurfaceLoader.Save(thumbnail_file, ImageFileFormat.Png, this.surface);
        }
    }
}
