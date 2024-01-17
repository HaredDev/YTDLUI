﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace YTDLUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        /*
         * Index for each data point in formated audio/video data
         * STREAMID = id for the audio/video stream
         * VRES = video resulotion
         * ASIZE = size of the audio stream
         * FPS = fps of the video stream
         * VSIZE = size of the video strea,
         * ABITRATE = bitrate of the audio stream
         * VBITRATE = bitrate of the video stream
         * APROTO = protocol of the audio stream
         * VPROTO = protocol of the video stream
         * ACODEC = the audio codec
         * VCODEC = the video codec
         * SAMPRATE = the sample rate for the audio stream
         * INFO = extra info
         * 
         */
        public readonly int STREAMID = 0, VRES = 2, ASIZE = 3, FPS = 3, VSIZE = 4, ABITRATE = 4, VBITRATE = 5, APROTO = 5, VPROTO = 6, ACODEC = 6, VCODEC = 7, SAMPRATE = 8, INFO = 9;

        private List<string> AudioStreams = new List<string>();
        private List<string> VideoStreams = new List<string>();
        private string CurrentTitle = "";
        private Boolean TaskSuccess = false;

        private Process runCMD(string args)
        {
            Process p = new Process();
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.Arguments = "/C " + args;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.CreateNoWindow = true;
            return p;
        }

        public void AppendText(string text)
        {
            Application.Current.Dispatcher.Invoke(() => { (Application.Current.MainWindow as MainWindow).AppendText(text); });
        }

        //return true if data can be extracted
        public int getYTData(string YTurl)
        {
            AppendText("[YT-DLP]Getting streams from: " + YTurl + "\n");
            Process p = this.runCMD("yt-dlp.exe -F " + YTurl + " --get-title");
            p.Start();
            string sOut = p.StandardOutput.ReadToEnd();
            while (!p.HasExited)
            {
            }
            if(p.ExitCode != 0)
            {
                return p.ExitCode;
            }
            string[] l = sOut.Split(new char[] {'\n'});
            List<string> audioStreams = new List<string>(), videoStreams = new List<string>();
            for (int i = 0; i < l.Length; i++)
            {
                string line = l[i];
                //Get audio streams
                if (line.Contains("audio only") && !line.Contains("unknown"))
                {
                    audioStreams.Add(line);
                }

                //Get video streams
                if (line.Contains("video only"))
                {
                    videoStreams.Add(line);
                }
            }

            CurrentTitle = l[l.Length - 2];

            //Format data output audio from yt-dlp
            int count = audioStreams.Count;
            for(int i = 0; i < count; i++)
            {
                string[] line = audioStreams[i].Replace("audio only", "|").Replace(" ", "|").Split(new char[] {'|'});
                string NewLine = "";
                for(int j = 0; j < line.Length; j++)
                {
                    if (!line[j].Equals(""))
                    {
                        if (j == (line.Length - 1))
                        {
                            NewLine += line[j];
                            string[] form = NewLine.Split(new char[] {'-'});
                            NewLine = "";
                            for (int k = 0; k < form.Length; k++)
                            {
                                if (k > SAMPRATE)
                                {
                                    NewLine += form[k] + " ";
                                } else
                                {
                                    NewLine += form[k] + "-";
                                }
                            }
                            audioStreams[i] = NewLine;
                        } else
                        {
                            NewLine += line[j] + "-";
                        }
                    }
                }
            }

            AppendText("[YT-DLUI]Available Audio Streams \n");
            for (int i = 0; i < audioStreams.Count; i++)
                AppendText("[YT-DLUI]" + audioStreams[i] + "\n");

            //Format data output video from yt-dlp
            count = videoStreams.Count;
            for (int i = 0; i < count; i++)
            {
                string PreLine = videoStreams[i].Replace("video only", "|").Replace(" ", "|").Replace("~", "|");
                if (PreLine.EndsWith("||"))
                    PreLine = PreLine.Remove(PreLine.Length - 2);
                string[] line = PreLine.Split(new char[] { '|' });
                string NewLine = "";
                for (int j = 0; j < line.Length; j++)
                {
                    if (!line[j].Equals(""))
                    {
                        if (j == (line.Length - 1))
                        {
                            NewLine += line[j];                     
                            string[] form = NewLine.Split(new char[] { '-' });
                            if (form.Length > 9)
                            {
                                NewLine = "";
                                for (int k = 0; k < form.Length; k++)
                                {
                                    if (k > SAMPRATE)
                                    {
                                        NewLine += form[k] + " ";
                                    }
                                    else
                                    {
                                        NewLine += form[k] + "-";
                                    }
                                }
                            }
                            videoStreams[i] = NewLine;
                        }         
                        else
                        {
                            NewLine += line[j] + "-";
                        }
                    }
                }
            }


            AppendText("[YT-DLUI]Available Video Streams \n");
            for (int i = 0; i < videoStreams.Count; i++)
                AppendText("[YT-DLUI]" + videoStreams[i] + "\n");

            AudioStreams = audioStreams;
            VideoStreams = videoStreams;
            return p.ExitCode;

        }

        private string getStreamURL(int streamID, string yturl)
        {
            Process p = this.runCMD("yt-dlp.exe -f " + streamID + " --get-url " + yturl);
            p.Start();
            string sOut = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            if(p.ExitCode != 0)
                return "";
            sOut = sOut.Remove(sOut.Length - 1);
            return "\"" + sOut + "\"";

        }

        public int RunFFMpeg(string yturl, int[] StreamIDS, string path)
        {
            string command = "ffmpeg.exe";
            foreach (int i in StreamIDS)
            {
                string sOut = getStreamURL(i, yturl);

                if (sOut.Equals(""))
                    return -1;
                command = command + " -i " + sOut;
            }

            command = command + " -codec copy -safe 0 " + "\"" + path + "\"";

            Process p = this.runCMD(command);
            p.Start();          
            string text = p.StandardError.ReadLine();
            while (text != null)
            {
                if (text != null)
                {
                    AppendText("[FFMPEG]" + text + "\n");
                }
                text = p.StandardError.ReadLine();
            }

            return p.ExitCode;
        }

        public List<VideoData> GetFormatedVideoList()
        {
            var VData = new List<VideoData>();
            foreach (string stream in VideoStreams)
            {
                String[] data = stream.Split(new char[] { '-' });
                if (data.Length == (INFO + 1) && data[INFO].Equals("Premium "))
                {
                    VData.Add(new VideoData(data[VRES], data[FPS], data[VSIZE], data[VBITRATE], data[VPROTO], data[VCODEC], data[INFO]));
                }
                else
                {
                    VData.Add(new VideoData(data[VRES], data[FPS], data[VSIZE], data[VBITRATE], data[VPROTO], data[VCODEC]));
                }
            }
            return VData;
        }


        public List<AudioData> GetFormatedAudioList()
        {
            var VData = new List<AudioData>();
            foreach (string stream in AudioStreams)
            {
                String[] data = stream.Split(new char[] { '-' });
                if (data.Length == (INFO + 1))
                {
                    VData.Add(new AudioData(data[ACODEC], data[SAMPRATE], data[ASIZE], data[ABITRATE], data[APROTO], data[INFO]));
                }
                else
                {
                    VData.Add(new AudioData(data[ACODEC], data[SAMPRATE], data[ASIZE], data[ABITRATE], data[APROTO]));
                }
            }
            return VData;
        }

        public List<string> GetAudioStreams
        {
            get { return AudioStreams; }
        }

        public List<string> GetVideoStreams
        {
            get { return VideoStreams; }
        }

        public string GetCurrentTitle
        {
            get { return CurrentTitle; }
        }

    }

    public class AudioData
    {
        private string acodec;
        private string samp;
        private string asize;
        private string abit;
        private string aprot;
        private string info;

        internal AudioData(string Acodec, string Samp, string Asize, string Abit, string Aprot, string Info = "")
        {
            this.acodec = Acodec;
            this.samp = Samp;
            this.asize = Asize;
            this.abit = Abit;
            this.aprot = Aprot;
            this.info = Info;
        }

        public string Samp
        {
            get { return samp; }
        }

        public string Asize
        {
            get { return asize; }
        }

        public string Abit
        {
            get { return abit; }
        }

        public string Aprot
        {
            get { return aprot; }
        }

        public string Acodec
        {
            get { return acodec; }
        }

        public string Info
        {
            get { return info; }
        }

    }

    public class VideoData
    {
        private string vres;
        private string fps;
        private string vsize;
        private string vbit;
        private string vprot;
        private string vcodec;
        private string info;

        internal VideoData(string Vres, string Fps, string Vsize, string Vbit, string Vprot, string Vcodec, string Info = "") { 
           this.vres = Vres;
           this.fps = Fps;
           this.vsize = Vsize;
           this.vbit = Vbit;
           this.vprot = Vprot;
           this.vcodec = Vcodec;
           this.info = Info;
        }

        public string Vres
        {
            get { return vres; }
        }

        public string Fps
        {
            get { return fps; }
        }

        public string Vsize
        {
            get { return vsize; }
        }

        public string Vbit
        {
            get { return vbit; }
        }

        public string Vprot
        {
            get { return vprot; }
        }

        public string Vcodec
        {
            get { return vcodec; }
        }

        public string Info
        {
            get { return info; }
        }

    }

}
