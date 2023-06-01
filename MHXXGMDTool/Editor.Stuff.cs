using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace MHXXGMDTool
{
    partial class Editor
    {
        private Gmd _gmd;
        private string OpenedFilePath;
        private string FileDialogTitle = "Select MHXX/MHGU GMD file";
        private string FileDialogFilter = "MHXX/MHGU GMD files|*.gmd";

        private void OpenFile(string fileDragDrop, bool isDragDrop)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Title = FileDialogTitle,
                Filter = FileDialogFilter
            };
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
            UpdateForm(0);
        }

        private void SaveFile(bool isSaveAs)
        {
            if (isSaveAs)
            {
                SaveFileDialog sfd = new SaveFileDialog
                {
                    Title = "Save as",
                    Filter = FileDialogFilter,
                    FileName = Path.GetFileNameWithoutExtension(OpenedFilePath)
                };
                if (sfd.ShowDialog() != DialogResult.OK)
                    return;
                OpenedFilePath = sfd.FileName;
            }
            FileInfo fi = new FileInfo(OpenedFilePath);
            _gmd.Save(fi.OpenWrite());
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

            Text = FormTitle;
            saveToolStripMenuItem.Enabled = false;
            saveAsToolStripMenuItem.Enabled = false;
            closeToolStripMenuItem.Enabled = false;
            exportToCSVToolStripMenuItem.Enabled = false;
            importFromCSVToolStripMenuItem.Enabled = false;
        }

        private void UpdateForm(uint index)
        {
            foreach (var entry in _gmd.Labels)
                treeViewEntries.Nodes.Add(entry.Name);
            treeViewEntries.SelectedNode = treeViewEntries.Nodes[(int)index];

            Text = FormTitle + " - " + Path.GetFileName(OpenedFilePath);
            saveToolStripMenuItem.Enabled = true;
            saveAsToolStripMenuItem.Enabled = true;
            closeToolStripMenuItem.Enabled = true;
            exportToCSVToolStripMenuItem.Enabled = true;
            importFromCSVToolStripMenuItem.Enabled = true;
        }

        private void ExportToCSV()
        {
            if (OpenedFilePath == "" || OpenedFilePath == null)
                return;
            SaveFileDialog sfd = new SaveFileDialog
            {
                Title = "Export to CSV",
                Filter = "CSV File|*.csv",
                FileName = Path.GetFileNameWithoutExtension(OpenedFilePath)
            };
            if (sfd.ShowDialog() != DialogResult.OK)
                return;
            var SavePath = sfd.FileName;
            sfd.Dispose();
            WriteCSV(OpenedFilePath, SavePath);
        }

        private void BatchExportCSV()
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
            foreach (var filename in files)
                WriteCSV(filename, fbd.SelectedPath + "\\" + Path.GetFileNameWithoutExtension(filename) + ".csv");
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
                    //sw.WriteLine(String.Format("\"{0}\",\"{1}\",\"{2}\"", entry.TextID, sName, sText));
                    sw.WriteLine(String.Format("{0}\0{1}\0{2}", entry.TextID, sName, sText));
                }
            }
        }

        private void ImportCSV()
        {
            if (OpenedFilePath == "" || OpenedFilePath == null)
                return;
            var FilePathNoExt = Path.GetFileNameWithoutExtension(OpenedFilePath);
            OpenFileDialog ofd = new OpenFileDialog
            {
                Title = "Select a CSV file",
                Filter = FilePathNoExt + ".csv|" + FilePathNoExt + ".csv"
            };
            if (ofd.ShowDialog() != DialogResult.OK)
                return;
            var SavePath = ofd.FileName;
            FileInfo fi = new FileInfo(SavePath);
            using (StreamReader sr = new StreamReader(fi.OpenRead()))
            {
                while (!sr.EndOfStream)
                {
                    //var data = Regex.Split(sr.ReadLine(), ",(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");
                    var data = sr.ReadLine().Split('\0');
                    //_gmd.Labels[i].Text = data[2].Replace("<br>", "\r\n").Replace("\"", "");
                    _gmd.Labels[(int)UInt32.Parse(data[0])].Text = data[2].Replace("<br>", "\r\n");
                }
            }
        }
    }
}