using System;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.IO;
using System.IO.Compression;
using Newtonsoft.Json.Linq;

namespace RadiumQuickDeploy
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string dots = "";
        string msg = "";

        public MainWindow()
        {
            InitializeComponent();
        }

        public bool OutputData()
        {
            bool zipDone = false;

            if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Radium"))
            {
                Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Radium");
            }

            string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Radium";
            try
            {
                if (File.Exists(path + "\\data.zip"))
                {
                    File.Delete(path + "\\data.zip");
                }

                using (var client = new WebClient())
                {
                    client.Headers.Add("user-agent", "Mozilla / 4.0(compatible; MSIE 6.0; Windows NT 5.2;)");

                    client.DownloadProgressChanged += (s, e) =>
                    {
                        msg = "Downloading Install Data (" + e.ProgressPercentage + "%)";
                    };
                    client.DownloadFileCompleted += (s, e) =>
                    {
                        zipDone = true;
                    };
                    client.DownloadFileAsync(new Uri("https://github.com/JJ12880/Rapid_Deploy/releases/download/1.0/data.zip"),
                        path + "\\data.zip");
                }
            }
            catch (Exception e)
            {
                if (File.Exists(path + "\\data.zip"))
                {
                    File.Delete(path + "\\data.zip");
                }
                return false;
            }

            try
            {
                
                if (!File.Exists(path + "\\Radium-qt.exe"))
                {
                    using (var client = new WebClient())
                    {
                        client.Headers.Add("user-agent", "Mozilla / 4.0(compatible; MSIE 6.0; Windows NT 5.2;)");
                        JObject repo = JObject.Parse(client.DownloadString("https://api.github.com/repos/tm2013/Radium/releases/latest"));
                        string downloadLink = (repo["assets"][1]["browser_download_url"]).ToString();

                        // This is a much quicker download so we won't show progress for this. 
                        client.DownloadFile(downloadLink, path + "\\Radium-qt.exe");
                    }
                }
            }
            catch (Exception e)
            {
                if (File.Exists(path + "\\Radium-qt.exe"))
                {
                    File.Delete(path + "\\Radium-qt.exe");
                }
                return false;
            }

            while (!zipDone)
            {
                // zip takes longer so wait for that to finish before continuing.
            }

            return true;

        }

        public bool ExtractData()
        {
            string basePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Radium";
            string zipPath = basePath + "\\data.zip";

            if (Directory.Exists(basePath + "\\database"))
            {
                Directory.Delete(basePath + "\\database", true);
            }

            if (Directory.Exists(basePath + "\\txleveldb"))
            {
                Directory.Delete(basePath + "\\txleveldb", true);
            }

            if (File.Exists(basePath + "\\blk0001.dat"))
            {
                File.Delete(basePath + "\\blk0001.dat");
            }

            try
            {
                ZipFile.ExtractToDirectory(zipPath, basePath);
                
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                return false;
            }

            if (!Directory.Exists(basePath + "\\database") || !Directory.Exists(basePath + "\\txleveldb") || !File.Exists(basePath + "\\blk0001.dat"))
            {
                return false;
            }
            else
            {
                File.Delete(basePath + "\\data.zip");
            }

            return true;
        }

        public bool CreateShortcut(string targetName, string shortcutPath, string shortcutName)
        {
            try
            {
                IWshRuntimeLibrary.WshShell wsh = new IWshRuntimeLibrary.WshShell();
                IWshRuntimeLibrary.IWshShortcut shortcut = wsh.CreateShortcut(shortcutPath + "\\" + shortcutName + ".lnk") as IWshRuntimeLibrary.IWshShortcut;
                shortcut.Arguments = "";
                shortcut.TargetPath = targetName;
                shortcut.Save();
            }
            catch (Exception e)
            {
                return false;
            }

            return true;
        }

        private void installButton_Click_1(object sender, RoutedEventArgs e)
        {
            if (installButton.Tag.ToString() == "1")
            {
                installButton.Tag = "2";
                installButton.Content = "Click again to confirm installation";
                return;
            }

            statusLabel.Visibility = Visibility.Visible;

            if (installButton.Tag.ToString() == "exit")
            {
                return;
            }

            installButton.Visibility = Visibility.Hidden;
            statusLabel.Visibility = Visibility.Visible;

            DispatcherTimer dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();

            msg = "Downloading Install Data";
            dynamic d = Task<bool>.Factory.StartNew(() => OutputData());
            do
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));
            } while (d.IsCompleted == false);
            if (!d.Result)
            {
                MessageBox.Show("Download Failed. Application must exit.");
                Application.Current.Shutdown();
            }

            msg = "Extracting Data";
            d = Task<bool>.Factory.StartNew(() => ExtractData());
            do
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));
            } while (d.IsCompleted == false);
            if (!d.Result)
            {
                MessageBox.Show("Extraction Failed. Application must exit.");
                Application.Current.Shutdown();
            }

            msg = "Creating Desktop Shortcut";
            if (!CreateShortcut(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Radium\\Radium-qt.exe", 
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Radium Wallet"))
            {
                MessageBox.Show("Failed to create shortcut. Radium was still successfully installed.");
            }

            dispatcherTimer.Stop();
            statusLabel.Content = "Complete";
            MessageBox.Show("Installation Complete! Press OK to exit.", "", MessageBoxButton.OK);
            Application.Current.Shutdown();
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (!msg.Contains("Downloading"))
            {
                if (dots.Length > 8)
                {
                    dots = "";
                }

                dots += " .";
            }
            statusLabel.Content = msg + dots;
        }

    }
}
