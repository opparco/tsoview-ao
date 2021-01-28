using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using Microsoft.DirectX;

namespace TDCG
{
    /// シーンセーブをファイルに書き出す
    public class PNGSceneSaveWriter
    {
        byte[] ToBytes(float[] floats)
        {
            byte[] data = new byte[ sizeof(Single) * floats.Length ];
            int offset = 0;
            foreach (float flo in floats)
            {
                byte[] buf_flo = BitConverter.GetBytes(flo);
                buf_flo.CopyTo(data, offset);
                offset += buf_flo.Length;
            }
            return data;
        }

        void WriteCAMI(PNGWriter pw, PNGSaveCameraDescription camera_desc)
        {
            float[] floats = new float[8];

            floats[0] = -camera_desc.Translation.X;
            floats[1] = -camera_desc.Translation.Y;
            floats[2] = -camera_desc.Translation.Z;
            floats[3] = 0.0f;

            floats[4] = -camera_desc.Angle.Y;
            floats[5] = -camera_desc.Angle.X;
            floats[6] = -camera_desc.Angle.Z;
            floats[7] = 0.0f;

            byte[] cami = ToBytes(floats);
            pw.WriteCAMI(cami);
        }

        void WriteLGTA(PNGWriter pw, Quaternion lamp_rotation)
        {
            Matrix m = Matrix.RotationQuaternion(lamp_rotation);

            float[] floats = new float[16];

            floats[0] = m.M11;
            floats[1] = m.M12;
            floats[2] = m.M13;
            floats[3] = m.M14;

            floats[4] = m.M21;
            floats[5] = m.M22;
            floats[6] = m.M23;
            floats[7] = m.M24;

            floats[8] = m.M31;
            floats[9] = m.M32;
            floats[10] = m.M33;
            floats[11] = m.M34;

            floats[12] = m.M41;
            floats[13] = m.M42;
            floats[14] = m.M43;
            floats[15] = m.M44;

            byte[] lgta = ToBytes(floats);
            pw.WriteLGTA(lgta);
        }

        void WriteFIGU(PNGWriter pw, SliderMatrix slider_matrix)
        {
            float[] floats = new float[7];

            floats[0] = slider_matrix.AgeRatio;
            floats[1] = slider_matrix.ArmRatio;
            floats[2] = slider_matrix.LegRatio;
            floats[3] = slider_matrix.WaistRatio;
            floats[4] = slider_matrix.OppaiRatio;
            floats[5] = slider_matrix.EyeRatio;
            floats[6] = 0.5f; // やわらか

            byte[] figu = ToBytes(floats);
            pw.WriteFIGU(figu);
        }

        void WriteScne(PNGWriter pw, PNGSaveData savedata)
        {
            pw.WriteTDCG();
            pw.WriteSCNE(savedata.figures.Length);
            WriteCAMI(pw, savedata.CameraDescription);
            foreach (Figure fig in savedata.figures)
            {
                WriteLGTA(pw, fig.LampRotation);
                pw.WriteFTMO(fig.Tmo);
                WriteFIGU(pw, fig.SliderMatrix);
                foreach (TSOFile tso in fig.TsoList)
                    pw.WriteFTSO(tso);
            }
        }

        public void Save(string thumbnail_file, string dest_file, PNGSaveData savedata)
        {
            PNGFile png = new PNGFile();

            png.WriteTaOb += delegate(BinaryWriter bw)
            {
                PNGWriter pw = new PNGWriter(bw);
                WriteScne(pw, savedata);
            };

            png.Load(thumbnail_file);
            Debug.WriteLine("Save File: " + dest_file);
            Stopwatch sw = new Stopwatch();
            sw.Start();
            png.Save(dest_file);
            sw.Stop();
            Debug.WriteLine(dest_file + " write time: " + sw.Elapsed);
        }
    }
}
