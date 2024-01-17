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

        public MainWindow()
        {
            InitializeComponent();
            bool Missing = false;
            if (!ExistsOnPath("ffmpeg.exe"))
            {
                ffmpegimg.IsEnabled = false;
                Missing = true;
                error.Content = "FFmpeg couldn't be found in path!";
            }
            if (!ExistsOnPath("yt-dlp.exe"))
            {
                ytdlpimg.IsEnabled = false;
                Missing = true;
                error.Content = error.Content.Equals("") ? "YT-DLP couldn't be found in path!" : "YT-DLP and FFmpeg couldn't be found in path!";
            }

            if (Missing)
                disableWindow();

            App app = ((App)Application.Current);

        }

        public void AppendText(string text)
        {
            Application.Current.Dispatcher.Invoke(() => { logout.AppendText(text); });
        }

        public void UpdateErrorContent(string text)
        {
            Application.Current.Dispatcher.Invoke(() => { error.Content = text; });
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

        private void disableWindow()
        {
            bar.Visibility = Visibility.Visible;
            yturl.IsEnabled= false;
            loadstream.IsEnabled= false;
            createmedia.IsEnabled= false;
        }

        private void enableWindow()
        {
            bar.Visibility = Visibility.Collapsed;
            yturl.IsEnabled = true;
            loadstream.IsEnabled = true;
            createmedia.IsEnabled = true;
        }

        private void loadstream_Click(object sender, RoutedEventArgs e)
        {
            logout.AppendText("------------------------New task started!------------------------\n");
            disableWindow();
            videostream.Items.Clear();
            audiostream.Items.Clear();
            getStreams(yturl.Text);
        }

        private async void getStreams(string url)
        {

            
            await Task.Run(() =>
            {
                App app = ((App)Application.Current);
                int exitCode = app.getYTData(url);
                if (exitCode == 0)
                {
                    AppendText("[UI]Received data streams!\n");
                    foreach (VideoData data in app.GetFormatedVideoList())
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            videostream.Items.Add(data);
                        });
                    }

                    foreach (AudioData data in app.GetFormatedAudioList())
                    {

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            audiostream.Items.Add(data);
                        });
                    }

                }
                else
                {
                    AppendText("[UI]Data streams couldn't be retrieved!\n");
                    UpdateErrorContent("Couldn't find any streams!\nAre you sure the url is correct and valid?");
                }
            });
            enableWindow();
        }

        private void TabSelectChange(object sender, SelectionChangedEventArgs e)
        {
            string value = tab.SelectedValue.ToString().Remove(0, 39);
            for (int i = 0; i < value.Length; i++)
            {
                if (value[i].Equals(' '))
                {
                    value = value.Remove(i, (value.Length-i)).ToLower();
                    break;
                }
            }

            if (value.Equals("log"))
            {
                ffmpegimg.Visibility = Visibility.Collapsed;
                ytdlpimg.Visibility = Visibility.Collapsed;
                ulrinputitle.Visibility = Visibility.Collapsed;
                yturl.Visibility = Visibility.Collapsed;
                loadstream.Visibility = Visibility.Collapsed;
                videostream.Visibility = Visibility.Collapsed;
                audiostream.Visibility = Visibility.Collapsed;
                createmedia.Visibility = Visibility.Collapsed;
                error.Visibility = Visibility.Collapsed;
                logout.Visibility = Visibility.Visible;
            }
            else
            {
                ffmpegimg.Visibility = Visibility.Visible;
                ytdlpimg.Visibility = Visibility.Visible;
                ulrinputitle.Visibility = Visibility.Visible;
                yturl.Visibility = Visibility.Visible;
                loadstream.Visibility = Visibility.Visible;
                videostream.Visibility = Visibility.Visible;
                audiostream.Visibility = Visibility.Visible;
                createmedia.Visibility = Visibility.Visible;
                error.Visibility = Visibility.Visible;
                logout.Visibility = Visibility.Collapsed;
            }
        }

        private void SelectChange(object sender, SelectionChangedEventArgs e)
        {
                if(videostream.SelectedItems.Count > 1)
                {
                    var tmp = videostream.SelectedItems[0];
                    videostream.SelectedItems.Clear(); ;
                    videostream.SelectedItem = tmp;
                }
        }

        private void create_Click(object sender, RoutedEventArgs e)
        {
            string tempName = "\\24kk23tempytdlfile9928j";

            if (videostream.SelectedItem == null && audiostream.SelectedItem == null)
            {
                error.Content = "No stream has been selected!";
                return;
            }
            App app = ((App)Application.Current);

            SaveFileDialog mediaFile = new SaveFileDialog();


            if (videostream.SelectedItem == null && audiostream.SelectedItem != null)
            {
                string[] stream = app.GetAudioStreams[audiostream.SelectedIndex].Split(new char[] {'-'});
                string ext = "";
                if (stream[app.ACODEC].Equals("opus"))
                    ext = "*.webm";
                else
                    ext = "*.m4a";

                error.Content = "";
                mediaFile.Filter = "Audio Only|" + ext;
                mediaFile.Title = "Save Media File";
                mediaFile.ShowDialog();
                if (mediaFile.FileName.Equals(""))
                {
                    error.Content = "Please select file to save as!";
                    return;
                }
                disableWindow();
                logout.AppendText("[UI]Selected Audio: " + stream[app.STREAMID] + " " + stream[app.ACODEC] + " " + stream[app.ABITRATE] + " " + stream[app.ASIZE] + "\n");
                string dir = Path.GetDirectoryName(mediaFile.FileName);
                string fileExt = Path.GetExtension(mediaFile.FileName).ToLower();
                GetFile(yturl.Text, new int[] { Int32.Parse(stream[app.STREAMID]) }, dir + tempName + fileExt, mediaFile.FileName);
            }
            else if(videostream.SelectedItem != null)
            {

                int[] streams = new int[1];
                string[] stream = null;

                if (audiostream.SelectedItem != null)
                {
                    streams = new int[2];
                    stream = app.GetAudioStreams[audiostream.SelectedIndex].Split(new char[] { '-' });
                    streams[1] = Int32.Parse(stream[app.STREAMID]);
                    logout.AppendText("[UI]Selected Audio: " + stream[app.STREAMID] + " " + stream[app.ACODEC] + " " + stream[app.ABITRATE] + " " + stream[app.ASIZE] + "\n");
                }

                stream = app.GetVideoStreams[videostream.SelectedIndex].Split(new char[] { '-' });
                streams[0] = Int32.Parse(stream[app.STREAMID]);
                logout.AppendText("[UI]Selected Video: " + stream[app.STREAMID] + " " + stream[app.VRES] + "p" + stream[app.FPS] + " " + stream[app.VCODEC] + " " + stream[app.VBITRATE] + " " + stream[app.VSIZE] + "\n");
                error.Content = "";
                mediaFile.Filter = "MP4 file|*.mp4|MKV file|*.mkv|MOV file|*.mov";
                mediaFile.Title = "Save Media File";
                mediaFile.ShowDialog();
                if (mediaFile.FileName.Equals(""))
                {
                    error.Content = "Please select file to save as!";
                    return;
                }
                disableWindow();
                string dir = Path.GetDirectoryName(mediaFile.FileName);
                string fileExt = Path.GetExtension(mediaFile.FileName).ToLower();
                GetFile(yturl.Text, streams, dir + tempName + fileExt, mediaFile.FileName);
            }

        }

        public async void GetFile(string url, int[] streams, string tempPath, string path)
        {
            await Task.Run(() => 
            {
                App app = ((App)Application.Current);
                int ExitCode = app.RunFFMpeg(url, streams, tempPath);
                if(ExitCode != -1)
                {
                    File.Move(tempPath, path);
                    AppendText("[UI]Media file downloaded!");
                }
                else
                { 
                    File.Delete(tempPath);
                    UpdateErrorContent("Something went wrong! Check the log output!");
                }
            });
            enableWindow();
        }
    }

}
