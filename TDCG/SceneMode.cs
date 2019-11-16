using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;

using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace TDCG
{
    public class SceneMode : Mode
    {
        public SceneMode(Device device, Sprite sprite) : base(device, sprite, "SCENE", "2-scene.png")
        {
            ClearHidden();
        }

        static string GetCellTexturePath()
        {
            return @"resources\scene-mode\cell.png";
        }

        static string GetCursorTexturePath()
        {
            return @"resources\scene-mode\cursor.png";
        }

        static string GetDottedTexturePath()
        {
            return @"resources\scene-mode\dotted.png";
        }

        Texture cell_texture;
        Texture cursor_texture;
        Texture dotted_texture;

        // on device lost
        public override void Dispose()
        {
            Debug.WriteLine("SceneMode.Dispose");

            if (dotted_texture != null)
                dotted_texture.Dispose();
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

            cell_texture = TextureLoader.FromFile(device, GetCellTexturePath(), 128, 128, 1, Usage.RenderTarget, Format.A8R8G8B8, Pool.Default, Filter.Linear, Filter.Linear, 0);
            cursor_texture = TextureLoader.FromFile(device, GetCursorTexturePath(), 128, 96, 1, Usage.RenderTarget, Format.A8R8G8B8, Pool.Default, Filter.Linear, Filter.Linear, 0);
            dotted_texture = TextureLoader.FromFile(device, GetDottedTexturePath(), 128, 96, 1, Usage.RenderTarget, Format.A8R8G8B8, Pool.Default, Filter.Linear, Filter.Linear, 0);
        }

        int selected_idx = 0;
        public int SelectedIdx { get { return selected_idx; } set { selected_idx = value; } }

        public override bool Update(Point sprite_p)
        {
            int y16 = sprite_p.Y / 16;
            int x16 = sprite_p.X / 16;

            for (int row = 0; row < 4; row++)
            {
                int y16_1 = row * 9;
                if (y16 >= y16_1 + 5 && y16 < y16_1 + 11)
                {
                    for (int col = 0; col < 6; col++)
                    {
                        int x16_1 = col * 9;
                        if (x16 >= x16_1 + 5 && x16 < x16_1 + 13)
                        {
                            selected_idx = col + row * 6;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        bool[] hidden = new bool[24];

        public void ClearHidden()
        {
            for (int i=0; i<hidden.Length; i++)
            {
                hidden[i] = false;
            }
        }

        public bool GetHidden(int idx)
        {
            return hidden[idx];
        }

        public void SetHidden(int idx, bool value)
        {
            hidden[idx] = value;
        }

        Color cell_col = Color.FromArgb(0xCC, Color.White);
        Color hide_col = Color.FromArgb(0xCC, Color.Black);

        void DrawCellSprite()
        {
            for (int row = 0; row < 4; row++)
            for (int col = 0; col < 6; col++)
            {
                int idx = col + row * 6;
                sprite.Draw(cell_texture, Rectangle.Empty, new Vector3(0, 0, 0), new Vector3((col * 9 + 5) * 16, (row * 9 + 5) * 16, 0), GetHidden(idx) ? hide_col : cell_col);
            }
        }

        Color cursor_col = Color.FromArgb(0xCC, Color.FromArgb(253, 218, 112)); // SCENE

        void DrawSprite(Texture texture, int idx)
        {
            int col = idx%6;
            int row = idx/6;

            sprite.Transform = Matrix.Scaling(device_rect.Width / 1024.0f, device_rect.Height / 768.0f, 1.0f);
            sprite.Begin(SpriteFlags.AlphaBlend);

            sprite.Draw(texture, Rectangle.Empty, new Vector3(0, 0, 0), new Vector3((col * 9 + 5) * 16, (row * 9 + 5) * 16, 0), cursor_col);

            sprite.End();
        }

        public void DrawCursorSprite(int idx)
        {
            DrawSprite(cursor_texture, idx);
        }

        public void DrawDottedSprite(int idx)
        {
            DrawSprite(dotted_texture, idx);
        }

        public override void Render()
        {
            sprite.Transform = Matrix.Scaling(device_rect.Width / 1024.0f, device_rect.Height / 768.0f, 1.0f);
            sprite.Begin(SpriteFlags.AlphaBlend);

            DrawModeSprite();
            DrawCellSprite();

            sprite.End();
        }
    }
}
