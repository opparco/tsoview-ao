using System;
using System.Drawing;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace PNGDiecut
{
public class Screen : IDisposable
{
    Device device;
    VertexBuffer vb;

    CustomVertex.PositionTextured[] vertices;

    Matrix world;
    Matrix view;
    Matrix projection;

    Matrix world_view;
    Matrix world_view_projection;

    public Screen(Device device)
    {
        this.device = device;
    }

    void CreateVertices(Rectangle rect)
    {
        vertices = new CustomVertex.PositionTextured[4];

        const float z = 0.5f;

        vertices[0] = new CustomVertex.PositionTextured(rect.Left, rect.Bottom, z, 0.0f, 1.0f);
        vertices[1] = new CustomVertex.PositionTextured(rect.Left, rect.Top, z, 0.0f, 0.0f);
        vertices[2] = new CustomVertex.PositionTextured(rect.Right, rect.Bottom, z, 1.0f, 1.0f);
        vertices[3] = new CustomVertex.PositionTextured(rect.Right, rect.Top, z, 1.0f, 0.0f);

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

    public void Create(Rectangle rect)
    {
        CreateVertices(rect);
        vb = new VertexBuffer(typeof(CustomVertex.PositionTextured), vertices.Length, device, Usage.Dynamic | Usage.WriteOnly, CustomVertex.PositionTextured.Format, Pool.Default);
        //vb.Created += new EventHandler(vb_Created);
        vb_Created(vb, null);

        world = Matrix.Translation(-rect.Width/2, -rect.Height/2, 0);
        view = Matrix.RotationX((float)Math.PI);
        projection = Matrix.OrthoRH(rect.Width, rect.Height, 0.0f, 1.0f);

        world_view = world * view;
        world_view_projection = world_view * projection;
    }

    public void AssignValue(Effect effect)
    {
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

    public void Dispose()
    {
        if (vb != null)
            vb.Dispose();
    }
}
}
