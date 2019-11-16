using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace TDCG
{
    /// <summary>
    /// フィギュア
    /// </summary>
    public class Figure : IDisposable
    {
        /// <summary>
        /// フィギュアが保持しているtsoリスト
        /// </summary>
        public List<TSOFile> TsoList = new List<TSOFile>();

        /// <summary>
        /// スライダ変形行列を使うか
        /// </summary>
        public static bool SliderMatrixEnabled = true;

        SliderMatrix slider_matrix = null;

        /// <summary>
        /// スライダ変形行列
        /// </summary>
        public SliderMatrix SliderMatrix { get { return slider_matrix; } }

        /// 着衣扱いか
        public bool Clothed
        {
            get { return slider_matrix.Clothed; }
        }

        /// 着衣扱いかを更新します。
        public void UpdateClothed()
        {
            bool clothed = false;
            foreach (TSOFile tso in TsoList)
            {
                switch (tso.Row)
                {
                    case 0x06: // 水着
                    case 0x09: // 上衣
                    case 0x0A: // 全衣
                        clothed = true;
                        break;
                }
            }
            slider_matrix.Clothed = clothed;
        }

        TMOFile tmo = null;
        /// <summary>
        /// tmo
        /// </summary>
        public TMOFile Tmo { get { return tmo; } set { tmo = value; } }

        /// tso nodeからtmo nodeを導出する辞書
        public Dictionary<TSONode, TMONode> nodemap;

        MatrixStack matrixStack = null;

        /// <summary>
        /// フィギュアを生成します。
        /// </summary>
        public Figure()
        {
            if (SliderMatrixEnabled)
                slider_matrix = new SliderMatrix();

            nodemap = new Dictionary<TSONode, TMONode>();
            matrixStack = new MatrixStack();
        }

        /// <summary>
        /// 指定位置にあるtsoの位置を入れ替えます。描画順を変更します。
        /// </summary>
        /// <param name="aidx">リスト上の位置a</param>
        /// <param name="bidx">リスト上の位置b</param>
        public void SwapAt(int aidx, int bidx)
        {
            Debug.Assert(aidx < bidx);
            TSOFile a = TsoList[aidx];
            TSOFile b = TsoList[bidx];
            TsoList.RemoveAt(bidx);
            TsoList.RemoveAt(aidx);
            TsoList.Insert(aidx, b);
            TsoList.Insert(bidx, a);
        }

        /// <summary>
        /// nodemapとbone行列を更新します。
        /// tmoが読み込まれていない場合は先頭のtsoからtmoを生成します。
        /// </summary>
        public void UpdateNodeMapAndBoneMatrices()
        {
            if (tmo == null)
                RegenerateTmo();

            UpdateNodeMap();

            UpdateBoneMatrices();
        }

        /// <summary>
        /// 先頭のtsoからtmoを生成します。
        /// </summary>
        public void RegenerateTmo()
        {
            if (TsoList.Count != 0)
            {
                Tmo = GenerateTmo(TsoList[0]);
            }
        }

        /// <summary>
        /// tmoを生成します。
        /// </summary>
        static TMOFile GenerateTmo(TSOFile tso)
        {
            TMOFile tmo = new TMOFile();
            tmo.header = new byte[8] { 0, 0, 0, 0, 0, 0, 0, 0 };
            tmo.opt0 = 1;
            tmo.opt1 = 0;

            int nodes_len = tso.nodes.Length;
            tmo.nodes = new TMONode[nodes_len];

            for (int i = 0; i < nodes_len; i++)
            {
                tmo.nodes[i] = new TMONode(i);
                tmo.nodes[i].Path = tso.nodes[i].Path;
                tmo.nodes[i].TransformationMatrix = tso.nodes[i].TransformationMatrix;
            }

            tmo.GenerateNodemapAndTree();

            //tmo.footer = new byte[4] { 0, 0, 0, 0 };

            return tmo;
        }

        /// tsoにあるのにtmoにないボーンがあれば
        /// tmoに追加します。
        static void SyncTmo(TSOFile tso, TMOFile tmo)
        {
            List<TSONode> add_nodes = new List<TSONode>();
            foreach (TSONode tso_node in tso.nodes)
            {
                TMONode tmo_node;
                if (! tmo.nodemap.TryGetValue(tso_node.Name, out tmo_node))
                {
                    add_nodes.Add(tso_node);
                }
            }

            if (add_nodes.Count != 0)
            {
                int nodes_len = tmo.nodes.Length;
                Array.Resize(ref tmo.nodes, nodes_len + add_nodes.Count);
                int x = nodes_len;
                foreach (TSONode add_node in add_nodes)
                {
                    tmo.nodes[x] = new TMONode(x);
                    tmo.nodes[x].Path = add_node.Path;
                    tmo.nodes[x].TransformationMatrix = add_node.TransformationMatrix;
                    x++;
                }
                tmo.GenerateNodemapAndTree();
            }
        }

        /// <summary>
        /// nodemapを更新します。
        /// </summary>
        public void UpdateNodeMap()
        {
            if (TsoList.Count != 0)
            {
                SyncTmo(TsoList[0], Tmo);
            }
            nodemap.Clear();
            foreach (TSOFile tso in TsoList)
                AddNodeMap(tso);
        }

        /// <summary>
        /// tsoに対するnodemapを追加します。
        /// </summary>
        /// <param name="tso">tso</param>
        void AddNodeMap(TSOFile tso)
        {
            foreach (TSONode tso_node in tso.nodes)
            {
                TMONode tmo_node;
                if (tmo.nodemap.TryGetValue(tso_node.Name, out tmo_node))
                    nodemap.Add(tso_node, tmo_node);
            }
        }

        /// <summary>
        /// bone行列更新時に呼び出されるハンドラ
        /// </summary>
        public event EventHandler UpdateBoneMatricesEvent;

        /// <summary>
        /// bone行列を更新します。
        /// </summary>
        public void UpdateBoneMatrices()
        {
            UpdateBoneMatrices(tmo);

            if (UpdateBoneMatricesEvent != null)
                UpdateBoneMatricesEvent(this, EventArgs.Empty);
        }

        /// <summary>
        /// bone行列を更新します。
        /// </summary>
        protected void UpdateBoneMatrices(TMOFile tmo)
        {
            if (tmo == null)
                return;

            if (tmo.w_hips_node != null)
            {
                matrixStack.LoadIdentity();
                UpdateBoneMatrices(tmo.w_hips_node);
            }
            foreach (TMONode tmo_node in tmo.root_nodes_except_w_hips)
            {
                matrixStack.LoadIdentity();
                UpdateBoneMatricesWithoutSlider(tmo_node);
            }
        }

        /// <summary>
        /// bone行列を更新します。
        /// </summary>
        protected void UpdateBoneMatrices(TMONode tmo_node)
        {
            matrixStack.Push();

            Matrix m = tmo_node.TransformationMatrix;

            if (slider_matrix != null)
            {
                string name = tmo_node.Name;

                slider_matrix.Transform(name, ref m);

                matrixStack.MultiplyMatrixLocal(m);
                m = matrixStack.Top;

                slider_matrix.TransformWithoutStack(name, ref m);
            }
            else
            {
                matrixStack.MultiplyMatrixLocal(m);
                m = matrixStack.Top;
            }

            tmo_node.combined_matrix = m;

            foreach (TMONode child_node in tmo_node.children)
                UpdateBoneMatrices(child_node);

            matrixStack.Pop();
        }

        /// <summary>
        /// bone行列を更新します（体型変更なし）。
        /// </summary>
        /// <param name="forced">obsolete</param>
        public void UpdateBoneMatricesWithoutSlider(bool forced = false)
        {
            UpdateBoneMatricesWithoutSlider(tmo);
        }

        /// <summary>
        /// bone行列を更新します（体型変更なし）。
        /// </summary>
        protected void UpdateBoneMatricesWithoutSlider(TMOFile tmo)
        {
            if (tmo == null)
                return;

            if (tmo.w_hips_node != null)
            {
                matrixStack.LoadIdentity();
                UpdateBoneMatricesWithoutSlider(tmo.w_hips_node);
            }
            foreach (TMONode tmo_node in tmo.root_nodes_except_w_hips)
            {
                matrixStack.LoadIdentity();
                UpdateBoneMatricesWithoutSlider(tmo_node);
            }
        }

        /// <summary>
        /// bone行列を更新します（体型変更なし）。
        /// </summary>
        protected void UpdateBoneMatricesWithoutSlider(TMONode tmo_node)
        {
            matrixStack.Push();

            Matrix m = tmo_node.TransformationMatrix;

            matrixStack.MultiplyMatrixLocal(m);
            m = matrixStack.Top;

            tmo_node.combined_matrix = m;

            foreach (TMONode child_node in tmo_node.children)
                UpdateBoneMatricesWithoutSlider(child_node);

            matrixStack.Pop();
        }

        /// <summary>
        /// スキン変形行列の配列を得ます。
        /// </summary>
        /// <param name="sub_mesh">サブメッシュ</param>
        /// <returns>スキン変形行列の配列</returns>
        public Matrix[] ClipBoneMatrices(TSOSubMesh sub_mesh)
        {
            Matrix[] matrices = new Matrix[sub_mesh.maxPalettes];

            for (int i = 0; i < sub_mesh.maxPalettes; i++)
            {
                TSONode tso_node = sub_mesh.GetBone(i);
                TMONode tmo_node;
                if (nodemap.TryGetValue(tso_node, out tmo_node))
                    matrices[i] = tso_node.offset_matrix * tmo_node.combined_matrix;
            }
            return matrices;
        }

        /// <summary>
        /// スキン変形行列の配列を得ます。
        /// </summary>
        /// <param name="tso">tso</param>
        /// <returns>スキン変形行列の配列</returns>
        public Matrix[] ClipBoneMatrices(TSOFile tso)
        {
            Matrix[] matrices = new Matrix[tso.nodes.Length];

            for (int i = 0; i < tso.nodes.Length; i++)
            {
                TSONode tso_node = tso.nodes[i];
                TMONode tmo_node;
                if (nodemap.TryGetValue(tso_node, out tmo_node))
                    matrices[i] = tso_node.offset_matrix * tmo_node.combined_matrix;
            }
            return matrices;
        }

        Quaternion lamp_rotation = Quaternion.Identity;

        //ランプの回転
        //xyz = (0,0,-1) を回転すると光源方向を得る
        public Quaternion LampRotation { get { return lamp_rotation; } set { lamp_rotation = value; } }

        /// <summary>
        /// 光源方向
        /// </summary>
        public Vector3 LightDirection
        {
            get { return Vector3.TransformCoordinate(new Vector3(0.0f, 0.0f, -1.0f), Matrix.RotationQuaternion(lamp_rotation)); }
        }

        Vector4 ToVector4(Vector3 v)
        {
            return new Vector4(v.X, v.Y, v.Z, 0.0f);
        }

        /// <summary>
        /// 光源方向ベクトルを得ます。
        /// </summary>
        /// <returns></returns>
        public Vector4 LightDirForced
        {
            get { return ToVector4(LightDirection); }
        }


        public void GetWorldMatrix(out Matrix m)
        {
            m = Matrix.Identity;

            if (slider_matrix != null)
            {
                //姉妹スライダによる変形
                m = Matrix.Scaling(slider_matrix.Local);
            }
        }

        /// <summary>
        /// 内部objectを破棄します。
        /// </summary>
        public void Dispose()
        {
            Debug.WriteLine("Figure.Dispose");

            foreach (TSOFile tso in TsoList)
                tso.Dispose();
        }

        bool hidden;
        /// 隠すか
        public bool Hidden { get { return hidden; } }

        /// 隠すかを切り替えます。
        public void SwitchHidden()
        {
            hidden = !hidden;
        }
    }
}
