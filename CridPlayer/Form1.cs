using LibVLCSharp.Shared;
using LibVLCSharp.WinForms;
using LibVLCSharp;
using UsmTool;
using System.IO;
using System;
using System.Diagnostics;
using static AdxReader;
using System.IO.Pipes;
using System.IO.MemoryMappedFiles;
using CridPlayer.FileTypes;
using static CridPlayer.FileTypes.ADX;
using System.Runtime.InteropServices;
using System.Buffers.Binary;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using FlyleafLib;
using FlyleafLib.MediaPlayer;
using OpenTK.FileTypes;

namespace CridPlayer
{
    public partial class Form1 : Form
    {
        byte[] video;
        byte[] audio;
        private static LibVLC _libVLC = new LibVLC();
        public MediaPlayer MPlayer = new MediaPlayer(_libVLC);
        public bool hasAudio = false;
        public Process process = null;
        public Player Player { get; set; }
        public Config Config { get; set; }
        public string filepath = "";
        public Form1()
        {
            InitializeComponent();
            Config = new Config();
            Player = new Player(Config);
            MPlayer.Stopped += (sender, e) =>
            {
                if(process != null)
                process.Kill();

                Player.Stop();
            };
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            ConvertFile cvf = new ConvertFile();
            byte[] key1 = Convert.FromHexString("207DFFFF");
            byte[] key2 = Convert.FromHexString("00B8F21B");
            if (MPlayer.IsPlaying)
            {
                MPlayer.Stop();
            }
            OpenFileDialog ofd = new OpenFileDialog();
            OpenCridForm ocf = new OpenCridForm();
            ocf.ShowDialog();
            if (filepath != ""&&!File.Exists(filepath))
            {
                MessageBox.Show("invalid filepath");
                return;
            }else if(filepath == "")
            {
                MessageBox.Show("No file selected");
                return;
            }
            //await Task.Run(() => cvf.Demux(filepath, key1, key2, ref video, ref audio));
            MemoryStream ms1 = new MemoryStream();
            MemoryStream ms2 = new MemoryStream();

            DynamicMemoryStream vdms = new DynamicMemoryStream(ms1);
            DynamicMemoryStream adms = new DynamicMemoryStream(ms2);
            //await Task.Run(() => cvf.DemuxASync(filepath, key1, key2, ref vdms, ref adms));
            await Task.Run(() => cvf.DemuxASync(filepath, key1, key2, ref ms1, ref ms2));
            while(ms1.ToArray().Length < (1024*1024))
            {
                Thread.Sleep(1000);
            }
            //if (video == null)
            //{
            //    MessageBox.Show("Error: Invalid/No CRID video file. check the target file and try again");
            //    return;
            //}
            //MemoryStream vdms = new MemoryStream(video);
            //MemoryStream adms = audio is null ? null : new MemoryStream(audio);
            if (adms != null) hasAudio = true;
            CridViewer.MediaPlayer = MPlayer;
            if (MPlayer.IsPlaying)
            {
                MPlayer.Stop();

            }
            PlayMediaFromMemoryStreamAsync(vdms, adms);
        }



        private void PlayMediaFromMemoryStream(MemoryStream vms, MemoryStream ams)
        {
            // Ensure the MemoryStream is at the beginning
            vms.Seek(0, SeekOrigin.Begin);

            // Create a media from the MemoryStream input
            var mediaInput = new StreamMediaInput(vms);
            var media = new Media(_libVLC, mediaInput); // Use LibVLC to create media from StreamMediaInput

            // Play the media using the MediaPlayer if there is audio
            if (hasAudio == true)
            {
                AdxReader areader = new AdxReader();
                AdxReader.adxdata musicdata = new AdxReader.adxdata();
                streamplayeradx(media, ams.ToArray());
            }
            else
            {
                streamplayeradx(media);
            }
        }

