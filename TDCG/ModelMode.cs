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
    public class ModelMode : Mode
    {
        public ModelMode(Device device, Sprite sprite) : base(device, sprite, "MODEL", "0-model.png")
        {
        }
        public override bool Update(Point sprite_p)
        {
            return false;
        }
        public override void Render()
        {
            DrawSprite(mode_texture);
        }
    }
}
