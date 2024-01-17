using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static System.Net.Mime.MediaTypeNames;
using Application = System.Windows.Application;
using Path = System.IO.Path;

namespace YTDLUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private bool IsWindowEnabled = true;

        public MainWindow()
        {
            InitializeComponent();
            bool Missing = false;
            if (!ExistsOnPath("ffmpeg.exe"))
            {
                FFmpegimg.IsEnabled = false;
                Missing = true;
                Error.Content = "FFmpeg couldn't be found in path!";
            }
            if (!ExistsOnPath("yt-dlp.exe"))
            {
                Ytdlpimg.IsEnabled = false;
                Missing = true;
                Error.Content = Error.Content.Equals("") ? "YT-DLP couldn't be found in path!" : "YT-DLP and FFmpeg couldn't be found in path!";
            }

            if (Missing)
                DisableWindow();

            App app = ((App)Application.Current);

        }

        public void AppendText(string text)
        {
            Application.Current.Dispatcher.Invoke(() => { Logout.AppendText(text); });
        }

        public void UpdateErrorContent(string text)
        {
            Application.Current.Dispatcher.Invoke(() => { Error.Content = text; });
        }

        public static bool ExistsOnPath(string fileName)
        {
            return GetFullPath(fileName) != null;
        }

        public static string GetFullPath(string fileName)
        {
            if (File.Exists(fileName))
                return Path.GetFullPath(fileName);

            var values = Environment.GetEnvironmentVariable("PATH");
            foreach (var path in values.Split(Path.PathSeparator))
            {
                var fullPath = Path.Combine(path, fileName);
                if (File.Exists(fullPath))
                    return fullPath;
            }
            return null;
        }

        private void DisableWindow()
        {
            IsWindowEnabled = false;
            Bar.Visibility = Visibility.Visible;
            Yturl.IsEnabled= false;
            Loadstream.IsEnabled= false;
            Createmedia.IsEnabled= false;
        }

        private void EnableWindow()
        {
            IsWindowEnabled = true;
            Bar.Visibility = Visibility.Collapsed;
            Yturl.IsEnabled = true;
            Loadstream.IsEnabled = true;
            Createmedia.IsEnabled = true;
        }

        private void Loadstream_Click(object sender, RoutedEventArgs e)
        {
            Logout.AppendText("------------------------New task started!------------------------\n");
            DisableWindow();
            Videostream.Items.Clear();
            Audiostream.Items.Clear();
            GetStreams(Yturl.Text);
        }

        private async void GetStreams(string url)
        {

            
            await Task.Run(() =>
            {
                App app = ((App)Application.Current);
                int exitCode = app.GetYTData(url);
                if (exitCode == 0)
                {
                    AppendText("[UI]Received data streams!\n");
                    foreach (VideoData data in app.GetFormatedVideoList())
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            Videostream.Items.Add(data);
                        });
                    }

                    foreach (AudioData data in app.GetFormatedAudioList())
                    {

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            Audiostream.Items.Add(data);
                        });
                    }

                }
                else
                {
                    AppendText("[UI]Data streams couldn't be retrieved!\n");
                    UpdateErrorContent("Couldn't find any streams!\nAre you sure the url is correct and valid?");
                }
            });
            EnableWindow();
        }

        private void TabSelectChange(object sender, SelectionChangedEventArgs e)
        {
            string Value = Tab.SelectedValue.ToString().Remove(0, 39);
            for (int i = 0; i < Value.Length; i++)
            {
                if (Value[i].Equals(' '))
                {
                    Value = Value.Remove(i, (Value.Length-i)).ToLower();
                    break;
                }
            }

            if (Value.Equals("log"))
            {
                FFmpegimg.Visibility = Visibility.Collapsed;
                Ytdlpimg.Visibility = Visibility.Collapsed;
                Ulrinputitle.Visibility = Visibility.Collapsed;
                Yturl.Visibility = Visibility.Collapsed;
                Loadstream.Visibility = Visibility.Collapsed;
                Videostream.Visibility = Visibility.Collapsed;
                Audiostream.Visibility = Visibility.Collapsed;
                Createmedia.Visibility = Visibility.Collapsed;
                Error.Visibility = Visibility.Collapsed;
                Bar.Visibility = Visibility.Collapsed;
                Logout.Visibility = Visibility.Visible;
            }
            else
            {
                FFmpegimg.Visibility = Visibility.Visible;
                Ytdlpimg.Visibility = Visibility.Visible;
                Ulrinputitle.Visibility = Visibility.Visible;
                Yturl.Visibility = Visibility.Visible;
                Loadstream.Visibility = Visibility.Visible;
                Videostream.Visibility = Visibility.Visible;
                Audiostream.Visibility = Visibility.Visible;
                Createmedia.Visibility = Visibility.Visible;
                Error.Visibility = Visibility.Visible;
                Logout.Visibility = Visibility.Collapsed;

                if(!IsWindowEnabled)
                    Bar.Visibility = Visibility.Visible;
                else
                    Bar.Visibility = Visibility.Collapsed;
            }
        }

        private void SelectChange(object sender, SelectionChangedEventArgs e)
        {
                if(Videostream.SelectedItems.Count > 1)
                {
                    var tmp = Videostream.SelectedItems[0];
                    Videostream.SelectedItems.Clear(); ;
                    Videostream.SelectedItem = tmp;
                }
        }

        private void Create_Click(object sender, RoutedEventArgs e)
        {
            string tempName = "\\24kk23tempytdlfile9928j";

            if (Videostream.SelectedItem == null && Audiostream.SelectedItem == null)
            {
                Error.Content = "No stream has been selected!";
                return;
            }

            App app = ((App)Application.Current);

            SaveFileDialog MediaFile = new SaveFileDialog();


            if (Videostream.SelectedItem == null && Audiostream.SelectedItem != null)
            {
                string[] Stream = app.GetAudioStreams[Audiostream.SelectedIndex].Split(new char[] {'-'});
                string ext = "";
                if (Stream[app.ACODEC].Equals("opus"))
                    ext = "*.webm";
                else
                    ext = "*.m4a";

                Error.Content = "";
                MediaFile.Filter = "Audio Only|" + ext;
                MediaFile.Title = "Save Media File";
                MediaFile.ShowDialog();
                if (MediaFile.FileName.Equals(""))
                {
                    Error.Content = "Please select file to save as!";
                    return;
                }
                DisableWindow();
                Logout.AppendText("[UI]Selected Audio: " + Stream[app.STREAMID] + " " + Stream[app.ACODEC] + " " + Stream[app.ABITRATE] + " " + Stream[app.ASIZE] + "\n");
                string dir = Path.GetDirectoryName(MediaFile.FileName);
                string fileExt = Path.GetExtension(MediaFile.FileName).ToLower();
                GetFile(Yturl.Text, new int[] { Int32.Parse(Stream[app.STREAMID]) }, dir + tempName + fileExt, MediaFile.FileName);
            }
            else if(Videostream.SelectedItem != null)
            {

                int[] Streams = new int[1];
                string[] Stream = null;

                if (Audiostream.SelectedItem != null)
                {
                    Streams = new int[2];
                    Stream = app.GetAudioStreams[Audiostream.SelectedIndex].Split(new char[] { '-' });
                    Streams[1] = Int32.Parse(Stream[app.STREAMID]);
                    Logout.AppendText("[UI]Selected Audio: " + Stream[app.STREAMID] + " " + Stream[app.ACODEC] + " " + Stream[app.ABITRATE] + " " + Stream[app.ASIZE] + "\n");
                }

                Stream = app.GetVideoStreams[Videostream.SelectedIndex].Split(new char[] { '-' });
                Streams[0] = Int32.Parse(Stream[app.STREAMID]);
                Logout.AppendText("[UI]Selected Video: " + Stream[app.STREAMID] + " " + Stream[app.VRES] + "p" + Stream[app.FPS] + " " + Stream[app.VCODEC] + " " + Stream[app.VBITRATE] + " " + Stream[app.VSIZE] + "\n");
                Error.Content = "";
                MediaFile.Filter = "MP4 file|*.mp4|MKV file|*.mkv|MOV file|*.mov";
                MediaFile.Title = "Save Media File";
                MediaFile.ShowDialog();
                if (MediaFile.FileName.Equals(""))
                {
                    Error.Content = "Please select file to save as!";
                    return;
                }
                DisableWindow();
                string dir = Path.GetDirectoryName(MediaFile.FileName);
                string fileExt = Path.GetExtension(MediaFile.FileName).ToLower();
                GetFile(Yturl.Text, Streams, dir + tempName + fileExt, MediaFile.FileName);
            }

        }

        public async void GetFile(string URl, int[] Streams, string TempPath, string Path)
        {
            await Task.Run(() => 
            {
                App app = ((App)Application.Current);
                int ExitCode = app.RunFFMpeg(URl, Streams, TempPath);
                if(ExitCode != -1)
                {
                    File.Move(TempPath, Path);
                    AppendText("[UI]Media file downloaded!");
                }
                else
                { 
                    File.Delete(TempPath);
                    UpdateErrorContent("Something went wrong! Check the log output!");
                }
            });
            EnableWindow();
        }
    }

}
