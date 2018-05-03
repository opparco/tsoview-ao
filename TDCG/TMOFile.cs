using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using TDCG.Extensions;

namespace TDCG
{
    /// <summary>
    /// TMOファイルを扱います。
    /// </summary>
    public class TMOFile
    {
        /// <summary>
        /// バイナリ値として読み取ります。
        /// </summary>
        protected BinaryReader reader;

        /// <summary>
        /// ヘッダ
        /// </summary>
        public byte[] header;
        /// <summary>
        /// オプション値0
        /// </summary>
        public int opt0;
        /// <summary>
        /// オプション値1
        /// </summary>
        public int opt1;
        /// <summary>
        /// bone配列
        /// </summary>
        public TMONode[] nodes;
        /// <summary>
        /// フッタ
        /// </summary>
        public byte[] footer;

        /// <summary>
        /// bone名称とboneを関連付ける辞書
        /// </summary>
        public Dictionary<string, TMONode> nodemap;

        internal TMONode w_hips_node = null;
        internal List<TMONode> root_nodes_except_w_hips;

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
            bw.Write(header);
            bw.Write(opt0);
            bw.Write(opt1);

            bw.Write(nodes.Length);
            foreach (TMONode node in nodes)
                node.Write(bw);

            bw.Write(1); // frames length
            {
                //  should be nodes.length == matrices.length
                bw.Write(nodes.Length);
                foreach (TMONode node in nodes)
                {
                    Matrix m = node.TransformationMatrix;
                    bw.Write(ref m);
                }
            }

            bw.Write((int)0); // footer
        }

        /// <summary>
        /// 'TMO1' を書き出します。
        /// </summary>
        public static void WriteMagic(BinaryWriter bw)
        {
            bw.Write(0x314F4D54);
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

        /// <summary>
        /// 指定ストリームから読み込みます。
        /// </summary>
        /// <param name="source_stream">ストリーム</param>
        public void Load(Stream source_stream)
        {
            this.reader = new BinaryReader(source_stream, System.Text.Encoding.Default);

            byte[] magic = reader.ReadBytes(4);

            if (magic[0] != (byte)'T' || magic[1] != (byte)'M' || magic[2] != (byte)'O' || magic[3] != (byte)'1')
                throw new Exception("File is not TMO");

            this.header = reader.ReadBytes(8);
            this.opt0 = reader.ReadInt32();
            this.opt1 = reader.ReadInt32();

            int nodes_len = reader.ReadInt32();
            nodes = new TMONode[nodes_len];
            for (int i = 0; i < nodes_len; i++)
            {
                nodes[i] = new TMONode(i);
                nodes[i].Read(reader);
            }

            GenerateNodemapAndTree();

            int frames_len = reader.ReadInt32(); // assume to be 1

            {
                int matrices_len = reader.ReadInt32();
                // should be nodes.length == matrices.length
                for (int i = 0; i < matrices_len; i++)
                {
                    Matrix m = Matrix.Identity;
                    reader.ReadMatrix(ref m);
                    nodes[i].TransformationMatrix = m;
                }
            }

            //tmo.footer = new byte[4] { 0, 0, 0, 0 };
        }

        internal void GenerateNodemapAndTree()
        {
            nodemap = new Dictionary<string, TMONode>();

            for (int i = 0; i < nodes.Length; i++)
            {
                nodemap.Add(nodes[i].Path, nodes[i]);
            }

            List<TMONode> root_nodes = new List<TMONode>();

            for (int i = 0; i < nodes.Length; i++)
            {
                int index = nodes[i].Path.LastIndexOf('|');
                if (index == 0)
                    root_nodes.Add(nodes[i]);
                if (index <= 0)
                    continue;
                string path = nodes[i].Path.Substring(0, index);
                nodes[i].parent = nodemap[path];
                nodes[i].parent.children.Add(nodes[i]);
            }

            root_nodes_except_w_hips = new List<TMONode>();

            foreach (TMONode node in root_nodes)
            {
                if (node.Path == "|W_Hips")
                    w_hips_node = node;
                else
                    root_nodes_except_w_hips.Add(node);
            }
        }

        /// <summary>
        /// 指定名称（短い形式）を持つnodeを検索します。
        /// </summary>
        /// <param name="name">node名称（短い形式）</param>
        /// <returns></returns>
        public TMONode FindNodeByName(string name)
        {
            foreach (TMONode node in nodes)
                if (node.Name == name)
                    return node;
            return null;
        }
    }

