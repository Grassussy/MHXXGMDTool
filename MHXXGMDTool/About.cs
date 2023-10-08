using System;
using System.Windows.Forms;

namespace MHXXGMDTool
{
    public partial class About : Form
    {
        readonly string Version = "1.0.0";

        public About()
        {
            InitializeComponent();
        }

        private void About_Load(object sender, EventArgs e)
        {
            pictureBox1.Image = Properties.Resources.image_valstrax;
            label1.Text = "MHXX GMD Tool";
            label2.Text = "Version " + Version;
            label3.Text = "This program is created by\nGrassussy";
            label4.Text = "2023-10-08";
        }
    }
}
