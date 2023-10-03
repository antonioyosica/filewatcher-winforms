using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FileWatcher.App
{
    public partial class MainForm : Form
    {
        private FileSystemWatcher fileSystemWatcher = new FileSystemWatcher();

        public MainForm()
        {
            InitializeComponent();

            // Configure the FileSystemWatcher
            fileSystemWatcher.Path = @"C:\Users\a.yosica\Documents\Docs";
            fileSystemWatcher.Filter = "*.txt";
            fileSystemWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;

            fileSystemWatcher.Changed += FileSystemWatcher_Changed;
            fileSystemWatcher.Created += FileSystemWatcher_Created;
            fileSystemWatcher.Deleted += FileSystemWatcher_Deleted;
        }

        private void FileSystemWatcher_Deleted(object sender, FileSystemEventArgs e)
        {
            // Handle file deletions
            string message = $"File {e.Name} has been deleted from {e.FullPath}";
            MessageBox.Show(message);
        }

        private void FileSystemWatcher_Created(object sender, FileSystemEventArgs e)
        {
            // Handle file creations
            string message = $"File {e.Name} has been created at {e.FullPath}";
            MessageBox.Show(message);
        }

        private void FileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            // Handle file changes
            string message = $"File {e.Name} has been changed at {e.FullPath}";
            MessageBox.Show(message);
        }
    }
}
