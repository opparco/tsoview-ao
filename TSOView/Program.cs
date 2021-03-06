using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace TSOView
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Debug.Listeners.Add(new TextWriterTraceListener(Console.Out));

            TSOConfig tso_config;

            string tso_config_file = Path.Combine(Application.StartupPath, @"config.xml");
            if (File.Exists(tso_config_file))
                tso_config = TSOConfig.Load(tso_config_file);
            else
                tso_config = new TSOConfig();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            //Application.Run(new TSOForm(tso_config, args));
            using (TSOForm tso_form = new TSOForm(tso_config, args))
            {
                tso_form.Show();
                if (tso_config.ShowConfigForm)
                {
                    // stale KeyUp event
                    tso_form.configForm.Show();
                    tso_form.configForm.Activate();
                }

                while (tso_form.Created)
                {
                    tso_form.Render();
                    Application.DoEvents();
                    System.Threading.Thread.Sleep(5);
                }
            }
        }
    }
}
