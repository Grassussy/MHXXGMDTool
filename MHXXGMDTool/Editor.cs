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

namespace MHXXGMDTool
{
    public partial class Editor : Form
    {
        private Gmd _gmd;
        private string OpenedFilePath;

        public Editor()
        {
            InitializeComponent();
        }

        private void OpenFile(string fileDragDrop, bool isDragDrop)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Select MHXX/MHGU GMD file";
            ofd.Filter = "MHXX/MHGU GMD files|*.gmd";
            if (isDragDrop)
            {
                if (fileDragDrop != OpenedFilePath)
                    CloseFile();
                else
                    return;
                OpenedFilePath = fileDragDrop;
            }
            else
            {
                if (ofd.ShowDialog() != DialogResult.OK)
                    return;
                if (ofd.FileName != OpenedFilePath)
                    CloseFile();
                else
                    return;
                OpenedFilePath = ofd.FileName;
            }
            ofd.Dispose();
            FileInfo fi = new FileInfo(OpenedFilePath);
            _gmd = new Gmd(fi.OpenRead());
            UpdateForm();
        }

        private void CloseFile()
        {
            OpenedFilePath = "";
            ClearForm();
        }

        private void ClearForm()
        {
            treeViewEntries.Nodes.Clear();
            textBoxText.Clear();
        }

        private void UpdateForm()
        {
            foreach (var entry in _gmd.Labels)
                treeViewEntries.Nodes.Add(entry.Name);
            treeViewEntries.SelectedNode = treeViewEntries.Nodes[0];
        }

        private void ExportToCSV()
        {
            if (OpenedFilePath == "" || OpenedFilePath == null)
                return;
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Title = "Export to CSV";
            sfd.Filter = "CSV File|*.csv";
            sfd.FileName = Path.GetFileNameWithoutExtension(OpenedFilePath);
            if (sfd.ShowDialog() != DialogResult.OK)
                return;
            var SaveFilePath = sfd.FileName;
            sfd.Dispose();
            WriteCSV(OpenedFilePath, SaveFilePath);
        }

        private void BatchExportCSV()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Select MHXX/MHGU GMD files";
            ofd.Filter = "MHXX/MHGU GMD files|*.gmd";
            ofd.Multiselect = true;
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.Description = "Select Export Folder";
            if (ofd.ShowDialog() != DialogResult.OK)
                return;
            if (fbd.ShowDialog() != DialogResult.OK)
                return;
            var files = ofd.FileNames;
            foreach (var filename in files)
                WriteCSV(filename, fbd.SelectedPath + "\\" + Path.GetFileNameWithoutExtension(filename) + ".csv");
        }

        private void WriteCSV(string filename, string output)
        {
            FileInfo fi = new FileInfo(filename);
            var batchGmd = new Gmd(fi.OpenRead());
            using (StreamWriter sw = new StreamWriter(output, false, Encoding.UTF8))
            {
                foreach (var entry in batchGmd.Labels)
                {
                    var sName = batchGmd.GetLabelCount() > 0 ? entry.Name : "";
                    var sText = entry.Text.Replace("\r\n", "\\r\\n");
                    sw.WriteLine(String.Format("{0},{1},\"{2}\"", entry.TextID, sName, sText));
                }
            }
        }

        #region Tool Strip Items
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFile("", false);
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {

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

        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }
        #endregion

        #region Component Events
        private void Editor_Load(object sender, EventArgs e)
        {
            string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            Text = "MHXX/MHGU GMD Tool v" + version;
        }

        private void Editor_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void Editor_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            if (files.Length > 0 && File.Exists(files[0]))
                OpenFile(files[0], true);
        }

        private void treeViewEntries_AfterSelect(object sender, TreeViewEventArgs e)
        {
            textBoxText.Text = _gmd.Labels[treeViewEntries.SelectedNode.Index].Text;
        }
        #endregion
    }
}