using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace TDCG
{
    using BYTE = Byte;
    using WORD = UInt16;

    [StructLayout(LayoutKind.Sequential, Pack=1)]
    struct TARGA_HEADER
    {
            public BYTE     id;
            public BYTE         colormap;
            public BYTE         imagetype;
            public WORD         colormaporigin;
            public WORD         colormaplength;
            public BYTE         colormapdepth;
            public WORD         x;
            public WORD         y;
            public WORD         width;
            public WORD         height;
            public BYTE         depth;
            public BYTE         type;
    };

    public sealed class D3DTextureManager
    {
        public static D3DTextureManager instance = new D3DTextureManager();

        public Device device;

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

        public void Clear()
        {
            if (d3d_texturemap.Count == 0)
            {
                Debug.WriteLine("d3d_texturemap has been cleared.");
            }
            else
            {
                Debug.WriteLine("d3d_texturemap clear.");
                d3d_texturemap.Clear();
                d3d_textureref.Clear();
            }
        }

        static readonly int sizeof_tga_header = Marshal.SizeOf(typeof(TARGA_HEADER));

        /// <summary>
        /// device上でDirect3Dテクスチャを作成します。
        /// </summary>
        public Texture CreateD3DTexture(int width, int height, int depth, byte[] data)
        {
            if (data.Length == 0)
                return null;

            for(int j = 0; j < data.Length; j += 4)
            {
                byte tmp = data[j + 2];
                data[j + 2] = data[j + 0];
                data[j + 0] = tmp;
            }

            Texture d3d_tex;
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                TARGA_HEADER header;

                header.id = 0;
                header.colormap = 0;
                header.imagetype = 2;
                header.colormaporigin = 0;
                header.colormaplength = 0;
                header.colormapdepth = 0;
                header.x = 0;
                header.y = 0;
                header.width = (ushort)width;
                header.height = (ushort)height;
                header.depth = (byte)(depth * 8);
                header.type = 0x20;

                IntPtr header_ptr = Marshal.AllocHGlobal(sizeof_tga_header);
                Marshal.StructureToPtr(header, header_ptr, false);
                byte[] header_buf = new byte[sizeof_tga_header];
                Marshal.Copy(header_ptr, header_buf, 0, sizeof_tga_header);
                Marshal.FreeHGlobal(header_ptr);
                bw.Write(header_buf);

                bw.Write(data);
                bw.Flush();
                ms.Seek(0, SeekOrigin.Begin);
                d3d_tex = TextureLoader.FromStream(device, ms);
            }
            data = null;
            return d3d_tex;
        }
    }
}
