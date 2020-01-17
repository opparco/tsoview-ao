using System;
using System.Diagnostics;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace TDCG.Editor
{
    /// <summary>
    /// 円を描画します。
    /// </summary>
    public class Circle
    {
        Device device;
        VertexBuffer vb;
        CustomVertex.PositionOnly[] vertices;

        public Circle(Device device)
        {
            this.device = device;
        }

        const int VERTICES = 24 + 1;

        void CreateVertices()
        {
            vertices = new CustomVertex.PositionOnly[VERTICES];

            const float radius = 1.0f;

            for (int i = 0; i < VERTICES; i++)
            {
                double degree = i * 15;
                double radian = Math.PI * degree / 180.0;
                float x = radius * (float)Math.Cos(radian);
                float y = radius * (float)Math.Sin(radian);
                vertices[i] = new CustomVertex.PositionOnly(x, y, 0);
            }
        }

        void vb_Created(object sender, EventArgs e)
        {
            VertexBuffer vb = (VertexBuffer)sender;

            {
                GraphicsStream gs = vb.Lock(0, 0, LockFlags.None);
                {
                    for (int i = 0; i < vertices.Length; i++)
                    {
                        gs.Write(vertices[i]);
                    }
                }
                vb.Unlock();
            }
        }

        /// 作成する
        public void Create()
        {
            CreateVertices();
            vb = new VertexBuffer(typeof(CustomVertex.PositionOnly), vertices.Length, device, Usage.Dynamic | Usage.WriteOnly, CustomVertex.PositionOnly.Format, Pool.Default);
            vb_Created(vb, null);
        }

        /// 描画する
        public void Draw(Effect effect)
        {
            device.VertexFormat = CustomVertex.PositionOnly.Format;
            device.SetStreamSource(0, vb, 0);

            int npass = effect.Begin(0);
            for (int ipass = 0; ipass < npass; ipass++)
            {
                effect.BeginPass(ipass);
                device.DrawPrimitives(PrimitiveType.LineStrip, 0, VERTICES - 1);
                effect.EndPass();
            }
            effect.End();
        }

        /// <summary>
        /// 内部objectを破棄します。
        /// </summary>
        public void Dispose()
        {
            Debug.WriteLine("Circle.Dispose");

            if (vb != null)
                vb.Dispose();
        }
    }
}
