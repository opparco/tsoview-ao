using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;

using Microsoft.DirectX.Direct3D;

namespace TDCG.Editor
{
    /// ボーンを選択するためのパネルを扱う
    public class NodeLocationCollection
    {
        Device device;

        //パネル上のボーン位置からボーン名称を得るための辞書
        Dictionary<int, string> location_namemap;

        //ボーン名称からパネル上のボーン位置を得るための辞書
        Dictionary<string, Point> name_locationmap;

        //パネル上にあるボーン名称の短い形式配列を得る
        public string[] GetNodeNames()
        {
            string[] names = new string[name_locationmap.Keys.Count];
            name_locationmap.Keys.CopyTo(names, 0);
            return names;
        }

        public NodeLocationCollection(Device device, string node_location_texture_path)
        {
            this.device = device;
            this.node_location_texture_path = node_location_texture_path;
            location_namemap = new Dictionary<int, string>();
            name_locationmap = new Dictionary<string, Point>();
        }

        static int GetLocationKey(int x16, int y16)
        {
            return x16 + 64*y16;
        }

        public void LoadNameLocationMap(string namemap_path)
        {
            char[] delim = { ' ' };
            using (StreamReader source = new StreamReader(File.OpenRead(namemap_path)))
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

        string node_location_texture_path;

        // on device reset
        public void Create(Rectangle client_rect)
        {
            node_location_texture = TextureLoader.FromFile(device, node_location_texture_path, 320, 640, 1, Usage.RenderTarget, Format.A8R8G8B8, Pool.Default, Filter.Linear, Filter.Linear, 0);
        }
    }
}
