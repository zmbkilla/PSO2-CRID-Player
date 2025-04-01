using Microsoft.Win32;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CridPlayer
{
    public partial class OpenCridForm : Form
    {
        public string filepath = "";
        public bool keepread = true;
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
                DirectoryTxt.Text = ofd.FolderName;
            }
        }

        private async void DirectoryTxt_TextChanged(object sender, EventArgs e)
        {
            await Task.Run(() => fillitems());
        }

        public async void fillitems()
        {
            if (Directory.Exists(DirectoryTxt.Text))
            {
                foreach (string files in Directory.GetFiles(DirectoryTxt.Text))
                {
                    FileStream fs = new FileStream(files, FileMode.Open, FileAccess.Read);
                    byte[] header = new byte[4];
                    fs.Read(header, 0, 4);
                    if (Encoding.ASCII.GetString(header) == "CRID")
                    {
                        try
                        {
                            
                            CRIDListBox.Invoke(new Action(() =>
                            {
                                if(keepread)
                                CRIDListBox.Items.Add(Path.GetFileName(files));
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
            await Task.Run(() => filldetails());
            string selecteditem = "";
            CRIDListBox.Invoke(new Action(() => selecteditem = CRIDListBox.SelectedItem.ToString()));
            filepath = Path.Combine(DirectoryTxt.Text, selecteditem);
        }

        public async void filldetails()
        {
            FileDetailsTxt.Invoke(new Action(() => FileDetailsTxt.Clear()));
            string selecteditem = "";
            CRIDListBox.Invoke(new Action(() => selecteditem = CRIDListBox.SelectedItem.ToString()));
            if (File.Exists(Path.Combine(DirectoryTxt.Text, selecteditem)))
            {


                FileStream fs = new FileStream(Path.Combine(DirectoryTxt.Text, selecteditem), FileMode.Open, FileAccess.Read);
                BinaryReader br = new BinaryReader(fs);
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

        private void OpenCridBtn_Click(object sender, EventArgs e)
        {
            keepread = false;
            Form1 frm1 = null;
            foreach(Form frm in Application.OpenForms)
            {
                if (frm.Name == "Form1")
                {
                    frm1 = frm as Form1;
                }
            }
            if(frm1 != null && File.Exists(filepath))
            {
                frm1.filepath = filepath;
                this.Close();
            }
        }
    }
}
