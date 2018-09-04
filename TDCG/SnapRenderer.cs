using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace TDCG
{
    /*
    Model画面, Scene画面でcellを描画するために
    tso別, fig別に描画を行う。

    描画先はsnap_texture (snap_surface) である。
    これを矩形で分割するとcell画像となる。
    */
    public class SnapRenderer
    {
        Device device;
        Sprite sprite;

        Texture snap_texture;
        Surface snap_surface;

        public SnapRenderer(Device device, Sprite sprite)
        {
            this.device = device;
            this.sprite = sprite;
        }

        public void Dispose()
        {
            if (snap_surface != null)
                snap_surface.Dispose();
            if (snap_texture != null)
                snap_texture.Dispose();
        }

        Rectangle device_rect;

        public void Create(Rectangle device_rect)
        {
            this.device_rect = device_rect;

            snap_texture = new Texture(device, 1024, 1024, 1, Usage.RenderTarget, Format.X8R8G8B8, Pool.Default);
            snap_surface = snap_texture.GetSurfaceLevel(0);
        }

        public void DrawSpriteSnapTSO(Figure fig)
        {
            Debug.WriteLine("DrawSpriteSnapTSO");

            device.SetRenderState(RenderStates.AlphaBlendEnable, false);

            sprite.Transform = Matrix.Scaling(device_rect.Width / 1024.0f * 0.75f, device_rect.Height / 768.0f * 0.75f, 1.0f);

            sprite.Begin(0);

            foreach (TSOFile tso in fig.TsoList)
            {
                int idx = tso.Row;
                int x16 = (idx%8)*7 + 4;
                int y16 = (idx/8)*9 + 5;

                sprite.Draw(snap_texture, new Rectangle((idx%8)*128, (idx/8)*128, 128, 128), new Vector3(0, 0, 0), new Vector3(x16 * 16 / 0.75f, y16 * 16 / 0.75f, 0), Color.White);
            }
            sprite.End();
        }

        public void DrawSpriteSnapFigure(List<Figure> FigureList)
        {
            Debug.WriteLine("DrawSpriteSnapFigure");

            device.SetRenderState(RenderStates.AlphaBlendEnable, false);

            sprite.Transform = Matrix.Scaling(device_rect.Width / 2048.0f, device_rect.Height / 1536.0f, 1.0f);

            sprite.Begin(0);
            int idx = 0;
            foreach (Figure fig in FigureList)
            {
                int x16 = (idx%6)*9 + 5;
                int y16 = (idx/6)*9 + 5;

                sprite.Draw(snap_texture, new Rectangle((idx%4)*256, (idx/4)*192, 256, 192), new Vector3(0, 0, 0), new Vector3(x16 * 32, y16 * 32, 0), Color.White);

                idx++;
            }
            sprite.End();
        }

        public void SnapTSO(int idx)
        {
            Debug.WriteLine("SnapTSO");

            Surface dev_surface = device.GetRenderTarget(0);

            int w = device_rect.Width;
            int h = device_rect.Height;
            Rectangle square_rect;
            if (h < w)
                square_rect = new Rectangle((w-h)/2, 0, h, h);
            else
                square_rect = new Rectangle(0, (h-w)/2, w, w);

            device.StretchRectangle(dev_surface, square_rect, snap_surface, new Rectangle((idx%8)*128, (idx/8)*128, 128, 128), TextureFilter.Point);

            dev_surface.Dispose();
        }

        public void SnapFigure(int idx)
        {
            Debug.WriteLine("SnapFigure");

            Surface dev_surface = device.GetRenderTarget(0);

            device.StretchRectangle(dev_surface, device_rect, snap_surface, new Rectangle((idx%4)*256, (idx/4)*192, 256, 192), TextureFilter.Point);

            dev_surface.Dispose();
        }
    }
}
