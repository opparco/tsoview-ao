using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.ComponentModel;
using System.Text;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using TDCG.Extensions;

namespace TDCG
{
    /// <summary>
    /// サブメッシュ
    /// </summary>
    public class TSOSubMesh
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

        int num_vertices;
        /// 頂点数
        public int NumberVertices
        {
            get { return num_vertices; }
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

        public string sha1;

        /// <summary>
        /// サブメッシュを読み込みます。
        /// </summary>
        public void Read(BinaryReader reader)
        {
            this.spec = reader.ReadInt32();
            int num_bone_indices = reader.ReadInt32();
            this.maxPalettes = 16;
            if (this.maxPalettes > num_bone_indices)
                this.maxPalettes = num_bone_indices;

            this.bone_indices = new int[num_bone_indices];
            for (int i = 0; i < num_bone_indices; i++)
            {
                this.bone_indices[i] = reader.ReadInt32();
            }

            int num_vertices = reader.ReadInt32();
            Vertex[] vertices = new Vertex[num_vertices];
            for (int i = 0; i < num_vertices; i++)
            {
                Vertex v = new Vertex();
                v.Read(reader);
                vertices[i] = v;
            }

            this.sha1 = D3DVertexBufferManager.instance.Create(vertices);

            this.num_vertices = num_vertices;
        }

        /// <summary>
        /// サブメッシュを再度読み込みます。
        /// </summary>
        public void ReadOnDeviceReset()
        {
            D3DVertexBufferManager.instance.ReadOnDeviceReset(this.sha1);
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

            D3DVertexBufferManager.instance.Write(this.sha1, bw);
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
    }

    /// <summary>
    /// メッシュ
    /// </summary>
    public class TSOMesh
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
        /// メッシュを再度読み込みます。
        /// </summary>
        public void ReadOnDeviceReset()
        {
            foreach (TSOSubMesh sub_mesh in this.sub_meshes)
            {
                sub_mesh.ReadOnDeviceReset();
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
            int num_skin_weights = reader.ReadInt32();
            this.skin_weights = new SkinWeight[num_skin_weights];
            for (int i = 0; i < num_skin_weights; i++)
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

            int num_skin_weights = 0;
            SkinWeight[] skin_weights = new SkinWeight[4];
            foreach (SkinWeight skin_weight in this.skin_weights)
            {
                if (skin_weight.weight == 0.0f)
                    continue;

                skin_weights[num_skin_weights++] = skin_weight;
            }
            bw.Write(num_skin_weights);

            for (int i = 0; i < num_skin_weights; i++)
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
    public class TSOTexture
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
        /// 名称
        /// </summary>
        public string Name { get { return name; } set { name = value; } }
        /// <summary>
        /// ファイル名
        /// </summary>
        public string FileName { get { return file; } set { file = value; } }

        public string sha1;

        /// <summary>
        /// テクスチャを読み込みます。
        /// </summary>
        public void Read(BinaryReader reader)
        {
            this.name = reader.ReadCString();
            this.file = reader.ReadCString();
            int width = reader.ReadInt32();
            int height = reader.ReadInt32();
            int depth = reader.ReadInt32();
            byte[] data = reader.ReadBytes( width * height * depth );

            this.sha1 = D3DTextureManager.instance.Create(width, height, depth, data);
        }

        /// <summary>
        /// テクスチャを再度読み込みます。
        /// </summary>
        public void ReadOnDeviceReset()
        {
            D3DTextureManager.instance.ReadOnDeviceReset(this.sha1);
        }

        /// <summary>
        /// テクスチャを書き出します。
        /// </summary>
        public void Write(BinaryWriter bw)
        {
            bw.WriteCString(this.name);
            bw.WriteCString(this.file);

            D3DTextureManager.instance.Write(this.sha1, bw);
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

        /// shader設定上の texture name と sha1 を関連付ける辞書
        Dictionary<string, string> d3d_texturemap;

        public Texture GetDirect3dTextureByName(string name)
        {
            string sha1;
            if (name != null && d3d_texturemap.TryGetValue(name, out sha1))
                return D3DTextureManager.instance.GetDirect3dTextureBySha1(sha1);
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
            GenerateD3DTexturemap();

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

        void GenerateD3DTexturemap()
        {
            d3d_texturemap = new Dictionary<string, string>();

            foreach (TSOTexture tex in textures)
            {
                d3d_texturemap.Add(tex.name, tex.sha1);
            }
        }

        /// <summary>
        /// 内部objectを再度読み込みます。
        /// </summary>
        public void ReadOnDeviceReset()
        {
            Debug.WriteLine("TSOFile.ReadOnDeviceReset");

            foreach (TSOTexture tex in textures)
            {
                tex.ReadOnDeviceReset();
            }

            foreach (TSOMesh mesh in meshes)
            foreach (TSOSubMesh sub_mesh in mesh.sub_meshes)
            {
                sub_mesh.ReadOnDeviceReset();
            }
        }

        /// <summary>
        /// 内部objectを破棄します。
        /// </summary>
        public void Dispose()
        {
            Debug.WriteLine("TSOFile.Dispose");

            foreach (TSOTexture tex in textures)
            {
                D3DTextureManager.instance.Release(tex.sha1);
            }

            foreach (TSOMesh mesh in meshes)
            foreach (TSOSubMesh sub_mesh in mesh.sub_meshes)
            {
                D3DVertexBufferManager.instance.Release(sub_mesh.sha1);
            }
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
