using Microsoft.Win32;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CridPlayer
{

    public partial class OpenCridForm : Form
    {
        public List<string> existing = new List<string>();
        public string filepath = "";
        public bool keepread = true;
        MD5 filehash = MD5.Create();
        int filecount = 0;
        string hashfilename;
        FileStream reader;
        CancellationTokenSource cts = new CancellationTokenSource();

        public OpenCridForm()
        {
            InitializeComponent();
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        private void OpenFileBtn_Click(object sender, EventArgs e)
        {
            OpenFolderDialog ofd = new OpenFolderDialog();
            ofd.ShowDialog();
            if (!Directory.Exists(ofd.FolderName))
            {
                MessageBox.Show("Invalid directory");
            }
            else
            {
                string[] listfiles = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "\\", "Filelist_*.txt");
                foreach (string file in listfiles)
                {
                    byte[] filenamehash = filehash.ComputeHash(Encoding.ASCII.GetBytes(ofd.FolderName));
                    string checkhash = BitConverter.ToString(filenamehash).Replace("-", "").ToLowerInvariant();
                    if (Path.GetFileNameWithoutExtension(file).Contains(checkhash))
                    {
                        string[] read = File.ReadAllLines(file);
                        if(read.Length < 3)
                        {
                            File.Delete(file);

                        }
                        else
                        {
                            existing.AddRange(read);
                            hashfilename = file;
                        }
                    }
                }
                DirectoryTxt.Text = ofd.FolderName;

            }




        }

        private async void DirectoryTxt_TextChanged(object sender, EventArgs e)
        {
            await Task.Run(() => fillitems(cts.Token));
        }

        public async void fillitems(CancellationToken CT)
        {
            FileStream fscheck = null;
            byte[] filenamehash = filehash.ComputeHash(Encoding.ASCII.GetBytes(DirectoryTxt.Text));
            string checkfilehash = BitConverter.ToString(filenamehash).Replace("-", "").ToLowerInvariant();
            bool justcreated = false;
            if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory + "\\Filelist_" + checkfilehash + ".txt"))
            {
                var testfile = File.Create(AppDomain.CurrentDomain.BaseDirectory + "\\Filelist_" + checkfilehash + ".txt");
                existing.Add(DirectoryTxt.Text);
                existing.Add("");
                hashfilename = AppDomain.CurrentDomain.BaseDirectory + "\\Filelist_" + checkfilehash + ".txt";
                justcreated = true;
                testfile.Dispose();
            }
            else
            {
            }


            if (Directory.Exists(DirectoryTxt.Text))
            {
                if (existing.Count > 0 && justcreated == false)
                {
                    string[] result = existing.ToArray()[2..];
                    CRIDListBox.Invoke(new Action(() => CRIDListBox.Items.AddRange(result)));
                    await Task.Run(()=> progressBar1.Invoke(new Action(() =>
                    {

                        progressBar1.Maximum = Directory.GetFiles(DirectoryTxt.Text).Count();
                        progressBar1.Value = CRIDListBox.Items.Count;
                        progressBar1.Update();
                        label1.Text = "Count = " + CRIDListBox.Items.Count;
                    })));
                }
                foreach (string files in Directory.GetFiles(DirectoryTxt.Text))
                {
                    if (CT.IsCancellationRequested)
                        break;
                    if (existing.Count > 0)
                    {
                        string filename = Path.GetFileName(files);
                        bool fileexist = false;
                        foreach (string fileList in existing)
                        {
                            if (filename == fileList)
                            {
                                fileexist = true;
                            }
                        }
                        if (fileexist)
                        {
                            progressBar1.Invoke(new Action(() => progressBar1.Value++));
                            continue;
                        }
                    }
                    FileStream fs = new FileStream(files, FileMode.Open, FileAccess.Read);
                    byte[] header = new byte[4];
                    fs.Read(header, 0, 4);
                    if (Encoding.ASCII.GetString(header) == "CRID")
                    {
                        try
                        {

                            CRIDListBox.Invoke(new Action(async () =>
                            {
                                if (keepread)
                                {
                                    CRIDListBox.Items.Add(Path.GetFileName(files));
                                    existing.Add(Path.GetFileName(files));
                                    label1.Invoke(new Action(()=>label1.Text = "Count = "+ CRIDListBox.Items.Count));
                                    await Task.Run(()=> progressBar1.Invoke(new Action(() =>
                                    {
                                        progressBar1.Value++;
                                        progressBar1.Update();

                                    })));
                                }
                                //CRIDListBox.Refresh();
                            }));
                        }
                        catch
                        {

                        }

                    }
                }
            }
            else
            {

            }
        }

        private async void CRIDListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            await Task.Run(() => filldetails(cts.Token));
            string selecteditem = "";
            CRIDListBox.Invoke(new Action(() => selecteditem = CRIDListBox.SelectedItem.ToString()));
            filepath = Path.Combine(DirectoryTxt.Text, selecteditem);
        }

        public async void filldetails(CancellationToken CT)
        {
            FileDetailsTxt.Invoke(new Action(() => FileDetailsTxt.Clear()));
            string selecteditem = "";
            CRIDListBox.Invoke(new Action(() => selecteditem = CRIDListBox.SelectedItem.ToString()));
            if (File.Exists(Path.Combine(DirectoryTxt.Text, selecteditem)))
            {


                reader = new FileStream(Path.Combine(DirectoryTxt.Text, selecteditem), FileMode.Open, FileAccess.Read);
                BinaryReader br = new BinaryReader(reader);
                br.BaseStream.Position = 0x20;
                FileDetailsTxt.Invoke(new Action(() => FileDetailsTxt.Text += Encoding.ASCII.GetString(br.ReadBytes(4)) + Environment.NewLine));
                int endofheader = 0x20 + BinaryPrimitives.ReadInt32BigEndian(br.ReadBytes(4));
                br.ReadInt32();
                int datastartpos = (0x20 + BinaryPrimitives.ReadInt32BigEndian(br.ReadBytes(4)));
                br.BaseStream.Position = 0x20 + datastartpos;
                List<byte> bytestring = new List<byte>();
                List<string> stringlist = new List<string>();
                for (int i = 0x20; i <= endofheader; i++)
                {
                    byte readbyte = br.ReadByte();
                    if (readbyte != 0)
                    {
                        bytestring.Add(readbyte);
                        if (i == endofheader)
                        {
                            i -= 1;
                        }
                    }
                    else
                    {
                        stringlist.Add(Encoding.GetEncoding(932).GetString(bytestring.ToArray()));
                        bytestring.Clear();
                    }
                }

                string usmname, usmvideo, usmaudio;
                usmname = "";
                usmvideo = "";
                usmaudio = "";
                string[] strings = stringlist.ToArray();
                for (int i = 0; i < strings.Length; i++)
                {
                    if (strings[i].Contains(".usm"))
                    {
                        usmname = strings[i];
                        usmvideo = strings[i + 1];
                        usmaudio = strings[i + 2];
                        break;
                    }
                }
                FileDetailsTxt.Invoke(new Action(() => FileDetailsTxt.Text += "USM Name = " + usmname + Environment.NewLine));
                FileDetailsTxt.Invoke(new Action(() => FileDetailsTxt.Text += "Source Video = " + usmvideo + Environment.NewLine));
                FileDetailsTxt.Invoke(new Action(() => FileDetailsTxt.Text += "Source Audio = " + usmaudio + Environment.NewLine));
            }
        }

        public List<string> usmlist_details()
        {
            List<string> result = new List<string>();
            string[] filelistarr = existing.ToArray()[2..];
            foreach(string file in filelistarr)
            {
                reader.Dispose();
                FileStream fs = new FileStream(Path.Combine(DirectoryTxt.Text, file), FileMode.Open, FileAccess.Read);
                BinaryReader br = new BinaryReader(fs);
                br.BaseStream.Position = 0x20;
                //FileDetailsTxt.Invoke(new Action(() => FileDetailsTxt.Text += Encoding.ASCII.GetString(br.ReadBytes(4)) + Environment.NewLine));
                br.ReadInt32();
                int endofheader = 0x20 + BinaryPrimitives.ReadInt32BigEndian(br.ReadBytes(4));
                br.ReadInt32();
                int datastartpos = (0x20 + BinaryPrimitives.ReadInt32BigEndian(br.ReadBytes(4)));
                br.BaseStream.Position = 0x20 + datastartpos;
                List<byte> bytestring = new List<byte>();
                List<string> stringlist = new List<string>();
                for (int i = 0x20; i <= endofheader; i++)
                {
                    byte readbyte = br.ReadByte();
                    if (readbyte != 0)
                    {
                        bytestring.Add(readbyte);
                        if (i == endofheader)
                        {
                            i -= 1;
                        }
                    }
                    else
                    {
                        stringlist.Add(Encoding.GetEncoding(932).GetString(bytestring.ToArray()));
                        bytestring.Clear();
                    }
                }

                string usmname, usmvideo, usmaudio;
                usmname = "";
                usmvideo = "";
                usmaudio = "";
                string[] strings = stringlist.ToArray();
                for (int i = 0; i < strings.Length; i++)
                {
                    if (strings[i].Contains(".usm"))
                    {
                        usmname = strings[i];
                        usmvideo = strings[i + 1];
                        usmaudio = strings[i + 2];
                        break;
                    }
                }

                result.Add(file + "     " + usmname);
                
            }
            return result;
        }

        private void OpenCridBtn_Click(object sender, EventArgs e)
        {
            keepread = false;
            if (existing.Count < 1)
                return;
            cts.Cancel();
            var writefile = File.WriteAllLinesAsync(hashfilename, existing);
            reader.Dispose();
            if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory + "\\USMList_" + Path.GetFileNameWithoutExtension(hashfilename)))
            {
                var createlist = File.Create(AppDomain.CurrentDomain.BaseDirectory + "\\USMList_" + Path.GetFileNameWithoutExtension(hashfilename)+".txt");
                createlist.Dispose();
            }
            string usmlist = AppDomain.CurrentDomain.BaseDirectory + "\\USMList_" + Path.GetFileNameWithoutExtension(hashfilename)+".txt";
            List<string> list = usmlist_details();
            File.WriteAllLines(usmlist,list);
            Form1 frm1 = null;
            foreach (Form frm in Application.OpenForms)
            {
                if (frm.Name == "Form1")
                {
                    frm1 = frm as Form1;
                }
            }
            if (frm1 != null && File.Exists(filepath))
            {
                frm1.filepath = filepath;
                this.Close();
            }
        }
    }


    public class DynamicMemoryStream : Stream
    {
        private readonly MemoryStream _memoryStream;
        private long _position = 0;

        public DynamicMemoryStream(MemoryStream memoryStream)
        {
            _memoryStream = memoryStream;
        }

        public override long Length => _memoryStream.Length;

        public override long Position
        {
            get => _position;
            set => _position = value;
        }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;  // Prevent writing inside VLC

        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesRead = 0;

            while (bytesRead < count)
            {
                lock (_memoryStream)  // Ensure thread safety
                {
                    if (_position < _memoryStream.Length)
                    {
                        _memoryStream.Position = _position;
                        int chunkSize = (int)Math.Min(count - bytesRead, _memoryStream.Length - _position);
                        int bytesReadThisTime = _memoryStream.Read(buffer, offset + bytesRead, chunkSize);
                        bytesRead += bytesReadThisTime;
                        _position += bytesReadThisTime;
                    }
                    else
                    {
                        break; // Stop if there’s no more data yet
                    }
                }
            }

            return bytesRead;
        }

        public override void Flush() { }

        public override long Seek(long offset, SeekOrigin origin)
        {
            lock (_memoryStream)
            {
                switch (origin)
                {
                    case SeekOrigin.Begin:
                        _position = offset;
                        break;
                    case SeekOrigin.Current:
                        _position += offset;
                        break;
                    case SeekOrigin.End:
                        _position = _memoryStream.Length - offset;
                        break;
                }
            }
            return _position;
        }

        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
