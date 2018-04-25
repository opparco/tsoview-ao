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
    public class SceneMode : Mode
    {
        public SceneMode(Device device, Sprite sprite) : base(device, sprite, "SCENE", "2-scene.png")
        {
        }

        static string GetCellTexturePath()
        {
            return Path.Combine(Application.StartupPath, @"cell.png");
        }

        Texture cell_texture;

        // on device lost
        public override void Dispose()
        {
            Debug.WriteLine("SceneMode.Dispose");

            if (cell_texture != null)
                cell_texture.Dispose();

            base.Dispose();
        }

        // on device reset
        public override void Create(Rectangle device_rect)
        {
            base.Create(device_rect);

            Size size = new Size(96, 128);
            ScaleByDevice(ref size);

            cell_texture = TextureLoader.FromFile(device, GetCellTexturePath(), size.Width, size.Height, 1, Usage.RenderTarget, Format.A8R8G8B8, Pool.Default, Filter.Linear, Filter.Linear, 0);
        }

        public override bool Update(Point sprite_p)
        {
            return false;
        }

        void DrawCellSprite()
        {
            sprite.Transform = Matrix.Identity;

            sprite.Begin(0);
            for (int row = 0; row < 2; row++)
            for (int col = 0; col < 8; col++)
            {
                Point p = new Point((col * 7 + 4) * 16, (row * 9 + 6) * 16);
                ScaleByDevice(ref p);

                sprite.Draw(cell_texture, Rectangle.Empty, new Vector3(0, 0, 0), new Vector3(p.X, p.Y, 0), Color.FromArgb(0xCC, Color.White));
            }
            sprite.End();
        }

        public override void Render()
        {
            DrawSprite(mode_texture);
            DrawCellSprite();
        }
    }
}
