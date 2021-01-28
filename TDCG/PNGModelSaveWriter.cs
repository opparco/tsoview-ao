using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using Microsoft.DirectX;

namespace TDCG
{
    /// モデルセーブをファイルに書き出す
    public class PNGModelSaveWriter
    {
        protected void WriteHsav(PNGWriter pw, PNGSaveData savedata)
        {
            pw.WriteTDCG();
            pw.WriteHSAV();
            Figure fig = savedata.figures[0];
            foreach (TSOFile tso in fig.TsoList)
                pw.WriteFTSO(tso);
        }

        public void Save(string thumbnail_file, string dest_file, PNGSaveData savedata)
        {
            PNGFile png = new PNGFile();

            png.WriteTaOb += delegate(BinaryWriter bw)
            {
                PNGWriter pw = new PNGWriter(bw);
                WriteHsav(pw, savedata);
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
