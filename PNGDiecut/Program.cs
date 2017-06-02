using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace PNGDiecut
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            TSOConfig tso_config;

            string tso_config_file = Path.Combine(Application.StartupPath, @"config.xml");
            if (File.Exists(tso_config_file))
                tso_config = TSOConfig.Load(tso_config_file);
            else
                tso_config = new TSOConfig();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1(tso_config, args));
        }
    }
}
