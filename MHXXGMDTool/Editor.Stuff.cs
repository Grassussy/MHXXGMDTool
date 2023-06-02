using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Reflection;

namespace MHXXGMDTool
{
    partial class Editor
    {
        private Gmd _gmd;

        private string FormTitle = "MHXX/MHGU GMD Tool v" + Assembly.GetExecutingAssembly().GetName().Version.ToString();
        private string _openFileName = "";
        private string FileDialogTitle = "Select MHXX/MHGU GMD file";
        private string FileDialogFilter = "MHXX/MHGU GMD files|*.gmd";

        private bool _fileOpen;
        private bool _hasChanges;

        private string TitleName()
        {
            return FormTitle + " - " + Path.GetFileName(_openFileName);
        }

        private void ConfirmOpenFile(string filename = "")
        {
            DialogResult dr = DialogResult.No;

            if (_fileOpen && _hasChanges)
                dr = MessageBox.Show("You have unsaved changes in " + Path.GetFileName(_openFileName) + ". Save changes before opening another file?", "Unsaved Changes", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);

            switch (dr)
            {
                case DialogResult.Yes:
                    dr = SaveFile();
                    if (dr == DialogResult.OK) OpenFile(filename);
                    break;
                case DialogResult.No:
                    OpenFile(filename);
                    break;
            }
        }

        private void OpenFile(string filename = "")
        {
            var ofd = new OpenFileDialog
            {
                Title = FileDialogTitle,
                Filter = FileDialogFilter
            };

            var dr = DialogResult.OK;

            if (filename == string.Empty) dr = ofd.ShowDialog();

            if (dr != DialogResult.OK) return;

            if (filename == string.Empty) filename = ofd.FileName;

            CloseFile();

            _openFileName = filename;

            try
            {
                var fi = new FileInfo(filename);
                _gmd = new Gmd(fi.OpenRead());
                _fileOpen = true;
                _hasChanges = false;
                UpdateForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private DialogResult SaveFile(bool saveAs = false)
        {
            var sfd = new SaveFileDialog();
            var dr = DialogResult.OK;

            sfd.Title = "Save as GMD";
            sfd.Filter = FileDialogFilter;
            sfd.FileName = Path.GetFileNameWithoutExtension(_openFileName);

            if (_openFileName == "" || saveAs)
                dr = sfd.ShowDialog();

            if ((_openFileName == "" || saveAs) && dr == DialogResult.OK)
                _openFileName = sfd.FileName;

            sfd.Dispose();

            if (dr != DialogResult.OK) return dr;

            try
            {
                FileInfo fi = new FileInfo(_openFileName);
                _gmd.Save(fi.OpenWrite());
                _hasChanges = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return dr;
        }

        private void CloseFile()
        {
            _openFileName = "";
            ClearForm();
        }

        private void ClearForm()
        {
            treeViewEntries.BeginUpdate();
            treeViewEntries.Nodes.Clear();
            treeViewEntries.EndUpdate();
            treeViewEntries.Focus();

            textBoxText.Clear();

            this.Text = FormTitle;

            saveToolStripMenuItem.Enabled = false;
            saveAsToolStripMenuItem.Enabled = false;
            closeToolStripMenuItem.Enabled = false;
            exportToCSVToolStripMenuItem.Enabled = false;
            importFromCSVToolStripMenuItem.Enabled = false;

            textBoxText.Enabled = false;
        }

        private void UpdateForm()
        {
            treeViewEntries.BeginUpdate();
            foreach (var entry in _gmd.Labels)
                treeViewEntries.Nodes.Add(entry.Name);
            treeViewEntries.SelectedNode = treeViewEntries.Nodes[0];
            treeViewEntries.EndUpdate();
            treeViewEntries.Focus();

            this.Text = TitleName();

            saveToolStripMenuItem.Enabled = true;
            saveAsToolStripMenuItem.Enabled = true;
            closeToolStripMenuItem.Enabled = true;
            exportToCSVToolStripMenuItem.Enabled = true;
            importFromCSVToolStripMenuItem.Enabled = true;

            textBoxText.Enabled = true;
        }

        private void ExportCSV(bool isBatch = false)
        {
            if (!isBatch)
            {
                if (_openFileName == "")
                    return;
                SaveFileDialog sfd = new SaveFileDialog
                {
                    Title = "Export to CSV",
                    Filter = "CSV File|*.csv",
                    FileName = Path.GetFileNameWithoutExtension(_openFileName)
                };
                if (sfd.ShowDialog() != DialogResult.OK)
                    return;
                var SavePath = sfd.FileName;
                sfd.Dispose();
                try
                {
                    WriteCSV(_openFileName, SavePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                OpenFileDialog ofd = new OpenFileDialog
                {
                    Title = FileDialogTitle + "s",
                    Filter = FileDialogFilter,
                    Multiselect = true
                };
                FolderBrowserDialog fbd = new FolderBrowserDialog();
                if (ofd.ShowDialog() != DialogResult.OK)
                    return;
                if (fbd.ShowDialog() != DialogResult.OK)
                    return;
                var files = ofd.FileNames;
                try
                {
                    foreach (var filename in files)
                        WriteCSV(filename, fbd.SelectedPath + "\\" + Path.GetFileNameWithoutExtension(filename) + ".csv");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void WriteCSV(string filename, string output)
        {
            FileInfo fi = new FileInfo(filename);
            var batchGmd = new Gmd(fi.OpenRead());
            using (StreamWriter sw = new StreamWriter(output, false, new UTF8Encoding(false)))
            {
                foreach (var entry in batchGmd.Labels)
                {
                    var sName = batchGmd.GetLabelCount() > 0 ? entry.Name : "";
                    var sText = entry.Text.Replace("\r\n", "<br>");
                    sw.WriteLine(String.Format("{0}\0{1}\0{2}", entry.TextID, sName, sText));
                }
            }
        }

        private void ImportCSV()
        {
            if (_openFileName == "")
                return;
            var FilePathNoExt = Path.GetFileNameWithoutExtension(_openFileName);
            OpenFileDialog ofd = new OpenFileDialog
            {
                Title = "Select a CSV file",
                Filter = FilePathNoExt + ".csv|" + FilePathNoExt + ".csv"
            };
            if (ofd.ShowDialog() != DialogResult.OK)
                return;
            var SavePath = ofd.FileName;
            try
            {
                FileInfo fi = new FileInfo(SavePath);
                using (StreamReader sr = new StreamReader(fi.OpenRead()))
                {
                    while (!sr.EndOfStream)
                    {
                        var data = sr.ReadLine().Split('\0');
                        _gmd.Labels[Int32.Parse(data[0])].Text = data[2].Replace("<br>", "\r\n");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}