using System;
using System.IO;

namespace TDCG
{
    /// 録画：シーンフレームを静止画として保存
public class Record
{
    Viewer viewer;
    int step;

    bool enabled = false;

    /// 録画中であるか
    public bool Enabled
    {
        get { return enabled; }
    }

    string dest_path = @"snapshots";
    int orig_frame_idx;
    int frame_len;
    int frame_idx;

    /// constructor
    public Record(Viewer viewer, int step)
    {
        this.viewer = viewer;
        this.step = step;
    }

    /// 録画を開始します。
    public void Start()
    {
        Directory.CreateDirectory(dest_path);

        orig_frame_idx = viewer.FrameIndex;
        viewer.MotionEnabled = true;
        frame_len = viewer.GetMaxFrameLength();
        frame_idx = 0;
        enabled = true;
    }

    /// 録画を停止します。
    public void End()
    {
        enabled = false;
        viewer.MotionEnabled = false;
        viewer.FrameIndex = orig_frame_idx;
    }

    /// シーンフレームを進めて静止画を保存します。
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
