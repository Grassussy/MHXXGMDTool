using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace MHXXGMDTool
{
    public partial class Editor : Form
    {
        private string FormTitle = "MHXX/MHGU GMD Tool v" + Assembly.GetExecutingAssembly().GetName().Version.ToString();

        public Editor()
        {
            InitializeComponent();
        }

        private void Editor_Load(object sender, EventArgs e)
        {
            ClearForm();
        }

        private void Editor_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void Editor_DragDrop(object sender, DragEventArgs e)
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            if (files.Length > 0 && File.Exists(files[0]))
                OpenFile(files[0], true);
        }

        private void treeViewEntries_AfterSelect(object sender, TreeViewEventArgs e)
        {
            textBoxText.Text = _gmd.Labels[treeViewEntries.SelectedNode.Index].Text;
        }

        private void textBoxText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control & e.KeyCode == Keys.A)
                textBoxText.SelectAll();

            if (e.Control & (e.KeyCode != Keys.V && e.KeyCode != Keys.C && e.KeyCode != Keys.X && e.KeyCode != Keys.Z && e.KeyCode != Keys.Left && e.KeyCode != Keys.Right))
                e.SuppressKeyPress = true;
        }

        private void textBoxText_KeyUp(object sender, KeyEventArgs e)
        {
            _gmd.Labels[treeViewEntries.SelectedNode.Index].Text = textBoxText.Text;
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFile("", false);
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFile(false);
            Text = FormTitle + " - " + Path.GetFileName(OpenedFilePath);
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFile(true);
            Text = FormTitle + " - " + Path.GetFileName(OpenedFilePath);
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CloseFile();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void exportToCSVToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ExportToCSV();
        }

        private void batchExportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            BatchExportCSV();
        }

        private void importCSVToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ImportCSV();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (About about = new About())
                about.ShowDialog();
        }
    }
}