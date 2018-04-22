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
    public class NodeLocationCollection
    {
        Device device;
        int number;
        Dictionary<int, string> location_namemap;
        Dictionary<string, Point> name_locationmap;

        public NodeLocationCollection(Device device, int number)
        {
            this.device = device;
            this.number = number;
            location_namemap = new Dictionary<int, string>();
            name_locationmap = new Dictionary<string, Point>();
            LoadNameLocationMap();
        }

        string GetNodeLocationsPath()
        {
            string relative_path = Path.Combine(@"resources\node-locations", string.Format("{0}.txt", this.number));
            return Path.Combine(Application.StartupPath, relative_path);
        }

        static int GetLocationKey(int x16, int y16)
        {
            return x16 + 64*y16;
        }

        void LoadNameLocationMap()
        {
            char[] delim = { ' ' };
            using (StreamReader source = new StreamReader(File.OpenRead(GetNodeLocationsPath())))
            {
                string line;
                while ((line = source.ReadLine()) != null)
                {
                    string[] tokens = line.Split(delim);
                    Debug.Assert(tokens.Length == 3, "tokens length should be 3");
                    string nodename = tokens[0];
                    int x16 = int.Parse(tokens[1]);
                    int y16 = int.Parse(tokens[2]);
                    location_namemap.Add(GetLocationKey(x16, y16), nodename);
                    name_locationmap.Add(nodename, new Point(16*x16, 16*y16));
                }
            }
        }

        public string FindNodeNameByLocation(int x16, int y16)
        {
            string name;
            if (location_namemap.TryGetValue(GetLocationKey(x16, y16), out name))
                return name;
            else
                return null;
        }

        public bool GetNodeLocation(string nodename, out Point location)
        {
            if (nodename != null && name_locationmap.TryGetValue(nodename, out location))
                return true;
            location = Point.Empty;
            return false;
        }

        public Texture node_location_texture;

        // on device lost
        public void Dispose()
        {
            if (node_location_texture != null)
                node_location_texture.Dispose();
        }

        string GetNodeLocationTexturePath()
        {
            string relative_path = Path.Combine(@"resources\node-locations", string.Format("{0}.png", this.number));
            return Path.Combine(Application.StartupPath, relative_path);
        }

        // on device reset
        public void Create(Rectangle client_rect)
        {
            node_location_texture = TextureLoader.FromFile(device, GetNodeLocationTexturePath(), client_rect.Width, client_rect.Height, 1, Usage.RenderTarget, Format.A8R8G8B8, Pool.Default, Filter.Linear, Filter.Linear, 0);
        }
    }

    public class PoseMode : Mode
    {
        NodeLocationCollection[] node_location_collections;
        NodeLocationCollection current_node_location_collection;

        string selected_nodename = null;
        public string SelectedNodeName { get { return selected_nodename; } set { selected_nodename = value; } }

        public PoseMode(Device device, Sprite sprite) : base(device, sprite, "POSE", "1-pose.png")
        {
            node_location_collections = new NodeLocationCollection[5];
            for (int i = 0; i < node_location_collections.Length; i++)
                node_location_collections[i] = new NodeLocationCollection(device, i);
            current_node_location_collection = node_location_collections[0];
        }

        // on device lost
        public override void Dispose()
        {
            if (node_sprite_texture != null)
                node_sprite_texture.Dispose();

            for (int i = 0; i < node_location_collections.Length; i++)
                node_location_collections[i].Dispose();

            base.Dispose();
        }

        static string GetNodeSpriteTexturePath()
        {
            return Path.Combine(Application.StartupPath, @"node-sprite.png");
        }

        Texture node_sprite_texture;

        // on device reset
        public override void Create(Rectangle client_rect)
        {
            base.Create(client_rect);

            Size size = new Size(16, 16);
            ScaleByClient(ref size);
            node_sprite_texture = TextureLoader.FromFile(device, GetNodeSpriteTexturePath(), size.Width, size.Height, 1, Usage.RenderTarget, Format.A8R8G8B8, Pool.Default, Filter.Linear, Filter.Linear, 0);

            for (int i = 0; i < node_location_collections.Length; i++)
                node_location_collections[i].Create(client_rect);
        }

        public override bool Update(Point sprite_p)
        {
            int y16 = sprite_p.Y / 16;
            int x16 = sprite_p.X / 16;

            selected_nodename = null;

            if (y16 >= 5 && y16 < 40 && x16 >= 5 && x16 < 24)
            {
                // nodes palette
                selected_nodename = current_node_location_collection.FindNodeNameByLocation(x16, y16);
                return true;
            }
            else if (y16 >= 41 && y16 < 44 && x16 >= 5 && x16 < 24)
            {
                int number = -1;
                if (x16 >= 5 && x16 < 8)
                    number = 0;
                else if (x16 >= 9 && x16 < 12)
                    number = 1;
                else if (x16 >= 13 && x16 < 16)
                    number = 2;
                else if (x16 >= 17 && x16 < 20)
                    number = 3;
                else if (x16 >= 21 && x16 < 24)
                    number = 4;
                if (number != -1)
                    current_node_location_collection = node_location_collections[number];
                return true;
            }
            else
                return false;
        }

        void DrawSelectedNodeSprite()
        {
            sprite.Transform = Matrix.Identity;

            Point location;
            if (current_node_location_collection.GetNodeLocation(selected_nodename, out location))
            {
                ScaleByClient(ref location);

                sprite.Begin(0);
                sprite.Draw(node_sprite_texture, Rectangle.Empty, new Vector3(0, 0, 0), new Vector3(location.X, location.Y, 0), Color.FromArgb(0xCC, Color.Cyan));
                sprite.End();
            }
        }

        public override void Render()
        {
            DrawSprite(mode_texture);
            DrawSprite(current_node_location_collection.node_location_texture);
            DrawSelectedNodeSprite();
        }
    }
}
