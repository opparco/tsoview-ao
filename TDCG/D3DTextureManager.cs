using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.DirectX.Direct3D;

namespace TDCG
{
    public sealed class D3DTextureManager
    {
        public static D3DTextureManager instance = new D3DTextureManager();

        /// sha1 と d3d texture を関連付ける辞書
        Dictionary<string, Texture> d3d_texturemap;
        /// 参照カウンタ
        Dictionary<string, int> d3d_textureref;

        D3DTextureManager()
        {
            d3d_texturemap = new Dictionary<string, Texture>();
            d3d_textureref = new Dictionary<string, int>();
        }

        public Texture GetDirect3dTextureBySha1(string sha1)
        {
            Texture d3d_tex;
            if (sha1 != null && d3d_texturemap.TryGetValue(sha1, out d3d_tex))
                return d3d_tex;
            else
                return null;
        }

        public bool ContainsKey(string sha1)
        {
            return d3d_texturemap.ContainsKey(sha1);
        }

        public void Add(string sha1, Texture d3d_tex)
        {
            d3d_texturemap.Add(sha1, d3d_tex);
            d3d_textureref.Add(sha1, 1);
            Debug.WriteLine("tex add " + sha1);
        }

        public void AddRef(string sha1)
        {
            d3d_textureref[sha1]++;
            Debug.WriteLine("tex ref " + sha1);
        }

        public void Release(string sha1)
        {
            Debug.WriteLine("tex release " + sha1);
            d3d_textureref[sha1]--;
            if (d3d_textureref[sha1] == 0)
            {
                Texture d3d_tex = d3d_texturemap[sha1];
                d3d_tex.Dispose();
                Debug.WriteLine("tex dispose " + sha1);
                d3d_texturemap.Remove(sha1);
                d3d_textureref.Remove(sha1);
            }
        }
    }
}
