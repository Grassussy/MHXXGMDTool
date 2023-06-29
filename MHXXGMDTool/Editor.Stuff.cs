using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace MHXXGMDTool
{
    partial class Editor
    {
        private Gmd _gmd;

        private readonly string formTitle = "MHXX GMD Tool v" + FileVersionInfo.GetVersionInfo("MHXXGMDTool.exe").ProductVersion;
        private readonly string fileDialogTitle = "Select MHXX GMD file";
        private readonly string fileDialogFilter = "MHXX GMD files|*.gmd";

        private string _openFileName = "";

        private bool _fileOpen;
        private bool _hasChanges;

        private string TitleName()
        {
            return formTitle + " - " + Path.GetFileName(_openFileName);
        }

        private void FileChanges()
        {
            this.Text = TitleName() + "*";
            saveToolStripMenuItem.Enabled = true;
            _hasChanges = true;
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
                Title = fileDialogTitle,
                Filter = fileDialogFilter
            };

            var dr = DialogResult.OK;

            if (filename == string.Empty)
                dr = ofd.ShowDialog();

            if (dr != DialogResult.OK)
                return;

            if (filename == string.Empty)
                filename = ofd.FileName;

            ofd.Dispose();

            CloseFile();

            _openFileName = filename;

            try
            {
                var fiInput = new FileInfo(filename);

                _gmd = new Gmd(fiInput.OpenRead());

                if (_gmd.Labels.Count > 0)
                {
                    UpdateForm();
                    _fileOpen = true;
                    _hasChanges = false;
                }
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
            sfd.Filter = fileDialogFilter;
            sfd.FileName = Path.GetFileNameWithoutExtension(_openFileName);

            if (_openFileName == "" || saveAs)
                dr = sfd.ShowDialog();

            if ((_openFileName == "" || saveAs) && dr == DialogResult.OK)
                _openFileName = sfd.FileName;

            if (dr != DialogResult.OK)
                return dr;

            sfd.Dispose();

            try
            {
                var fiInput = new FileInfo(_openFileName);
                var i = treeViewEntries.SelectedNode.Index;

                _gmd.Save(fiInput.Create());

                _hasChanges = false;

                if (saveAs)
                {
                    CloseFile();
                    _gmd = new Gmd(fiInput.OpenRead());
                    _openFileName = fiInput.Name;
                    UpdateForm(i);
                }
                else
                {
                    this.Text = this.Text.Replace("*", "");
                    saveToolStripMenuItem.Enabled = false;
                }
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

            this.Text = formTitle;

            saveToolStripMenuItem.Enabled = false;
            saveAsToolStripMenuItem.Enabled = false;
            closeToolStripMenuItem.Enabled = false;
            exportToCSVToolStripMenuItem.Enabled = false;
            importFromCSVToolStripMenuItem.Enabled = false;
            importFromGMDToolStripMenuItem.Enabled = false;

            toolStripComboBoxFontSize.Enabled = false;
            toolStripButtonAddFontSize.Enabled = false;

            textBoxText.Enabled = false;
        }

        private void UpdateForm(int index = 0)
        {
            treeViewEntries.BeginUpdate();
            foreach (var entry in _gmd.Labels)
                treeViewEntries.Nodes.Add((entry.TextID + 1).ToString("00000") + " " + entry.Name);
            treeViewEntries.SelectedNode = treeViewEntries.Nodes[index];
            treeViewEntries.EndUpdate();

            this.ActiveControl = null;

            this.Text = TitleName();

            saveAsToolStripMenuItem.Enabled = true;
            closeToolStripMenuItem.Enabled = true;
            exportToCSVToolStripMenuItem.Enabled = true;
            importFromCSVToolStripMenuItem.Enabled = true;
            importFromGMDToolStripMenuItem.Enabled = true;

            toolStripComboBoxFontSize.Enabled = true;
            toolStripButtonAddFontSize.Enabled = true;

            textBoxText.Enabled = true;
        }

        private void ExportCSV(bool isBatch = false)
        {
            string[] files;
            string source;
            string destination;

            var sfd = new SaveFileDialog();
            var fbd = new FolderBrowserDialog();

            if (isBatch)
            {
                fbd.UseDescriptionForTitle = true;
                fbd.Description = "Select source folder containing MHXX GMD files";
                if (fbd.ShowDialog() != DialogResult.OK)
                    return;
                source = fbd.SelectedPath;

                fbd.Description = "Select destination folder";
                if (fbd.ShowDialog() != DialogResult.OK)
                    return;
                destination = fbd.SelectedPath;

                files = Directory.GetFiles(source, "*.gmd", SearchOption.AllDirectories);

                fbd.Dispose();
            }
            else
            {
                sfd.Title = "Export to CSV";
                sfd.Filter = "CSV File|*.csv";
                sfd.FileName = Path.GetFileNameWithoutExtension(_openFileName);

                if (sfd.ShowDialog() != DialogResult.OK)
                    return;

                files = new[] { _openFileName };
                source = "";
                destination = sfd.FileName;

                sfd.Dispose();
            }

            try
            {
                var listFailed = new List<string>();
                var countTrue = 0;
                var countFalse = 0;

                foreach (var input in files)
                {
                    var output = isBatch ? destination + input.Replace(source, "").Replace(".gmd", ".csv") : destination;

                    if (WriteCSV(input, isBatch, output))
                        countTrue++;
                    else
                    {
                        countFalse++;
                        if (isBatch)
                            listFailed.Add(input.Replace(destination, ""));
                    }
                }

                if (isBatch)
                {
                    if (countFalse == 0)
                        MessageBox.Show("Batch export completed successfully.\n" + countTrue + " file(s) successfully exported.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    else if (countTrue == 0)
                        MessageBox.Show("Failed to batch export.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    else
                    {
                        var list = "";
                        foreach (var s in listFailed)
                            list += s.Replace(source + "\\", "") + Environment.NewLine;
                        MessageBox.Show("Only " + countTrue + " out of " + (countTrue + countFalse) + " file(s) successfully exported.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        CreateLogForUnsuccessfulFiles("export", list);
                    }
                }
                else
                {
                    if (countFalse == 0)
                        MessageBox.Show("Export CSV completed successfully.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    else
                        MessageBox.Show("Failed to export to CSV.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool WriteCSV(string input, bool isBatch, string output)
        {
            var fiInput = new FileInfo(input);
            var fiOutput = new FileInfo(output);
            Gmd inputGMD;

            fiOutput.Directory.Create();

            if (isBatch)
                inputGMD = new Gmd(fiInput.OpenRead());
            else
                inputGMD = _gmd;

            if (inputGMD.Labels.Count == 0)
                return false;

            using (var sw = new StreamWriter(output, false, new UTF8Encoding(false)))
            {
                foreach (var entry in inputGMD.Labels)
                {
                    var sName = inputGMD.GetRealLabelCount() > 0 ? (entry.Name != "" ? entry.Name : "unnamed_" + (entry.TextID + 1).ToString("00000")) : "unnamed_" + (entry.TextID + 1).ToString("00000");
                    var sText = entry.Text.Replace("\r\n", "<br>");

                    if (Properties.Settings.Default.Export_IncludeID == true)
                        sw.Write(entry.TextID + "\t");
                    if (Properties.Settings.Default.Export_IncludeName == true && inputGMD.GetRealLabelCount() > 0)
                        sw.Write(sName + "\t");

                    sw.WriteLine(sText);
                }
            }

            return true;
        }

        private void ImportCSV(bool isBatch = false)
        {
            string[] files;
            string source;
            string destination;

            var ofd = new OpenFileDialog();
            var fbd = new FolderBrowserDialog();

            if (isBatch)
            {
                fbd.UseDescriptionForTitle = true;
                fbd.Description = "Select source folder containing CSV files";
                if (fbd.ShowDialog() != DialogResult.OK)
                    return;
                source = fbd.SelectedPath;

                fbd.Description = "Select destination folder containing MHXX GMD files";
                if (fbd.ShowDialog() != DialogResult.OK)
                    return;
                destination = fbd.SelectedPath;

                files = Directory.GetFiles(source, "*.csv", SearchOption.AllDirectories);

                fbd.Dispose();
            }
            else
            {
                var s = Path.GetFileNameWithoutExtension(_openFileName);

                ofd.Title = "Select a CSV file";
                ofd.Filter = s + ".csv|" + s + ".csv";
                ofd.FileName = s;

                if (ofd.ShowDialog() != DialogResult.OK)
                    return;

                files = new[] { ofd.FileName };
                source = "";
                destination = "";

                ofd.Dispose();
            }

            try
            {
                var listFailed = new List<string>();
                var countTrue = 0;
                var countFalse = 0;

                foreach (var input in files)
                {
                    var output = isBatch ? destination + input.Replace(source, "").Replace(".csv", ".gmd") : "";

                    if (ImportData(input, isBatch, output))
                        countTrue++;
                    else
                    {
                        countFalse++;
                        if (isBatch)
                            listFailed.Add(input.Replace(destination, ""));
                    }
                }

                if (isBatch)
                {
                    if (countFalse == 0)
                        MessageBox.Show("Batch import completed successfully.\n" + countTrue + " file(s) successfully imported.", "Import", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    else if (countTrue == 0)
                        MessageBox.Show("Failed to batch import.", "Import", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    else
                    {
                        var list = "";
                        foreach (var s in listFailed)
                            list += s.Replace(source + "\\", "") + Environment.NewLine;
                        MessageBox.Show("Only " + countTrue + " out of " + (countTrue + countFalse) + " file(s) successfully imported.", "Import", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        CreateLogForUnsuccessfulFiles("import", list);
                    }
                }
                else
                {
                    if (countFalse == 0)
                    {
                        textBoxText.Text = _gmd.Labels[treeViewEntries.SelectedNode.Index].Text;
                        FileChanges();
                        MessageBox.Show("Import from CSV completed successfully.", "Import", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                        MessageBox.Show("Failed to import from CSV.", "Import", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool ImportData(string input, bool isBatch, string output)
        {
            var fiInput = new FileInfo(input);
            var fiOutput = isBatch ? new FileInfo(output) : null;
            Gmd outputGMD;

            if (isBatch)
                if (fiOutput.Exists == true)
                    outputGMD = new Gmd(fiOutput.OpenRead());
                else
                    return false;
            else
                outputGMD = _gmd;

            using var sr = new StreamReader(fiInput.OpenRead(), new UTF8Encoding(false));

            if (File.ReadAllLines(input).Length == outputGMD.Labels.Count)
            {
                var i = 0;

                while (!sr.EndOfStream)
                {
                    var data = sr.ReadLine().Split('\t');
                    outputGMD.Labels[i].Text = data[^1].Replace("<br>", "\r\n");
                    i++;
                }

                if (isBatch)
                    outputGMD.Save(fiOutput.Create());

                return true;
            }
            else
                return false;
        }

        private void ImportFromGMD(bool isBatch = false)
        {
            string[] files;
            string source;
            string destination;

            var ofd = new OpenFileDialog();
            var fbd = new FolderBrowserDialog();

            if (isBatch)
            {
                fbd.UseDescriptionForTitle = true;
                fbd.Description = "Select source folder containing MHXX GMD files";
                if (fbd.ShowDialog() != DialogResult.OK)
                    return;
                source = fbd.SelectedPath;

                fbd.Description = "Select destination folder containing MHXX GMD files";
                if (fbd.ShowDialog() != DialogResult.OK)
                    return;
                destination = fbd.SelectedPath;

                files = Directory.GetFiles(source, "*.gmd", SearchOption.AllDirectories);

                fbd.Dispose();
            }
            else
            {
                var s = Path.GetFileNameWithoutExtension(_openFileName);

                ofd.Title = fileDialogTitle;
                ofd.Filter = s + ".gmd|" + s + ".gmd";
                ofd.FileName = s;

                if (ofd.ShowDialog() != DialogResult.OK)
                    return;

                files = new[] { ofd.FileName };
                source = "";
                destination = "";

                ofd.Dispose();
            }

            try
            {
                var listFailed = new List<string>();
                var countTrue = 0;
                var countFalse = 0;

                foreach (var input in files)
                {
                    var output = isBatch ? destination + input.Replace(source, "") : "";

                    if (ImportGMDData(input, isBatch, output))
                        countTrue++;
                    else
                    {
                        countFalse++;
                        if (isBatch)
                            listFailed.Add(input.Replace(destination, ""));
                    }
                }

                if (isBatch)
                {
                    if (countFalse == 0)
                        MessageBox.Show("Batch import (GMD) completed successfully.\n" + countTrue + " file(s) successfully imported.", "Import", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    else if (countTrue == 0)
                        MessageBox.Show("Failed to batch import (GMD).", "Import", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    else
                    {
                        var list = "";
                        foreach (var s in listFailed)
                            list += s.Replace(source + "\\", "") + Environment.NewLine;
                        MessageBox.Show("Only " + countTrue + " out of " + (countTrue + countFalse) + " file(s) successfully imported.", "Import", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        CreateLogForUnsuccessfulFiles("import", list);
                    }
                }
                else
                {
                    if (countFalse == 0)
                    {
                        textBoxText.Text = _gmd.Labels[treeViewEntries.SelectedNode.Index].Text;
                        FileChanges();
                        MessageBox.Show("Import from GMD completed successfully.", "Import", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                        MessageBox.Show("Failed to import from GMD.", "Import", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool ImportGMDData(string filename, bool isBatch, string output)
        {
            var fiInput = new FileInfo(filename);
            var fiOutput = isBatch ? new FileInfo(output) : null;
            var inputGMD = new Gmd(fiInput.OpenRead());
            Gmd outputGMD;

            if (isBatch)
                if (fiOutput.Exists == true)
                    outputGMD = new Gmd(fiOutput.OpenRead());
                else
                    return false;
            else
                outputGMD = _gmd;

            for (var i = 0; i < outputGMD.Labels.Count; i++)
                if (outputGMD.GetRealLabelCount() == 0)
                    if (outputGMD.Labels.Count == inputGMD.Labels.Count)
                        outputGMD.Labels[i].Text = inputGMD.Labels[i].Text;
                    else
                        return false;
                else
                    foreach (var inputLabels in inputGMD.Labels)
                        if (outputGMD.Labels[i].Name == inputLabels.Name)
                        {
                            outputGMD.Labels[i].Text = inputLabels.Text;
                            break;
                        }

            if (isBatch)
                outputGMD.Save(fiOutput.Create());

            return true;
        }

        private void CreateLogForUnsuccessfulFiles(string s, string list)
        {
            try
            {
                using (var sr = new StreamWriter("log.txt", false, new UTF8Encoding(false)))
                    sr.Write("List of files that failed to " + s + ":" + Environment.NewLine + Environment.NewLine + list);
                File.SetAttributes("log.txt", FileAttributes.Hidden | FileAttributes.System);
                using (var process = new Process())
                {
                    process.StartInfo.FileName = "notepad.exe";
                    process.StartInfo.Arguments = "log.txt";
                    process.Start();
                    process.WaitForExit();
                }
                File.Delete("log.txt");
            }
            catch (Exception ex)
            {
                if ((new FileInfo("log.txt").Exists))
                    File.Delete("log.txt");
                MessageBox.Show(this, ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}