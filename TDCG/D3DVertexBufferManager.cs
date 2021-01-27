using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using System.Windows.Forms;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using ICSharpCode.SharpZipLib.Core;

namespace TDCG
{
    /// 頂点構造体
    struct VertexFormat
    {
        /// 位置
        public Vector3 position;
        /// スキンウェイト0
        public float weight_0;
        /// スキンウェイト1
        public float weight_1;
        /// スキンウェイト2
        public float weight_2;
        /// スキンウェイト3
        public float weight_3;
        /// ボーンインデックス
        public uint bone_indices;
        /// 法線
        public Vector3 normal;
        /// テクスチャU座標
        public float u;
        /// テクスチャV座標
        public float v;
    }

    public sealed class D3DVertexBufferManager
    {
        public static D3DVertexBufferManager instance = new D3DVertexBufferManager();

        public Device device;

        /// sha1 と d3d vertex buffer を関連付ける辞書
        Dictionary<string, VertexBuffer> d3d_vbmap;
        /// 参照カウンタ
        Dictionary<string, int> d3d_vbref;

        D3DVertexBufferManager()
        {
            d3d_vbmap = new Dictionary<string, VertexBuffer>();
            d3d_vbref = new Dictionary<string, int>();
        }

        public VertexBuffer GetDirect3dVertexBufferBySha1(string sha1)
        {
            VertexBuffer d3d_vb;
            if (sha1 != null && d3d_vbmap.TryGetValue(sha1, out d3d_vb))
                return d3d_vb;
            else
                return null;
        }

        public bool ContainsKey(string sha1)
        {
            return d3d_vbmap.ContainsKey(sha1);
        }

        public void Add(string sha1, VertexBuffer d3d_vb)
        {
            d3d_vbmap.Add(sha1, d3d_vb);
            d3d_vbref.Add(sha1, 1);
            Debug.WriteLine("vb add " + sha1);
        }

        public void AddRef(string sha1)
        {
            d3d_vbref[sha1]++;
            Debug.WriteLine("vb ref " + sha1);
        }

        public void Release(string sha1)
        {
            Debug.WriteLine("vb release " + sha1);
            d3d_vbref[sha1]--;
            if (d3d_vbref[sha1] == 0)
            {
                VertexBuffer d3d_vb = d3d_vbmap[sha1];
                d3d_vb.Dispose();
                Debug.WriteLine("vb dispose " + sha1);
                d3d_vbmap.Remove(sha1);
                d3d_vbref.Remove(sha1);
            }
        }

        public void Clear()
        {
            if (d3d_vbmap.Count == 0)
            {
                Debug.WriteLine("d3d_vbmap has been cleared.");
            }
            else
            {
                Debug.WriteLine("d3d_vbmap clear.");
                d3d_vbmap.Clear();
                d3d_vbref.Clear();
            }
        }

        static string GetSha1HexString(Stream stream)
        {
            byte[] data;
            using (SHA1 sha1 = SHA1.Create())
            {
                data = sha1.ComputeHash(stream);
            }

            StringBuilder string_builder = new StringBuilder();

            for (int i = 0; i < data.Length; i++)
            {
                string_builder.Append(data[i].ToString("x2"));
            }

            return string_builder.ToString();
        }

        static string CombineStartupPath(string path)
        {
            return Path.Combine(Application.StartupPath, path);
        }

        static string GetObjectPath(string sha1)
        {
            return CombineStartupPath(string.Format(@"objects\{0}.bin", sha1));
        }

        public string Create(Vertex[] vertices)
        {
            string sha1 = null;
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                int num_vertices = vertices.Length;
                bw.Write(num_vertices);
                for (int i = 0; i < num_vertices; i++)
                {
                    vertices[i].Write(bw);
                }
                bw.Flush();
                ms.Seek(0, SeekOrigin.Begin);
                sha1 = GetSha1HexString(ms);
                string object_path = GetObjectPath(sha1);
                if (! File.Exists(object_path))
                {
                    using (FileStream file = File.Create(object_path))
                    {
                        ms.Seek(0, SeekOrigin.Begin);

                        byte[] buffer = new byte[4096];
                        StreamUtils.Copy(ms, file, buffer);
                    }
                }
            }

            if (ContainsKey(sha1))
            {
                AddRef(sha1);
            }
            else
            {
                VertexBuffer d3d_vb = CreateD3DVertexBuffer(vertices);
                Add(sha1, d3d_vb);
            }
            return sha1;
        }

        public void ReadOnDeviceReset(string sha1)
        {
            string object_path = GetObjectPath(sha1);
            using (FileStream file = File.OpenRead(object_path))
            using (BinaryReader reader = new BinaryReader(file))
            {
                int num_vertices = reader.ReadInt32();
                Vertex[] vertices = new Vertex[num_vertices];
                for (int i = 0; i < num_vertices; i++)
                {
                    Vertex v = new Vertex();
                    v.Read(reader);
                    vertices[i] = v;
                }

                if (ContainsKey(sha1))
                {
                    AddRef(sha1);
                }
                else
                {
                    VertexBuffer d3d_vb = CreateD3DVertexBuffer(vertices);
                    Add(sha1, d3d_vb);
                }
            }
        }

        public void Write(string sha1, Stream dest)
        {
            string object_path = GetObjectPath(sha1);
            using (FileStream file = File.OpenRead(object_path))
            {
                byte[] buffer = new byte[4096];
                StreamUtils.Copy(file, dest, buffer);
            }
        }

        /// <summary>
        /// device上でDirect3D頂点バッファを作成します。
        /// </summary>
        /// <param name="device">device</param>
        VertexBuffer CreateD3DVertexBuffer(Vertex[] vertices)
        {
            VertexBuffer vb = new VertexBuffer(typeof(VertexFormat), vertices.Length, device, Usage.Dynamic | Usage.WriteOnly, VertexFormats.None, Pool.Default);

            //
            // rewrite vertex buffer
            //
            GraphicsStream gs = vb.Lock(0, 0, LockFlags.None);
            {
                for (int i = 0; i < vertices.Length; i++)
                {
                    Vertex v = vertices[i];

                    gs.Write(v.position);
                    for (int j = 0; j < 4; j++)
                        gs.Write(v.skin_weights[j].weight);
                    gs.Write(v.bone_indices);
                    gs.Write(v.normal);
                    gs.Write(v.u);
                    gs.Write(v.v);
                }
            }
            vb.Unlock();
            vertices = null;
            return vb;
        }
    }
}
