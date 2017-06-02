using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PNGDiecut
{
public partial class Form1 : Form
{
    // キー入力を保持
    internal bool[] keys = new bool[256];
    internal bool[] keysEnabled = new bool[256];

    internal int keySave        = (int)Keys.Return;

    public Viewer viewer;

    public Form1(TSOConfig tso_config, string[] args)
    {
        InitializeComponent();
        this.ClientSize = tso_config.ClientSize;

        for (int i = 0; i < keysEnabled.Length; i++)
        {
            keysEnabled[i] = true;
        }
        this.KeyDown += new KeyEventHandler(form_OnKeyDown);
        this.KeyUp += new KeyEventHandler(form_OnKeyUp);

        this.DragDrop += new DragEventHandler(form_OnDragDrop);
        this.DragOver += new DragEventHandler(form_OnDragOver);

        viewer = new Viewer();
        viewer.DeviceSize = tso_config.DeviceSize;
        if (viewer.InitializeGraphices(this))
        {
            foreach (string arg in args)
                viewer.LoadAnyFile(arg, true);

            this.timer1.Enabled = true;
        }
    }

    private void form_OnKeyDown(object sender, KeyEventArgs e)
    {
        if ((int)e.KeyCode < keys.Length)
        {
            keys[(int)e.KeyCode] = true;
        }
    }

    private void form_OnKeyUp(object sender, KeyEventArgs e)
    {
        if ((int)e.KeyCode < keys.Length)
        {
            keys[(int)e.KeyCode] = false;
            keysEnabled[(int)e.KeyCode] = true;
        }
    }

    private void form_OnDragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            if ((e.KeyState & 8) == 8)
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.Move;
        }
    }

    private void form_OnDragDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            foreach (string src in (string[])e.Data.GetData(DataFormats.FileDrop))
                viewer.LoadAnyFile(src, (e.KeyState & 8) == 8);
        }
    }

    public void FrameMove()
    {
        if (keysEnabled[keySave] && keys[keySave])
        {
            keysEnabled[keySave] = false;
            viewer.Save();
        }
    }

    private void timer1_Tick(object sender, EventArgs e)
    {
        this.FrameMove();
        viewer.Render();
    }
}
}
