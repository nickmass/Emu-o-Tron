/* This is far far far to slow to use and will be always unless I use
 * tons of win api calls and even then it would be slowish
 * Though I do like the idea of having a file browser I can customize
 * to hide unsupported mappers and display extra rom header info,
 * so I will leave this code in here even though it isn't being used
 * and if far from complete
 *
 */


using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DirectXEmu
{
    public partial class OpenRomDialog : Form
    {
        string currentDirectory;
        public OpenRomDialog()
        {
            InitializeComponent();
        }

        private void openRomDialog_Load(object sender, EventArgs e)
        {
            currentDirectory = "";
            directoryList.View = View.List;
            directoryList.FullRowSelect = true;
            directoryList.ItemSelectionChanged += new ListViewItemSelectionChangedEventHandler(directoryList_ItemSelectionChanged);
            string[] directoryPaths = Directory.GetLogicalDrives();
            foreach (string directory in directoryPaths)
            {
                ListViewItem directoryItem = new ListViewItem();
                if (currentDirectory.EndsWith(@"\") || currentDirectory == "")
                    directoryItem.Text = directory.Substring(currentDirectory.Length);
                else
                    directoryItem.Text = directory.Substring(currentDirectory.Length + 1);
                directoryItem.Tag = directory;
                directoryList.Items.Add(directoryItem);
            }

            if (currentDirectory != "")
            {
                string[] filePaths = Directory.GetFiles(currentDirectory);
                fileList.View = View.Details;
                fileList.Sorting = SortOrder.Ascending;
                fileList.FullRowSelect = true;
                ColumnHeader nameHeader = new ColumnHeader();
                nameHeader.Text = "Name";
                ColumnHeader sizeHeader = new ColumnHeader();
                sizeHeader.Text = "Size";
                fileList.Columns.Add(nameHeader);
                fileList.Columns.Add(sizeHeader);
                foreach (string file in filePaths)
                {
                    FileInfo fileInfo = new FileInfo(file);
                    ListViewItem fileListViewItem = new ListViewItem(new string[] { fileInfo.Name, (fileInfo.Length / 1024).ToString() + "KB" });
                    fileList.Items.Add(fileListViewItem);
                }
            }
        }

        void directoryList_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            try
            {
                directoryList.Items.Clear();
                currentDirectory = (string)e.Item.Tag;
                string[] directoryPaths;
                if (currentDirectory == "")
                {
                    directoryPaths = Directory.GetLogicalDrives();
                }
                else
                {
                    directoryPaths = Directory.GetDirectories(currentDirectory);
                    ListViewItem directoryItem = new ListViewItem();
                    directoryItem.Text = "..";
                    string upDir = currentDirectory.Substring(0, currentDirectory.LastIndexOf(@"\"));
                    if (upDir.IndexOf(@"\") == -1)
                        upDir += @"\";
                    directoryItem.Tag = upDir;
                    if (currentDirectory == upDir)
                        directoryItem.Tag = "";
                    directoryList.Items.Add(directoryItem);
                    Text = upDir;
                }
                foreach (string directory in directoryPaths)
                {
                    try
                    {
                        Directory.GetDirectories(directory);
                        ListViewItem directoryItem = new ListViewItem();
                        if (currentDirectory.EndsWith(@"\") || currentDirectory == "")
                            directoryItem.Text = directory.Substring(currentDirectory.Length);
                        else
                            directoryItem.Text = directory.Substring(currentDirectory.Length + 1);
                        directoryItem.Tag = directory;
                        directoryList.Items.Add(directoryItem);
                    }
                    catch (Exception dirExcep)
                    {

                    }
                }
                fileList.Clear();
                fileList.View = View.Details;
                fileList.Sorting = SortOrder.Ascending;
                fileList.FullRowSelect = true;
                ColumnHeader nameHeader = new ColumnHeader();
                nameHeader.Text = "Name";
                ColumnHeader sizeHeader = new ColumnHeader();
                sizeHeader.Text = "Size";
                fileList.Columns.Add(nameHeader);
                fileList.Columns.Add(sizeHeader);
                if (currentDirectory != "")
                {
                    string[] filePaths = Directory.GetFiles(currentDirectory);
                    foreach (string file in filePaths)
                    {
                        FileInfo fileInfo = new FileInfo(file);
                        ListViewItem fileListViewItem = new ListViewItem(new string[] { fileInfo.Name, (fileInfo.Length / 1024).ToString() + "KB" });
                        fileList.Items.Add(fileListViewItem);
                    }
                }
            }
            catch(Exception excep)
            {
                MessageBox.Show(excep.Message);
            }
        }
    }
}