        private void PlayMediaFromMemoryStreamAsync(DynamicMemoryStream vms, DynamicMemoryStream ams)
        {
            // Ensure the MemoryStream is at the beginning
            vms.Seek(0, SeekOrigin.Begin);

            // Create a media from the MemoryStream input
            var mediaInput = new StreamMediaInput(vms);
            var media = new Media(_libVLC, mediaInput); // Use LibVLC to create media from StreamMediaInput

            // Play the media using the MediaPlayer if there is audio
            if (hasAudio == true)
            {
                AdxReader areader = new AdxReader();
                AdxReader.adxdata musicdata = new AdxReader.adxdata();
                StreamplayeradxAs(media, ams);
            }
            else
            {
                streamplayeradx(media);
            }
        }

        public async void streamplayeradx(Media media, byte[] adxData = null)
        {
            if (hasAudio)
            {
                Player.OpenAsync(new MemoryStream(adxData));
                flyleafHost1.Player = Player;
                flyleafHost1.Player.Play();
            }
            MPlayer.Play(media);
            while (MPlayer.IsPlaying)
            {
                if(hasAudio)
                Player.CurTime = MPlayer.Time;
            }
        }

        private void StreamplayeradxAs(Media media, DynamicMemoryStream adxData)
        {
            if (hasAudio)
            {
                Player.OpenAsync(adxData);
                flyleafHost1.Player = Player;
                flyleafHost1.Player.Play();
            }
            MPlayer.Play(media);
            while (MPlayer.IsPlaying)
            {
                if (hasAudio)
                    Player.CurTime = MPlayer.Time;
            }
        }

        static void WaitForFileAccess(string filePath)
        {
            while (true)
            {
                try
                {
                    using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                    {
                        // If it can be opened for write access, it's not locked anymore
                        break;
                    }
                }
                catch (IOException)
                {
                    // File is still locked, wait before retrying
                    Thread.Sleep(100);
                }
            }
        }


        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, int dwFlags = 0);

        const int SYMBOLIC_LINK_FILE = 0x0;

        private async void adxBtn_Click(object sender, EventArgs e)
        {
            ConvertFile cvf = new ConvertFile();
            byte[] key1 = Convert.FromHexString("207DFFFF");
            byte[] key2 = Convert.FromHexString("00B8F21B");
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == DialogResult.OK && ofd.FileName != "")
            {
                if (!File.Exists(ofd.FileName))
                {
                    MessageBox.Show("Error file does not exist or error reading file location");
                    return;
                }
            }
            await Task.Run(() => cvf.Demux(ofd.FileName, key1, key2, ref video, ref audio));
            MemoryStream vdms = new MemoryStream(video);
            MemoryStream adms = audio is null ? null : new MemoryStream(audio);
            if (adms != null) hasAudio = true;
            CridViewer.MediaPlayer = MPlayer;
            if (MPlayer.IsPlaying)
            {
                MPlayer.Stop();

            }
            //PlayMediaFromMemoryStream(vdms, adms);

            if (hasAudio)
            {
                ADX adxhandler = new ADX();
                adxhandler.adx_parse(adms.ToArray());
                AdxReader adxr = new AdxReader();
                adxdata addata = new adxdata();
                adxr.getadxencoding(adms, ref addata);
                string res = "sample rate: "+addata.samplerate +"\n channels: " + addata.channels+"\n bitspersample: "+addata.bitspersample;
                //MessageBox.Show(res);
                //lets try this ai mess of a code
                bool looping = adxhandler.ADX_Info.Loop >0? true : false;
                //adxhandler.AdxDec(adms.ToArray(), looping);
                byte[] test = null;
                //await Task.Run(() => test = ADXDecoder.DecodeADX(adms.ToArray(),addata.startoffset,addata.samplecount,addata.channels,addata.blocksize));
                //await Task.Run(() => test = ADXDecoder.AddWavHeader(test, addata.samplerate, addata.channels));
                MemoryStream outstream = new MemoryStream();
                //await Task.Run(() => test = addec.GenHeader(adms,addata));

                //adcontext.Channels = addata.channels;
                //adcontext.SampleRate = addata.samplerate;
                Config = new Config();
                Player = new Player(Config);
                Player.OpenAsync(adms);
                flyleafHost1.Player = Player;
                flyleafHost1.Player.Play();
               

            }
            
        }



    }

    


}
