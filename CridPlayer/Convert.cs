using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CridPlayer;
using OpenTK.FileTypes;

namespace UsmTool
{

    internal class VersionList
    {
        public Version[]? list { get; set; }
    }


    internal class Version
    {
        public string? version { get; set; }
        public string[]? videos { get; set; }
        public Version[]? videoGroups { get; set; }
        public ulong? key { get; set; }
        public bool? encAudio { get; set; }
    }
    public class ConvertFile
    {

        public ulong? EncryptionKey(string videoFilename)
        {
            //ulong key1 = EncryptionKeyInFilename(videoFilename);
            //(ulong, bool)? blk = EncryptionKeyInBLK(videoFilename);
            ulong key1 = 0x207DFFFF;
            ulong blk = 0x00B8F21B;
            if (blk == null) return null;
            ulong key2 = blk;
            //audioEnc = blk.Value.Item2;

            ulong finalKey = 0x100000000000000;
            if ((key1 + key2 & 0xFFFFFFFFFFFFFF) != 0) finalKey = key1 + key2 & 0xFFFFFFFFFFFFFF;
            return finalKey;
        }

        public (byte[], byte[])? KeySplitter(ulong? key)
        {
            if (key == null) return null;
            byte[] keyArray = new byte[8];
            BitConverter.GetBytes(key.Value).CopyTo(keyArray, 0);
            byte[] key1 = keyArray[..4];
            byte[] key2 = keyArray[4..];
            return (key1, key2);
        }


        public bool Demux(string filenameArg, byte[] key1Arg, byte[] key2Arg,ref byte[] video, ref byte[] audio)
        {
            if (!File.Exists(filenameArg)) throw new FileNotFoundException($"File {filenameArg} doesn't exist...");
            string filename = Path.GetFileName(filenameArg);
            byte[] key1, key2;
            if (key1Arg.Length == 0 && key2Arg.Length == 0)
            {
                Console.WriteLine($"Finding encryption key for {filename}...");
                (byte[], byte[])? split = KeySplitter(EncryptionKey(filename));
                if (split == null) return false;
                key1 = split.Value.Item1;
                key2 = split.Value.Item2;
            }
            else
            {
                key1 = key1Arg;
                key2 = key2Arg;
            }
            key1 = key1.Reverse().ToArray();
            key2 = key2.Reverse().ToArray();

            USM file = new(filenameArg, key1, key2);
            //check if file is usm
            byte[] check = File.ReadAllBytes(filenameArg)[0..4];
            if (Encoding.ASCII.GetString(check).ToString() != "CRID")
                return false;
            
            file.Demux(true, true,ref video,ref audio);
            Console.WriteLine("Extraction completed !");
            return true;
        }
        public bool DemuxASync(string filenameArg, byte[] key1Arg, byte[] key2Arg, ref MemoryStream VidMS, ref MemoryStream ADXMS)
        {
            if (!File.Exists(filenameArg)) throw new FileNotFoundException($"File {filenameArg} doesn't exist...");
            string filename = Path.GetFileName(filenameArg);
            byte[] key1, key2;
            if (key1Arg.Length == 0 && key2Arg.Length == 0)
            {
                Console.WriteLine($"Finding encryption key for {filename}...");
                (byte[], byte[])? split = KeySplitter(EncryptionKey(filename));
                if (split == null) return false;
                key1 = split.Value.Item1;
                key2 = split.Value.Item2;
            }
            else
            {
                key1 = key1Arg;
                key2 = key2Arg;
            }
            key1 = key1.Reverse().ToArray();
            key2 = key2.Reverse().ToArray();

            USM file = new(filenameArg, key1, key2);
            //check if file is usm
            byte[] check = File.ReadAllBytes(filenameArg)[0..4];
            if (Encoding.ASCII.GetString(check).ToString() != "CRID")
                return false;

            file.DemuxAsync(true, true, ref VidMS, ref ADXMS);
            return true;
        }
    }
}

