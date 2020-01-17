using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace TDCG.Editor
{
    /// カメラ設定
    public class CameraConfig
    {
        /// 視野角を変更すると呼ばれる
        public event EventHandler ChangeFovy;
        /// 回転角を変更すると呼ばれる
        public event EventHandler ChangeRoll;

        float fovy = (float)(Math.PI / 6.0);
        float roll = 0;

        /// カメラのY軸（垂直）方向の視野角を得ます。単位は radian です。
        public float Fovy
        {
            get { return fovy; }
        }

        /// カメラのZ軸回転角を得ます。単位は radian です。
        public float Roll
        {
            get { return roll; }
        }

        /// 視野角を得ます。単位は degree です。
        public float GetFovyDegree()
        {
            return (float)(this.fovy * 180.0 / Math.PI);
        }

        /// 視野角を設定します。単位は degree です。
        public void SetFovyDegree(float fovy)
        {
            this.fovy = (float)(Math.PI * fovy / 180.0);
            //Console.WriteLine("update fovy {0} rad", this.fovy);
            if (ChangeFovy != null)
                ChangeFovy(this, EventArgs.Empty);
        }

        /// 回転角を得ます。単位は degree です。
        public float GetRollDegree()
        {
            return (float)(this.roll * 180.0 / Math.PI);
        }

        /// 回転角を設定します。単位は degree です。
        public void SetRollDegree(float roll)
        {
            this.roll = (float)(Math.PI * roll / 180.0);
            //Console.WriteLine("update roll {0} rad", this.roll);
            if (ChangeRoll != null)
                ChangeRoll(this, EventArgs.Empty);
        }
    }

    /// 深度マップ設定
    public class DepthMapConfig
    {
        /// zn を変更すると呼ばれる
        public event EventHandler ChangeZnearPlane;
        /// zf を変更すると呼ばれる
        public event EventHandler ChangeZfarPlane;

        float zn = 15.0f;
        float zf = 50.0f;

        /// 近クリップ面までの距離
        public float ZnearPlane
        {
            get { return zn; }
            set
            {
                zn = value;
                if (ChangeZnearPlane != null)
                    ChangeZnearPlane(this, EventArgs.Empty);
            }
        }

        /// 遠クリップ面までの距離
        public float ZfarPlane
        {
            get { return zf; }
            set
            {
                zf = value;
                if (ChangeZfarPlane != null)
                    ChangeZfarPlane(this, EventArgs.Empty);
            }
        }

        public float Zdistance
        {
            get { return zf - zn; }
        }
    }

    /// occlusion 設定
    public class OcclusionConfig
    {
        /// 強度を変更すると呼ばれる
        public event EventHandler ChangeIntensity;
        /// 半径を変更すると呼ばれる
        public event EventHandler ChangeRadius;

        float intensity = 0.5f;
        float radius = 2.5f;

        /// 強度
        public float Intensity
        {
            get { return intensity; }
            set
            {
                intensity = value;
                if (ChangeIntensity != null)
                    ChangeIntensity(this, EventArgs.Empty);
            }
        }

        /// 半径
        public float Radius
        {
            get { return radius; }
            set
            {
                radius = value;
                if (ChangeRadius != null)
                    ChangeRadius(this, EventArgs.Empty);
            }
        }
    }

    /// diffusion 設定
    public class DiffusionConfig
    {
        /// 強度を変更すると呼ばれる
        public event EventHandler ChangeIntensity;
        /// 範囲を変更すると呼ばれる
        public event EventHandler ChangeExtent;

        float intensity = 0.5f;
        float extent = 2.0f;

        /// 強度
        public float Intensity
        {
            get { return intensity; }
            set
            {
                intensity = value;
                if (ChangeIntensity != null)
                    ChangeIntensity(this, EventArgs.Empty);
            }
        }

        /// 範囲
        public float Extent
        {
            get { return extent; }
            set
            {
                extent = value;
                if (ChangeExtent != null)
                    ChangeExtent(this, EventArgs.Empty);
            }
        }
    }
}
