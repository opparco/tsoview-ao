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
    public class ModelMode : Mode
    {
        public ModelMode(Device device, Sprite sprite) : base(device, sprite, "MODEL", "0-model.png")
        {
        }

        static string GetCellTexturePath()
        {
            return Path.Combine(Application.StartupPath, @"resources\model-mode\cell.png");
        }

        static string GetCursorTexturePath()
        {
            return Path.Combine(Application.StartupPath, @"resources\model-mode\cursor.png");
        }

        Texture cell_texture;
        Texture cursor_texture;

        // on device lost
        public override void Dispose()
        {
            Debug.WriteLine("ModelMode.Dispose");

            if (cursor_texture != null)
                cursor_texture.Dispose();
            if (cell_texture != null)
                cell_texture.Dispose();

            base.Dispose();
        }

        // on device reset
        public override void Create(Rectangle device_rect)
        {
            base.Create(device_rect);

            cell_texture = TextureLoader.FromFile(device, GetCellTexturePath(), 96, 128, 1, Usage.RenderTarget, Format.A8R8G8B8, Pool.Default, Filter.Linear, Filter.Linear, 0);
            cursor_texture = TextureLoader.FromFile(device, GetCursorTexturePath(), 96, 96, 1, Usage.RenderTarget, Format.A8R8G8B8, Pool.Default, Filter.Linear, Filter.Linear, 0);
        }

        public override bool Update(Point sprite_p)
        {
            return false;
        }

        Color cell_col = Color.FromArgb(0xCC, Color.White);

        void DrawCellSprite()
        {
            sprite.Transform = Matrix.Scaling(device_rect.Width / 1024.0f, device_rect.Height / 768.0f, 1.0f);

            sprite.Begin(0);
            for (int row = 0; row < 4; row++)
            for (int col = 0; col < 8; col++)
            {
                sprite.Draw(cell_texture, Rectangle.Empty, new Vector3(0, 0, 0), new Vector3((col * 7 + 4) * 16, (row * 9 + 5) * 16, 0), cell_col);
            }
            sprite.End();
        }

        Color cursor_col = Color.FromArgb(0xCC, Color.FromArgb(136, 255, 156)); // MODEL

        public void DrawCursorSprite(int idx)
        {
            int col = idx%8;
            int row = idx/8;

            sprite.Transform = Matrix.Scaling(device_rect.Width / 1024.0f, device_rect.Height / 768.0f, 1.0f);

            sprite.Begin(0);
            sprite.Draw(cursor_texture, Rectangle.Empty, new Vector3(0, 0, 0), new Vector3((col * 7 + 4) * 16, (row * 9 + 5) * 16, 0), cursor_col);
            sprite.End();
        }

        public override void Render()
        {
            DrawSprite(mode_texture);
            DrawCellSprite();
        }
    }
}