    /// <summary>
    /// boneを扱います。
    /// </summary>
    public class TMONode
    {
        int id;
        string path;
        string name;

        Vector3 scaling;
        Quaternion rotation;
        Vector3 translation;

        Matrix transformation_matrix;
        bool need_update_transformation;

        /// <summary>
        /// TMONodeを生成します。
        /// </summary>
        public TMONode(int id)
        {
            this.id = id;
        }

        /// <summary>
        /// TMONodeを読み込みます。
        /// </summary>
        public void Read(BinaryReader reader)
        {
            this.Path = reader.ReadCString();
        }

        /// <summary>
        /// TMONodeを書き出します。
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
            get { return rotation; }
            set
            {
                rotation = value;
                need_update_transformation = true;
            }
        }

        /// <summary>
        /// 位置変位
        /// </summary>
        public Vector3 Translation
        {
            get { return translation; }
            set
            {
                translation = value;
                need_update_transformation = true;
            }
        }

        /// <summary>
        /// 子nodeリスト
        /// </summary>
        public List<TMONode> children = new List<TMONode>();

        /// <summary>
        /// 親node
        /// </summary>
        public TMONode parent;

        /// <summary>
        /// ワールド座標系での位置と向きを表します。これはviewerから更新されます。
        /// </summary>
        public Matrix combined_matrix;

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
        /// 名称の短い形式。これはTMOFile中で重複する可能性があります。
        /// </summary>
        public string Name { get { return name; } }

        /// <summary>
        /// ワールド座標系での位置を得ます。
        /// </summary>
        /// <returns></returns>
        public Vector3 GetWorldPosition()
        {
            TMONode node = this;
            Vector3 v = Vector3.Empty;
            while (node != null)
            {
                v = Vector3.TransformCoordinate(v, node.TransformationMatrix);
                node = node.parent;
            }
            return v;
        }

        public Quaternion GetWorldRotation()
        {
            TMONode node = this;
            Quaternion q = Quaternion.Identity;
            while (node != null)
            {
                q.Multiply(node.Rotation);
                node = node.parent;
            }
            return q;
        }

        /// <summary>
        /// ワールド座標系での位置と向きを得ます。
        /// </summary>
        /// <returns></returns>
        public Matrix GetWorldCoordinate()
        {
            TMONode node = this;
            Matrix m = Matrix.Identity;
            while (node != null)
            {
                m.Multiply(node.TransformationMatrix);
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
            get
            {
                return Matrix.RotationQuaternion(rotation);
            }
        }

        /// <summary>
        /// 位置行列
        /// </summary>
        public Matrix TranslationMatrix
        {
            get
            {
                return Matrix.Translation(translation);
            }
        }

        static Vector3 Reciprocal(Vector3 v)
        {
            return new Vector3(1 / v.X, 1 / v.Y, 1 / v.Z);
        }

        static void ScalingLocal(ref Matrix m, Vector3 scaling)
        {
            m.M11 *= scaling.X;
            m.M21 *= scaling.X;
            m.M31 *= scaling.X;
            m.M41 *= scaling.X;

            m.M12 *= scaling.Y;
            m.M22 *= scaling.Y;
            m.M32 *= scaling.Y;
            m.M42 *= scaling.Y;

            m.M13 *= scaling.Z;
            m.M23 *= scaling.Z;
            m.M33 *= scaling.Z;
            m.M43 *= scaling.Z;
        }

        /// <summary>
        /// 変形行列。これは 拡大行列 x 回転行列 x 位置行列 です。
        /// </summary>
        public Matrix TransformationMatrix
        {
            get
            {
                if (need_update_transformation)
                {
                    Matrix m = RotationMatrix;
                    Helper.Scale1(ref m, scaling);
                    m.M41 = translation.X;
                    m.M42 = translation.Y;
                    m.M43 = translation.Z;
                    if (parent != null)
                        ScalingLocal(ref m, Reciprocal(parent.Scaling));
                    transformation_matrix = m;
                    need_update_transformation = false;
                }
                return transformation_matrix;
            }
            set
            {
                transformation_matrix = value;
                Matrix m = value;
                if (parent != null)
                    ScalingLocal(ref m, parent.Scaling);
                translation = Helper.DecomposeMatrix(ref m, out scaling);
                rotation = Quaternion.RotationMatrix(m);
            }
        }
    }
}
