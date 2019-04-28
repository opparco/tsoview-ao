using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.ComponentModel;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using TDCG.Extensions;

namespace TDCG
{
    using BYTE  = Byte;
    using WORD  = UInt16;
    using DWORD = UInt32;
    using LONG  = Int32;

    [StructLayout(LayoutKind.Sequential, Pack=1)]
    struct TARGA_HEADER
    {
	    public BYTE     id;
	    public BYTE		colormap;
	    public BYTE		imagetype;
	    public WORD		colormaporigin;
	    public WORD		colormaplength;
	    public BYTE		colormapdepth;
	    public WORD		x;
	    public WORD		y;
	    public WORD		width;
	    public WORD		height;
	    public BYTE		depth;
	    public BYTE		type;
    };

    [StructLayout(LayoutKind.Sequential, Pack=1)]
    struct BITMAPFILEHEADER
    {
        public WORD bfType;
        public DWORD bfSize;
        public WORD bfReserved1;
        public WORD bfReserved2;
        public DWORD bfOffBits;
    }

    [StructLayout(LayoutKind.Sequential, Pack=1)]
    struct BITMAPINFOHEADER
    {
        //public DWORD      biSize;
        public LONG biWidth;
        public LONG biHeight;
        public WORD biPlanes;
        public WORD biBitCount;
        public DWORD biCompression;
        public DWORD biSizeImage;
        public LONG biXPelsPerMeter;
        public LONG biYPelsPerMeter;
        public DWORD biClrUsed;
        public DWORD biClrImportant;
        public DWORD bV5RedMask;
        public DWORD bV5GreenMask;
        public DWORD bV5BlueMask;
        public DWORD bV5AlphaMask;
    }

    /// <summary>
    /// サブメッシュ
    /// </summary>
    public class TSOSubMesh : IDisposable
    {
        /// <summary>
        /// シェーダ設定番号
        /// </summary>
        public int spec;
        /// <summary>
        /// ボーン参照配列
        /// </summary>
        public int[] bone_indices;
        /// <summary>
        /// ボーン参照リスト
        /// </summary>
        public List<TSONode> bones;
        /// <summary>
        /// 頂点配列
        /// </summary>
        public Vertex[] vertices;

        /// <summary>
        /// Direct3D頂点バッファ
        /// </summary>
        public VertexBuffer vb = null;

        /// <summary>
        /// パレット長さ
        /// </summary>
        public int maxPalettes;

        /// <summary>
        /// ボーン数
        /// </summary>
        public int NumberBones
        {
            get { return bone_indices.Length; }
        }

        /// <summary>
        /// 指定indexにあるボーンを得ます。
        /// </summary>
        /// <param name="i">index</param>
        /// <returns>ボーン</returns>
        public TSONode GetBone(int i)
        {
            return bones[i];
        }

        /// <summary>
        /// サブメッシュを読み込みます。
        /// </summary>
        public void Read(BinaryReader reader)
        {
            this.spec = reader.ReadInt32();
            int bone_indices_count = reader.ReadInt32(); //numbones
            this.maxPalettes = 16;
            if (this.maxPalettes > bone_indices_count)
                this.maxPalettes = bone_indices_count;

            this.bone_indices = new int[bone_indices_count];
            for (int i = 0; i < bone_indices_count; i++)
            {
                this.bone_indices[i] = reader.ReadInt32();
            }

            int vertices_count = reader.ReadInt32(); //numvertices
            this.vertices = new Vertex[vertices_count];
            for (int i = 0; i < vertices_count; i++)
            {
                Vertex v = new Vertex();
                v.Read(reader);
                this.vertices[i] = v;
            }
        }

        /// <summary>
        /// サブメッシュを書き出します。
        /// </summary>
        public void Write(BinaryWriter bw)
        {
            bw.Write(this.spec);

            bw.Write(this.bone_indices.Length);
            foreach (int bone_index in this.bone_indices)
                bw.Write(bone_index);

            bw.Write(this.vertices.Length);
            for (int i = 0; i < this.vertices.Length; i++)
            {
                this.vertices[i].Write(bw);
            }
        }

        /// <summary>
        /// 指定ボーン参照を追加します。
        /// 注意：this.bonesは更新しません。
        /// </summary>
        /// <param name="bone_index">ボーン参照</param>
        /// <returns>ボーン参照配列の添字</returns>
        public int AddBoneIndex(int bone_index)
        {
            if (bone_indices.Length >= 16)
                return -1;

            Array.Resize(ref bone_indices, bone_indices.Length + 1);
            maxPalettes++;
            
            int end = bone_indices.Length - 1;
            bone_indices[end] = bone_index;
            
            return end;
        }

        /// <summary>
        /// 指定nodeを追加します。
        /// </summary>
        /// <param name="node">node</param>
        /// <returns>ボーン参照配列の添字</returns>
        public int AddBone(TSONode node)
        {
            int end = AddBoneIndex(node.Id);
            if (end != -1)
                bones.Add(node);
            return end;
        }

