using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using Microsoft.DirectX;

namespace TDCG
{
    public class PNGSaveCameraDescription
    {
        public Vector3 Translation = Vector3.Empty;
        public Vector3 Angle = Vector3.Empty;

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
        /// フィギュアリスト
        /// </summary>
        public List<Figure> FigureList = new List<Figure>();
    }

    public class PNGSaveLoader
    {
        /// <summary>
        /// 指定パスからPNGFileを読み込みフィギュアを作成します。
        /// </summary>
        /// <param name="source_file">PNGFileのパス</param>
        static public PNGSaveData FromFile(string source_file)
        {
            PNGSaveData sav = new PNGSaveData();
            try
            {
                PNGFile png = new PNGFile();
                Figure fig = null;

                png.Hsav += delegate (string type)
                {
                    sav.type = type;
                    fig = new Figure();
                    sav.FigureList.Add(fig);
                };
                png.Pose += delegate (string type)
                {
                    sav.type = type;
                };
                png.Scne += delegate (string type)
                {
                    sav.type = type;
                };
                png.Cami += delegate (Stream dest, int extract_length)
                {
                    byte[] buf = new byte[extract_length];
                    dest.Read(buf, 0, extract_length);

                    List<float> factor = new List<float>();
                    for (int offset = 0; offset < extract_length; offset += sizeof(float))
                    {
                        float flo = BitConverter.ToSingle(buf, offset);
                        factor.Add(flo);
                    }

                    sav.CameraDescription = new PNGSaveCameraDescription();
                    sav.CameraDescription.Translation = new Vector3(-factor[0], -factor[1], -factor[2]);
                    sav.CameraDescription.Angle = new Vector3(-factor[5], -factor[4], -factor[6]);
                };
                png.Lgta += delegate (Stream dest, int extract_length)
                {
                    byte[] buf = new byte[extract_length];
                    dest.Read(buf, 0, extract_length);

                    List<float> factor = new List<float>();
                    for (int offset = 0; offset < extract_length; offset += sizeof(float))
                    {
                        float flo = BitConverter.ToSingle(buf, offset);
                        factor.Add(flo);
                    }

                    Matrix m;
                    m.M11 = factor[0];
                    m.M12 = factor[1];
                    m.M13 = factor[2];
                    m.M14 = factor[3];

                    m.M21 = factor[4];
                    m.M22 = factor[5];
                    m.M23 = factor[6];
                    m.M24 = factor[7];

                    m.M31 = factor[8];
                    m.M32 = factor[9];
                    m.M33 = factor[10];
                    m.M34 = factor[11];

                    m.M41 = factor[12];
                    m.M42 = factor[13];
                    m.M43 = factor[14];
                    m.M44 = factor[15];

                    sav.LampRotation = Quaternion.RotationMatrix(m);
                };
                png.Ftmo += delegate (Stream dest, int extract_length)
                {
                    sav.Tmo = new TMOFile();
                    sav.Tmo.Load(dest);
                };
                png.Figu += delegate (Stream dest, int extract_length)
                {
                    fig = new Figure();
                    fig.LampRotation = sav.LampRotation;
                    fig.Tmo = sav.Tmo;
                    sav.FigureList.Add(fig);

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
                    if (fig.slider_matrix != null)
                    {
                        fig.slider_matrix.AgeRatio = ratios[0];
                        fig.slider_matrix.ArmRatio = ratios[1];
                        fig.slider_matrix.LegRatio = ratios[2];
                        fig.slider_matrix.WaistRatio = ratios[3];
                        fig.slider_matrix.OppaiRatio = ratios[4];
                        fig.slider_matrix.EyeRatio = ratios[5];
                    }
                };
                png.Ftso += delegate (Stream dest, int extract_length, byte[] opt1)
                {
                    TSOFile tso = new TSOFile();
                    tso.Load(dest);
                    tso.Row = opt1[0];
                    fig.TsoList.Add(tso);
                };
                Debug.WriteLine("loading " + source_file);
                png.Load(source_file);

                if (sav.type == "HSAV")
                {
                    BMPSaveData data = new BMPSaveData();

                    using (Stream stream = File.OpenRead(source_file))
                        data.Read(stream);

                    if (fig.slider_matrix != null && data.bitmap.Size == new Size(128, 256))
                    {
                        fig.slider_matrix.AgeRatio = data.GetSliderValue(4);
                        fig.slider_matrix.ArmRatio = data.GetSliderValue(5);
                        fig.slider_matrix.LegRatio = data.GetSliderValue(6);
                        fig.slider_matrix.WaistRatio = data.GetSliderValue(7);
                        fig.slider_matrix.OppaiRatio = data.GetSliderValue(0);
                        fig.slider_matrix.EyeRatio = data.GetSliderValue(8);
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
            return sav;
        }
    }
}
