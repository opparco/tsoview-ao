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
    public class PoseMode : Mode
    {
        NodeLocationCollection[] node_location_collections;
        NodeLocationCollection current_node_location_collection;

        string selected_nodename = null;
        public string SelectedNodeName { get { return selected_nodename; } set { selected_nodename = value; } }

        public string[] NodeNames
        {
            get
            {
                return current_node_location_collection.GetNodeNames();
            }
        }

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
        public override void Create(Rectangle device_rect)
        {
            base.Create(device_rect);

            Size size = new Size(16, 16);
            ScaleByDevice(ref size);

            node_sprite_texture = TextureLoader.FromFile(device, GetNodeSpriteTexturePath(), size.Width, size.Height, 1, Usage.RenderTarget, Format.A8R8G8B8, Pool.Default, Filter.Linear, Filter.Linear, 0);

            for (int i = 0; i < node_location_collections.Length; i++)
                node_location_collections[i].Create(device_rect);
        }

        static int FindNodeNumberByLocation(int x16)
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
            return number;
        }

        public override bool Update(Point sprite_p)
        {
            int y16 = sprite_p.Y / 16;
            int x16 = sprite_p.X / 16;

            if (y16 >= 5 && y16 < 40 && x16 >= 5 && x16 < 24)
            {
                // node location collection
                selected_nodename = current_node_location_collection.FindNodeNameByLocation(x16, y16);
                return true;
            }
            else if (y16 >= 41 && y16 < 44 && x16 >= 5 && x16 < 24)
            {
                // node location collection tab
                int number = FindNodeNumberByLocation(x16);
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
                ScaleByDevice(ref location);

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
