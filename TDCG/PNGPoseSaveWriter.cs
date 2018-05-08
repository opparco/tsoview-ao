using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using Microsoft.DirectX;

namespace TDCG
{
    /// ポーズセーブをファイルに書き出す
    public class PNGPoseSaveWriter
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

        void WritePose(PNGWriter pw, PNGSaveData savedata)
        {
            pw.WriteTDCG();
            pw.WritePOSE();
            WriteCAMI(pw, savedata.CameraDescription);
            Figure fig = savedata.figures[0];
            WriteLGTA(pw, fig.LampRotation);
            pw.WriteFTMO(fig.Tmo);
        }

        public void Save(string thumbnail_file, string dest_file, PNGSaveData savedata)
        {
            PNGFile png = new PNGFile();

            png.WriteTaOb += delegate(BinaryWriter bw)
            {
                PNGWriter pw = new PNGWriter(bw);
                WritePose(pw, savedata);
            };

            png.Load(thumbnail_file);
            Debug.WriteLine("Save File: " + dest_file);
            png.Save(dest_file);
        }
    }
}
