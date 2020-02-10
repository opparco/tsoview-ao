using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace TDCG
{
    /// スライダ変形行列
    public class SliderMatrix
    {

        /// 変形行列 ChichiR1 0.0 着衣
        /// 変形行列 ChichiR2 0.0 着衣
        /// 変形行列 ChichiR3 0.0 着衣
        /// 変形行列 ChichiR1 0.0
        /// 変形行列 ChichiR2 0.0
        /// 変形行列 ChichiR3 0.0
        /// 変形行列 ChichiR4 0.0
        /// 変形行列 ChichiR5 0.0
        /// 変形行列 ChichiR5_end 0.0

        /// 変形行列 ChichiL1 0.0 着衣
        /// 変形行列 ChichiL2 0.0 着衣
        /// 変形行列 ChichiL3 0.0 着衣
        /// 変形行列 ChichiL1 0.0
        /// 変形行列 ChichiL2 0.0
        /// 変形行列 ChichiL3 0.0
        /// 変形行列 ChichiL4 0.0
        /// 変形行列 ChichiL5 0.0
        /// 変形行列 ChichiL5_End 0.0
        static readonly uint[] ChichiMinB = {
    //ChichiMinR

    //MMUL p 0019E780 p 11562878 p 0019E780 
    0x3F56C754, 0x00000000, 0x3DBE28B8, 0x00000000,
    0x3C69226E, 0x3F7DD95E, 0xBE03A8A4, 0x00000000,
    0xBD506C94, 0x3D772E50, 0x3EEB68AD, 0x00000000,
    0xBD600757, 0x40101B09, 0xBE9C753E, 0x3F800000,

    //MMUL p 0019E780 p 115628B8 p 0019E780 
    0x3F89104D, 0x00000000, 0xBE414595, 0x00000000,
    0x3CA0F17D, 0x3F7A9175, 0x3EBAD6AA, 0x00000000,
    0x3D75CF90, 0xBDBAFD6F, 0x3F8EAE29, 0x00000000,
    0xBECC022A, 0xBF988BEB, 0x3FB19C39, 0x3F800000,

    //MMUL p 0019E780 p 115628F8 p 0019E780 
    0x3F422681, 0x00000000, 0x00000000, 0x00000000,
    0x00000000, 0x3F317375, 0x00000000, 0x00000000,
    0x00000000, 0x00000000, 0x3FA636E3, 0x00000000,
    0xBE5B2357, 0xBF028F4B, 0x3EFBE76D, 0x3F800000,

    //MMUL p 0019E780 p 113B55B8 p 0019E780 
    0x3F56C754, 0x00000000, 0x3DBE28B8, 0x00000000,
    0x3C69226E, 0x3F7DD95E, 0xBE03A8A4, 0x00000000,
    0xBD506C94, 0x3D772E50, 0x3EEB68AD, 0x00000000,
    0xBD25DCEF, 0x400F0721, 0xBEDE275B, 0x3F800000,

    //MMUL p 0019E780 p 113B55F8 p 0019E780 
    0x3F6E719B, 0x00000000, 0xBE281CF8, 0x00000000,
    0x3C977D2A, 0x3F6BD945, 0x3EAFDD01, 0x00000000,
    0x3D75CF90, 0xBDBAFD6F, 0x3F8EAE29, 0x00000000,
    0xBECC022A, 0xBF988BEB, 0x3FB19C39, 0x3F800000,

    //MMUL p 0019E780 p 113B5638 p 0019E780 
    0x3F5F3494, 0x00000000, 0x00000000, 0x00000000,
    0x00000000, 0x3F3C869C, 0x00000000, 0x00000000,
    0x00000000, 0x00000000, 0x3FA636E3, 0x00000000,
    0xBE5B2357, 0xBF028F4B, 0x3EFBE76D, 0x3F800000,

    //MMUL p 0019E780 p 113B5678 p 0019E780 
    0x3FB9D884, 0x00000000, 0x00000000, 0x00000000,
    0x00000000, 0x3FB9D884, 0x00000000, 0x00000000,
    0x00000000, 0x00000000, 0x3FB9D884, 0x00000000,
    0xBDC28F6A, 0xBC8851DE, 0x3EC5844D, 0x3F800000,

    //MMUL p 0019E780 p 113B56B8 p 0019E780 
    0x3F800000, 0x00000000, 0x00000000, 0x00000000,
    0x00000000, 0x3F800000, 0x00000000, 0x00000000,
    0x00000000, 0x00000000, 0x3F800000, 0x00000000,
    0xBD831254, 0x3D39F285, 0x3E746AA1, 0x3F800000,

    //MMUL p 0019E780 p 113B56F8 p 0019E780 
    0x3F800000, 0x00000000, 0x00000000, 0x00000000,
    0x00000000, 0x3F800000, 0x00000000, 0x00000000,
    0x00000000, 0x00000000, 0x3F800000, 0x00000000,
    0x39947ADD, 0x3D4BAF96, 0x3DE1D3AA, 0x3F800000,

    //ChichiMinL

    //MMUL p 0019E780 p 115629F8 p 0019E780 
    0x3F56C765, 0xA1581703, 0xBDBE265C, 0x00000000,
    0xBC691F7E, 0x3F7DD95E, 0xBE03A8E7, 0x00000000,
    0x3D506A10, 0x3D772E50, 0x3EEB68AD, 0x00000000,
    0x3D6004D3, 0x40101B09, 0xBE9C751D, 0x3F800000,

    //MMUL p 0019E780 p 11562A38 p 0019E780 
    0x3F8908C4, 0x00000000, 0x3E413BE2, 0x00000000,
    0xBCA0F289, 0x3F7A9175, 0x3EBAD6AA, 0x00000000,
    0xBD75D13D, 0xBDBAFD6F, 0x3F8EAE29, 0x00000000,
    0x3ECC1659, 0xBF989763, 0x3FB19360, 0x3F800000,

    //MMUL p 0019E780 p 11562A78 p 0019E780 
    0x3F423162, 0x00000000, 0x00000000, 0x00000000,
    0x00000000, 0x3F317375, 0x00000000, 0x00000000,
    0x00000000, 0x00000000, 0x3FA636E3, 0x00000000,
    0x3E5B72C5, 0xBF0297D0, 0x3EFBF9E8, 0x3F800000,

    //MMUL p 0019E780 p 113B5738 p 0019E780 
    0x3F56C765, 0xA1581703, 0xBDBE265C, 0x00000000,
    0xBC691F7E, 0x3F7DD95E, 0xBE03A8E7, 0x00000000,
    0x3D506A10, 0x3D772E50, 0x3EEB68AD, 0x00000000,
    0x3D25DB26, 0x400F0721, 0xBEDE275B, 0x3F800000,

    //MMUL p 0019E780 p 113B5778 p 0019E780 
    0x3F6E643D, 0x00000000, 0x3E281494, 0x00000000,
    0xBC977E36, 0x3F6BD945, 0x3EAFDD01, 0x00000000,
    0xBD75D13D, 0xBDBAFD6F, 0x3F8EAE29, 0x00000000,
    0x3ECC1659, 0xBF989763, 0x3FB19360, 0x3F800000,

    //MMUL p 0019E780 p 113B57B8 p 0019E780 
    0x3F5F4107, 0x00000000, 0x00000000, 0x00000000,
    0x00000000, 0x3F3C869C, 0x00000000, 0x00000000,
    0x00000000, 0x00000000, 0x3FA636E3, 0x00000000,
    0x3E5B72C5, 0xBF0297D0, 0x3EFBF9E8, 0x3F800000,

    //MMUL p 0019E780 p 113B57F8 p 0019E780 
    0x3FB9D884, 0x00000000, 0x00000000, 0x00000000,
    0x00000000, 0x3FB9D884, 0x00000000, 0x00000000,
    0x00000000, 0x00000000, 0x3FB9D884, 0x00000000,
    0x3DC2F0EE, 0xBC8855A4, 0x3EC5844D, 0x3F800000,

    //MMUL p 0019E780 p 113B5838 p 0019E780 
    0x3F800000, 0x00000000, 0x00000000, 0x00000000,
    0x00000000, 0x3F800000, 0x00000000, 0x00000000,
    0x00000000, 0x00000000, 0x3F800000, 0x00000000,
    0x3D83E491, 0x3D39F1E4, 0x3E746AA1, 0x3F800000,

    //MMUL p 0019E780 p 113B5878 p 0019E780 
    0x3F800000, 0x00000000, 0x00000000, 0x00000000,
    0x00000000, 0x3F800000, 0x00000000, 0x00000000,
    0x00000000, 0x00000000, 0x3F800000, 0x00000000,
    0xB994445F, 0x3D4BAF96, 0x3DE1D324, 0x3F800000
};

        static float[] ChichiMinF;

        static void GetChichiMinM(ref Matrix m, int off)
        {
            m.M11 = ChichiMinF[off + 0];
            m.M12 = ChichiMinF[off + 1];
            m.M13 = ChichiMinF[off + 2];
            m.M14 = ChichiMinF[off + 3];

            m.M21 = ChichiMinF[off + 4];
            m.M22 = ChichiMinF[off + 5];
            m.M23 = ChichiMinF[off + 6];
            m.M24 = ChichiMinF[off + 7];

            m.M31 = ChichiMinF[off + 8];
            m.M32 = ChichiMinF[off + 9];
            m.M33 = ChichiMinF[off + 10];
            m.M34 = ChichiMinF[off + 11];

            m.M41 = ChichiMinF[off + 12];
            m.M42 = ChichiMinF[off + 13];
            m.M43 = ChichiMinF[off + 14];
            m.M44 = ChichiMinF[off + 15];
        }

        /// おっぱいスライダoppai_flat_ratioでのscaling factor
        public static Vector3 GetMinChichi()
        {
            return new Vector3(0.8350f, 0.8240f, 0.7800f);
        }

        /// おっぱいスライダ0.5でのscaling factor
        public static Vector3 GetMidChichi()
        {
            return new Vector3(1.0f, 1.0f, 1.0f);
        }

        /// おっぱいスライダ1.0でのscaling factor
        public static Vector3 GetMaxChichi()
        {
            return new Vector3(1.2500f, 1.3000f, 1.1800f);
        }

        // MinEyeL
        // MinEyeR
        // MaxEyeL
        // MaxEyeR
        static readonly uint[] EyeB = {
    //MMUL p 0019E660 p 0EE6DD18 p 0019E660 
    0x3F72D955, 0x3E1F6AE8, 0x3D1D7265, 0x00000000,
    0xBE1CD5C7, 0x3F7375E2, 0xBC24CBDD, 0x00000000,
    0xBD092242, 0x3CBBCB71, 0x3F7F1C44, 0x00000000,
    0xBB7DA800, 0x3DF0241C, 0x3CBA53BF, 0x3F800000,

    //MMUL p 0019E660 p 0EE6DD58 p 0019E660 
    0x3F72D949, 0xBE1F6A62, 0xBD1D72EC, 0x00000000,
    0x3E1CD540, 0x3F7375D9, 0xBC2C1643, 0x00000000,
    0x3D092285, 0x3CBBCC55, 0x3F7F1C3F, 0x00000000,
    0x3B7C4B00, 0x3DF01978, 0x3CBB51F8, 0x3F800000,

    //MMUL p 0019E660 p 0EE51BB0 p 0019E660 
    0x3F70F58B, 0xBE756B14, 0x3E141177, 0x00000000,
    0x3E7EB7D2, 0x3F73289C, 0xBD1779FC, 0x00000000,
    0xBE013C05, 0x3DA39804, 0x3F7CCCA8, 0x00000000,
    0xBE27D290, 0xBD6064A0, 0x3DAF2864, 0x3F800000,

    //MMUL p 0019E660 p 0EE51BF0 p 0019E660 
    0x3F70F55E, 0x3E756CF8, 0xBE1411F1, 0x00000000,
    0xBE7EB9B6, 0x3F73287E, 0xBD17B7ED, 0x00000000,
    0x3E013C41, 0x3DA398D2, 0x3F7CCC99, 0x00000000,
    0x3E27BF04, 0xBD60B158, 0x3DAF312C, 0x3F800000,
};

        static float[] EyeF;

        static void GetEyeM(ref Matrix m, int off)
        {
            m.M11 = EyeF[off + 0];
            m.M12 = EyeF[off + 1];
            m.M13 = EyeF[off + 2];
            m.M14 = EyeF[off + 3];

            m.M21 = EyeF[off + 4];
            m.M22 = EyeF[off + 5];
            m.M23 = EyeF[off + 6];
            m.M24 = EyeF[off + 7];

            m.M31 = EyeF[off + 8];
            m.M32 = EyeF[off + 9];
            m.M33 = EyeF[off + 10];
            m.M34 = EyeF[off + 11];

            m.M41 = EyeF[off + 12];
            m.M42 = EyeF[off + 13];
            m.M43 = EyeF[off + 14];
            m.M44 = EyeF[off + 15];
        }

        /// たれ目つり目スライダ0.0での変形
        public static void GetMinEyeR(ref Matrix m)
        {
            GetEyeM(ref m, 1 * 16);
        }

        /// たれ目つり目スライダ1.0での変形
        public static void GetMaxEyeR(ref Matrix m)
        {
            GetEyeM(ref m, 3 * 16);
        }

        /// たれ目つり目スライダ0.0での変形
        public static void GetMinEyeL(ref Matrix m)
        {
            GetEyeM(ref m, 0 * 16);
        }

        /// たれ目つり目スライダ1.0での変形
        public static void GetMaxEyeL(ref Matrix m)
        {
            GetEyeM(ref m, 2 * 16);
        }

        /// 指定割合に比例するscaling factorを得ます。
        public static Vector3 GetVector3Ratio(Vector3 min, Vector3 max, float ratio)
        {
            return Vector3.Lerp(min, max, ratio);
        }

        /// 指定割合に比例する変形行列を得ます。
        public static Matrix GetMatrixRatio(Vector3 min, Vector3 max, float ratio)
        {
            return Matrix.Scaling(Vector3.Lerp(min, max, ratio));
        }

        /// 指定割合に比例する変形行列を得ます。
        public static void GetMatrixRatio(out Matrix m, ref Matrix min, ref Matrix max, float ratio)
        {
            m.M11 = Helper.Lerp(min.M11, max.M11, ratio);
            m.M12 = Helper.Lerp(min.M12, max.M12, ratio);
            m.M13 = Helper.Lerp(min.M13, max.M13, ratio);
            m.M14 = Helper.Lerp(min.M14, max.M14, ratio);

            m.M21 = Helper.Lerp(min.M21, max.M21, ratio);
            m.M22 = Helper.Lerp(min.M22, max.M22, ratio);
            m.M23 = Helper.Lerp(min.M23, max.M23, ratio);
            m.M24 = Helper.Lerp(min.M24, max.M24, ratio);

            m.M31 = Helper.Lerp(min.M31, max.M31, ratio);
            m.M32 = Helper.Lerp(min.M32, max.M32, ratio);
            m.M33 = Helper.Lerp(min.M33, max.M33, ratio);
            m.M34 = Helper.Lerp(min.M34, max.M34, ratio);

            m.M41 = Helper.Lerp(min.M41, max.M41, ratio);
            m.M42 = Helper.Lerp(min.M42, max.M42, ratio);
            m.M43 = Helper.Lerp(min.M43, max.M43, ratio);
            m.M44 = Helper.Lerp(min.M44, max.M44, ratio);
        }

        /// 指定割合に比例する変形行列を得ます。
        public static Matrix GetMatrixRatio(Matrix min, Matrix max, float ratio)
        {
            Matrix m;
            GetMatrixRatio(out m, ref min, ref max, ratio);
            return m;
        }

        static SliderMatrix()
        {
            ChichiMinF = new float[ChichiMinB.Length];
            for (int i = 0; i < ChichiMinB.Length; i++)
            {
                byte[] bytes = BitConverter.GetBytes(ChichiMinB[i]);
                ChichiMinF[i] = BitConverter.ToSingle(bytes, 0);
            }

            EyeF = new float[EyeB.Length];
            for (int i = 0; i < EyeB.Length; i++)
            {
                byte[] bytes = BitConverter.GetBytes(EyeB[i]);
                EyeF[i] = BitConverter.ToSingle(bytes, 0);
            }
        }

        /// 拡大変位
        public Vector3 Local;
        /// 拡大変位
        public Vector3 FaceOya;

        /// 拡大変位
        public Vector3 SpineDummy;
        /// 拡大変位
        public Vector3 Spine1;

        /// 拡大変位
        public Vector3 HipsDummy;
        /// 拡大変位
        public Vector3 UpLeg;
        /// 拡大変位
        public Vector3 UpLegRoll;
        /// 拡大変位
        public Vector3 LegRoll;

        /// 拡大変位
        public Vector3 ArmDummy;
        /// 拡大変位
        public Vector3 Arm;

        /// 拡大変位
        public Vector3 Chichi;

        /// 変形行列
        public Matrix EyeR;
        /// 変形行列
        public Matrix EyeL;

        /// スライダ変形行列を生成します。
        public SliderMatrix()
        {
            ArmRatio = 0.5f;
            LegRatio = 0.5f;
            WaistRatio = 0.0f; //scaling factorから見て胴まわりの基準は0.0である
            OppaiRatio = 0.5f;
            AgeRatio = 0.5f;
            EyeRatio = 0.5f;
        }

        float arm_ratio;
        /// うでスライダ割合
        public float ArmRatio
        {
            get { return arm_ratio; }
            set
            {
                arm_ratio = value;
                ArmDummy = Vector3.Lerp(new Vector3(1.0f, 1.0f, 1.0f), new Vector3(1.0f, 1.1760f, 1.0f), arm_ratio);
                Arm = Vector3.Lerp(new Vector3(1.0f, 0.7350f, 1.0f), new Vector3(1.0f, 1.1760f, 1.0f), arm_ratio);
            }
        }

        float leg_ratio;
        /// あしスライダ割合
        public float LegRatio
        {
            get { return leg_ratio; }
            set
            {
                leg_ratio = value;
                HipsDummy = Vector3.Lerp(new Vector3(1.0f, 1.0f, 1.0f), new Vector3(1.2001f, 1.0f, 1.0f), leg_ratio);
                UpLeg = Vector3.Lerp(new Vector3(0.8091f, 1.0f, 0.8190f), new Vector3(1.2001f, 1.0f, 1.0f), leg_ratio);
                UpLegRoll = Vector3.Lerp(new Vector3(0.8091f, 1.0f, 0.8190f), new Vector3(1.2012f, 1.0f, 1.0f), leg_ratio);
                LegRoll = Vector3.Lerp(new Vector3(0.8091f, 1.0f, 0.8190f), new Vector3(0.9878f, 1.0f, 1.0f), leg_ratio);
            }
        }

        float waist_ratio;
        /// 胴まわりスライダ割合
        public float WaistRatio
        {
            get { return waist_ratio; }
            set
            {
                waist_ratio = value;
                SpineDummy = Vector3.Lerp(new Vector3(1.0f, 1.0f, 1.0f), new Vector3(1.0890f, 1.0f, 0.9230f), waist_ratio);
                Spine1 = Vector3.Lerp(new Vector3(1.0f, 1.0f, 1.0f), new Vector3(1.1800f, 1.0f, 1.0f), waist_ratio);
            }
        }

        float oppai_ratio;
        /// おっぱいスライダ割合
        public float OppaiRatio
        {
            get { return oppai_ratio; }
            set
            {
                oppai_ratio = value;

                if (Flat())
                {
                    Chichi = GetMinChichi();
                }
                else
                {
                    if (oppai_ratio < 0.5f)
                        Chichi = Vector3.Lerp(GetMinChichi(), GetMidChichi(), (oppai_ratio - oppai_flat_ratio) / (0.5f - oppai_flat_ratio));
                    else
                        Chichi = Vector3.Lerp(GetMidChichi(), GetMaxChichi(), (oppai_ratio - 0.5f) / (1.0f - 0.5f));
                }
            }
        }

        /// 貧乳境界割合
        static float oppai_flat_ratio = 0.20f; // 0.2250f ?

        /// 貧乳であるか
        bool Flat()
        {
            return oppai_ratio < oppai_flat_ratio;
        }

        float age_ratio;
        /// 姉妹スライダ割合
        public float AgeRatio
        {
            get { return age_ratio; }
            set
            {
                age_ratio = value;
                // linear
                {
                    float scale = Helper.Lerp(0.9520f, 1.0480f, age_ratio);
                    Local = new Vector3(scale, scale, scale);
                }
                // linear
                {
                    float scale = Helper.Lerp(1.2860f, 0.9230f, age_ratio);
                    FaceOya.X = scale;
                    FaceOya.Z = scale;
                }
                // linear ?
                {
                    float scale_1 = Helper.Lerp(1.2660f, 0.9230f, age_ratio);
                    float scale_2 = Helper.Lerp(0.8850f, 1.0600f, age_ratio);
                    FaceOya.Y = scale_1 * scale_2;
                }
            }
        }

        float eye_ratio;
        /// たれ目つり目スライダ割合
        public float EyeRatio
        {
            get { return eye_ratio; }
            set
            {
                eye_ratio = value;

                Matrix minEyeR = Matrix.Identity;
                Matrix maxEyeR = Matrix.Identity;
                Matrix minEyeL = Matrix.Identity;
                Matrix maxEyeL = Matrix.Identity;

                GetMaxEyeR(ref maxEyeR);
                GetMinEyeR(ref minEyeR);
                GetMaxEyeL(ref maxEyeL);
                GetMinEyeL(ref minEyeL);

                GetMatrixRatio(out EyeR, ref minEyeR, ref maxEyeR, eye_ratio);
                GetMatrixRatio(out EyeL, ref minEyeL, ref maxEyeL, eye_ratio);
            }
        }

        static readonly Dictionary<string, int> MinChichiClothedMap = new Dictionary<string, int>() {
        {"Chichi_Right1", 0 * 16},
        {"Chichi_Right2", 1 * 16},
        {"Chichi_Right3", 2 * 16},
        {"Chichi_Right4", 6 * 16},
        {"Chichi_Right5", 7 * 16},
        {"Chichi_Right5_end", 8 * 16},
        {"Chichi_Left1", (0+9) * 16},
        {"Chichi_Left2", (1+9) * 16},
        {"Chichi_Left3", (2+9) * 16},
        {"Chichi_Left4", (6+9) * 16},
        {"Chichi_Left5", (7+9) * 16},
        {"Chichi_Left5_End", (8+9) * 16}
    };

        static readonly Dictionary<string, int> MinChichiMap = new Dictionary<string, int>() {
        {"Chichi_Right1", 3 * 16},
        {"Chichi_Right2", 4 * 16},
        {"Chichi_Right3", 5 * 16},
        {"Chichi_Right4", 6 * 16},
        {"Chichi_Right5", 7 * 16},
        {"Chichi_Right5_end", 8 * 16},
        {"Chichi_Left1", (3+9) * 16},
        {"Chichi_Left2", (4+9) * 16},
        {"Chichi_Left3", (5+9) * 16},
        {"Chichi_Left4", (6+9) * 16},
        {"Chichi_Left5", (7+9) * 16},
        {"Chichi_Left5_End", (8+9) * 16}
    };

        /// 着衣扱いか
        public bool Clothed = false;

        /// おっぱい変形：貧乳を行います。
        void TransformChichiFlat(string name, ref Matrix m)
        {
            Dictionary<string, int> min_chichi_map;
            if (Clothed)
                min_chichi_map = MinChichiClothedMap;
            else
                min_chichi_map = MinChichiMap;
            Matrix c = Matrix.Identity;
            {
                int off;
                if (min_chichi_map.TryGetValue(name, out off))
                    GetChichiMinM(ref c, off);
            }
            GetMatrixRatio(out m, ref c, ref m, oppai_ratio / oppai_flat_ratio);
        }

        /// おっぱい変形を行います。
        void ScaleChichi(string name, ref Matrix m)
        {
            switch (name)
            {
                case "Chichi_Right1":
                case "Chichi_Left1":
                    Helper.Scale1(ref m, this.Chichi);
                    break;
                default:
                    m.M41 /= this.Chichi.X;
                    m.M42 /= this.Chichi.Y;
                    m.M43 /= this.Chichi.Z;
                    break;
            }
        }

        /// 表情変形を行います。
        public void TransformFace(string name, ref Matrix m)
        {
            switch (name)
            {
                case "face_oya":
                    Helper.Scale1(ref m, this.FaceOya);
                    break;
                case "eyeline_sita_L":
                case "L_eyeline_oya_L":
                case "Me_Right_Futi":
                    m *= this.EyeR;
                    break;
                case "eyeline_sita_R":
                case "R_eyeline_oya_R":
                case "Me_Left_Futi":
                    m *= this.EyeL;
                    break;
            }
        }

        /// 体型変形を行います。
        public void Scale(string name, ref Matrix m)
        {
            switch (name)
            {
                case "W_Spine_Dummy":
                    Helper.Scale1(ref m, this.SpineDummy);
                    break;
                case "W_Spine1":
                case "W_Spine2":
                    Helper.Scale1(ref m, this.Spine1);
                    break;

                case "W_LeftHips_Dummy":
                case "W_RightHips_Dummy":
                    Helper.Scale1(ref m, this.HipsDummy);
                    break;
                case "W_LeftUpLeg":
                case "W_RightUpLeg":
                    Helper.Scale1(ref m, this.UpLeg);
                    break;
                case "W_LeftUpLegRoll":
                case "W_RightUpLegRoll":
                case "W_LeftLeg":
                case "W_RightLeg":
                    Helper.Scale1(ref m, this.UpLegRoll);
                    break;
                case "W_LeftLegRoll":
                case "W_RightLegRoll":
                case "W_LeftFoot":
                case "W_RightFoot":
                case "W_LeftToeBase":
                case "W_RightToeBase":
                    Helper.Scale1(ref m, this.LegRoll);
                    break;

                case "W_LeftArm_Dummy":
                case "W_RightArm_Dummy":
                    Helper.Scale1(ref m, this.ArmDummy);
                    break;
                case "W_LeftArm":
                case "W_RightArm":
                case "W_LeftArmRoll":
                case "W_RightArmRoll":
                case "W_LeftForeArm":
                case "W_RightForeArm":
                case "W_LeftForeArmRoll":
                case "W_RightForeArmRoll":
                    Helper.Scale1(ref m, this.Arm);
                    break;
            }
        }

        static Regex re_chichi = new Regex(@"\AChichi");

        /// 体型変形を行います。
        /// ここで変形した行列は MatrixStack に入ります。
        public void Transform(string name, ref Matrix m)
        {
            bool chichi_p = re_chichi.IsMatch(name);

            if (chichi_p)
            {
                ScaleChichi(name, ref m);

                if (Flat())
                {
                    TransformChichiFlat(name, ref m);
                }
            }
            else
                TransformFace(name, ref m);
        }
        /// 体型変形を行います。
        /// ここで変形した行列は MatrixStack に入りません。
        public void TransformWithoutStack(string name, ref Matrix m)
        {
            bool chichi_p = re_chichi.IsMatch(name);

            if (!chichi_p)
                Scale(name, ref m);
        }
    }
}
