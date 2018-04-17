using System;
using System.Collections.Generic;
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
        Dictionary<int, string> location_namemap = new Dictionary<int, string>();
        Dictionary<string, Point> name_locationmap = new Dictionary<string, Point>();

        public SpriteRenderer(Device device)
        {
            this.device = device;

            location_namemap.Add(GetLocationKey(14, 25), "W_Hips");
            location_namemap.Add(GetLocationKey(14, 23), "W_Spine_Dummy");

            name_locationmap.Add("W_Hips", new Point(16*14, 16*25));
            name_locationmap.Add("W_Spine_Dummy", new Point(16*14, 16*23));
        }

        // on device lost
        public void Dispose()
        {
            if (node_sprite_texture != null)
                node_sprite_texture.Dispose();

            for (int i = 0; i < phase_textures.Length; i++)
                phase_textures[i].Dispose();
        }

        Rectangle rect; // client size

        void ScaleByClient(ref Point location)
        {
            location.X = location.X * rect.Width / 1024;
            location.Y = location.Y * rect.Height / 768;
        }

        void ScaleByClient(ref Size size)
        {
            size.Width = size.Width * rect.Width / 1024;
            size.Height = size.Height * rect.Height / 768;
        }

        static int GetLocationKey(int x16, int y16)
        {
            return x16 + 64*y16;
        }

        // on device reset
        public void Create(Rectangle rect)
        {
            this.rect = rect;

            for (int i = 0; i < phase_textures.Length; i++)
                phase_textures[i] = TextureLoader.FromFile(device, GetPhaseTexturePath(i), rect.Width, rect.Height, 1, Usage.RenderTarget, Format.A8R8G8B8, Pool.Default, Filter.Linear, Filter.Linear, 0);

            Size size = new Size(16, 16);
            ScaleByClient(ref size);
            node_sprite_texture = TextureLoader.FromFile(device, GetNodeSpriteTexturePath(), size.Width, size.Height, 1, Usage.RenderTarget, Format.A8R8G8B8, Pool.Default, Filter.Linear, Filter.Linear, 0);
        }

        int phase_tab = 1;

        string FindNodeNameByLocation(int x16, int y16)
        {
            string name;
            if (location_namemap.TryGetValue(GetLocationKey(x16, y16), out name))
                return name;
            else
                return null;
        }

        public bool Update(Point sprite_p, out string nodename)
        {
            int y16 = sprite_p.Y / 16;
            int x16 = sprite_p.X / 16;

            nodename = null;

            if (y16 >= 1 && y16 < 3)
            {
                // phase tab
                if (x16 >= 11 && x16 < 24)
                    phase_tab = 0; // MODEL
                else if (x16 >= 25 && x16 < 38)
                    phase_tab = 1; // POSE
                else if (x16 >= 39 && x16 < 52)
                    phase_tab = 2; // SCENE
                return true;
            }
            else if (y16 >= 5 && y16 < 40 && x16 >= 5 && x16 < 24)
            {
                // nodes palette
                nodename = FindNodeNameByLocation(x16, y16);
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

        bool GetNodeLocation(string nodename, out Point location)
        {
            if (nodename != null && name_locationmap.TryGetValue(nodename, out location))
                return true;
            location = Point.Empty;
            return false;
        }

        void DrawSelectedNodeSprite(Sprite sprite, string nodename)
        {
            sprite.Transform = Matrix.Identity;

            Point location;
            if (GetNodeLocation(nodename, out location))
            {
                ScaleByClient(ref location);

                sprite.Begin(0);
                sprite.Draw(node_sprite_texture, Rectangle.Empty, new Vector3(0, 0, 0), new Vector3(location.X, location.Y, 0), Color.FromArgb(0xCC, Color.Cyan));
                sprite.End();
            }
        }

        public void Render(Sprite sprite, string nodename)
        {
            DrawPhaseSprite(sprite);
            DrawSelectedNodeSprite(sprite, nodename);
        }
    }
}
