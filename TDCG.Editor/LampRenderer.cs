using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace TDCG.Editor
{
    /// ランプを描画する
    public class LampRenderer
    {
        Device device;

        public Effect effect_circle;
        public Effect effect_pole;

        Circle circle = null;
        Pole pole = null;

        Rectangle device_rect;
        float lamp_radius;
        Point lamp_center;
#if false
        float lamp_circle_scale;
#endif

        public LampRenderer(Device device, Sprite sprite)
        {
            this.device = device;

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
        public void Create(Rectangle device_rect, float lamp_radius, Point lamp_center)
        {
            circle.Create();
            pole.Create();

            this.device_rect = device_rect;
            this.lamp_radius = lamp_radius;
            this.lamp_center = lamp_center;
#if false
            lamp_circle_scale = lamp_radius * 2.0f / device_rect.Height;
#endif
        }

        ProjectionMode projection_mode = ProjectionMode.Perspective;

        /// カメラ回転行列
        Matrix camera_rotation = Matrix.Identity;

        /// ランプ回転行列
        Matrix lamp_rotation = Matrix.Identity;

        public void SetTransform(ProjectionMode projection_mode, ref Matrix camera_rotation, ref Matrix lamp_rotation)
        {
            this.projection_mode = projection_mode;
            this.camera_rotation = camera_rotation;
            this.lamp_rotation = lamp_rotation;
        }

        void DrawLampPole()
        {
            float scale = lamp_radius;
            Matrix lamp_scaling = Matrix.Scaling(scale, scale, scale);

            Matrix world_view_matrix = lamp_rotation * Matrix.Invert(camera_rotation);
            world_view_matrix.M43 = -1.0f;
            world_view_matrix = world_view_matrix * lamp_scaling;
            world_view_matrix.M41 = -device_rect.Width / 2 + lamp_center.X;
            world_view_matrix.M42 = device_rect.Height / 2 - lamp_center.Y;

            Matrix proj = Matrix.OrthoRH(device_rect.Width, device_rect.Height, 1.0f, 500.0f);
            Matrix world_view_projection_matrix = world_view_matrix * proj;

            effect_pole.SetValue("wvp", world_view_projection_matrix);
            effect_pole.SetValue("col", new Vector4(1.0f, 1.0f, 1.0f, 1.0f));

            pole.Draw(effect_pole);
        }

        void DrawLampCircleW()
        {
            float scale = lamp_radius;
            Matrix lamp_scaling = Matrix.Scaling(scale, scale, scale);

            Matrix world_view_matrix = Matrix.Identity;
            world_view_matrix.M43 = -1.0f;
            world_view_matrix = world_view_matrix * lamp_scaling;
            world_view_matrix.M41 = -device_rect.Width / 2 + lamp_center.X;
            world_view_matrix.M42 = device_rect.Height / 2 - lamp_center.Y;

            Matrix proj = Matrix.OrthoRH(device_rect.Width, device_rect.Height, 1.0f, 500.0f);
            Matrix world_view_projection_matrix = world_view_matrix * proj;

            effect_circle.SetValue("wvp", world_view_projection_matrix);
            effect_circle.SetValue("col", new Vector4(1.0f, 1.0f, 0.0f, 1.0f));

            circle.Draw(effect_circle);
        }

        //ランプを描画する
        public void Render()
        {
            DrawLampPole();
            DrawLampCircleW();
        }
    }
}
