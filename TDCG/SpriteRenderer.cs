using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace TDCG
{
    public class SpriteRenderer : IDisposable
    {
        Texture[] phase_textures = new Texture[3];
        Texture node_sprite_texture;

        static string[] phase_filenames = { "0-model.png", "1-pose.png", "2-scene.png" };
        static string GetPhaseTexturePath(int i)
        {
            string relative_path = Path.Combine(@"resources\phases", phase_filenames[i]);
            return Path.Combine(Application.StartupPath, relative_path);
        }

        static string GetNodeSpriteTexturePath()
        {
            return Path.Combine(Application.StartupPath, @"node-sprite.png");
        }

        Device device;

        public SpriteRenderer(Device device)
        {
            this.device = device;
        }

        // on device lost
        public void Dispose()
        {
            if (node_sprite_texture != null)
                node_sprite_texture.Dispose();

            for (int i = 0; i < phase_textures.Length; i++)
                phase_textures[i].Dispose();
        }

        // on device reset
        public void Create(Rectangle rect)
        {
            for (int i = 0; i < phase_textures.Length; i++)
                phase_textures[i] = TextureLoader.FromFile(device, GetPhaseTexturePath(i), rect.Width, rect.Height, 1, Usage.RenderTarget, Format.A8R8G8B8, Pool.Default, Filter.Linear, Filter.Linear, 0);

            node_sprite_texture = TextureLoader.FromFile(device, GetNodeSpriteTexturePath());
        }

        int phase_tab = 1;

        public bool Update(Point sprite_p, out string node_name)
        {
            int y16 = sprite_p.Y / 16;
            int x16 = sprite_p.X / 16;

            node_name = null;

            if (y16 >= 1 && y16 < 3)
            {
                // phase tab
                if (x16 >= 11 && x16 < 24)
                {
                    phase_tab = 0; // MODEL
                }
                else if (x16 >= 25 && x16 < 38)
                {
                    phase_tab = 1; // POSE
                }
                else if (x16 >= 39 && x16 < 52)
                {
                    phase_tab = 2; // SCENE
                }
                return true;
            }
            else if (y16 >= 5 && y16 < 40 && x16 >= 5 && x16 < 24)
            {
                // nodes palette
                {
                    if (y16 == 25 && x16 == 14)
                    {
                        node_name = "W_Hips";
                    }
                    else if (y16 == 23 && x16 == 14)
                    {
                        node_name = "W_Spine_Dummy";
                    }
                }
                return true;
            }
            return false;
        }

        void DrawPhaseSprite(Sprite sprite)
        {
            sprite.Transform = Matrix.Identity;

            sprite.Begin(0);
            sprite.Draw(phase_textures[phase_tab], Rectangle.Empty, new Vector3(0, 0, 0), new Vector3(0, 0, 0), Color.FromArgb(0xCC, Color.White));
            sprite.End();
        }

        void GetNodeLocation(TMONode node, out Point location)
        {
            if (node.Name == "W_Spine_Dummy")
                location = new Point(16*14, 16*23);
            else // W_Hips
                location = new Point(16*14, 16*25);
        }

        void DrawSelectedNodeSprite(Sprite sprite, TMONode node)
        {
            if (node == null)
                return;

            sprite.Transform = Matrix.Identity;

            Point location;
            GetNodeLocation(node, out location);

            sprite.Begin(0);
            sprite.Draw(node_sprite_texture, new Rectangle(0, 0, 16, 16), new Vector3(0, 0, 0), new Vector3(location.X, location.Y, 0), Color.FromArgb(0xCC, Color.Cyan));
            sprite.End();
        }

        public void Render(Sprite sprite, TMONode node)
        {
            DrawPhaseSprite(sprite);
            DrawSelectedNodeSprite(sprite, node);
        }
    }
}