        /// <summary>
        /// ボーン参照リストを生成します。
        /// </summary>
        public void LinkBones(TSONode[] nodes)
        {
            this.bones = new List<TSONode>();

            foreach (int bone_index in bone_indices)
                this.bones.Add(nodes[bone_index]);
        }

        /// 頂点数
        public int VerticesCount
        {
            get { return vertices.Length; }
        }

        /// <summary>
        /// 頂点をDirect3Dバッファに書き込みます。
        /// </summary>
        public void WriteBuffer()
        {
            if (vb != null)
                WriteBuffer(vb.Device);
        }

        /// <summary>
        /// 頂点をDirect3Dバッファに書き込みます。
        /// </summary>
        /// <param name="device">device</param>
        public void WriteBuffer(Device device)
        {
            if (vb != null)
                vb.Dispose();
            vb = new VertexBuffer(typeof(VertexFormat), vertices.Length, device, Usage.Dynamic | Usage.WriteOnly, VertexFormats.None, Pool.Default);
            vb.Created += new EventHandler(vb_Created);
            vb_Created(vb, null);
        }

        void vb_Created(object sender, EventArgs e)
        {
            VertexBuffer vb = (VertexBuffer)sender;

            //
            // rewrite vertex buffer
            //
            {
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
            }

        }

        /// <summary>
        /// Direct3Dバッファを破棄します。
        /// </summary>
        public void Dispose()
        {
            if (vb != null)
                vb.Dispose();
        }
    }

    /// <summary>
    /// メッシュ
    /// </summary>
    public class TSOMesh : IDisposable
    {
        string name;
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get { return name; } set { name = value; } }

        /// <summary>
        /// 変形行列
        /// </summary>
        public Matrix transform_matrix;
        /// <summary>
        /// unknown1
        /// </summary>
        public UInt32 unknown1;
        /// <summary>
        /// サブメッシュ配列
        /// </summary>
        public TSOSubMesh[] sub_meshes;

        /// <summary>
        /// メッシュを読み込みます。
        /// </summary>
        public void Read(BinaryReader reader)
        {
            this.name = reader.ReadCString().Replace(":", "_colon_").Replace("#", "_sharp_"); //should be compatible with directx naming conventions 
            reader.ReadMatrix(ref this.transform_matrix);
            this.unknown1 = reader.ReadUInt32();
            UInt32 mesh_count = reader.ReadUInt32();
            this.sub_meshes = new TSOSubMesh[mesh_count];
            for (int i = 0; i < mesh_count; i++)
            {
                TSOSubMesh sub_mesh = new TSOSubMesh();
                sub_mesh.Read(reader);
                this.sub_meshes[i] = sub_mesh;
            }
        }

        /// <summary>
        /// メッシュを書き出します。
        /// </summary>
        public void Write(BinaryWriter bw)
        {
            bw.WriteCString(this.name);
            Matrix m = this.transform_matrix;
            bw.Write(ref m);
            bw.Write(this.unknown1);
            bw.Write(this.sub_meshes.Length);

            foreach (TSOSubMesh sub_mesh in this.sub_meshes)
                sub_mesh.Write(bw);
        }

        /// <summary>
        /// ボーン参照リストを生成します。
        /// </summary>
        public void LinkBones(TSONode[] nodes)
        {
            foreach (TSOSubMesh sub_mesh in sub_meshes)
                sub_mesh.LinkBones(nodes);
        }

