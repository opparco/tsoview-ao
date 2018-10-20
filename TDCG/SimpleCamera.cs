using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace TDCG
{
    /// <summary>
    /// カメラ
    /// </summary>
    public class SimpleCamera
    {
        //角度
        Vector3 angle;
        
        //回転中心
        Vector3 center;
        
        //位置変位
        Vector3 translation;
        
        //更新する必要があるか
        bool need_update;
        
        //view行列
        Matrix view;

        /// <summary>
        /// 角度
        /// </summary>
        public Vector3 Angle { get { return angle; } set { angle = value; } }

        /// <summary>
        /// 回転中心
        /// </summary>
        public Vector3 Center
        {
            get { return center; }
            set
            {
                center = value;
                need_update = true;
            }
        }

        /// <summary>
        /// 位置変位
        /// </summary>
        public Vector3 Translation
        {
            get { return translation; }
            set
            {
                translation = value;
                need_update = true;
            }
        }
    
        /// <summary>
        /// 更新する必要があるか
        /// </summary>
        public bool NeedUpdate { get { return need_update; } }

        /// <summary>
        /// view行列
        /// </summary>
        public Matrix ViewMatrix { get { return view; } }

        public Vector3 ViewTranslation { get { return new Vector3(-view.M41, -view.M42, -view.M43); } }

        /// 回転行列
        public Matrix RotationMatrix { get { return Matrix.RotationYawPitchRoll(angle.Y, angle.X, angle.Z); } }

        /// 回転quaternion
        public Quaternion RotationQuaternion { get { return Quaternion.RotationYawPitchRoll(angle.Y, angle.X, angle.Z); } }

        /// <summary>
        /// カメラを生成します。
        /// </summary>
        public SimpleCamera()
        {
            angle = Vector3.Empty;
            center = Vector3.Empty;
            translation = new Vector3(0.0f, 0.0f, +10.0f);
            need_update = true;
            view = Matrix.Identity;
        }

        /// <summary>
        /// カメラの位置と姿勢をリセットします。
        /// </summary>
        public void Reset()
        {
            center = Vector3.Empty;
            translation = new Vector3(0.0f, 0.0f, +10.0f);
            angle = Vector3.Empty;
            need_update = true;
        }

        /// <summary>
        /// view座標上のカメラの位置をリセットします。
        /// </summary>
        public void ResetTranslation()
        {
            translation = new Vector3(0.0f, 0.0f, +10.0f);
            need_update = true;
        }

        /// <summary>
        /// カメラの位置を更新します。
        /// </summary>
        /// <param name="dirX">移動方向（経度）</param>
        /// <param name="dirY">移動方向（緯度）</param>
        public void Move(float dirX, float dirY)
        {
            if (dirX == 0.0f && dirY == 0.0f)
                return;

            angle.Y -= dirX;
            angle.X += dirY;
            need_update = true;
        }

        /// <summary>
        /// カメラの位置と姿勢を更新します。
        /// </summary>
        public void Update()
        {
            if (!need_update)
                return;

            Matrix m = Matrix.RotationYawPitchRoll(angle.Y, angle.X, angle.Z);
            m.M41 = center.X;
            m.M42 = center.Y;
            m.M43 = center.Z;
            m.M44 = 1;

            view = Matrix.Invert(m) * Matrix.Translation(-translation);

            need_update = false;
        }

        /// <summary>
        /// view行列を取得します。
        /// </summary>
        public Matrix GetViewMatrix()
        {
            return view;
        }

        /// <summary>
        /// 回転中心を設定します。
        /// </summary>
        /// <param name="x">回転中心x座標</param>
        /// <param name="y">回転中心y座標</param>
        /// <param name="z">回転中心z座標</param>
        public void SetCenter(float x, float y, float z)
        {
            this.Center = new Vector3(x, y, z);
        }

        /// <summary>
        /// 位置変位を設定します。
        /// </summary>
        /// <param name="x">X変位</param>
        /// <param name="y">Y変位</param>
        /// <param name="z">Z変位</param>
        public void SetTranslation(float x, float y, float z)
        {
            this.Translation = new Vector3(x, y, z);
        }

        /// <summary>
        /// 角度を設定します。
        /// </summary>
        /// <param name="angle">角度</param>
        public void SetAngle(Vector3 angle)
        {
            this.angle = angle;
            need_update = true;
        }
        /// <summary>
        /// 角度を設定します。
        /// </summary>
        /// <param name="x">X軸回転角</param>
        /// <param name="y">Y軸回転角</param>
        /// <param name="z">Z軸回転角</param>
        public void SetAngle(float x, float y, float z)
        {
            SetAngle(new Vector3(x, y, z));
        }

        /// <summary>
        /// view座標上で移動します。
        /// </summary>
        /// <param name="dx">X軸移動距離</param>
        /// <param name="dy">Y軸移動距離</param>
        /// <param name="dz">Z軸移動距離</param>
        public void MoveView(float dx, float dy, float dz)
        {
            this.translation.X += dx;
            this.translation.Y += dy;
            this.translation.Z += dz;
            need_update = true;
        }
    }
}
