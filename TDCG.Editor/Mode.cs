using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;

using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace TDCG.Editor
{
    public abstract class Mode
    {
        protected Device device;
        protected Sprite sprite;
        //mode name
        string name;
        public string Name { get { return name; } }

        public Mode(Device device, Sprite sprite, string name)
        {
            this.device = device;
            this.sprite = sprite;
            this.name = name;
        }

        protected Texture mode_texture;

        // on device lost
        public virtual void Dispose()
        {
            if (mode_texture != null)
                mode_texture.Dispose();
        }

        protected Rectangle device_rect;

        protected string mode_texture_path;

        // on device reset
        public virtual void Create(Rectangle device_rect)
        {
            this.device_rect = device_rect;

            mode_texture = TextureLoader.FromFile(device, mode_texture_path, 1024, 768, 1, Usage.RenderTarget, Format.A8R8G8B8, Pool.Default, Filter.Linear, Filter.Linear, 0);
        }

        public abstract bool Update(Point sprite_p);

        Color mode_col = Color.FromArgb(0xCC, Color.White);

        protected void DrawModeSprite()
        {
            sprite.Draw(mode_texture, Rectangle.Empty, new Vector3(0, 0, 0), new Vector3(0, 0, 0), mode_col);
        }

        public abstract void Render();
    }
}
