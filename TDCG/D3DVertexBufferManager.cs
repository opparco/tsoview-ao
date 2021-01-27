using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

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

        /// <summary>
        /// device上でDirect3D頂点バッファを作成します。
        /// </summary>
        /// <param name="device">device</param>
        public VertexBuffer CreateD3DVertexBuffer(Vertex[] vertices)
        {
            VertexBuffer vb = new VertexBuffer(typeof(VertexFormat), vertices.Length, device, Usage.Dynamic | Usage.WriteOnly, VertexFormats.None, Pool.Default);
            //vb.Created += new EventHandler(vb_Created);
            //vb_Created(vb, null);

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

        void vb_Created(object sender, EventArgs e)
        {
            VertexBuffer vb = (VertexBuffer)sender;

            //todo: load vertices from file.
        }
    }
}
