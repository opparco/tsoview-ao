using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace TDCG
{
    /// モデル上に重ねてランプを描画する
    public class LampRenderer
    {
        public Effect effect_circle;
        public Effect effect_pole;

        Circle circle = null;
        Pole pole = null;

        public LampRenderer(Device device, Sprite sprite)
        {
            circle = new Circle(device);
            pole = new Pole(device);
        }

        // on device lost
        public void Dispose()
        {
            Debug.WriteLine("LampRenderer.Dispose");

            if (pole != null)
                pole.Dispose();
            if (circle != null)
                circle.Dispose();

            if (effect_pole != null)
                effect_pole.Dispose();
            if (effect_circle != null)
                effect_circle.Dispose();
        }

        // on device reset
        public void Create(Rectangle device_rect)
        {
            circle.Create();
            pole.Create();
        }

        /// camera回転行列
        public Matrix Rotation_Camera = Matrix.Identity;

        /// view変換行列
        public Matrix Transform_View = Matrix.Identity;

        /// projection変換行列
        public Matrix Transform_Projection = Matrix.Identity;

        void GetWorldViewMatrix(float scale, ref Vector3 world_position, ref Matrix world_rotation, ref Matrix world, out Matrix world_view_matrix)
        {
            Matrix world_matrix = Matrix.Scaling(scale, scale, scale) * world_rotation;

            // translation
            world_matrix.M41 = world_position.X;
            world_matrix.M42 = world_position.Y;
            world_matrix.M43 = world_position.Z;

            world_view_matrix = world_matrix * world * Transform_View;
        }

        float UnprojectScaling(ref Matrix world_view_matrix)
        {
            return -world_view_matrix.M43 / Transform_Projection.M22;
        }

        void DrawLampPole(ref Vector3 world_position, ref Matrix world_rotation, ref Matrix world)
        {
            float scale = 0.046875f; //= 36/768
            Matrix world_view_matrix;
            GetWorldViewMatrix(scale, ref world_position, ref world_rotation, ref world, out world_view_matrix);
            scale *= UnprojectScaling(ref world_view_matrix);
            GetWorldViewMatrix(scale, ref world_position, ref world_rotation, ref world, out world_view_matrix);
            Matrix world_view_projection_matrix = world_view_matrix * Transform_Projection;

            effect_pole.SetValue("wvp", world_view_projection_matrix);
            effect_pole.SetValue("col", new Vector4(1.0f, 1.0f, 1.0f, 1.0f));

            pole.Draw(effect_pole);
        }

        void DrawLampCircleW(ref Vector3 world_position, ref Matrix world)
        {
            float scale = 0.046875f; //= 36/768
            Matrix world_view_matrix;
            GetWorldViewMatrix(scale, ref world_position, ref Rotation_Camera, ref world, out world_view_matrix);
            scale *= UnprojectScaling(ref world_view_matrix);
            GetWorldViewMatrix(scale, ref world_position, ref Rotation_Camera, ref world, out world_view_matrix);
            Matrix world_view_projection_matrix = world_view_matrix * Transform_Projection;

            effect_circle.SetValue("wvp", world_view_projection_matrix);
            effect_circle.SetValue("col", new Vector4(1.0f, 1.0f, 0.0f, 1.0f));

            circle.Draw(effect_circle);
        }

        //モデル上に重ねてランプを描画する
        //@param fig: ランプを描画するモデル
        public void Render(Figure fig, ref Vector3 world_position, ref Matrix world)
        {
            Matrix world_rotation = Matrix.RotationQuaternion(fig.LampRotation);
            DrawLampPole(ref world_position, ref world_rotation, ref world);
            DrawLampCircleW(ref world_position, ref world);
        }
    }
}

