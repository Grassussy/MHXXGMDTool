using System;
using System.IO;
using System.Windows.Forms;

namespace MHXXGMDTool
{
    public partial class Editor : Form
    {
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
                ConfirmOpenFile(files[0]);
        }

        private void Editor_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_hasChanges)
            {
                DialogResult dr = MessageBox.Show("Would you like to save your changes before exiting?", "Unsaved Changes", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

                if (dr == DialogResult.Yes)
                {
                    if (SaveFile() != DialogResult.OK)
                        e.Cancel = true;
                }
                else if (dr == DialogResult.Cancel)
                    e.Cancel = true;
            }
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
            _hasChanges = true;
            Text = TitleName() + "*";
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ConfirmOpenFile();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFile();
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFile(true);
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_hasChanges)
            {
                DialogResult dr = MessageBox.Show("Would you like to save your changes before closing the file?", "Unsaved Changes", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

                if (dr == DialogResult.Yes) SaveFile();
                else if (dr == DialogResult.Cancel) return;
            }
            CloseFile();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void exportToCSVToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ExportCSV();
        }

        private void batchExportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ExportCSV(true);
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