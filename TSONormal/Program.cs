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
                slider_matrix.AgeRate = ratios[0];
                slider_matrix.ArmRate = ratios[1];
                slider_matrix.LegRate = ratios[2];
                slider_matrix.WaistRate = ratios[3];
                slider_matrix.OppaiRate = ratios[4];
                slider_matrix.EyeRate = ratios[5];
            }
            fig.UpdateNodeMap();
            if (fig_file != null)
                fig.UpdateBoneMatrices();
            else
                fig.UpdateBoneMatricesWithoutSlider(true);

            Matrix[] bone_matrices = fig.ClipBoneMatrices(tso);

            MqoMesh[] meshes = new MqoMesh[tso.meshes.Length];
            for (int i = 0; i < tso.meshes.Length; i++)
            {
                meshes[i] = MqoMesh.FromTSOMesh(tso.meshes[i]);
            }

            foreach (MqoMesh mesh in meshes)
            {
                // mqo: 頂点位置を更新する。
                foreach (MqoVert v in mesh.vertices)
                {
                    v.position = v.CalcSkindeformPosition(bone_matrices);
                }

                // mqo: 頂点法線を更新する。
                mesh.UpdateVerticesNormal();

                // tso: 頂点法線を更新する。
                foreach (MqoVert v in mesh.vertices)
                {
                    foreach (TSOPair pair in v.rel)
                    {
                        pair.a.normal = v.normal;
                    }
                }
            }

            tso.Save(@"out.tso");
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
