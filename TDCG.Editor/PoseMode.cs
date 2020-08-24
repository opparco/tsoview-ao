using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;

using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace TDCG.Editor
{
    public class PoseMode : Mode
    {
        Texture node_sprite_texture;
        Texture node_location_menu_texture;

        NodeLocationCollection[] node_location_collections;
        NodeLocationCollection current_node_location_collection;

        string selected_nodename = null;
        public string SelectedNodeName { get { return selected_nodename; } set { selected_nodename = value; } }

        public string[] NodeNames
        {
            get
            {
                if (collection_enabled)
                    return current_node_location_collection.GetNodeNames();
                else
                    return new string[] {};
            }
        }

        public PoseMode(Device device, Sprite sprite, string root_path) : base(device, sprite, "POSE")
        {
            mode_texture_path = Path.Combine(root_path, @"modes\1-pose.png");

            node_sprite_texture_path = Path.Combine(root_path, @"node-sprite.png");
            node_location_menu_texture_path = Path.Combine(root_path, @"node-locations\menu.png");

            node_location_collections = new NodeLocationCollection[5];
            for (int i = 0; i < node_location_collections.Length; i++)
            {
                string namemap_path = Path.Combine(root_path, string.Format(@"node-locations\{0}.txt", i));
                string texture_path = Path.Combine(root_path, string.Format(@"node-locations\{0}.png", i));
                NodeLocationCollection collection = new NodeLocationCollection(device, texture_path);
                collection.LoadNameLocationMap(namemap_path);
                node_location_collections[i] = collection;
            }
            current_node_location_collection = node_location_collections[0];
        }

        // on device lost
        public override void Dispose()
        {
            Debug.WriteLine("PoseMode.Dispose");

            for (int i = 0; i < node_location_collections.Length; i++)
                node_location_collections[i].Dispose();

            if (node_location_menu_texture != null)
                node_location_menu_texture.Dispose();

            if (node_sprite_texture != null)
                node_sprite_texture.Dispose();

            base.Dispose();
        }

        string node_sprite_texture_path;
        string node_location_menu_texture_path;

        // on device reset
        public override void Create(Rectangle device_rect)
        {
            base.Create(device_rect);

            node_sprite_texture = TextureLoader.FromFile(device, node_sprite_texture_path, 16, 16, 1, Usage.RenderTarget, Format.A8R8G8B8, Pool.Default, Filter.Linear, Filter.Linear, 0);

            node_location_menu_texture = TextureLoader.FromFile(device, node_location_menu_texture_path, 320, 640, 1, Usage.RenderTarget, Format.A8R8G8B8, Pool.Default, Filter.Linear, Filter.Linear, 0);

            for (int i = 0; i < node_location_collections.Length; i++)
                node_location_collections[i].Create(device_rect);
        }

        static int FindNodeNumberByLocation(int x16)
        {
            int number = -1;
            if (x16 >= 1 && x16 < 4)
                number = 0;
            else if (x16 >= 5 && x16 < 8)
                number = 1;
            else if (x16 >= 9 && x16 < 12)
                number = 2;
            else if (x16 >= 13 && x16 < 16)
                number = 3;
            else if (x16 >= 17 && x16 < 20)
                number = 4;
            return number;
        }

        bool collection_enabled = false;

        public override bool Update(Point sprite_p)
        {
            int y16 = sprite_p.Y / 16;
            int x16 = sprite_p.X / 16;

            if (x16 >= 1 && x16 < 20)
            {
                if (collection_enabled && y16 >= 5 && y16 < 40)
                {
                    // node location collection
                    selected_nodename = current_node_location_collection.FindNodeNameByLocation(x16, y16);
                    return true;
                }
                else if (y16 >= 41 && y16 < 44)
                {
                    // node location collection tab
                    int number = FindNodeNumberByLocation(x16);
                    if (number != -1)
                    {
                        NodeLocationCollection node_location_collection = node_location_collections[number];
                        if (current_node_location_collection == node_location_collection)
                            collection_enabled = ! collection_enabled;
                        else
                            collection_enabled = true;
                        current_node_location_collection = node_location_collection;
                        return true;
                    }
                }
            }
            return false;
        }

        void DrawNodeCollectionMenuSprite()
        {
            sprite.Draw(node_location_menu_texture, Rectangle.Empty, new Vector3(0, 0, 0), new Vector3(8, 72, 0), Color.FromArgb(0xCC, Color.White));
        }

        void DrawNodeCollectionSprite()
        {
            sprite.Draw(current_node_location_collection.node_location_texture, Rectangle.Empty, new Vector3(0, 0, 0), new Vector3(8, 72, 0), Color.FromArgb(0xCC, Color.White));
        }

        void DrawSelectedNodeSprite()
        {
            Point location;
            if (current_node_location_collection.GetNodeLocation(selected_nodename, out location))
            {
                sprite.Draw(node_sprite_texture, Rectangle.Empty, new Vector3(0, 0, 0), new Vector3(location.X, location.Y, 0), Color.FromArgb(0xCC, Color.Cyan));
            }
        }

        public override void Render()
        {
            sprite.Transform = Matrix.Scaling(device_rect.Width / 1024.0f, device_rect.Height / 768.0f, 1.0f);
            sprite.Begin(SpriteFlags.AlphaBlend);

            DrawModeSprite();
            DrawNodeCollectionMenuSprite();
            if (collection_enabled)
            {
                DrawNodeCollectionSprite();
                DrawSelectedNodeSprite();
            }

            sprite.End();
        }
    }
}
