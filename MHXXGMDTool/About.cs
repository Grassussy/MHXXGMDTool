﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MHXXGMDTool
{
    public partial class About : Form
    {
        string Version = "1.0.0";

        public About()
        {
            InitializeComponent();
        }

        private void About_Load(object sender, EventArgs e)
        {
            pictureBox1.Image = Properties.Resources.image_valstrax;
            label1.Text = "MHXX GMD Tool";
            label2.Text = "Version " + Version;
            label3.Text = "This program is created by\nTouchGrassus";
            label4.Text = "2023-06-02";
        }
    }
}