using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace FileWatcher.App
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        FileSystemWatcher watcher;
        static readonly object locker = new object();
        private Timer timer = new Timer();
        private bool isWatching;
        private bool canChange;
        private string filePath = string.Empty;
        DateTime lastRead = DateTime.MinValue;

        public MainWindow()
        {
            InitializeComponent();

            if (!string.IsNullOrEmpty(Settings.Default.PathSetting))
            {
                txtDirectory.Text = Settings.Default.PathSetting;
                ListDirectory(treeFiles, txtDirectory.Text);
            }
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {

            var dialog = new FolderBrowserDialog();

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtDirectory.Text = dialog.SelectedPath;
            }
            ListDirectory(treeFiles, txtDirectory.Text);

        }

        private static bool IsFileLocked(string file)
        {
            FileStream stream = null;

            try
            {
                stream = new FileInfo(file).Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (FileNotFoundException err)
            {
                throw err;
            }
            catch (IOException)
            {
                //the file is unavailable because it is:  
                //still being written to  
                //or being processed by another thread  
                //or does not exist (has already been processed)  
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            //file is not locked  
            return false;
        }

        public void AppendListViewcalls(string input)
        {
            this.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate ()
            {
                this.lstResults.Items.Add(input);

            }));
        }

        private void ListDirectory(System.Windows.Controls.TreeView treeView, string path)
        {
            try
            {
                treeView.Items.Clear();
                var rootDirectoryInfo = new DirectoryInfo(path);
                treeView.Items.Add(CreateDirectoryItems(rootDirectoryInfo));
            }
            catch (Exception ex)
            {
                AppendListViewcalls(ex.Message);
            }
        }

        private static TreeViewItem CreateDirectoryItems(DirectoryInfo directoryInfo)
        {
            var directoryItem = new TreeViewItem { Header = directoryInfo.Name };
            foreach (var directory in directoryInfo.GetDirectories())
                directoryItem.Items.Add(CreateDirectoryItems(directory));

            foreach (var file in directoryInfo.GetFiles())
                directoryItem.Items.Add(new TreeViewItem { Header = file.Name, Tag = file.FullName });

            return directoryItem;

        }

        private void btnListen_Click(object sender, RoutedEventArgs e)
        {
            //We want to check whether the filewatche is on or not and display usefull signal to the user either to start or stop  
            if (isWatching)
            {
                btnListen.Content = "Começar a observar";
                stopWatching();
            }
            else
            {
                btnListen.Content = "Parar de observar";
                startWatching();
            }

        }

        private void startWatching()
        {
            if (!isDirectoryValid(txtDirectory.Text))
            {
                AppendListViewcalls(DateTime.Now + " - Watch Directory Invalid");
                return;
            }
            isWatching = true;
            timer.Enabled = true;
            timer.Start();
            timer.Interval = 500;
            AppendListViewcalls(DateTime.Now + " - Watcher Started");

            watcher = new FileSystemWatcher();
            watcher.Path = txtDirectory.Text;
            watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
               | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            watcher.Filter = "*.*";
            watcher.Created += new FileSystemEventHandler(OnChanged);
            watcher.Renamed += new RenamedEventHandler(OnChanged);
            watcher.Changed += new FileSystemEventHandler(OnChanged);
            watcher.EnableRaisingEvents = true;
        }

        private void stopWatching()
        {
            isWatching = false;
            timer.Enabled = false;
            timer.Stop();
            AppendListViewcalls(DateTime.Now + " - Watcher Stopped");
        }

        private bool isDirectoryValid(string path)
        {
            if (Directory.Exists(path))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        protected void OnChanged(object source, FileSystemEventArgs e)
        {
            //Specify what to do when a file is changed, created, or deleted  

            //filter file types  
            if (Regex.IsMatch(System.IO.Path.GetExtension(e.FullPath), @"\.txt", RegexOptions.IgnoreCase))
            {
                try
                {
                    while (IsFileLocked(e.FullPath))
                    {
                        System.Threading.Thread.Sleep(100);
                    }

                    lock (locker)
                    {
                        //Process file  
                        //Do further activities  
                        DateTime lastWriteTime = File.GetLastWriteTime(e.FullPath);
                        if (lastWriteTime != lastRead)
                        {
                            AppendListViewcalls("File: \"" + e.FullPath + "\"- " + DateTime.Now + " - Processed the changes successfully");
                            lastRead = lastWriteTime;
                        }
                    }
                }
                catch (FileNotFoundException)
                {
                    //Stop processing  
                }
                catch (Exception ex)
                {
                    AppendListViewcalls("File: \"" + e.FullPath + "\" ERROR processing file (" + ex.Message + ")");
                }
            }

            else
                AppendListViewcalls("File: \"" + e.FullPath + "\" has been ignored");
        }

        private void treeFiles_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            try
            {
                var item = (TreeViewItem)e.NewValue;
                filePath = item.Tag.ToString();

                //Check changes for .txt files only  
                if (Regex.IsMatch(System.IO.Path.GetExtension(filePath), @"\.txt", RegexOptions.IgnoreCase))
                {
                    canChange = false;
                    txtEditor.Clear();
                    string contents = File.ReadAllText(filePath);
                    txtEditor.Text = contents;
                    canChange = true;
                }
            }
            catch (Exception ex)
            {
                AppendListViewcalls(ex.Message);
            }
        }

        private void txtEditor_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (canChange)
                {
                    System.IO.File.WriteAllText(filePath, txtEditor.Text);
                }
            }
            catch (Exception ex)
            {
                AppendListViewcalls(ex.Message);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //Save Path on closure  
            Settings.Default.PathSetting = txtDirectory.Text;
            Settings.Default.Save();
        }
    }
}
