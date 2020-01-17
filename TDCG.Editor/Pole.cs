using System;
using System.Diagnostics;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace TDCG.Editor
{
    /// <summary>
    /// 軸を描画します。
    /// </summary>
    public class Pole
    {
        Device device;
        VertexBuffer vb;
        CustomVertex.PositionOnly[] vertices;

        public Pole(Device device)
        {
            this.device = device;
        }

        void CreateVertices()
        {
            vertices = new CustomVertex.PositionOnly[2];

            vertices[0] = new CustomVertex.PositionOnly(0, 0, 0);
            vertices[1] = new CustomVertex.PositionOnly(0, 0, 1.0f);
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
                device.DrawPrimitives(PrimitiveType.LineStrip, 0, 1);
                effect.EndPass();
            }
            effect.End();
        }

        /// <summary>
        /// 内部objectを破棄します。
        /// </summary>
        public void Dispose()
        {
            Debug.WriteLine("Pole.Dispose");

            if (vb != null)
                vb.Dispose();
        }
    }
}
