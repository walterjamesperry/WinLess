﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using WinLess.Models;
using WinLess.Helpers;
using WinLess.Less;

namespace WinLess
{
    public partial class mainForm : Form
    {
        private static mainForm activeOrInActiveMainForm = null;
        public static mainForm ActiveOrInActiveMainForm { 
            get {
                return activeOrInActiveMainForm;
            }
        }
        

        private bool finishedLoading;
        private bool doInitialCompile;
        
        private delegate void AddCompileResultDelegate(Models.CompileResult result);
        
        public mainForm(bool doInitialCompile)
        {
            try
            {
                activeOrInActiveMainForm = this;
                
                InitializeComponent();
                initFilesDataGridViewCheckAllCheckBox();
                foldersListBox.DataSource = Program.Settings.DirectoryList.Directories;
                compileResultsDataGridView.DataSource = new List<Models.CompileResult>();

                this.finishedLoading = false;
                this.doInitialCompile = doInitialCompile;
            }
            catch (Exception e)
            {
                ExceptionHandler.LogException(e);
            }
        }
   
        #region filesTabPage

        #region foldersListBox

        #region Events

        private void foldersListBox_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy; // Okay
            }
            else
            {
                e.Effect = DragDropEffects.None; // Unknown data, ignore it
            }
        }

        private void foldersListBox_DragDrop(object sender, DragEventArgs e)
        {
            string[] fullPaths = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (string fullPath in fullPaths)
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(fullPath);
                if (directoryInfo.Exists && !foldersListBox.Items.Contains(directoryInfo.FullName))
                {
                    Program.Settings.DirectoryList.AddDirectory(directoryInfo.FullName);
                    foldersListBox_DataChanged();
                    selectDirectory();
                    Program.Settings.SaveSettings();
                }
            }
        }

        private void foldersListBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                removeDirectory();
            }
        }

        private void foldersListBox_SelectedValueChanged(object sender, EventArgs e)
        {
            selectDirectory();
        }

        private void foldersListBox_DataChanged()
        {
            ((CurrencyManager)foldersListBox.BindingContext[foldersListBox.DataSource]).Refresh();
            filesDataGridView_DataChanged();
        }

        #endregion

        #region Methods

        private void removeDirectory()
        {
            if (foldersListBox.SelectedItem != null)
            {
                Program.Settings.DirectoryList.RemoveDirectory((Models.Directory)foldersListBox.SelectedItem);
                foldersListBox_DataChanged();
                filesDataGridView.DataSource = new List<Models.File>();
                filesDataGridView_DataChanged();
                Program.Settings.SaveSettings();
            }
        }


        private void selectDirectory()
        {
            Models.Directory directory = (Models.Directory)foldersListBox.SelectedItem;
            if (directory != null)
            {
                filesDataGridView.DataSource = directory.Files;
            }
            else
            {
                filesDataGridView.DataSource = new List<Models.File>();
            }

            filesDataGridView_DataChanged();
        }

        #endregion

        #endregion

        #region filesDataGridView

        #region Events

        private void filesDataGridView_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            Program.Settings.SaveSettings();
        }

        private void filesDataGridView_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            filesDataGridView_OpenSelectedFile();
        }

        private void filesDataGridView_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex >= 0 && e.RowIndex >= 0)
            {
                filesDataGridView.CurrentCell = filesDataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex];
            }
        }

        #endregion

        #region Methods

        private void initFilesDataGridViewCheckAllCheckBox()
        {
            // add checkbox header
            Rectangle rect = filesDataGridView.GetCellDisplayRectangle(0, -1, true);
            // set checkbox header to center of header cell. +1 pixel to position correctly.
            rect.X = 10;
            rect.Y = 4;

            CheckBox checkAllFilesCheckbox = new CheckBox();
            checkAllFilesCheckbox.Name = "checkboxHeader";
            checkAllFilesCheckbox.Size = new Size(15, 15);
            checkAllFilesCheckbox.Location = rect.Location;
            checkAllFilesCheckbox.CheckedChanged += new EventHandler(checkAllFilesCheckbox_CheckedChanged);

            filesDataGridView.Controls.Add(checkAllFilesCheckbox);
        }

        private void checkAllFilesCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            for (int i = 0; i < filesDataGridView.RowCount; i++)
            {
                filesDataGridView[0, i].Value = ((CheckBox)filesDataGridView.Controls.Find("checkboxHeader", true)[0]).Checked;
            }
            filesDataGridView.EndEdit();
        }
        
        private void filesDataGridView_DataChanged()
        {
            List<Models.File> files = (List<Models.File>)filesDataGridView.DataSource;
            files.Sort((x, y) => string.Compare(x.FullPath, y.FullPath));
            ((CurrencyManager)filesDataGridView.BindingContext[filesDataGridView.DataSource]).Refresh();
        }  

        private void filesDataGridView_OpenSelectedFile()
        {
            DataGridViewCell cell = filesDataGridView.SelectedCells[0];
            Models.File file = (Models.File)cell.OwningRow.DataBoundItem;
            string filePath;
            if (cell.ColumnIndex == 1)
            {
                filePath = file.FullPath;
            }
            else
            {
                filePath = file.OutputPath;
            }

            if (System.IO.File.Exists(filePath))
            {
                Process process = new Process();
                process.StartInfo.FileName = filePath;
                process.Start();
            }
        }

        #endregion

        #region fileContextMenuStrip

        private void openFiletoolStripMenuItem_Click(object sender, EventArgs e)
        {
            filesDataGridView_OpenSelectedFile();
        }

        private void fileOpenFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DataGridViewCell cell = filesDataGridView.SelectedCells[0];
            Models.File file = (Models.File)cell.OwningRow.DataBoundItem;
            string filePath;
            if (cell.ColumnIndex == 1)
            {
                filePath = file.FullPath;
            }
            else
            {
                filePath = file.OutputPath;
            }

            FileInfo fileInfo = new FileInfo(filePath);
            string directoryPath = fileInfo.DirectoryName;

            if (System.IO.Directory.Exists(directoryPath))
            {
                Process process = new Process();
                process.StartInfo.FileName = directoryPath;
                process.Start();
            }
        }

        private void fileSelectOutputToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DataGridViewCell cell = filesDataGridView.SelectedCells[0];
            Models.File file = (Models.File)cell.OwningRow.DataBoundItem;
            FileInfo fileInfo = new FileInfo(file.OutputPath);
            outputFolderBrowserDialog.SelectedPath = fileInfo.DirectoryName;
            if (outputFolderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                file.OutputPath = string.Format("{0}\\{1}", outputFolderBrowserDialog.SelectedPath, fileInfo.Name);
                filesDataGridView_DataChanged();
                Program.Settings.SaveSettings();
            }
        }

        #endregion

        #endregion

        #region Buttons

        private void addDirectoryButton_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                Program.Settings.DirectoryList.AddDirectory(folderBrowserDialog.SelectedPath);
                foldersListBox_DataChanged();
                Program.Settings.SaveSettings();
            }
        }

        private void removeDirectoryButton_Click(object sender, EventArgs e)
        {
            removeDirectory();
        }

        private void refreshDirectoryButton_Click(object sender, EventArgs e)
        {
            if (foldersListBox.SelectedItem != null)
            {
                Models.Directory directory = (Models.Directory)foldersListBox.SelectedItem;
                directory.Refresh();
                selectDirectory();
                Program.Settings.SaveSettings();
            }
        }

        private void compileSelectedButton_Click(object sender, EventArgs e)
        {
            List<Models.File> files = (List<Models.File>)filesDataGridView.DataSource;
            foreach (Models.File file in files)
            {
                if (file.Enabled)
                {
                    Compiler.CompileLessFile(file.FullPath, file.OutputPath, file.Minify);
                }
            }
        }

        #endregion

        #endregion

        #region compilerTabPage

        #region compileResultsDataGridView
        
        public void addCompileResult(Models.CompileResult result)
        {
            if (InvokeRequired)
            {
                this.Invoke(new AddCompileResultDelegate(addCompileResult), new object[] { result });
                return;
            }

            List<Models.CompileResult> compileResults = (List<Models.CompileResult>)compileResultsDataGridView.DataSource;
            compileResults.Insert(0, result);
            compileResultsDataGridView_DataChanged();

            if (string.Compare(result.ResultText, "success", StringComparison.InvariantCultureIgnoreCase) != 0)
            {
                showErrorNotification("Compile error", result.ResultText);
            }
            else if (Program.Settings.ShowSuccessMessages)
            {
                showSuccessNotification("Successful compile", result.FullPath);
            }
        }

        private void compileResultsDataGridView_DataChanged()
        {
            ((CurrencyManager)compileResultsDataGridView.BindingContext[compileResultsDataGridView.DataSource]).Refresh();
        }

        #endregion

        #region Buttons

        private void clearCompileResultsButton_Click(object sender, EventArgs e)
        {
            compileResultsDataGridView.DataSource = new List<Models.CompileResult>();
            compileResultsDataGridView_DataChanged();
        }
        
        #endregion

        #endregion

        #region notifyIcon

        #region Events

        private void mainForm_Resize(object sender, EventArgs e)
        {
            if (FormWindowState.Minimized == this.WindowState)
            {
                this.Hide();
            }
        }

        private void notifyIcon_DoubleClick(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                restoreApp();
            }
            else
            {
                minimizeApp();
            }
        }

        private void notifyIcon_BalloonTipClicked(object sender, EventArgs e)
        {
            restoreApp();
            tabControl.SelectTab(compilerTabPage);
        }

        #endregion

        #region Methods

        private void minimizeApp()
        {
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.Hide();
        }

        private void restoreApp()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
        }

        private void showSuccessNotification(string title, string message){
            notifyIcon.ShowBalloonTip(500, title, message, ToolTipIcon.Info);
        }

        private void showErrorNotification(string title, string message)
        {
            notifyIcon.ShowBalloonTip(500, title, message, ToolTipIcon.Error);
        }

        #endregion

        #region contextMenu

        private void notifyIconMenuOpen_Click(object sender, EventArgs e)
        {
            restoreApp();
        }

        private void notifyIconMenuExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        #endregion

        #endregion

        #region menu

        #region Events

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            settingsForm form = new settingsForm();
            form.ShowDialog(this);
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            aboutForm form = new aboutForm();
            form.ShowDialog(this);
        }

        #endregion

        private void mainForm_Activated(object sender, EventArgs e)
        {
            if (!this.finishedLoading)
            {
                this.finishedLoading = true;

                if (Program.Settings.StartMinified)
                {
                    minimizeApp();
                }

                if (this.doInitialCompile)
                {
                    Program.Settings.DirectoryList.Directories.ForEach(d => d.Files.ForEach(f => f.Compile(false)));
                }         
            }
        }

        #endregion 
    }
}
