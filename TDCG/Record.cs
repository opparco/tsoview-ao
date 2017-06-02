using System;
using System.IO;

namespace TDCG
{
public class Record
{
    Viewer viewer;
    int step;

    bool enabled = false;
    public bool Enabled
    {
        get { return enabled; }
    }

    string dest_path = @"snapshots";
    int orig_frame_idx;
    int frame_len;
    int frame_idx;

    public Record(Viewer viewer, int step)
    {
        this.viewer = viewer;
        this.step = step;
    }

    public void Start()
    {
        Directory.CreateDirectory(dest_path);

        orig_frame_idx = viewer.FrameIndex;
        viewer.MotionEnabled = true;
        frame_len = viewer.GetMaxFrameLength();
        frame_idx = 0;
        enabled = true;
    }

    public void End()
    {
        enabled = false;
        viewer.MotionEnabled = false;
        viewer.FrameIndex = orig_frame_idx;
    }

    public void Next()
    {
        if (frame_idx < frame_len)
        {
            viewer.FrameMove(frame_idx);
            viewer.Render();
            viewer.SaveToPng(Path.Combine(dest_path, string.Format("{0:D3}.png", frame_idx)));

            frame_idx += step;
        }
        else
            End();
    }
}
}
