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

        public NodeRenderer(Device device, Sprite sprite)
        {
            circle = new Circle(device);
            pole = new Pole(device);
        }

        // on device lost
        public void Dispose()
        {
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

#if false
        void DrawPoleZ()
        {
            float scale = 5.0f;
            Matrix world_matrix = Matrix.Scaling(scale, scale, scale);
            Matrix world_view_matrix = world_matrix * Transform_View;
            Matrix world_view_projection_matrix = world_view_matrix * Transform_Projection;

            effect_pole.SetValue("wvp", world_view_projection_matrix);
            effect_pole.SetValue("col", new Vector4(0.0f, 0.0f, 1.0f, 1.0f));

            pole.Draw(effect_pole);
        }

        void DrawPoleY()
        {
            float scale = 5.0f;
            Matrix world_matrix = Matrix.Scaling(scale, scale, scale) * Matrix.RotationX((float)(-Math.PI / 2.0));
            Matrix world_view_matrix = world_matrix * Transform_View;
            Matrix world_view_projection_matrix = world_view_matrix * Transform_Projection;

            effect_pole.SetValue("wvp", world_view_projection_matrix);
            effect_pole.SetValue("col", new Vector4(0.0f, 1.0f, 0.0f, 1.0f));

            pole.Draw(effect_pole);
        }

        void DrawPoleX()
        {
            float scale = 5.0f;
            Matrix world_matrix = Matrix.Scaling(scale, scale, scale) * Matrix.RotationY((float)(+Math.PI / 2.0));
            Matrix world_view_matrix = world_matrix * Transform_View;
            Matrix world_view_projection_matrix = world_view_matrix * Transform_Projection;

            effect_pole.SetValue("wvp", world_view_projection_matrix);
            effect_pole.SetValue("col", new Vector4(1.0f, 0.0f, 0.0f, 1.0f));

            pole.Draw(effect_pole);
        }

        void DrawCircleZ()
        {
            float scale = 2.5f;
            Matrix world_matrix = Matrix.Scaling(scale, scale, scale);
            Matrix world_view_matrix = world_matrix * Transform_View;
            Matrix world_view_projection_matrix = world_view_matrix * Transform_Projection;

            effect_circle.SetValue("wvp", world_view_projection_matrix);
            effect_circle.SetValue("col", new Vector4(0.0f, 0.0f, 1.0f, 1.0f));

            circle.Draw(effect_circle);
        }

        void DrawCircleY()
        {
            float scale = 2.5f;
            Matrix world_matrix = Matrix.Scaling(scale, scale, scale) * Matrix.RotationX((float)(-Math.PI / 2.0));
            Matrix world_view_matrix = world_matrix * Transform_View;
            Matrix world_view_projection_matrix = world_view_matrix * Transform_Projection;

            effect_circle.SetValue("wvp", world_view_projection_matrix);
            effect_circle.SetValue("col", new Vector4(0.0f, 1.0f, 0.0f, 1.0f));

            circle.Draw(effect_circle);
        }

        void DrawCircleX()
        {
            float scale = 2.5f;
            Matrix world_matrix = Matrix.Scaling(scale, scale, scale) * Matrix.RotationY((float)(+Math.PI / 2.0));
            Matrix world_view_matrix = world_matrix * Transform_View;
            Matrix world_view_projection_matrix = world_view_matrix * Transform_Projection;

            effect_circle.SetValue("wvp", world_view_projection_matrix);
            effect_circle.SetValue("col", new Vector4(1.0f, 0.0f, 0.0f, 1.0f));

            circle.Draw(effect_circle);
        }

        void DrawCircleW()
        {
            float scale = 1.25f;
            Matrix world_matrix = Matrix.Scaling(scale, scale, scale);
            Matrix world_view_matrix = world_matrix;

            world_view_matrix.M41 += Transform_View.M41;
            world_view_matrix.M42 += Transform_View.M42;
            world_view_matrix.M43 += Transform_View.M43;

            Matrix world_view_projection_matrix = world_view_matrix * Transform_Projection;

            effect_circle.SetValue("wvp", world_view_projection_matrix);
            effect_circle.SetValue("col", new Vector4(1.0f, 1.0f, 1.0f, 1.0f));

            circle.Draw(effect_circle);
        }
#endif

        void DrawNodePoleZ(ref Vector3 world_position, ref Matrix world_rotation, ref Matrix world)
        {
            float scale = 0.25f;
            Matrix world_matrix = Matrix.Scaling(scale, scale, scale) * world_rotation;

            // translation
            world_matrix.M41 = world_position.X;
            world_matrix.M42 = world_position.Y;
            world_matrix.M43 = world_position.Z;

            Matrix world_view_matrix = world_matrix * world * Transform_View;
            Matrix world_view_projection_matrix = world_view_matrix * Transform_Projection;

            effect_pole.SetValue("wvp", world_view_projection_matrix);
            effect_pole.SetValue("col", new Vector4(0.0f, 0.0f, 1.0f, 1.0f));

            pole.Draw(effect_pole);
        }

        void DrawNodePoleY(ref Vector3 world_position, ref Matrix world_rotation, ref Matrix world)
        {
            float scale = 0.25f;
            Matrix world_matrix = Matrix.Scaling(scale, scale, scale) * Matrix.RotationX((float)(-Math.PI / 2.0)) * world_rotation;

            // translation
            world_matrix.M41 = world_position.X;
            world_matrix.M42 = world_position.Y;
            world_matrix.M43 = world_position.Z;

            Matrix world_view_matrix = world_matrix * world * Transform_View;
            Matrix world_view_projection_matrix = world_view_matrix * Transform_Projection;

            effect_pole.SetValue("wvp", world_view_projection_matrix);
            effect_pole.SetValue("col", new Vector4(0.0f, 1.0f, 0.0f, 1.0f));

            pole.Draw(effect_pole);
        }

        void DrawNodePoleX(ref Vector3 world_position, ref Matrix world_rotation, ref Matrix world)
        {
            float scale = 0.25f;
            Matrix world_matrix = Matrix.Scaling(scale, scale, scale) * Matrix.RotationY((float)(+Math.PI / 2.0)) * world_rotation;

            // translation
            world_matrix.M41 = world_position.X;
            world_matrix.M42 = world_position.Y;
            world_matrix.M43 = world_position.Z;

            Matrix world_view_matrix = world_matrix * world * Transform_View;
            Matrix world_view_projection_matrix = world_view_matrix * Transform_Projection;

            effect_pole.SetValue("wvp", world_view_projection_matrix);
            effect_pole.SetValue("col", new Vector4(1.0f, 0.0f, 0.0f, 1.0f));

            pole.Draw(effect_pole);
        }

        void DrawNodePoleZ(ref Vector3 world_position, ref Matrix world)
        {
            Matrix world_rotation = Rotation_Camera;
            DrawNodePoleZ(ref world_position, ref world_rotation, ref world);
        }

        void DrawNodePoleY(ref Vector3 world_position, ref Matrix world)
        {
            Matrix world_rotation = Rotation_Camera;
            DrawNodePoleY(ref world_position, ref world_rotation, ref world);
        }

        void DrawNodePoleX(ref Vector3 world_position, ref Matrix world)
        {
            Matrix world_rotation = Rotation_Camera;
            DrawNodePoleX(ref world_position, ref world_rotation, ref world);
        }

        void DrawNodeCircleW(ref Vector3 world_position, ref Matrix world)
        {
            float scale = 0.125f;
            Matrix world_matrix = Matrix.Scaling(scale, scale, scale) * Rotation_Camera;

            // translation
            world_matrix.M41 = world_position.X;
            world_matrix.M42 = world_position.Y;
            world_matrix.M43 = world_position.Z;

            Matrix world_view_matrix = world_matrix * world * Transform_View;
            Matrix world_view_projection_matrix = world_view_matrix * Transform_Projection;

            effect_circle.SetValue("wvp", world_view_projection_matrix);
            effect_circle.SetValue("col", new Vector4(0.5f, 0.5f, 0.5f, 1.0f));

            circle.Draw(effect_circle);
        }

        void DrawSelectedNodeCircleW(ref Vector3 world_position, ref Matrix world)
        {
            float scale = 0.25f;
            Matrix world_matrix = Matrix.Scaling(scale, scale, scale) * Rotation_Camera;

            // translation
            world_matrix.M41 = world_position.X;
            world_matrix.M42 = world_position.Y;
            world_matrix.M43 = world_position.Z;

            Matrix world_view_matrix = world_matrix * world * Transform_View;
            Matrix world_view_projection_matrix = world_view_matrix * Transform_Projection;

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
