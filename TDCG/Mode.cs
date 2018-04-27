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
        string mode_filename;

        public Mode(Device device, Sprite sprite, string name, string mode_filename)
        {
            this.device = device;
            this.sprite = sprite;
            this.name = name;
            this.mode_filename = mode_filename;
        }

        protected Texture mode_texture;

        // on device lost
        public virtual void Dispose()
        {
            if (mode_texture != null)
                mode_texture.Dispose();
        }

        protected Rectangle device_rect;

        string GetModeTexturePath()
        {
            string relative_path = Path.Combine(@"resources\modes", mode_filename);
            return Path.Combine(Application.StartupPath, relative_path);
        }

        // on device reset
        public virtual void Create(Rectangle device_rect)
        {
            this.device_rect = device_rect;

            mode_texture = TextureLoader.FromFile(device, GetModeTexturePath(), 1024, 768, 1, Usage.RenderTarget, Format.A8R8G8B8, Pool.Default, Filter.Linear, Filter.Linear, 0);
        }

        public abstract bool Update(Point sprite_p);

        Color mode_col = Color.FromArgb(0xCC, Color.White);

        protected void DrawModeSprite()
        {
            sprite.Transform = Matrix.Scaling(device_rect.Width / 1024.0f, device_rect.Height / 768.0f, 1.0f);

            sprite.Begin(0);
            sprite.Draw(mode_texture, Rectangle.Empty, new Vector3(0, 0, 0), new Vector3(0, 0, 0), mode_col);
            sprite.End();
        }

        public abstract void Render();
    }
}
