using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace MHXXGMDTool
{
    partial class Editor
    {
        private Gmd _gmd;

        private readonly string FormTitle = "MHXX/MHGU GMD Tool v" + Assembly.GetExecutingAssembly().GetName().Version.ToString();
        private readonly string FileDialogTitle = "Select MHXX/MHGU GMD file";
        private readonly string FileDialogFilter = "MHXX/MHGU GMD files|*.gmd";

        private string _openFileName = "";
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
                var fi = new FileInfo(_openFileName);
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
            string[] files;
            string savePath;
            string source;
            string destination;

            var sfd = new SaveFileDialog
            {
                Title = "Export to CSV",
                Filter = "CSV File|*.csv",
                FileName = Path.GetFileNameWithoutExtension(_openFileName)
            };
            var fbd = new FolderBrowserDialog();

            if (isBatch)
            {
                fbd.Description = "Select folder that contains MHXX GMD files";
                if (fbd.ShowDialog() != DialogResult.OK) return;
                source = fbd.SelectedPath;
                fbd.Description = "Select destination folder";
                if (fbd.ShowDialog() != DialogResult.OK) return;
                destination = fbd.SelectedPath;

                files = Directory.GetFiles(source, "*.gmd", SearchOption.AllDirectories);
                savePath = destination;
                fbd.Dispose();
            }
            else
            {
                if (sfd.ShowDialog() != DialogResult.OK) return;
                files = new[] { _openFileName };
                savePath = sfd.FileName;
                source = "";
                sfd.Dispose();
            }

            try
            {
                var fileCount = 0;
                foreach (var fileName in files)
                {
                    WriteCSV(fileName, isBatch ? savePath + fileName.Replace(source, "").Replace(".gmd", ".csv") : savePath);
                    fileCount++;
                }
                MessageBox.Show(isBatch ? "Batch export to CSV completed successfully.\n" + fileCount + " file(s) successfully exported." : "Export to CSV completed successfully.", "Export to CSV", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void WriteCSV(string filename, string output)
        {
            var fi = new FileInfo(filename);
            (new FileInfo(output)).Directory.Create();
            var batchGmd = new Gmd(fi.OpenRead());
            using var sw = new StreamWriter(output, false, new UTF8Encoding(false));
            foreach (var entry in batchGmd.Labels)
            {
                var sName = batchGmd.GetLabelCount() > 0 ? entry.Name : "";
                var sText = entry.Text.Replace("\r\n", "<br>");
                sw.WriteLine(String.Format("{0}\0{1}\0{2}", entry.TextID, sName, sText));
            }
        }

        private void ImportCSV()
        {
            if (_openFileName == "")
                return;
            var FilePathNoExt = Path.GetFileNameWithoutExtension(_openFileName);
            var ofd = new OpenFileDialog
            {
                Title = "Select a CSV file",
                Filter = FilePathNoExt + ".csv|" + FilePathNoExt + ".csv"
            };
            if (ofd.ShowDialog() != DialogResult.OK)
                return;
            var SavePath = ofd.FileName;
            try
            {
                var fi = new FileInfo(SavePath);
                using (var sr = new StreamReader(fi.OpenRead()))
                {
                    while (!sr.EndOfStream)
                    {
                        var data = sr.ReadLine().Split('\0');
                        _gmd.Labels[Int32.Parse(data[0])].Text = data[2].Replace("<br>", "\r\n");
                    }
                }
                MessageBox.Show("Import from CSV completed successfully.", "Import from CSV", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}