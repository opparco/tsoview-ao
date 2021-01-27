using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.DirectX.Direct3D;

namespace TDCG
{
    public sealed class D3DVertexBufferManager
    {
        public static D3DVertexBufferManager instance = new D3DVertexBufferManager();

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
    }
}
