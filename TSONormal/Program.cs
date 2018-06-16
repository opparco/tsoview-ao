using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using Direct3D = Microsoft.DirectX.Direct3D;
using TDCG;

namespace TSONormal
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("TSONormal.exe <tso file> <tmo file> [fig file]");
                return;
            }
            string tso_file = args[0];
            string tmo_file = args[1];

            string fig_file = null;
            if (args.Length > 2)
            {
                fig_file = args[2];
            }

            TSOFile tso = new TSOFile();
            tso.Load(tso_file);

            TMOFile tmo = new TMOFile();
            tmo.Load(tmo_file);

            Figure fig = new Figure();
            fig.TsoList.Add(tso);
            fig.Tmo = tmo;

            if (fig_file != null)
            {
                List<float> ratios = ReadFloats(fig_file);
                /*
                ◆FIGU
                スライダの位置。値は float型で 0.0 .. 1.0
                    0: 姉妹
                    1: うで
                    2: あし
                    3: 胴まわり
                    4: おっぱい
                    5: つり目たれ目
                    6: やわらか
                 */
                SliderMatrix slider_matrix = fig.SliderMatrix;
                slider_matrix.AgeRatio = ratios[0];
                slider_matrix.ArmRatio = ratios[1];
                slider_matrix.LegRatio = ratios[2];
                slider_matrix.WaistRatio = ratios[3];
                slider_matrix.OppaiRatio = ratios[4];
                slider_matrix.EyeRatio = ratios[5];
            }
            fig.UpdateNodeMap();
            if (fig_file != null)
                fig.UpdateBoneMatrices();
            else
                fig.UpdateBoneMatricesWithoutSlider(true);

            foreach (TSOMesh mesh in tso.meshes)
                foreach (TSOSubMesh sub in mesh.sub_meshes)
                {
                    Matrix[] clipped_bone_matrices = fig.ClipBoneMatrices(sub);

                    for (int i = 0; i < sub.vertices.Length; i++)
                    {
                        CalcSkindeform(ref sub.vertices[i], clipped_bone_matrices);
                    }
                }

            // tmo.nodesをtso.nodesに代入
            foreach (TSONode tso_node in tso.nodes)
            {
                TMONode tmo_node;
                if (fig.nodemap.TryGetValue(tso_node, out tmo_node))
                    tso_node.TransformationMatrix = tmo_node.TransformationMatrix;
            }

            tso.Save(@"out.tso");
        }

        // DONE:
        // mqo_mesh.CreateVerticesAndFaces();
        // 頂点を集約する。
        //      create MqoVert[] from Vertex[]
        // 面を作成する。
        //      create MqoFace[]
        //
        // TODO:
        // .tmoを元に頂点位置を更新する。
        //      update MqoVert#position
        //
        // 頂点位置を元に面法線を計算する。
        //      update MqoFace#normal
        //
        // 面法線を元に頂点法線を計算する。
        //      update MqoVert#normal
        //
        // 頂点法線を.tsoに保存する。
        //      update Vertex#normal

        // 頂点の位置と法線を更新する。
        // v: 対象とする頂点
        // bone_matrices: 変形行列リスト
        public static void CalcSkindeform(ref Vertex v, Matrix[] bone_matrices)
        {
            Vector3 pos = Vector3.Empty;
            for (int i = 0; i < 4; i++)
            {
                Matrix m = bone_matrices[v.skin_weights[i].bone_index];
                float w = v.skin_weights[i].weight;
                pos += Vector3.TransformCoordinate(v.position, m) * w;
            }
            v.position = pos;

            Vector3 nor = Vector3.Empty;
            for (int i = 0; i < 4; i++)
            {
                Matrix m = bone_matrices[v.skin_weights[i].bone_index];
                m.M41 = 0;
                m.M42 = 0;
                m.M43 = 0;
                float w = v.skin_weights[i].weight;
                nor += Vector3.TransformCoordinate(v.normal, m) * w;
            }
            v.normal = Vector3.Normalize(nor);
        }

        static List<float> ReadFloats(string dest_file)
        {
            List<float> floats = new List<float>();
            string line;
            using (StreamReader source = new StreamReader(File.OpenRead(dest_file)))
                while ((line = source.ReadLine()) != null)
                {
                    floats.Add(Single.Parse(line));
                }
            return floats;
        }
    }
}
