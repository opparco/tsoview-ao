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
    public class SceneMode : Mode
    {
        public SceneMode(Device device, Sprite sprite) : base(device, sprite, "SCENE", "2-scene.png")
        {
        }
        public override bool Update(Point sprite_p)
        {
            return false;
        }
        public override void Render()
        {
            DrawSprite();
        }
    }
}