        /// <summary>
        /// 内部objectを破棄します。
        /// </summary>
        public void Dispose()
        {
            if (sub_meshes != null)
            foreach (TSOSubMesh sub_mesh in sub_meshes)
                sub_mesh.Dispose();
        }
    }

    /// 頂点構造体
    public struct VertexFormat
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

    /// <summary>
    /// 頂点
    /// </summary>
    public class Vertex
    {
        /// <summary>
        /// 位置
        /// </summary>
        public Vector3 position;
        /// <summary>
        /// 法線
        /// </summary>
        public Vector3 normal;
        /// <summary>
        /// テクスチャU座標
        /// </summary>
        public Single u;
        /// <summary>
        /// テクスチャV座標
        /// </summary>
        public Single v;
        /// <summary>
        /// スキンウェイト配列
        /// </summary>
        public SkinWeight[] skin_weights;
        /// <summary>
        /// ボーンインデックス
        /// </summary>
        public uint bone_indices;

        /// 選択中であるか
        public bool selected = false;

        /// <summary>
        /// 頂点を読みとります。
        /// </summary>
        public void Read(BinaryReader reader)
        {
            reader.ReadVector3(ref this.position);
            reader.ReadVector3(ref this.normal);
            this.u = reader.ReadSingle();
            this.v = reader.ReadSingle();
            int skin_weights_count = reader.ReadInt32();
            this.skin_weights = new SkinWeight[skin_weights_count];
            for (int i = 0; i < skin_weights_count; i++)
            {
                int bone_index = reader.ReadInt32();
                float weight = reader.ReadSingle();
                this.skin_weights[i] = new SkinWeight(bone_index, weight);
            }

            FillSkinWeights();
            GenerateBoneIndices();
        }

        /// <summary>
        /// スキンウェイト配列を充填します。
        /// </summary>
        public void FillSkinWeights()
        {
            Array.Sort(this.skin_weights);
            int len = skin_weights.Length;
            Array.Resize(ref this.skin_weights, 4);
            for (int i = len; i < 4; i++)
                this.skin_weights[i] = new SkinWeight(0, 0.0f);
        }

        /// <summary>
        /// ボーンインデックスを生成します。
        /// </summary>
        public void GenerateBoneIndices()
        {
            byte[] idx = new byte[4];
            for (int i = 0; i < 4; i++)
                idx[i] = (byte)this.skin_weights[i].bone_index;

            this.bone_indices = BitConverter.ToUInt32(idx, 0);
        }

        /// <summary>
        /// 頂点を書き出します。
        /// </summary>
        public void Write(BinaryWriter bw)
        {
            bw.Write(ref this.position);
            bw.Write(ref this.normal);
            bw.Write(this.u);
            bw.Write(this.v);

            int skin_weights_count = 0;
            SkinWeight[] skin_weights = new SkinWeight[4];
            foreach (SkinWeight skin_weight in this.skin_weights)
            {
                if (skin_weight.weight == 0.0f)
                    continue;

                skin_weights[skin_weights_count++] = skin_weight;
            }
            bw.Write(skin_weights_count);

            for (int i = 0; i < skin_weights_count; i++)
            {
                bw.Write(skin_weights[i].bone_index);
                bw.Write(skin_weights[i].weight);
            }
        }

        /// <summary>
        /// スキン変形後の位置を得ます。
        /// </summary>
        /// <param name="bone_matrices">スキン変形行列の配列</param>
        /// <returns>スキン変形後の位置</returns>
        public Vector3 CalcSkindeformPosition(Matrix[] bone_matrices)
        {
            Vector3 pos = Vector3.Empty;
            for (int i = 0; i < 4; i++)
            {
                Matrix m = bone_matrices[skin_weights[i].bone_index];
                float w = skin_weights[i].weight;
                pos += Vector3.TransformCoordinate(position, m) * w;
            }
            return pos;
        }
    }

    /// <summary>
    /// スキンウェイト
    /// </summary>
    public class SkinWeight : IComparable
    {
        /// <summary>
        /// ボーンインデックス
        /// </summary>
        public int bone_index;

        /// <summary>
        /// ウェイト
        /// </summary>
        public float weight;

        /// <summary>
        /// スキンウェイトを生成します。
        /// </summary>
        /// <param name="bone_index">ボーンインデックス</param>
        /// <param name="weight">ウェイト</param>
        public SkinWeight(int bone_index, float weight)
        {
            this.bone_index = bone_index;
            this.weight = weight;
        }

        /// <summary>
        /// 比較関数
        /// </summary>
        /// <param name="obj">比較対象スキンウェイト</param>
        /// <returns>比較結果</returns>
        public int CompareTo(object obj)
        {
            return -weight.CompareTo(((SkinWeight)obj).weight);
        }
    }

    /// <summary>
    /// スクリプト
    /// </summary>
    public class TSOScript
    {
        /// <summary>
        /// 名称
        /// </summary>
        internal string name;
        /// <summary>
        /// テキスト行配列
        /// </summary>
        public string[] lines;

        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get { return name; } set { name = value; } }

        /// <summary>
        /// スクリプトを読み込みます。
        /// </summary>
        public void Load(string source_file)
        {
            this.lines = File.ReadAllLines(source_file);
        }

        /// <summary>
        /// スクリプトを読み込みます。
        /// </summary>
        public void Read(BinaryReader reader)
        {
            this.name = reader.ReadCString();
            UInt32 line_count = reader.ReadUInt32();
            this.lines = new string[line_count];
            for (int i = 0; i < line_count; i++)
            {
                lines[i] = reader.ReadCString();
            }
        }

        /// <summary>
        /// スクリプトを書き出します。
        /// </summary>
        public void Save(string dest_file)
        {
            File.WriteAllLines(dest_file, this.lines);
        }

        /// <summary>
        /// スクリプトを書き出します。
        /// </summary>
        public void Write(BinaryWriter bw)
        {
            bw.WriteCString(this.name);
            bw.Write(this.lines.Length);

            foreach (string line in this.lines)
                bw.WriteCString(line);
        }
    }

    /// <summary>
    /// サブスクリプト
    /// </summary>
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class TSOSubScript
    {
        internal string name;
        internal string file;
        /// <summary>
        /// テキスト行配列
        /// </summary>
        public string[] lines;
        /// <summary>
        /// シェーダ設定
        /// </summary>
        public Shader shader = null;

        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get { return name; } set { name = value; } }
        /// <summary>
        /// ファイル名
        /// </summary>
        public string FileName { get { return file; } set { file = value; } }

        /// <summary>
        /// サブスクリプトを読み込みます。
        /// </summary>
        public void Load(string source_file)
        {
            this.lines = File.ReadAllLines(source_file);
        }

        /// <summary>
        /// サブスクリプトを読み込みます。
        /// </summary>
        public void Read(BinaryReader reader)
        {
            this.name = reader.ReadCString();
            this.file = reader.ReadCString();
            UInt32 line_count = reader.ReadUInt32();
            this.lines = new string[line_count];
            for (int i = 0; i < line_count; i++)
            {
                this.lines[i] = reader.ReadCString();
            }

            //Console.WriteLine("name {0} file {1}", this.name, this.file);
        }

        /// <summary>
        /// サブスクリプトを書き出します。
        /// </summary>
        public void Save(string dest_file)
        {
            File.WriteAllLines(dest_file, this.lines);
        }

        /// <summary>
        /// サブスクリプトを書き出します。
        /// </summary>
        public void Write(BinaryWriter bw)
        {
            bw.WriteCString(this.name);
            bw.WriteCString(this.file);
            bw.Write(this.lines.Length);

            foreach (string line in this.lines)
                bw.WriteCString(line);
        }

        /// <summary>
        /// シェーダ設定を生成します。
        /// </summary>
        public void GenerateShader()
        {
            this.shader = new Shader();
            this.shader.Load(this.lines);
        }

        /// <summary>
        /// シェーダ設定を保存します。
        /// </summary>
        public void SaveShader()
        {
            this.lines = this.shader.GetLines();
        }
    }

    /// <summary>
    /// テクスチャ
    /// </summary>
    public class TSOTexture : IDisposable
    {
        /// <summary>
        /// 名称
        /// </summary>
        internal string name;
        /// <summary>
        /// ファイル名
        /// </summary>
        internal string file;
        /// <summary>
        /// 幅
        /// </summary>
        public int width;
        /// <summary>
        /// 高さ
        /// </summary>
        public int height;
        /// <summary>
        /// 色深度
        /// </summary>
        public int depth;
        /// <summary>
        /// ビットマップ配列
        /// </summary>
        public byte[] data;

        internal Texture d3d_tex = null;

        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get { return name; } set { name = value; } }
        /// <summary>
        /// ファイル名
        /// </summary>
        public string FileName { get { return file; } set { file = value; } }

        /// <summary>
        /// テクスチャを読み込みます。
        /// </summary>
        public void Load(string source_file)
        {
            string ext = Path.GetExtension(source_file).ToLower();
            using (BinaryReader br = new BinaryReader(File.OpenRead(source_file)))
            {
                if (ext == ".tga")
                    LoadTGA(br);
                else
                if (ext == ".bmp")
                    LoadBMP(br);
            }
            this.file = "\"" + Path.GetFileName(source_file) + "\"";
        }

        static readonly int sizeof_tga_header = Marshal.SizeOf(typeof(TARGA_HEADER));
        static readonly int sizeof_bfh = Marshal.SizeOf(typeof(BITMAPFILEHEADER));

        /// <summary>
        /// TGA形式のテクスチャを読み込みます。
        /// </summary>
        public void LoadTGA(BinaryReader br)
        {
            TARGA_HEADER header;

            byte[] header_buf = br.ReadBytes(sizeof_tga_header);
            GCHandle header_handle = GCHandle.Alloc(header_buf, GCHandleType.Pinned);
            header = (TARGA_HEADER)Marshal.PtrToStructure(header_handle.AddrOfPinnedObject(), typeof(TARGA_HEADER));
            header_handle.Free();

            if (header.imagetype != 0x02)
                throw new Exception("Invalid imagetype: " + file);
            if (header.depth != 24 && header.depth != 32)
                throw new Exception("Invalid depth: " + file);

            this.width = header.width;
            this.height = header.height;
            this.depth = header.depth / 8;
            this.data = br.ReadBytes( this.width * this.height * this.depth );
        }

        /// <summary>
        /// BMP形式のテクスチャを読み込みます。
        /// </summary>
        public void LoadBMP(BinaryReader br)
        {
            BITMAPFILEHEADER bfh;
            BITMAPINFOHEADER bih;

            byte[] buf;

            bfh.bfType = br.ReadUInt16();

            if (bfh.bfType != 0x4D42)
                throw new Exception("Invalid imagetype: " + file);

            buf = br.ReadBytes(12);
            bfh.bfSize = BitConverter.ToUInt32(buf, 0x00);
            bfh.bfReserved1 = BitConverter.ToUInt16(buf, 0x04);
            bfh.bfReserved2 = BitConverter.ToUInt16(buf, 0x06);
            bfh.bfOffBits = BitConverter.ToUInt16(buf, 0x08);

            uint biSize = br.ReadUInt32();
            //Console.WriteLine("biSize {0}", biSize);

            buf = br.ReadBytes((int)biSize - 4);
            bih.biWidth             = BitConverter.ToInt32(buf, 0x00);
            bih.biHeight            = BitConverter.ToInt32(buf, 0x04);
            bih.biPlanes            = BitConverter.ToUInt16(buf, 0x08);
            bih.biBitCount          = BitConverter.ToUInt16(buf, 0x0A);
            bih.biCompression       = BitConverter.ToUInt32(buf, 0x0C);
            bih.biSizeImage         = BitConverter.ToUInt32(buf, 0x10);
            bih.biXPelsPerMeter     = BitConverter.ToInt32(buf, 0x14);
            bih.biYPelsPerMeter     = BitConverter.ToInt32(buf, 0x18);
            bih.biClrUsed           = BitConverter.ToUInt32(buf, 0x1C);
            bih.biClrImportant      = BitConverter.ToUInt32(buf, 0x20);
            bih.bV5RedMask      = 0x00ff0000;
            bih.bV5GreenMask    = 0x0000ff00;
            bih.bV5BlueMask     = 0x000000ff;
            bih.bV5AlphaMask    = 0;
            if (biSize >= 56)
            {
                bih.bV5RedMask      = BitConverter.ToUInt32(buf, 0x24);
                bih.bV5GreenMask    = BitConverter.ToUInt32(buf, 0x28);
                bih.bV5BlueMask     = BitConverter.ToUInt32(buf, 0x2C);
                bih.bV5AlphaMask    = BitConverter.ToUInt32(buf, 0x30);
            }

            if (bih.biBitCount != 24 && bih.biBitCount != 32)
                throw new Exception("Invalid depth: " + file);

            this.width = bih.biWidth;
            this.height = bih.biHeight;
            this.depth = bih.biBitCount / 8;
            buf = br.ReadBytes( this.width * this.height * this.depth );

            if (biSize >= 56)
            {
                /*
                 * change channels order RGBA to ARGB
                 */
                for (int i = 0; i < buf.Length; i += 4)
                {
                    byte A = buf[i + 0];
                    byte B = buf[i + 1];
                    byte G = buf[i + 2];
                    byte R = buf[i + 3];
                    buf[i + 0] = B;
                    buf[i + 1] = G;
                    buf[i + 2] = R;
                    buf[i + 3] = A;
                }
            }
            this.data = buf;
        }

        /// <summary>
        /// テクスチャを読み込みます。
        /// </summary>
        public void Read(BinaryReader reader)
        {
            this.name = reader.ReadCString();
            this.file = reader.ReadCString();
            this.width = reader.ReadInt32();
            this.height = reader.ReadInt32();
            this.depth = reader.ReadInt32();
            byte[] buf = reader.ReadBytes( this.width * this.height * this.depth );

            for(int j = 0; j < buf.Length; j += 4)
            {
                byte tmp = buf[j + 2];
                buf[j + 2] = buf[j + 0];
                buf[j + 0] = tmp;
            }
            this.data = buf;
        }

        /// <summary>
        /// TGA形式のテクスチャを書き出します。
        /// </summary>
        public void SaveTGA(BinaryWriter bw)
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
            header.type = 0;

            IntPtr header_ptr = Marshal.AllocHGlobal(sizeof_tga_header);
            Marshal.StructureToPtr(header, header_ptr, false);
            byte[] header_buf = new byte[sizeof_tga_header];
            Marshal.Copy(header_ptr, header_buf, 0, sizeof_tga_header);
            Marshal.FreeHGlobal(header_ptr);
            bw.Write(header_buf);

            bw.Write(data, 0, data.Length);
        }

        /// <summary>
        /// BMP形式のテクスチャを書き出します。
        /// </summary>
        public void SaveBMP(BinaryWriter bw)
        {
            /*
             * write bmp v3 header
             */
            bw.Write((byte)'B');
            bw.Write((byte)'M');
            bw.Write((uint)(14 + 40 + data.Length)); // bfSize
            bw.Write((ushort)0);
            bw.Write((ushort)0);
            bw.Write((uint)(14 + 40)); // bfOffBits
            bw.Write((uint)40); // biSize
            bw.Write(width);
            bw.Write(height);
            bw.Write((ushort)1); // biPlanes
            bw.Write((ushort)(depth * 8)); // biBitCount
            bw.Write((uint)0); // biCompression
            bw.Write((uint)data.Length); // biSizeImage
            bw.Write((int)0);
            bw.Write((int)0);
            bw.Write((uint)0);
            bw.Write((uint)0);

            bw.Write(data, 0, data.Length);
        }

        /// <summary>
        /// テクスチャを書き出します。
        /// </summary>
        public void Write(BinaryWriter bw)
        {
            bw.WriteCString(this.name);
            bw.WriteCString(this.file);
            bw.Write(this.width);
            bw.Write(this.height);
            bw.Write(this.depth);

            byte[] buf = new byte[this.data.Length];
            Array.Copy(this.data, 0, buf, 0, buf.Length);

            for(int j = 0; j < buf.Length; j += 4)
            {
                byte tmp = buf[j + 2];
                buf[j + 2] = buf[j + 0];
                buf[j + 0] = tmp;
            }
            bw.Write(buf);
        }

        /// <summary>
        /// 指定deviceで開きます。
        /// </summary>
        /// <param name="device">device</param>
        public void Open(Device device)
        {
            /*
            string dest_file = file.Trim('"');
            if (dest_file == "")
                return;
            string ext = Path.GetExtension(dest_file).ToLower();
            */
            if (data.Length == 0)
                return;
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
                header.type = 0;

                IntPtr header_ptr = Marshal.AllocHGlobal(sizeof_tga_header);
                Marshal.StructureToPtr(header, header_ptr, false);
                byte[] header_buf = new byte[sizeof_tga_header];
                Marshal.Copy(header_ptr, header_buf, 0, sizeof_tga_header);
                Marshal.FreeHGlobal(header_ptr);
                bw.Write(header_buf);

                int stride = width * depth;
                int offset = width * height * depth - stride;
                for (int y = 0; y < height; y++)
                {
                    bw.Write(data, offset, stride);
                    offset -= stride;
                }
                bw.Flush();
                ms.Seek(0, SeekOrigin.Begin);
                d3d_tex = TextureLoader.FromStream(device, ms);
            }
        }

        /// <summary>
        /// Direct3Dテクスチャを破棄します。
        /// </summary>
        public void Dispose()
        {
            if (d3d_tex != null)
                d3d_tex.Dispose();
        }
    }

    /// <summary>
    /// node (bone)
    /// </summary>
    public class TSONode
    {
        private int id;
        private string path;
        private string name;

        private Vector3 scaling;
        private Quaternion rotation;
        private Vector3 translation;

        private Matrix transformation_matrix;
        private bool need_update_transformation;

        /// <summary>
        /// TSONodeを生成します。
        /// </summary>
        public TSONode(int id)
        {
            this.id = id;
        }

        /// <summary>
        /// TSONodeを読み込みます。
        /// </summary>
        public void Read(BinaryReader reader)
        {
            this.Path = reader.ReadCString();
        }

        /// <summary>
        /// TSONodeを書き出します。
        /// </summary>
        public void Write(BinaryWriter bw)
        {
            bw.WriteCString(this.Path);
        }

        /// <summary>
        /// 拡大変位
        /// </summary>
        public Vector3 Scaling
        {
            get { return scaling; }
            set
            {
                scaling = value;
                need_update_transformation = true;
            }
        }

        /// <summary>
        /// 回転変位
        /// </summary>
        public Quaternion Rotation
        {
            get {
                return rotation;
            }
            set {
                rotation = value;
                need_update_transformation = true;
            }
        }

        /// <summary>
        /// 位置変位
        /// </summary>
        public Vector3 Translation
        {
            get {
                return translation;
            }
            set {
                translation = value;
                need_update_transformation = true;
            }
        }

        /// <summary>
        /// 子nodeリスト
        /// </summary>
        public List<TSONode> children = new List<TSONode>();

        /// <summary>
        /// 親node
        /// </summary>
        public TSONode parent;

        /// <summary>
        /// 姿勢行列。これはboneローカル座標系をワールド座標系に変換します。
        /// </summary>
        public Matrix posing_matrix;

        /// <summary>
        /// オフセット行列。これはワールド座標系をboneローカル座標系に変換します。
        /// </summary>
        public Matrix offset_matrix;

        /// <summary>
        /// Id
        /// </summary>
        public int Id { get { return id; } }
        
        /// <summary>
        /// 名称
        /// </summary>
        public string Path
        {
            get { return path; }
            set
            {
                path = value;
                name = path.Substring(path.LastIndexOf('|') + 1);
            }
        }

        /// <summary>
        /// 名称の短い形式。これはTSOFile中で重複する可能性があります。
        /// </summary>
        public string Name { get { return name; } }

        /// <summary>
        /// ワールド座標系での位置を得ます。
        /// </summary>
        /// <returns></returns>
        public Vector3 GetWorldPosition()
        {
            TSONode node = this;
            Vector3 v = Vector3.Empty;
            while (node != null)
            {
                v = Vector3.TransformCoordinate(v, node.TransformationMatrix);
                node = node.parent;
            }
            return v;
        }

        /// <summary>
        /// ワールド座標系での位置と向きを得ます。
        /// </summary>
        /// <returns></returns>
        public Matrix GetWorldCoordinate()
        {
            TSONode node = this;
            Matrix m = Matrix.Identity;
            while (node != null)
            {
                m *= node.TransformationMatrix;
                node = node.parent;
            }
            return m;
        }

        /// <summary>
        /// 拡大行列
        /// </summary>
        public Matrix ScalingMatrix
        {
            get
            {
                return Matrix.Scaling(scaling);
            }
        }

        /// <summary>
        /// 回転行列
        /// </summary>
        public Matrix RotationMatrix
        {
            get {
                return Matrix.RotationQuaternion(rotation);
            }
        }

        /// <summary>
        /// 位置行列
        /// </summary>
        public Matrix TranslationMatrix
        {
            get {
                return Matrix.Translation(translation);
            }
        }

        /// <summary>
        /// 変形行列。これは 拡大行列 x 回転行列 x 位置行列 です。
        /// </summary>
        public Matrix TransformationMatrix
        {
            get {
                if (need_update_transformation)
                {
                    Matrix m = RotationMatrix;
                    Helper.Scale1(ref m, scaling);
                    m.M41 = translation.X;
                    m.M42 = translation.Y;
                    m.M43 = translation.Z;
                    transformation_matrix = m;
                    need_update_transformation = false;
                }
                return transformation_matrix;
            }
            set {
                transformation_matrix = value;
                translation = Helper.DecomposeMatrix(ref value, out scaling);
                rotation = Quaternion.RotationMatrix(value);
            }
        }
    }

    /// <summary>
    /// TSOファイルを扱います。
    /// </summary>
    public class TSOFile : IDisposable, IComparable
    {
        /// <summary>
        /// bone配列
        /// </summary>
        public TSONode[] nodes;
        /// <summary>
        /// テクスチャ配列
        /// </summary>
        public TSOTexture[] textures;
        /// <summary>
        /// スクリプト配列
        /// </summary>
        public TSOScript[] scripts;
        /// <summary>
        /// サブスクリプト配列
        /// </summary>
        public TSOSubScript[] sub_scripts;
        /// <summary>
        /// メッシュ配列
        /// </summary>
        public TSOMesh[] meshes;

        /// <summary>
        /// 名称の短い形式とboneを関連付ける辞書
        /// </summary>
        public Dictionary<string, TSONode> nodemap;

        /// shader設定上の texture name と d3d texture を関連付ける辞書
        Dictionary<string, Texture> d3d_texturemap;

        public Texture GetDirect3dTextureByName(string name)
        {
            Texture d3d_tex;
            if (name != null && d3d_texturemap.TryGetValue(name, out d3d_tex))
                return d3d_tex;
            else
                return null;
        }

        /// <summary>
        /// 指定パスに保存します。
        /// </summary>
        /// <param name="dest_file">パス</param>
        public void Save(string dest_file)
        {
            using (Stream dest_stream = File.Create(dest_file))
                Save(dest_stream);
        }

        /// <summary>
        /// 指定ストリームに保存します。
        /// </summary>
        /// <param name="dest_stream">ストリーム</param>
        public void Save(Stream dest_stream)
        {
            BinaryWriter bw = new BinaryWriter(dest_stream);

            WriteMagic(bw);

            bw.Write(nodes.Length);
            foreach (TSONode node in nodes)
                node.Write(bw);

            bw.Write(nodes.Length);
            Matrix m = Matrix.Identity;
            foreach (TSONode node in nodes)
            {
                m = node.TransformationMatrix;
                bw.Write(ref m);
            }

            bw.Write(textures.Length);
            foreach (TSOTexture tex in textures)
                tex.Write(bw);

            bw.Write(scripts.Length);
            foreach (TSOScript script in scripts)
                script.Write(bw);

            bw.Write(sub_scripts.Length);
            foreach (TSOSubScript sub_script in sub_scripts)
                sub_script.Write(bw);

            bw.Write(meshes.Length);
            foreach (TSOMesh mesh in meshes)
                mesh.Write(bw);
        }

        /// <summary>
        /// 'TSO1' を書き出します。
        /// </summary>
        public static void WriteMagic(BinaryWriter bw)
        {
            bw.Write(0x314F5354);
        }

        /// <summary>
        /// 指定パスから読み込みます。
        /// </summary>
        /// <param name="source_file">パス</param>
        public void Load(string source_file)
        {
            using (Stream source_stream = File.OpenRead(source_file))
                Load(source_stream);
        }

        void ComputePosingMatrix(TSONode node, ref Matrix m)
        {
            node.posing_matrix = node.TransformationMatrix * m;

            foreach (TSONode child in node.children)
                ComputePosingMatrix(child, ref node.posing_matrix);
        }

        /// <summary>
        /// 姿勢行列を計算します。
        /// </summary>
        public void ComputePosingMatrices()
        {
            Matrix m = Matrix.Identity;
            ComputePosingMatrix(this.nodes[0], ref m);
        }

        /// <summary>
        /// オフセット行列を計算します。
        /// </summary>
        public void ComputeOffsetMatrices()
        {
            for (int i = 0, len = this.nodes.Length; i < len; i++)
                nodes[i].offset_matrix = Matrix.Invert(nodes[i].posing_matrix);
        }

        /// <summary>
        /// 指定ストリームから読み込みます。
        /// </summary>
        /// <param name="source_stream">ストリーム</param>
        public void Load(Stream source_stream)
        {
            BinaryReader reader = new BinaryReader(source_stream, System.Text.Encoding.Default);

            byte[] magic = reader.ReadBytes(4);

            if (magic[0] != (byte)'T' || magic[1] != (byte)'S' || magic[2] != (byte)'O' || magic[3] != (byte)'1')
                throw new Exception("File is not TSO");

            int node_count = reader.ReadInt32();
            nodes = new TSONode[node_count];
            for (int i = 0; i < node_count; i++)
            {
                nodes[i] = new TSONode(i);
                nodes[i].Read(reader);
            }

            GenerateNodemapAndTree();

            int node_matrix_count = reader.ReadInt32();
            Matrix m = Matrix.Identity;
            for (int i = 0; i < node_matrix_count; i++)
            {
                reader.ReadMatrix(ref m);
                nodes[i].TransformationMatrix = m;
            }
            ComputePosingMatrices();
            ComputeOffsetMatrices();

            UInt32 texture_count = reader.ReadUInt32();
            textures = new TSOTexture[texture_count];
            for (int i = 0; i < texture_count; i++)
            {
                textures[i] = new TSOTexture();
                textures[i].Read(reader);
            }

            UInt32 script_count = reader.ReadUInt32();
            scripts = new TSOScript[script_count];
            for (int i = 0; i < script_count; i++)
            {
                scripts[i] = new TSOScript();
                scripts[i].Read(reader);
            }

            UInt32 sub_script_count = reader.ReadUInt32();
            sub_scripts = new TSOSubScript[sub_script_count];
            for (int i = 0; i < sub_script_count; i++)
            {
                sub_scripts[i] = new TSOSubScript();
                sub_scripts[i].Read(reader);
                sub_scripts[i].GenerateShader();
            }

            UInt32 mesh_count = reader.ReadUInt32();
            meshes = new TSOMesh[mesh_count];
            for (int i = 0; i < mesh_count; i++)
            {
                meshes[i] = new TSOMesh();
                meshes[i].Read(reader);
                meshes[i].LinkBones(nodes);

                //Console.WriteLine("mesh name {0} len {1}", mesh.name, mesh.sub_meshes.Length);
            }
        }

        internal void GenerateNodemapAndTree()
        {
            nodemap = new Dictionary<string, TSONode>();

            for (int i = 0; i < nodes.Length; i++)
            {
                nodemap.Add(nodes[i].Name, nodes[i]);
            }

            for (int i = 0; i < nodes.Length; i++)
            {
                int idx = nodes[i].Path.LastIndexOf('|');
                if (idx <= 0)
                    continue;
                string parent_path = nodes[i].Path.Substring(0, idx);
                string parent_name = parent_path.Substring(parent_path.LastIndexOf('|') + 1);
                nodes[i].parent = nodemap[parent_name];
                nodes[i].parent.children.Add(nodes[i]);
            }
        }

        /// <summary>
        /// 指定名称の短い形式を持つnodeを検索します。
        /// </summary>
        /// <param name="name">名称の短い形式</param>
        /// <returns></returns>
        public TSONode FindNodeByName(string name)
        {
            TSONode node;
            if (nodemap.TryGetValue(name, out node))
                return node;
            else
                return null;
        }

        internal void GenerateDirect3dTexturemap()
        {
            d3d_texturemap = new Dictionary<string, Texture>();

            foreach (TSOTexture tex in textures)
                d3d_texturemap.Add(tex.name, tex.d3d_tex);
        }

        /// <summary>
        /// 指定device上で開きます。
        /// </summary>
        /// <param name="device">device</param>
        public void Open(Device device)
        {
            foreach (TSOMesh mesh in meshes)
            foreach (TSOSubMesh sub_mesh in mesh.sub_meshes)
                sub_mesh.WriteBuffer(device);

            foreach (TSOTexture tex in textures)
            {
                tex.Open(device);
            }
            GenerateDirect3dTexturemap();
        }

        /// <summary>
        /// 内部objectを破棄します。
        /// </summary>
        public void Dispose()
        {
            Debug.WriteLine("TSOFile.Dispose");

            d3d_texturemap.Clear();

            foreach (TSOMesh mesh in meshes)
                mesh.Dispose();
            foreach (TSOTexture tex in textures)
                tex.Dispose();
        }

        /// ファイル名
        public string FileName;

        /// 出現部位
        public byte Row;

        public int CompareTo(object obj)
        {
            return Row.CompareTo(((TSOFile)obj).Row);
        }

        bool hidden;
        /// 隠すか
        public bool Hidden { get { return hidden; } }

        /// 隠すかを切り替えます。
        public void SwitchHidden()
        {
            hidden = ! hidden;
        }
    }
}
