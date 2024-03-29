﻿using MHXXGMDTool.Properties;
using System;
using System.Diagnostics;
using System.Drawing;
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
            this.Icon = Icon.ExtractAssociatedIcon(Process.GetCurrentProcess().MainModule.FileName);
            toolStripComboBoxFontSize.SelectedIndex = 0;
            ClearForm();

            //Loading Settings
            if (Settings.Default.WindowLocation != new Point(-1, -1))
                this.Location = Settings.Default.WindowLocation;
        }

        private void Editor_DragEnter(object sender, DragEventArgs e)
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            if (files.Length > 0 && File.Exists(files[0]) && Path.GetExtension(files[0]) == ".gmd")
                e.Effect = DragDropEffects.Copy;
        }

        private void Editor_DragDrop(object sender, DragEventArgs e)
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop, false);
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

            //Saving Settings
            Settings.Default.WindowLocation = this.Location;
            Settings.Default.Save();
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
            if (_gmd.Labels[treeViewEntries.SelectedNode.Index].Text == textBoxText.Text)
                return;

            if (textBoxText.Text.Contains("<br>"))
            {
                textBoxText.Text = textBoxText.Text.Replace("<br>", "\r\n");
                textBoxText.SelectionStart = textBoxText.Text.Length;
                textBoxText.SelectionLength = 0;
            }

            UpdateGMDTable();
        }

        private void UpdateGMDTable()
        {
            _gmd.Labels[treeViewEntries.SelectedNode.Index].Text = textBoxText.Text;
            FileChanges();
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

                _hasChanges = false;
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

        private void importFromCSVToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ImportCSV();
        }

        private void importFromGMDToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ImportFromGMD();
        }

        private void batchExportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ExportCSV(true);
        }

        private void batchImportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ImportCSV(true);
        }

        private void batchImportGMDToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ImportFromGMD(true);
        }

        private void exportSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using var exportSettings = new ExportSettings();
            exportSettings.ShowDialog();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using var about = new About();
            about.ShowDialog();
        }

        private void toolStripButtonAddFontSize_Click(object sender, EventArgs e)
        {
            if (textBoxText.SelectedText == "")
                return;

            var fontSize = toolStripComboBoxFontSize.SelectedItem;
            var position = textBoxText.SelectionStart;
            var replacedText = $"<SIZE {fontSize}>{textBoxText.SelectedText}</SIZE>";

            textBoxText.SelectedText = replacedText;
            textBoxText.Select(position, replacedText.Length);
            textBoxText.Focus();

            UpdateGMDTable();
        }
    }
}