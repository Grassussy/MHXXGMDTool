using MHXXGMDTool.Properties;
using System;
using System.Windows.Forms;

namespace MHXXGMDTool
{
    public partial class ExportSettings : Form
    {
        public ExportSettings()
        {
            InitializeComponent();
        }

        private void ExportSettings_Load(object sender, EventArgs e)
        {
            checkBox1.Checked = Settings.Default.Export_IncludeID;
            checkBox2.Checked = Settings.Default.Export_IncludeName;
        }

        private void ExportSettings_FormClosing(object sender, FormClosingEventArgs e)
        {
            Settings.Default.Save();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Default.Export_IncludeID = checkBox1.Checked;
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Default.Export_IncludeName = checkBox2.Checked;
        }
    }
}
