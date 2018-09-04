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

        /// camera回転行列
        Matrix camera_rotation = Matrix.Identity;

        public void SetTransform(ProjectionMode projection_mode, ref Matrix camera_rotation)
        {
            this.projection_mode = projection_mode;
            this.camera_rotation = camera_rotation;
        }

        void GetWorldViewMatrix(float scale, ref Vector3 world_position, ref Matrix world_rotation, ref Matrix world, out Matrix world_view_matrix)
        {
            Matrix world_matrix = Matrix.Scaling(scale, scale, scale) * world_rotation;

            // translation
            world_matrix.M41 = world_position.X;
            world_matrix.M42 = world_position.Y;
            world_matrix.M43 = world_position.Z;

            world_view_matrix = world_matrix * world * device.Transform.View;
        }

        float UnprojectScaling(ref Matrix world_view_matrix)
        {
            float d;
            if (projection_mode == ProjectionMode.Ortho)
                d = 1.0f;
            else
                d = -world_view_matrix.M43;
            return d / device.Transform.Projection.M22;
        }

        void DrawLampPole(ref Vector3 world_position, ref Matrix world_rotation, ref Matrix world)
        {
#if false
            float scale = lamp_circle_scale;
            Matrix world_view_matrix;
            GetWorldViewMatrix(scale, ref world_position, ref world_rotation, ref world, out world_view_matrix);
            scale *= UnprojectScaling(ref world_view_matrix);
            GetWorldViewMatrix(scale, ref world_position, ref world_rotation, ref world, out world_view_matrix);
            Matrix world_view_projection_matrix = world_view_matrix * transform_projection;
#endif
            float scale = lamp_radius;
            Matrix lamp_scaling = Matrix.Scaling(scale, scale, scale);

            Matrix world_view_matrix = world_rotation * Matrix.Invert(camera_rotation);
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

        void DrawLampCircleW(ref Vector3 world_position, ref Matrix world)
        {
#if false
            float scale = lamp_circle_scale;
            Matrix world_view_matrix;
            GetWorldViewMatrix(scale, ref world_position, ref camera_rotation, ref world, out world_view_matrix);
            scale *= UnprojectScaling(ref world_view_matrix);
            GetWorldViewMatrix(scale, ref world_position, ref camera_rotation, ref world, out world_view_matrix);
            Matrix world_view_projection_matrix = world_view_matrix * transform_projection;
#endif
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

