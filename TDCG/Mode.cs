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
    public abstract class Mode
    {
        protected Device device;
        protected Sprite sprite;
        //mode name
        string name;
        public string Name { get { return name; } }
        //filename of the mode picture
        string filename;

        public Mode(Device device, Sprite sprite, string name, string filename)
        {
            this.device = device;
            this.sprite = sprite;
            this.name = name;
            this.filename = filename;
        }

        Texture texture;

        // on device lost
        public virtual void Dispose()
        {
            if (texture != null)
                texture.Dispose();
        }

        Rectangle client_rect;

        protected void ScaleByClient(ref Point location)
        {
            location.X = location.X * client_rect.Width / 1024;
            location.Y = location.Y * client_rect.Height / 768;
        }

        protected void ScaleByClient(ref Size size)
        {
            size.Width = size.Width * client_rect.Width / 1024;
            size.Height = size.Height * client_rect.Height / 768;
        }

        string GetTexturePath()
        {
            string relative_path = Path.Combine(@"resources\modes", filename);
            return Path.Combine(Application.StartupPath, relative_path);
        }

        // on device reset
        public virtual void Create(Rectangle client_rect)
        {
            this.client_rect = client_rect;
            texture = TextureLoader.FromFile(device, GetTexturePath(), client_rect.Width, client_rect.Height, 1, Usage.RenderTarget, Format.A8R8G8B8, Pool.Default, Filter.Linear, Filter.Linear, 0);
        }

        public abstract bool Update(Point sprite_p);

        protected void DrawSprite()
        {
            sprite.Transform = Matrix.Identity;

            sprite.Begin(0);
            sprite.Draw(texture, Rectangle.Empty, new Vector3(0, 0, 0), new Vector3(0, 0, 0), Color.FromArgb(0xCC, Color.White));
            sprite.End();
        }

        public abstract void Render();
    }
}
