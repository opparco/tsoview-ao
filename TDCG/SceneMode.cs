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
            return Path.Combine(Application.StartupPath, @"resources\scene-mode\cell.png");
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

            cell_texture = TextureLoader.FromFile(device, GetCellTexturePath(), 128, 128, 1, Usage.RenderTarget, Format.A8R8G8B8, Pool.Default, Filter.Linear, Filter.Linear, 0);
        }

        public override bool Update(Point sprite_p)
        {
            return false;
        }

        void DrawCellSprite()
        {
            sprite.Transform = Matrix.Scaling(device_rect.Width / 1024.0f, device_rect.Height / 768.0f, 1.0f);

            sprite.Begin(0);
            for (int row = 0; row < 2; row++)
            for (int col = 0; col < 6; col++)
            {
                sprite.Draw(cell_texture, Rectangle.Empty, new Vector3(0, 0, 0), new Vector3((col * 9 + 5) * 16, (row * 9 + 5) * 16, 0), Color.FromArgb(0xCC, Color.White));
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
