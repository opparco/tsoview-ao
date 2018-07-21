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

        float lamp_circle_scale;

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
        }

        // on device reset
        public void Create(Rectangle device_rect, int lamp_radius)
        {
            circle.Create();
            pole.Create();

            lamp_circle_scale = lamp_radius * 2.0f / device_rect.Height;
        }

        ProjectionMode projection_mode = ProjectionMode.Perspective;

        /// camera回転行列
        Matrix camera_rotation = Matrix.Identity;

        /// view変換行列
        Matrix transform_view = Matrix.Identity;

        /// projection変換行列
        Matrix transform_projection = Matrix.Identity;

        public void SetTransform(ProjectionMode projection_mode, ref Matrix camera_rotation, ref Matrix view, ref Matrix proj)
        {
            this.projection_mode = projection_mode;
            this.camera_rotation = camera_rotation;
            this.transform_view = view;
            this.transform_projection = proj;
        }

        void GetWorldViewMatrix(float scale, ref Vector3 world_position, ref Matrix world_rotation, ref Matrix world, out Matrix world_view_matrix)
        {
            Matrix world_matrix = Matrix.Scaling(scale, scale, scale) * world_rotation;

            // translation
            world_matrix.M41 = world_position.X;
            world_matrix.M42 = world_position.Y;
            world_matrix.M43 = world_position.Z;

            world_view_matrix = world_matrix * world * transform_view;
        }

        float UnprojectScaling(ref Matrix world_view_matrix)
        {
            float d;
            if (projection_mode == ProjectionMode.Ortho)
                d = 1.0f;
            else
                d = -world_view_matrix.M43;
            return d / transform_projection.M22;
        }

        void DrawLampPole(ref Vector3 world_position, ref Matrix world_rotation, ref Matrix world)
        {
            float scale = lamp_circle_scale;
            Matrix world_view_matrix;
            GetWorldViewMatrix(scale, ref world_position, ref world_rotation, ref world, out world_view_matrix);
            scale *= UnprojectScaling(ref world_view_matrix);
            GetWorldViewMatrix(scale, ref world_position, ref world_rotation, ref world, out world_view_matrix);
            Matrix world_view_projection_matrix = world_view_matrix * transform_projection;

            effect_pole.SetValue("wvp", world_view_projection_matrix);
            effect_pole.SetValue("col", new Vector4(1.0f, 1.0f, 1.0f, 1.0f));

            pole.Draw(effect_pole);
        }

        void DrawLampCircleW(ref Vector3 world_position, ref Matrix world)
        {
            float scale = lamp_circle_scale;
            Matrix world_view_matrix;
            GetWorldViewMatrix(scale, ref world_position, ref camera_rotation, ref world, out world_view_matrix);
            scale *= UnprojectScaling(ref world_view_matrix);
            GetWorldViewMatrix(scale, ref world_position, ref camera_rotation, ref world, out world_view_matrix);
            Matrix world_view_projection_matrix = world_view_matrix * transform_projection;

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

