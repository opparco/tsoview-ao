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
    public class SpriteRenderer : IDisposable
    {
        Mode[] modes;
        Mode current_mode;
        public string CurrentModeName { get { return current_mode.Name; } }

        public ModelMode model_mode { get { return modes[0] as ModelMode; } }
        public PoseMode pose_mode { get { return modes[1] as PoseMode; } }
        public SceneMode scene_mode { get { return modes[2] as SceneMode; } }

        public string[] NodeNames { get { return pose_mode.NodeNames; } }

        public SpriteRenderer(Device device, Sprite sprite)
        {
            modes = new Mode[3];
            modes[0] = new ModelMode(device, sprite);
            modes[1] = new PoseMode(device, sprite);
            modes[2] = new SceneMode(device, sprite);
            current_mode = modes[1];
        }

        // on device lost
        public void Dispose()
        {
            Debug.WriteLine("SpriteRenderer.Dispose");

            for (int i = 0; i < modes.Length; i++)
                modes[i].Dispose();
        }

        // on device reset
        public void Create(Rectangle client_rect)
        {
            for (int i = 0; i < modes.Length; i++)
                modes[i].Create(client_rect);
        }

        int FindModeTabByLocation(int x16, int y16)
        {
            if (x16 >= 11 && x16 < 24)
                return 0; // MODEL
            else if (x16 >= 25 && x16 < 38)
                return 1; // POSE
            else if (x16 >= 39 && x16 < 52)
                return 2; // SCENE

            return -1;
        }

        public bool Update(Point sprite_p)
        {
            int y16 = sprite_p.Y / 16;
            int x16 = sprite_p.X / 16;

            if (y16 >= 1 && y16 < 3)
            {
                int mode_tab = FindModeTabByLocation(x16, y16);
                if (mode_tab != -1)
                    current_mode = modes[mode_tab];
                return mode_tab != -1;
            }
            else
                return current_mode.Update(sprite_p);
        }

        public void Render()
        {
            current_mode.Render();
        }
    }
}
