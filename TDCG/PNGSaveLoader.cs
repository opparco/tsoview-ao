using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using Direct3D = Microsoft.DirectX.Direct3D;

using TDCG.Extensions;

namespace TDCG
{
    public class PNGSaveCameraDescription
    {
        public Vector3 Translation = Vector3.Empty;
        public Vector3 Angle = Vector3.Empty;

        public void Read(Stream dest)
        {
            Vector4 translation = new Vector4();
            Vector4 angle = new Vector4(); // (x,y,z,w) = (yaw,pitch,roll,0)
            using (BinaryReader reader = new BinaryReader(dest))
            {
                reader.ReadVector4(ref translation);
                reader.ReadVector4(ref angle);
            }
            Translation = new Vector3(-translation.X, -translation.Y, -translation.Z);
            Angle = new Vector3(-angle.Y, -angle.X, -angle.Z);
        }

        static List<float> ReadFloats(string file)
        {
            List<float> floats = new List<float>();
            string line;
            using (StreamReader source = new StreamReader(File.OpenRead(file)))
            while ((line = source.ReadLine()) != null)
            {
                floats.Add(Single.Parse(line));
            }
            return floats;
        }

        public void ReadFromTextFile(string file)
        {
            List<float> factor = ReadFloats(file);
            Translation = new Vector3(-factor[0], -factor[1], -factor[2]);
            Angle = new Vector3(-factor[5], -factor[4], -factor[6]);
        }
    }

    /// <summary>
    /// セーブファイルの内容を保持します。
    /// </summary>
    public class PNGSaveData
    {
        /// <summary>
        /// タイプ
        /// </summary>
        public string type = null;
        /// <summary>
        /// 最後に読み込んだカメラ設定
        /// </summary>
        public PNGSaveCameraDescription CameraDescription = null;
        /// <summary>
        /// 最後に読み込んだランプ回転
        /// </summary>
        public Quaternion LampRotation = Quaternion.Identity;
        /// <summary>
        /// 最後に読み込んだtmo
        /// </summary>
        public TMOFile Tmo;
        /// <summary>
        /// フィギュア配列
        /// </summary>
        public Figure[] figures;
    }

    public class PNGSaveLoader
    {
        /// <summary>
        /// 指定パスからPNGFileを読み込みセーブファイルの内容を得ます。
        /// </summary>
        /// <param name="source_file">PNGFileのパス</param>
        static public PNGSaveData FromFile(string source_file, Func<int, int, int, byte[], Texture> create_d3d_texture, Func<Vertex[], VertexBuffer> create_d3d_vertex_buffer)
        {
            PNGSaveData savedata = new PNGSaveData();
            try
            {
                PNGFile png = new PNGFile();
                Figure fig = null;
                int fig_idx = 0;

                png.Hsav += delegate (string type, uint opt0, uint opt1)
                {
                    savedata.type = type;
                    fig = new Figure();
                    savedata.figures = new Figure[1];
                    savedata.figures[fig_idx++] = fig;
                };
                png.Pose += delegate (string type, uint opt0, uint opt1)
                {
                    savedata.type = type;
                };
                png.Scne += delegate (string type, uint opt0, uint opt1)
                {
                    savedata.type = type;
                    savedata.figures = new Figure[opt1];
                };
                png.Cami += delegate (Stream dest, int extract_length)
                {
                    savedata.CameraDescription = new PNGSaveCameraDescription();
                    savedata.CameraDescription.Read(dest);
                };
                png.Lgta += delegate (Stream dest, int extract_length)
                {
                    Matrix m = new Matrix();
                    using (BinaryReader reader = new BinaryReader(dest))
                    {
                        reader.ReadMatrix(ref m);
                    }
                    savedata.LampRotation = Quaternion.RotationMatrix(m);
                };
                png.Ftmo += delegate (Stream dest, int extract_length)
                {
                    savedata.Tmo = new TMOFile();
                    savedata.Tmo.Load(dest);
                };
                png.Figu += delegate (Stream dest, int extract_length)
                {
                    fig = new Figure();
                    fig.LampRotation = savedata.LampRotation;
                    fig.Tmo = savedata.Tmo;
                    savedata.figures[fig_idx++] = fig;

                    byte[] buf = new byte[extract_length];
                    dest.Read(buf, 0, extract_length);

                    List<float> ratios = new List<float>();
                    for (int offset = 0; offset < extract_length; offset += sizeof(float))
                    {
                        float flo = BitConverter.ToSingle(buf, offset);
                        ratios.Add(flo);
                    }
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
                    if (slider_matrix != null)
                    {
                        slider_matrix.AgeRatio = ratios[0];
                        slider_matrix.ArmRatio = ratios[1];
                        slider_matrix.LegRatio = ratios[2];
                        slider_matrix.WaistRatio = ratios[3];
                        slider_matrix.OppaiRatio = ratios[4];
                        slider_matrix.EyeRatio = ratios[5];
                    }
                };
                png.Ftso += delegate (Stream dest, int extract_length, byte[] opt1)
                {
                    TSOFile tso = new TSOFile();
                    tso.Load(dest, create_d3d_texture, create_d3d_vertex_buffer);
                    tso.Row = opt1[0];
                    fig.TsoList.Add(tso);
                };
                Debug.WriteLine("loading " + source_file);
                png.Load(source_file);

                if (savedata.type == "HSAV")
                {
                    BMPSaveData data = new BMPSaveData();

                    using (Stream stream = File.OpenRead(source_file))
                        data.Read(stream);

                    SliderMatrix slider_matrix = fig.SliderMatrix;
                    if (slider_matrix != null && data.bitmap.Size == new Size(128, 256))
                    {
                        slider_matrix.AgeRatio = data.GetSliderValue(4);
                        slider_matrix.ArmRatio = data.GetSliderValue(5);
                        slider_matrix.LegRatio = data.GetSliderValue(6);
                        slider_matrix.WaistRatio = data.GetSliderValue(7);
                        slider_matrix.OppaiRatio = data.GetSliderValue(0);
                        slider_matrix.EyeRatio = data.GetSliderValue(8);
                    }

                    foreach (TSOFile tso in fig.TsoList)
                    {
                        string file = data.GetFileName(tso.Row);
                        if (file != "")
                            tso.FileName = Path.GetFileName(file);
                        else
                            tso.FileName = string.Format("{0:X2}", tso.Row);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex);
            }
            return savedata;
        }
    }
}
