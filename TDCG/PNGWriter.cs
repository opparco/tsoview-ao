using System;
using System.Collections.Generic;
using System.IO;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Checksums;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace TDCG
{
    /// <summary>
    /// PNGFileを書き出すメソッド群
    /// </summary>
    public class PNGWriter
    {
        /// <summary>
        /// CSCチェックを行うオブジェクト
        /// </summary>
        protected static Crc32 crc = new Crc32();

        /// <summary>
        /// 指定ライタにbyte配列を書き出します。
        /// </summary>
        /// <param name="bw">ライタ</param>
        /// <param name="bytes">bute配列</param>
        public static void Write(BinaryWriter bw, byte[] bytes)
        {
            if (bw != null)
                bw.Write(bytes);
        }
        /// <summary>
        /// 指定ライタにチャンクを書き出します。
        /// </summary>
        /// <param name="bw">ライタ</param>
        /// <param name="type">チャンクタイプ</param>
        /// <param name="chunk_data">チャンク</param>
        public static void WriteChunk(BinaryWriter bw, string type, byte[] chunk_data)
        {
            byte[] buf = BitConverter.GetBytes((UInt32)chunk_data.Length);
            Array.Reverse(buf);
            Write(bw, buf);

            byte[] chunk_type = System.Text.Encoding.ASCII.GetBytes(type);
            Write(bw, chunk_type);
            Write(bw, chunk_data);

            crc.Reset();
            crc.Update(chunk_type);
            crc.Update(chunk_data);

            byte[] crc_buf = BitConverter.GetBytes((UInt32)crc.Value);
            Array.Reverse(crc_buf);
            Write(bw, crc_buf);
        }
        public static void WriteChunk(BinaryWriter bw, string type, byte[] chunk_data, int len)
        {
            byte[] buf = BitConverter.GetBytes((UInt32)len);
            Array.Reverse(buf);
            Write(bw, buf);

            byte[] chunk_type = System.Text.Encoding.ASCII.GetBytes(type);
            Write(bw, chunk_type);
            bw.BaseStream.Write(chunk_data, 0, len);

            crc.Reset();
            crc.Update(chunk_type);
            crc.Update(chunk_data, 0, len);

            byte[] crc_buf = BitConverter.GetBytes((UInt32)crc.Value);
            Array.Reverse(crc_buf);
            Write(bw, crc_buf);
        }
        /// <summary>
        /// 指定ライタにIHDRチャンクを書き出します。
        /// </summary>
        /// <param name="bw">ライタ</param>
        /// <param name="chunk_data">チャンク</param>
        public static void WriteIHDR(BinaryWriter bw, byte[] chunk_data)
        {
            WriteChunk(bw, "IHDR", chunk_data);
        }
        /// <summary>
        /// 指定ライタにIDATチャンクを書き出します。
        /// </summary>
        /// <param name="bw">ライタ</param>
        /// <param name="chunk_data">チャンク</param>
        public static void WriteIDAT(BinaryWriter bw, byte[] chunk_data)
        {
            WriteChunk(bw, "IDAT", chunk_data);
        }
        /// <summary>
        /// 指定ライタにIENDチャンクを書き出します。
        /// </summary>
        /// <param name="bw">ライタ</param>
        public static void WriteIEND(BinaryWriter bw)
        {
            WriteChunk(bw, "IEND", new byte[] {});
        }

        /// 書き出し先となるライタ
        protected BinaryWriter writer;

        /// PNGWriterを生成します。
        public PNGWriter(BinaryWriter bw)
        {
            this.writer = bw;
        }

        /// TaObチャンクを書き込みます。
        protected void WriteTaOb(string type, uint opt0, uint opt1, byte[] data)
        {
            //Console.WriteLine("WriteTaOb {0}", type);
            //Console.WriteLine("taOb extract length {0}", data.Length);

            using (MemoryStream dest = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(dest, System.Text.Encoding.ASCII))
                {
                    byte[] chunk_type = System.Text.Encoding.ASCII.GetBytes(type);
                    bw.Write(chunk_type);
                    bw.Write(opt0);
                    bw.Write(opt1);
                    bw.Write((UInt32)data.Length);
                    bw.Write((UInt32)dest.Length);

                    using (DeflaterOutputStream gzip = new DeflaterOutputStream(dest))
                    {
                        gzip.IsStreamOwner = false;
                        gzip.Write(data, 0, data.Length);
                    }
                    //Console.WriteLine("taOb length {0}", dest.Length);

                    PNGWriter.WriteChunk(writer, "taOb", dest.GetBuffer(), (int)dest.Length);
                }
            }
        }

        /// TaObチャンクを書き込みます。
        protected void WriteTaOb(string type, byte[] data)
        {
            WriteTaOb(type, 0, 0, data);
        }

        /// TDCGチャンクを書き込みます。
        public void WriteTDCG()
        {
            byte[] data = System.Text.Encoding.ASCII.GetBytes("$XP$");
            WriteTaOb("TDCG", data);
        }

        /// HSAVチャンクを書き込みます。
        public void WriteHSAV()
        {
            byte[] data = System.Text.Encoding.ASCII.GetBytes("$XP$");
            WriteTaOb("HSAV", data);
        }

        /// POSEチャンクを書き込みます。
        public void WritePOSE()
        {
            byte[] data = System.Text.Encoding.ASCII.GetBytes("$XP$");
            WriteTaOb("POSE", data);
        }

        /// SCNEチャンクを書き込みます。
        public void WriteSCNE(int figure_count)
        {
            byte[] data = System.Text.Encoding.ASCII.GetBytes("$XP$");
            WriteTaOb("SCNE", 0, (uint)figure_count, data);
        }

        /// CAMIチャンクを書き込みます。
        public void WriteCAMI(byte[] data)
        {
            WriteTaOb("CAMI", data);
        }

        /// LGTAチャンクを書き込みます。
        public void WriteLGTA(byte[] data)
        {
            WriteTaOb("LGTA", data);
        }

        /// FIGUチャンクを書き込みます。
        public void WriteFIGU(byte[] data)
        {
            WriteTaOb("FIGU", data);
        }

        /// ファイルを書き込みます。
        protected void WriteFile(string type, uint opt0, uint opt1, Stream source)
        {
            //Console.WriteLine("WriteTaOb {0}", type);
            //Console.WriteLine("taOb extract length {0}", source.Length);

            using (MemoryStream dest = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(dest, System.Text.Encoding.ASCII))
                {
                    byte[] chunk_type = System.Text.Encoding.ASCII.GetBytes(type);
                    bw.Write(chunk_type);
                    bw.Write(opt0);
                    bw.Write(opt1);
                    bw.Write((UInt32)source.Length);
                    bw.Write((UInt32)dest.Length);

                    using (DeflaterOutputStream gzip = new DeflaterOutputStream(dest))
                    {
                        gzip.IsStreamOwner = false;

                        byte[] buffer = new byte[4096];
                        StreamUtils.Copy(source, gzip, buffer);
                    }
                    //Console.WriteLine("taOb length {0}", dest.Length);

                    PNGWriter.WriteChunk(writer, "taOb", dest.GetBuffer(), (int)dest.Length);
                }
            }
        }

        /// FTMOチャンクを書き込みます。
        public void WriteFTMO(TMOFile tmo)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                tmo.Save(ms);
                ms.Seek(0, SeekOrigin.Begin);
                WriteFTMO(ms);
            }
        }

        /// FTMOチャンクを書き込みます。
        public void WriteFTMO(Stream stream)
        {
            WriteFile("FTMO", 0xADCFB72F, 0, stream);
        }

        /// FTSOチャンクを書き込みます。
        public void WriteFTSO(TSOFile tso)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                tso.Save(ms);
                ms.Seek(0, SeekOrigin.Begin);
                WriteFTSO(tso.Row, ms);
            }
        }

        /// FTSOチャンクを書き込みます。
        public void WriteFTSO(uint opt1, Stream stream)
        {
            WriteFile("FTSO", 0x26F5B8FE, opt1, stream);
        }
    }
}
