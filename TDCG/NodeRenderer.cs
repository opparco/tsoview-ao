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
    /// モデル上に重ねてボーンを描画する
    public class NodeRenderer
    {
        public Effect effect_circle;
        public Effect effect_pole;

        Circle circle = null;
        Pole pole = null;

        float node_circle_scale;
        float selected_node_circle_scale;

        public NodeRenderer(Device device, Sprite sprite)
        {
            circle = new Circle(device);
            pole = new Pole(device);

            node_circle_scale = 6.0f/384.0f;
            selected_node_circle_scale = 18.0f/384.0f;
        }

        // on device lost
        public void Dispose()
        {
            Debug.WriteLine("NodeRenderer.Dispose");

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

        void DrawNodePoleZ(ref Vector3 world_position, ref Matrix world_rotation, ref Matrix world)
        {
            float scale = selected_node_circle_scale;
            Matrix world_view_matrix;
            GetWorldViewMatrix(scale, ref world_position, ref world_rotation, ref world, out world_view_matrix);
            scale *= UnprojectScaling(ref world_view_matrix);
            GetWorldViewMatrix(scale, ref world_position, ref world_rotation, ref world, out world_view_matrix);
            Matrix world_view_projection_matrix = world_view_matrix * transform_projection;

            effect_pole.SetValue("wvp", world_view_projection_matrix);
            effect_pole.SetValue("col", new Vector4(0.0f, 0.0f, 1.0f, 1.0f));

            pole.Draw(effect_pole);
        }

        void DrawNodePoleY(ref Vector3 world_position, ref Matrix world_rotation, ref Matrix world)
        {
            float scale = selected_node_circle_scale;
            Matrix world_rotation_x = Matrix.RotationX((float)(-Math.PI / 2.0)) * world_rotation;
            Matrix world_view_matrix;
            GetWorldViewMatrix(scale, ref world_position, ref world_rotation_x, ref world, out world_view_matrix);
            scale *= UnprojectScaling(ref world_view_matrix);
            GetWorldViewMatrix(scale, ref world_position, ref world_rotation_x, ref world, out world_view_matrix);
            Matrix world_view_projection_matrix = world_view_matrix * transform_projection;

            effect_pole.SetValue("wvp", world_view_projection_matrix);
            effect_pole.SetValue("col", new Vector4(0.0f, 1.0f, 0.0f, 1.0f));

            pole.Draw(effect_pole);
        }

        void DrawNodePoleX(ref Vector3 world_position, ref Matrix world_rotation, ref Matrix world)
        {
            float scale = selected_node_circle_scale;
            Matrix world_rotation_y = Matrix.RotationY((float)(+Math.PI / 2.0)) * world_rotation;
            Matrix world_view_matrix;
            GetWorldViewMatrix(scale, ref world_position, ref world_rotation_y, ref world, out world_view_matrix);
            scale *= UnprojectScaling(ref world_view_matrix);
            GetWorldViewMatrix(scale, ref world_position, ref world_rotation_y, ref world, out world_view_matrix);
            Matrix world_view_projection_matrix = world_view_matrix * transform_projection;

            effect_pole.SetValue("wvp", world_view_projection_matrix);
            effect_pole.SetValue("col", new Vector4(1.0f, 0.0f, 0.0f, 1.0f));

            pole.Draw(effect_pole);
        }

        void DrawNodeCircleW(ref Vector3 world_position, ref Matrix world)
        {
            float scale = node_circle_scale;
            Matrix world_view_matrix;
            GetWorldViewMatrix(scale, ref world_position, ref camera_rotation, ref world, out world_view_matrix);
            scale *= UnprojectScaling(ref world_view_matrix);
            GetWorldViewMatrix(scale, ref world_position, ref camera_rotation, ref world, out world_view_matrix);
            Matrix world_view_projection_matrix = world_view_matrix * transform_projection;

            effect_circle.SetValue("wvp", world_view_projection_matrix);
            effect_circle.SetValue("col", new Vector4(0.5f, 0.5f, 0.5f, 1.0f));

            circle.Draw(effect_circle);
        }

        void DrawSelectedNodeCircleW(ref Vector3 world_position, ref Matrix world)
        {
            float scale = selected_node_circle_scale;
            Matrix world_view_matrix;
            GetWorldViewMatrix(scale, ref world_position, ref camera_rotation, ref world, out world_view_matrix);
            scale *= UnprojectScaling(ref world_view_matrix);
            GetWorldViewMatrix(scale, ref world_position, ref camera_rotation, ref world, out world_view_matrix);
            Matrix world_view_projection_matrix = world_view_matrix * transform_projection;

            effect_circle.SetValue("wvp", world_view_projection_matrix);
            effect_circle.SetValue("col", new Vector4(0.0f, 1.0f, 1.0f, 1.0f));

            circle.Draw(effect_circle);
        }

        void DrawNode(TMONode node, ref Matrix world)
        {
            Vector3 world_position = node.GetWorldPosition();

            DrawNodeCircleW(ref world_position, ref world);
        }

        void DrawSelectedNode(TMONode selected_node, ref Matrix world)
        {
            if (selected_node == null)
                return;

            Vector3 world_position = selected_node.GetWorldPosition();
            Matrix world_rotation = Matrix.RotationQuaternion(selected_node.GetWorldRotation());

            DrawNodePoleX(ref world_position, ref world_rotation, ref world);
            DrawNodePoleY(ref world_position, ref world_rotation, ref world);
            DrawNodePoleZ(ref world_position, ref world_rotation, ref world);
            DrawSelectedNodeCircleW(ref world_position, ref world);
        }

        //モデル上に重ねてボーンを描画する
        //@param fig: ボーンを描画するモデル
        //@param selected_node: 選択ボーン
        //@param nodes: 描画ボーン
        public void Render(Figure fig, TMONode selected_node, TMONode[] nodes)
        {
            Matrix world;
            fig.GetWorldMatrix(out world);

            foreach (TMONode node in nodes)
            {
                DrawNode(node, ref world);
            }
            DrawSelectedNode(selected_node, ref world);
        }
    }
}
