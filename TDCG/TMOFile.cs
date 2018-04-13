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
    /// 変形行列を扱います。
    /// </summary>
    public class TMOMat
    {
        /// Direct3D Matrix
        public Matrix m;

        /// <summary>
        /// TMOMatを作成します。
        /// </summary>
        public TMOMat()
        {
        }

        /// <summary>
        /// 行列を読み込みます。
        /// </summary>
        public void Read(BinaryReader reader)
        {
            reader.ReadMatrix(ref this.m);
        }

        /// <summary>
        /// 行列を書き出します。
        /// </summary>
        public void Write(BinaryWriter bw)
        {
            bw.Write(ref this.m);
        }

        /// <summary>
        /// TMOMatを作成します。
        /// </summary>
        /// <param name="m">matrix</param>
        public TMOMat(ref Matrix m)
        {
            this.m = m;
        }

        static Vector3 Reciprocal(Vector3 v)
        {
            return new Vector3(1/v.X, 1/v.Y, 1/v.Z);
        }

        /// <summary>
        /// 指定行列で縮小します。位置は変更しません。
        /// </summary>
        /// <param name="scaling">scaling matrix</param>
        public void Scale0(Vector3 scaling)
        {
            Scale1(Reciprocal(scaling));
        }

        /// <summary>
        /// 指定行列で拡大します。位置は変更しません。
        /// </summary>
        /// <param name="scaling">scaling matrix</param>
        public void Scale1(Vector3 scaling)
        {
            Helper.Scale1(ref m, scaling);
        }

        /// <summary>
        /// 指定角度でX軸回転します。
        /// </summary>
        /// <param name="angle">角度（ラジアン）</param>
        public void RotateX(float angle)
        {
            if (angle == 0.0f)
                return;

            Vector3 v = new Vector3(m.M11, m.M12, m.M13);
            m *= Matrix.RotationAxis(v, angle);
        }

        /// <summary>
        /// 指定角度でY軸回転します。
        /// </summary>
        /// <param name="angle">角度（ラジアン）</param>
        public void RotateY(float angle)
        {
            if (angle == 0.0f)
                return;

            Vector3 v = new Vector3(m.M21, m.M22, m.M23);
            m *= Matrix.RotationAxis(v, angle);
        }

        /// <summary>
        /// 指定角度でZ軸回転します。
        /// </summary>
        /// <param name="angle">角度（ラジアン）</param>
        public void RotateZ(float angle)
        {
            if (angle == 0.0f)
                return;

            Vector3 v = new Vector3(m.M31, m.M32, m.M33);
            m *= Matrix.RotationAxis(v, angle);
        }

        /// <summary>
        /// 指定変位だけ移動します。
        /// </summary>
        /// <param name="translation">変位</param>
        public void Move(Vector3 translation)
        {
            m.M41 += translation.X;
            m.M42 += translation.Y;
            m.M43 += translation.Z;
        }

        /// <summary>
        /// 補間を行います。
        /// </summary>
        /// <param name="mat0">行列0</param>
        /// <param name="mat1">行列1</param>
        /// <param name="mat2">行列2</param>
        /// <param name="mat3">行列3</param>
        /// <param name="length">分割数</param>
        /// <returns>分割数だけTMOMatを持つ配列</returns>
        public static TMOMat[] Slerp(TMOMat mat0, TMOMat mat1, TMOMat mat2, TMOMat mat3, int length)
        {
            return Slerp(mat0, mat1, mat2, mat3, length, 0.5f);
        }

        /// <summary>
        /// 補間を行います。
        /// </summary>
        /// <param name="mat0">行列0</param>
        /// <param name="mat1">行列1</param>
        /// <param name="mat2">行列2</param>
        /// <param name="mat3">行列3</param>
        /// <param name="length">分割数</param>
        /// <param name="p1">補間速度係数</param>
        /// <returns>分割数だけTMOMatを持つ配列</returns>
        public static TMOMat[] Slerp(TMOMat mat0, TMOMat mat1, TMOMat mat2, TMOMat mat3, int length, float p1)
        {
            TMOMat[] ret = new TMOMat[length];

            Matrix m1 = mat1.m;
            Matrix m2 = mat2.m;

            Vector3 scaling1;
            Vector3 scaling2;
            Vector3 v1 = Helper.DecomposeMatrix(ref m1, out scaling1);
            Vector3 v2 = Helper.DecomposeMatrix(ref m2, out scaling2);

            Quaternion q1 = Quaternion.RotationMatrix(m1);
            Quaternion q2 = Quaternion.RotationMatrix(m2);

            Vector3 v0 = new Vector3(mat0.m.M41, mat0.m.M42, mat0.m.M43);
            //Vector3 v1 = new Vector3(mat1.m.M41, mat1.m.M42, mat1.m.M43);
            //Vector3 v2 = new Vector3(mat2.m.M41, mat2.m.M42, mat2.m.M43);
            Vector3 v3 = new Vector3(mat3.m.M41, mat3.m.M42, mat3.m.M43);

            float p0 = 0.0f;
            float p2 = 1.0f;
            float dt = 1.0f/length;
            for (int i = 0; i < length; i++)
            {
                float t = dt*i;
                float p = t*t*(p2-2*p1+p0) + t*(2*p1-2*p0) + p0;
                Matrix m = Matrix.Scaling(Vector3.Lerp(scaling1, scaling2, p)) * Matrix.RotationQuaternion(Quaternion.Slerp(q1, q2, p)) * Matrix.Translation(Vector3.CatmullRom(v0, v1, v2, v3, p));
                ret[i] = new TMOMat(ref m);
            }
            return ret;
        }

        /// <summary>
        /// 加減算を行います。
        /// </summary>
        /// <param name="mat0">行列0</param>
        /// <param name="mat1">行列1</param>
        /// <param name="mat2">行列2</param>
        /// <returns>行列1 - 行列2 + 行列0</returns>
        public static TMOMat AddSub(TMOMat mat0, TMOMat mat1, TMOMat mat2)
        {
            Matrix m0 = mat0.m;
            Matrix m1 = mat1.m;
            Matrix m2 = mat2.m;
            Vector3 t0 = Helper.DecomposeMatrix(ref m0);
            Vector3 t1 = Helper.DecomposeMatrix(ref m1);
            Vector3 t2 = Helper.DecomposeMatrix(ref m2);
            Matrix m = m1 * Matrix.Invert(m2) * m0 * Matrix.Translation(t1 - t2 + t0);
            return new TMOMat(ref m);
        }

        /// 左右反転します。
        public void Flip()
        {
            Helper.FlipMatrix(ref m);
        }

        /// 180度Y軸回転します。
        public void Turn()
        {
            Helper.TurnMatrix(ref m);
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
        /// 指定名称（短い形式）を持つ子nodeを検索します。
        /// </summary>
        /// <param name="name">名称（短い形式）</param>
        /// <returns></returns>
        public TMONode FindChildByName(string name)
        {
            foreach (TMONode child_node in children)
                if (child_node.name == name)
                    return child_node;
            return null;
        }

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
                    transformation_matrix = m;
                    need_update_transformation = false;
                }
                return transformation_matrix;
            }
            set
            {
                transformation_matrix = value;
                translation = Helper.DecomposeMatrix(ref value, out scaling);
                rotation = Quaternion.RotationMatrix(value);
            }
        }
    }
}
