using System;
using System.Collections.Generic;
using System.Diagnostics;
//using System.Drawing;
using System.Threading;
//using System.ComponentModel;
using System.Windows.Forms;
using System.IO;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace TDCG
{
    /// スライダ変形行列
public class SliderMatrix
{
    /// 変形行列 ChichiR1 0.0 着衣
    public static void GetMinChichiR1Clothed(ref Matrix m)
    {
//Chichi_Right1
m.M11 = 0.838979f; m.M12 = 0.000000f; m.M13 = 0.092851f; m.M14 = 0.000000f;
m.M21 = 0.014229f; m.M22 = 0.991598f; m.M23 = -0.128573f; m.M24 = 0.000000f;
m.M31 = -0.050885f; m.M32 = 0.060347f; m.M33 = 0.459783f; m.M34 = 0.000000f;
m.M41 = -0.054701f; m.M42 = 2.251649f; m.M43 = -0.305583f; m.M44 = 1.000000f;
    }

    /// 変形行列 ChichiR2 0.0 着衣
    public static void GetMinChichiR2Clothed(ref Matrix m)
    {
//Chichi_Right2
m.M11 = 1.070810f; m.M12 = 0.000000f; m.M13 = -0.188741f; m.M14 = 0.000000f;
m.M21 = 0.019647f; m.M22 = 0.978782f; m.M23 = 0.364919f; m.M24 = 0.000000f;
m.M31 = 0.060012f; m.M32 = -0.091303f; m.M33 = 1.114690f; m.M34 = 0.000000f;
m.M41 = -0.398438f; m.M42 = -1.191769f; m.M43 = 1.387583f; m.M44 = 1.000000f;
    }

    /// 変形行列 ChichiR3 0.0 着衣
    public static void GetMinChichiR3Clothed(ref Matrix m)
    {
//Chichi_Right3
m.M11 = 0.758401f; m.M12 = 0.000000f; m.M13 = -0.000001f; m.M14 = 0.000000f;
m.M21 = -0.000001f; m.M22 = 0.693168f; m.M23 = 0.000000f; m.M24 = 0.000000f;
m.M31 = 0.000000f; m.M32 = 0.000000f; m.M33 = 1.298550f; m.M34 = 0.000000f;
m.M41 = -0.214013f; m.M42 = -0.509995f; m.M43 = 0.491998f; m.M44 = 1.000000f;
    }

    /// 変形行列 ChichiR1 0.0
    public static void GetMinChichiR1(ref Matrix m)
    {
//Chichi_Right1
m.M11 = 0.838979f; m.M12 = 0.000000f; m.M13 = 0.092851f; m.M14 = 0.000000f;
m.M21 = 0.014229f; m.M22 = 0.991598f; m.M23 = -0.128573f; m.M24 = 0.000000f;
m.M31 = -0.050885f; m.M32 = 0.060347f; m.M33 = 0.459783f; m.M34 = 0.000000f;
m.M41 = -0.040500f; m.M42 = 2.234809f; m.M43 = -0.433894f; m.M44 = 1.000000f;
    }

    /// 変形行列 ChichiR2 0.0
    public static void GetMinChichiR2(ref Matrix m)
    {
//Chichi_Right2
m.M11 = 0.931422f; m.M12 = 0.000000f; m.M13 = -0.164172f; m.M14 = 0.000000f;
m.M21 = 0.018493f; m.M22 = 0.921285f; m.M23 = 0.343482f; m.M24 = 0.000000f;
m.M31 = 0.060012f; m.M32 = -0.091303f; m.M33 = 1.114690f; m.M34 = 0.000000f;
m.M41 = -0.398447f; m.M42 = -1.191758f; m.M43 = 1.387569f; m.M44 = 1.000000f;
    }

    /// 変形行列 ChichiR3 0.0
    public static void GetMinChichiR3(ref Matrix m)
    {
//Chichi_Right3
m.M11 = 0.871895f; m.M12 = 0.000000f; m.M13 = -0.000002f; m.M14 = 0.000000f;
m.M21 = 0.000000f; m.M22 = 0.736429f; m.M23 = 0.000000f; m.M24 = 0.000000f;
m.M31 = 0.000000f; m.M32 = 0.000000f; m.M33 = 1.298550f; m.M34 = 0.000000f;
m.M41 = -0.214005f; m.M42 = -0.510005f; m.M43 = 0.492004f; m.M44 = 1.000000f;
    }

    /// 変形行列 ChichiR4 0.0
    public static void GetMinChichiR4(ref Matrix m)
    {
//Chichi_Right4
m.M11 = 1.451921f; m.M12 = 0.000000f; m.M13 = 0.000002f; m.M14 = 0.000000f;
m.M21 = 0.000001f; m.M22 = 1.451920f; m.M23 = 0.000000f; m.M24 = 0.000000f;
m.M31 = -0.000001f; m.M32 = 0.000000f; m.M33 = 1.451920f; m.M34 = 0.000000f;
m.M41 = -0.094991f; m.M42 = -0.016644f; m.M43 = 0.385771f; m.M44 = 1.000000f;
    }

    /// 変形行列 ChichiR5 0.0
    public static void GetMinChichiR5(ref Matrix m)
    {
//Chichi_Right5
m.M11 = 1.000000f; m.M12 = 0.000000f; m.M13 = 0.000000f; m.M14 = 0.000000f;
m.M21 = 0.000000f; m.M22 = 1.000000f; m.M23 = 0.000000f; m.M24 = 0.000000f;
m.M31 = 0.000000f; m.M32 = 0.000000f; m.M33 = 1.000000f; m.M34 = 0.000000f;
m.M41 = -0.064000f; m.M42 = 0.045398f; m.M43 = 0.238688f; m.M44 = 1.000000f;
    }

    /// 変形行列 ChichiR5_end 0.0
    public static void GetMinChichiR5E(ref Matrix m)
    {
//Chichi_Right5_end
m.M11 = 1.000000f; m.M12 = 0.000000f; m.M13 = 0.000000f; m.M14 = 0.000000f;
m.M21 = 0.000000f; m.M22 = 1.000000f; m.M23 = 0.000000f; m.M24 = 0.000000f;
m.M31 = 0.000000f; m.M32 = 0.000000f; m.M33 = 1.000000f; m.M34 = 0.000000f;
m.M41 = 0.000284f; m.M42 = 0.049729f; m.M43 = 0.110267f; m.M44 = 1.000000f;
    }

    /// 変形行列 ChichiL1 0.0 着衣
    public static void GetMinChichiL1Clothed(ref Matrix m)
    {
//Chichi_Left1
m.M11 = 0.838980f; m.M12 = 0.000000f; m.M13 = -0.092847f; m.M14 = 0.000000f;
m.M21 = -0.014229f; m.M22 = 0.991598f; m.M23 = -0.128574f; m.M24 = 0.000000f;
m.M31 = 0.050882f; m.M32 = 0.060347f; m.M33 = 0.459783f; m.M34 = 0.000000f;
m.M41 = 0.054688f; m.M42 = 2.251649f; m.M43 = -0.305581f; m.M44 = 1.000000f;
    }

    /// 変形行列 ChichiL2 0.0 着衣
    public static void GetMinChichiL2Clothed(ref Matrix m)
    {
//Chichi_Left2
m.M11 = 1.070580f; m.M12 = 0.000000f; m.M13 = 0.188707f; m.M14 = 0.000000f;
m.M21 = -0.019646f; m.M22 = 0.978782f; m.M23 = 0.364920f; m.M24 = 0.000000f;
m.M31 = -0.060013f; m.M32 = -0.091303f; m.M33 = 1.114691f; m.M34 = 0.000000f;
m.M41 = 0.398617f; m.M42 = -1.192122f; m.M43 = 1.387319f; m.M44 = 1.000000f;
    }

    /// 変形行列 ChichiL3 0.0 着衣
    public static void GetMinChichiL3Clothed(ref Matrix m)
    {
//Chichi_Left3
m.M11 = 0.758566f; m.M12 = -0.000001f; m.M13 = -0.000001f; m.M14 = 0.000000f;
m.M21 = -0.000001f; m.M22 = 0.693168f; m.M23 = -0.000001f; m.M24 = 0.000000f;
m.M31 = 0.000000f; m.M32 = 0.000000f; m.M33 = 1.298550f; m.M34 = 0.000000f;
m.M41 = 0.214296f; m.M42 = -0.510124f; m.M43 = 0.492126f; m.M44 = 1.000000f;
    }

    /// 変形行列 ChichiL1 0.0
    public static void GetMinChichiL1(ref Matrix m)
    {
//Chichi_Left1
m.M11 = 0.838980f; m.M12 = 0.000000f; m.M13 = -0.092847f; m.M14 = 0.000000f;
m.M21 = -0.014229f; m.M22 = 0.991598f; m.M23 = -0.128574f; m.M24 = 0.000000f;
m.M31 = 0.050882f; m.M32 = 0.060347f; m.M33 = 0.459783f; m.M34 = 0.000000f;
m.M41 = 0.040488f; m.M42 = 2.234809f; m.M43 = -0.433894f; m.M44 = 1.000000f;
    }

    /// 変形行列 ChichiL2 0.0
    public static void GetMinChichiL2(ref Matrix m)
    {
//Chichi_Left2
m.M11 = 0.931217f; m.M12 = 0.000000f; m.M13 = 0.164142f; m.M14 = 0.000000f;
m.M21 = -0.018493f; m.M22 = 0.921285f; m.M23 = 0.343482f; m.M24 = 0.000000f;
m.M31 = -0.060013f; m.M32 = -0.091303f; m.M33 = 1.114691f; m.M34 = 0.000000f;
m.M41 = 0.398606f; m.M42 = -1.192112f; m.M43 = 1.387297f; m.M44 = 1.000000f;
    }

    /// 変形行列 ChichiL3 0.0
    public static void GetMinChichiL3(ref Matrix m)
    {
//Chichi_Left3
m.M11 = 0.872086f; m.M12 = 0.000000f; m.M13 = 0.000000f; m.M14 = 0.000000f;
m.M21 = 0.000000f; m.M22 = 0.736429f; m.M23 = 0.000001f; m.M24 = 0.000000f;
m.M31 = 0.000000f; m.M32 = 0.000000f; m.M33 = 1.298550f; m.M34 = 0.000000f;
m.M41 = 0.214306f; m.M42 = -0.510132f; m.M43 = 0.492164f; m.M44 = 1.000000f;
    }

    /// 変形行列 ChichiL4 0.0
    public static void GetMinChichiL4(ref Matrix m)
    {
//Chichi_Left4
m.M11 = 1.451921f; m.M12 = 0.000000f; m.M13 = 0.000000f; m.M14 = 0.000000f;
m.M21 = 0.000001f; m.M22 = 1.451920f; m.M23 = -0.000001f; m.M24 = 0.000000f;
m.M31 = 0.000000f; m.M32 = 0.000000f; m.M33 = 1.451920f; m.M34 = 0.000000f;
m.M41 = 0.095201f; m.M42 = -0.016645f; m.M43 = 0.385758f; m.M44 = 1.000000f;
    }

    /// 変形行列 ChichiL5 0.0
    public static void GetMinChichiL5(ref Matrix m)
    {
//Chichi_Left5
m.M11 = 1.000000f; m.M12 = 0.000000f; m.M13 = 0.000000f; m.M14 = 0.000000f;
m.M21 = 0.000000f; m.M22 = 1.000000f; m.M23 = 0.000000f; m.M24 = 0.000000f;
m.M31 = 0.000000f; m.M32 = 0.000000f; m.M33 = 1.000000f; m.M34 = 0.000000f;
m.M41 = 0.064401f; m.M42 = 0.045399f; m.M43 = 0.238688f; m.M44 = 1.000000f;
    }

    /// 変形行列 ChichiL5_End 0.0
    public static void GetMinChichiL5E(ref Matrix m)
    {
//Chichi_Left5_End
m.M11 = 1.000000f; m.M12 = 0.000000f; m.M13 = 0.000000f; m.M14 = 0.000000f;
m.M21 = 0.000000f; m.M22 = 1.000000f; m.M23 = 0.000000f; m.M24 = 0.000000f;
m.M31 = 0.000000f; m.M32 = 0.000000f; m.M33 = 1.000000f; m.M34 = 0.000000f;
m.M41 = -0.000283f; m.M42 = 0.049725f; m.M43 = 0.110266f; m.M44 = 1.000000f;
    }

    /// おっぱいスライダFlatRatioでのscaling factor
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

    /// たれ目つり目スライダ0.0での変形
    public static Matrix GetMinEyeR()
    {
        Matrix m = Matrix.Identity;
        m.M11 = 1.04776F; m.M12 = -0.165705F; m.M13 = -0.042457F; m.M14 = 0;
        m.M21 = 0.169162F; m.M22 = 1.012264F; m.M23 = -0.011601F; m.M24 = 0;
        m.M31 = 0.036979F; m.M32 = 0.024401F; m.M33 = 1.100661F; m.M34 = 0;
        m.M41 = 0.004252F; m.M42 = 0.124786F; m.M43 = 0.025256F; m.M44 = 1;
        return m;
    }

    /// たれ目つり目スライダ1.0での変形
    public static Matrix GetMaxEyeR()
    {
        Matrix m = Matrix.Identity;
        m.M11 = 1.039604F; m.M12 = 0.255108F; m.M13 = -0.15971F; m.M14 = 0;
        m.M21 = -0.27475F; m.M22 = 1.011007F; m.M23 = -0.040911F; m.M24 = 0;
        m.M31 = 0.139395F; m.M32 = 0.085025F; m.M33 = 1.090691F; m.M34 = 0;
        m.M41 = 0.180933F; m.M42 = -0.058389F; m.M43 = 0.094482F; m.M44 = 1;
        return m;
    }

    /// たれ目つり目スライダ0.0での変形
    public static Matrix GetMinEyeL()
    {
        Matrix m = Matrix.Identity;
        m.M11 = 1.04776F; m.M12 = 0.165707F; m.M13 = 0.042456F; m.M14 = 0;
        m.M21 = -0.169164F; m.M22 = 1.012264F; m.M23 = -0.01111F; m.M24 = 0;
        m.M31 = -0.036979F; m.M32 = 0.0244F; m.M33 = 1.100662F; m.M34 = 0;
        m.M41 = -0.004275F; m.M42 = 0.124808F; m.M43 = 0.025122F; m.M44 = 1;
        return m;
    }

    /// たれ目つり目スライダ1.0での変形
    public static Matrix GetMaxEyeL()
    {
        Matrix m = Matrix.Identity;
        m.M11 = 1.039607F; m.M12 = -0.255101F; m.M13 = 0.159708F; m.M14 = 0;
        m.M21 = 0.274743F; m.M22 = 1.01101F; m.M23 = -0.040846F; m.M24 = 0;
        m.M31 = -0.139394F; m.M32 = 0.085025F; m.M33 = 1.090691F; m.M34 = 0;
        m.M41 = -0.181016F; m.M42 = -0.058312F; m.M43 = 0.094464F; m.M44 = 1;
        return m;
    }

    /// 指定比率に比例するscaling factorを得ます。
    public static Vector3 GetVector3Ratio(Vector3 min, Vector3 max, float ratio)
    {
        return Vector3.Lerp(min, max, ratio);
    }

    /// 指定比率に比例する変形行列を得ます。
    public static Matrix GetMatrixRatio(Vector3 min, Vector3 max, float ratio)
    {
        return Matrix.Scaling(Vector3.Lerp(min, max, ratio));
    }

    /// 指定比率に比例する変形行列を得ます。
    public static Matrix GetMatrixRatio(Matrix min, Matrix max, float ratio)
    {
        Matrix m;

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

        return m;
    }

    /// face_oyaの変形行列
    public static Matrix FaceOyaDefault;

    static SliderMatrix()
    {
        FaceOyaDefault = Matrix.Scaling(1.1045F, 1.064401F, 1.1045F);
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
    /// うでスライダ比率
    public float ArmRatio
    {
        get { return arm_ratio; }
        set {
            arm_ratio = value;
            ArmDummy    = Vector3.Lerp(new Vector3(1.0f, 1.0f, 1.0f), new Vector3(1.0f, 1.1760f, 1.0f), arm_ratio);
            Arm         = Vector3.Lerp(new Vector3(1.0f, 0.7350f, 1.0f), new Vector3(1.0f, 1.1760f, 1.0f), arm_ratio);
        }
    }

    float leg_ratio;
    /// あしスライダ比率
    public float LegRatio
    {
        get { return leg_ratio; }
        set
        {
            leg_ratio = value;
            HipsDummy   = Vector3.Lerp(new Vector3(1.0f, 1.0f, 1.0f), new Vector3(1.2001f, 1.0f, 1.0f), leg_ratio);
            UpLeg       = Vector3.Lerp(new Vector3(0.8091f, 1.0f, 0.8190f), new Vector3(1.2001f, 1.0f, 1.0f), leg_ratio);
            UpLegRoll   = Vector3.Lerp(new Vector3(0.8091f, 1.0f, 0.8190f), new Vector3(1.2012f, 1.0f, 1.0f), leg_ratio);
            LegRoll     = Vector3.Lerp(new Vector3(0.8091f, 1.0f, 0.8190f), new Vector3(0.9878f, 1.0f, 1.0f), leg_ratio);
        }
    }

    float waist_ratio;
    /// 胴まわりスライダ比率
    public float WaistRatio
    {
        get { return waist_ratio; }
        set
        {
            waist_ratio = value;
            SpineDummy  = Vector3.Lerp(new Vector3(1.0f, 1.0f, 1.0f), new Vector3(1.0890f, 1.0f, 0.9230f), waist_ratio);
            Spine1      = Vector3.Lerp(new Vector3(1.0f, 1.0f, 1.0f), new Vector3(1.1800f, 1.0f, 1.0f), waist_ratio);
        }
    }

    float oppai_ratio;
    /// おっぱいスライダ比率
    public float OppaiRatio
    {
        get { return oppai_ratio; }
        set
        {
            oppai_ratio = value;
            
            if (Flat())
            {
                Chichi = Vector3.Lerp(new Vector3(1.0f, 1.0f, 1.0f), GetMinChichi(), oppai_ratio / FlatRatio);
            }
            else
            {
                if (oppai_ratio < 0.5f)
                    Chichi = Vector3.Lerp(GetMinChichi(), GetMidChichi(), (oppai_ratio - FlatRatio) / (0.5f - FlatRatio));
                else
                    Chichi = Vector3.Lerp(GetMidChichi(), GetMaxChichi(), (oppai_ratio - 0.5f) / (1.0f - 0.5f));
            }
        }
    }

    /// 貧乳であるか
    public bool Flat()
    {
        return oppai_ratio < FlatRatio;
    }

    /// 貧乳境界比率
    public static float FlatRatio = 0.20f; // 0.2250f ?

    float age_ratio;
    /// 姉妹スライダ比率
    public float AgeRatio
    {
        get { return age_ratio; }
        set
        {
            age_ratio = value;
            // linear
            {
                const float a = 0.9520f;
                const float b = 1.0480f;
                float scale = a + (b - a) * age_ratio;
                Local = new Vector3(scale, scale, scale);
            }
            // linear
            {
                const float a = 1.2860f;
                const float b = 0.9230f;
                float scale = a + (b - a) * age_ratio;
                FaceOya.X = scale;
                FaceOya.Z = scale;
            }
            // linear ?
            {
                const float a = 1.2660f * 0.8850f;
                const float b = 0.9230f * 1.0600f;
                float scale = a + (b - a) * age_ratio;
                FaceOya.Y = scale;
            }
        }
    }

    float eye_ratio;
    /// たれ目つり目スライダ比率
    public float EyeRatio
    {
        get { return eye_ratio; }
        set {
            eye_ratio = value;
            EyeR = GetMatrixRatio(GetMinEyeR(), GetMaxEyeR(), eye_ratio) * Matrix.Invert(FaceOyaDefault);
            EyeL = GetMatrixRatio(GetMinEyeL(), GetMaxEyeL(), eye_ratio) * Matrix.Invert(FaceOyaDefault);
        }
    }

    /// おっぱい変形：貧乳着衣を行います。
    public void TransformChichiFlatClothed(TMONode tmo_node, ref Matrix m)
    {
        float ratio = oppai_ratio / FlatRatio;

        Matrix c = Matrix.Identity;

        switch (tmo_node.Name)
        {
            case "Chichi_Right1":
                GetMinChichiR1Clothed(ref c);
                break;
            case "Chichi_Right2":
                GetMinChichiR2Clothed(ref c);
                break;
            case "Chichi_Right3":
                GetMinChichiR3Clothed(ref c);
                break;
            case "Chichi_Right4":
                GetMinChichiR4(ref c);
                break;
            case "Chichi_Right5":
                GetMinChichiR5(ref c);
                break;
            case "Chichi_Right5_end":
                GetMinChichiR5E(ref c);
                break;
            case "Chichi_Left1":
                GetMinChichiL1Clothed(ref c);
                break;
            case "Chichi_Left2":
                GetMinChichiL2Clothed(ref c);
                break;
            case "Chichi_Left3":
                GetMinChichiL3Clothed(ref c);
                break;
            case "Chichi_Left4":
                GetMinChichiL4(ref c);
                break;
            case "Chichi_Left5":
                GetMinChichiL5(ref c);
                break;
            case "Chichi_Left5_End":
                GetMinChichiL5E(ref c);
                break;
        }
        m = GetMatrixRatio(c, m, ratio);
    }

    /// おっぱい変形：貧乳を行います。
    public void TransformChichiFlat(TMONode tmo_node, ref Matrix m)
    {
        float ratio = oppai_ratio / FlatRatio;

        Matrix c = Matrix.Identity;

        switch (tmo_node.Name)
        {
            case "Chichi_Right1":
                GetMinChichiR1(ref c);
                break;
            case "Chichi_Right2":
                GetMinChichiR2(ref c);
                break;
            case "Chichi_Right3":
                GetMinChichiR3(ref c);
                break;
            case "Chichi_Right4":
                GetMinChichiR4(ref c);
                break;
            case "Chichi_Right5":
                GetMinChichiR5(ref c);
                break;
            case "Chichi_Right5_end":
                GetMinChichiR5E(ref c);
                break;
            case "Chichi_Left1":
                GetMinChichiL1(ref c);
                break;
            case "Chichi_Left2":
                GetMinChichiL2(ref c);
                break;
            case "Chichi_Left3":
                GetMinChichiL3(ref c);
                break;
            case "Chichi_Left4":
                GetMinChichiL4(ref c);
                break;
            case "Chichi_Left5":
                GetMinChichiL5(ref c);
                break;
            case "Chichi_Left5_End":
                GetMinChichiL5E(ref c);
                break;
        }
        m = GetMatrixRatio(c, m, ratio);
    }

    /// おっぱい変形を行います。
    public void ScaleChichi(TMONode tmo_node, ref Matrix m)
    {
        switch (tmo_node.Name)
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
    public void TransformFace(TMONode tmo_node, ref Matrix m)
    {
        switch (tmo_node.Name)
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
    public void Scale(TMONode tmo_node, ref Matrix m)
    {
        switch (tmo_node.Name)
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
}
}
