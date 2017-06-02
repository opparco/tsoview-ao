using System;
using System.Drawing;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace TDCG
{
///スクリーン全面に描画します。
public class Screen
{
    Device device;

    public Screen(Device device)
    {
        this.device = device;
    }

    // 描画面は4頂点で作成
    VertexBuffer vb;
    CustomVertex.PositionTextured[] vertices;

    void CreateVertices(Rectangle rect)
    {
        vertices = new CustomVertex.PositionTextured[4];

        const float z = 0.5f;

        vertices[0] = new CustomVertex.PositionTextured(0, 0, z, 0.0f, 0.0f);
        vertices[1] = new CustomVertex.PositionTextured(rect.Width, 0, z, 1.0f, 0.0f);
        vertices[2] = new CustomVertex.PositionTextured(0, rect.Height, z, 0.0f, 1.0f);
        vertices[3] = new CustomVertex.PositionTextured(rect.Width, rect.Height, z, 1.0f, 1.0f);

        for (int i = 0; i < 4; i++)
        {
            vertices[i].X -= 0.5f;
            vertices[i].Y -= 0.5f;
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

    Matrix world_view_projection;

    public void Create(Rectangle rect)
    {
        CreateVertices(rect);
        vb = new VertexBuffer(typeof(CustomVertex.PositionTextured), vertices.Length, device, Usage.Dynamic | Usage.WriteOnly, CustomVertex.PositionTextured.Format, Pool.Default);
        vb_Created(vb, null);

        Matrix world = Matrix.Translation(-rect.Width/2, -rect.Height/2, 0);
        Matrix view = Matrix.RotationX((float)Math.PI);
        Matrix projection = Matrix.OrthoRH(rect.Width, rect.Height, 0.0f, 1.0f);

        Matrix world_view = world * view;
        world_view_projection = world_view * projection;
    }

    public void AssignWorldViewProjection(Effect effect)
    {
        //todo: shared
        effect.SetValue("wvp", world_view_projection);
    }

    public void Draw(Effect effect)
    {
        device.VertexFormat = CustomVertex.PositionTextured.Format;
        device.SetStreamSource(0, vb, 0);

        int npass = effect.Begin(0);
        for (int ipass = 0; ipass < npass; ipass++)
        {
            effect.BeginPass(ipass);
            device.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
            effect.EndPass();
        }
        effect.End();
    }

    /// <summary>
    /// 内部objectを破棄します。
    /// </summary>
    public void Dispose()
    {
        if (vb != null)
            vb.Dispose();
    }
}
}
