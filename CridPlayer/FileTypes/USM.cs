﻿using System.Text;
using CridPlayer;
using OpenTK.Utils;

namespace OpenTK.FileTypes
{
    internal struct Info
    {
        public uint signature;
        public uint dataSize;
        public byte dataOffset;
        public ushort paddingSize;
        public byte chno;
        public byte dataType;
        public uint frameTime;
        public uint frameRate;
    }
    internal class USM
    {
        private readonly string _filename;
        private readonly string _path;
        private readonly byte[] _key1;
        private readonly byte[] _key2;
        private byte[] _videoMask1;
        private byte[] _videoMask2;
        private byte[] _audioMask;
        public bool done = false;
        public USM(string filename, byte[] key1, byte[] key2)
        {
            _path = filename;
            _filename = Path.GetFileName(filename);
            _key1 = key1;
            _key2 = key2;
            //MessageBox.Show($"key1={Convert.ToHexString(_key1)} key2={Convert.ToHexString(_key2)}");
            InitMask(key1, key2);
        }
        private void InitMask(byte[] key1, byte[] key2)
        {
            _videoMask1 = new byte[0x20];
            _videoMask1[0x00] = key1[0];
            _videoMask1[0x01] = key1[1];
            _videoMask1[0x02] = key1[2];
            _videoMask1[0x03] = (byte)(key1[3] - 0x34);
            _videoMask1[0x04] = (byte)(key2[0] + 0xF9);
            _videoMask1[0x05] = (byte)(key2[1] ^ 0x13);
            _videoMask1[0x06] = (byte)(key2[2] + 0x61);
            _videoMask1[0x07] = (byte)(_videoMask1[0x00] ^ 0xFF);
            _videoMask1[0x08] = (byte)(_videoMask1[0x02] + _videoMask1[0x01]);
            _videoMask1[0x09] = (byte)(_videoMask1[0x01] - _videoMask1[0x07]);
            _videoMask1[0x0A] = (byte)(_videoMask1[0x02] ^ 0xFF);
            _videoMask1[0x0B] = (byte)(_videoMask1[0x01] ^ 0xFF);
            _videoMask1[0x0C] = (byte)(_videoMask1[0x0B] + _videoMask1[0x09]);
            _videoMask1[0x0D] = (byte)(_videoMask1[0x08] - _videoMask1[0x03]);
            _videoMask1[0x0E] = (byte)(_videoMask1[0x0D] ^ 0xFF);
            _videoMask1[0x0F] = (byte)(_videoMask1[0x0A] - _videoMask1[0x0B]);
            _videoMask1[0x10] = (byte)(_videoMask1[0x08] - _videoMask1[0x0F]);
            _videoMask1[0x11] = (byte)(_videoMask1[0x10] ^ _videoMask1[0x07]);
            _videoMask1[0x12] = (byte)(_videoMask1[0x0F] ^ 0xFF);
            _videoMask1[0x13] = (byte)(_videoMask1[0x03] ^ 0x10);
            _videoMask1[0x14] = (byte)(_videoMask1[0x04] - 0x32);
            _videoMask1[0x15] = (byte)(_videoMask1[0x05] + 0xED);
            _videoMask1[0x16] = (byte)(_videoMask1[0x06] ^ 0xF3);
            _videoMask1[0x17] = (byte)(_videoMask1[0x13] - _videoMask1[0x0F]);
            _videoMask1[0x18] = (byte)(_videoMask1[0x15] + _videoMask1[0x07]);
            _videoMask1[0x19] = (byte)(0x21 - _videoMask1[0x13]);
            _videoMask1[0x1A] = (byte)(_videoMask1[0x14] ^ _videoMask1[0x17]);
            _videoMask1[0x1B] = (byte)(_videoMask1[0x16] + _videoMask1[0x16]);
            _videoMask1[0x1C] = (byte)(_videoMask1[0x17] + 0x44);
            _videoMask1[0x1D] = (byte)(_videoMask1[0x03] + _videoMask1[0x04]);
            _videoMask1[0x1E] = (byte)(_videoMask1[0x05] - _videoMask1[0x16]);
            _videoMask1[0x1F] = (byte)(_videoMask1[0x1D] ^ _videoMask1[0x13]);

            byte[] table2 = Encoding.ASCII.GetBytes("URUC");
            _videoMask2 = new byte[0x20];
            _audioMask = new byte[0x20];
            for (int i = 0; i < 0x20; i++)
            {
                _videoMask2[i] = (byte)(_videoMask1[i] ^ 0xFF);
                _audioMask[i] = (byte)((i & 1) == 1 ? table2[i >> 1 & 3] : _videoMask1[i] ^ 0xFF);
            }
        }

        private void MaskVideo(ref byte[] data, int size)
        {
            const int dataOffset = 0x40;
            size -= dataOffset;
            //size -= 0x40;
            if (size < 0x200) return;
            byte[] mask = new byte[0x20];
            Array.Copy(_videoMask2, mask, 0x20);
            for (int i = 0x100; i < size; i++) mask[i & 0x1F] = (byte)((data[i + dataOffset] ^= mask[i & 0x1F]) ^ _videoMask2[i & 0x1F]);
            Array.Copy(_videoMask1, mask, 0x20);
            for (int i = 0; i < 0x100; i++) data[i + dataOffset] ^= mask[i & 0x1F] ^= data[0x100 + i + dataOffset];
        }

        private void DemuxVideo(ref byte[] data, int size)
        {
            const int dataOffset = 0x40;
            // Assume dataOffset is defined earlier and dynamic
            size -= dataOffset;
            if (size < 0x200) return;

            // Remove decryption, just shift the data if needed
            Buffer.BlockCopy(data, dataOffset, data, 0, size);
            Array.Clear(data, size, data.Length - size);
        }

        // Not used anyway, but might be in the future
        private void MaskAudio(ref byte[] data, uint size)
        {
            const uint dataOffset = 0x140;
            if (size <= dataOffset) return;

            size -= dataOffset;
            for (int i = 0; i < size; i++)  // To be confirmed, could start at the current index of data as well...
            {
                data[i + dataOffset] ^= _audioMask[i & 0x1F];
            }
        }

        public Dictionary<string, List<string>> Demux(bool videoExtract, bool audioExtract,ref byte[] videoout,ref byte[] audiout)
        {

            FileStream filePointer = File.OpenRead(_path);  // TODO: Use a binary reader
            long fileSize = filePointer.Length;
            Info info = new();
            MemoryStream vms = new MemoryStream();
            MemoryStream ams = new MemoryStream();

            Dictionary<string, BinaryWriter> fileStreams = new(); // File paths as keys
            Dictionary<string, List<string>> filePaths = new();
            string path;
            while (fileSize > 0)
            {

                byte[] byteBlock = new byte[32];
                filePointer.Read(byteBlock, 0, byteBlock.Length);
                fileSize -= 32;

                info.signature = Tools.Bswap(BitConverter.ToUInt32(byteBlock, 0));
                info.dataSize = Tools.Bswap(BitConverter.ToUInt32(byteBlock, 4));
                info.dataOffset = byteBlock[9];
                info.paddingSize = Tools.Bswap(BitConverter.ToUInt16(byteBlock, 10));
                info.chno = byteBlock[12];
                info.dataType = byteBlock[15];
                info.frameTime = Tools.Bswap(BitConverter.ToUInt32(byteBlock, 16));
                info.frameRate = Tools.Bswap(BitConverter.ToUInt32(byteBlock, 20));

                int size = (int)(info.dataSize - info.dataOffset - info.paddingSize);
                filePointer.Seek(info.dataOffset - 0x18, SeekOrigin.Current);
                byte[] data = new byte[size];
                filePointer.Read(data);
                filePointer.Seek(info.paddingSize, SeekOrigin.Current);
                fileSize -= info.dataSize - 0x18;

                switch (info.signature)
                {
                    case 0x43524944: // CRID

                        break;
                    case 0x40534656: // @SFV    Video block
                        switch (info.dataType)
                        {
                            case 0:
                                if (videoExtract)
                                {
                                    MaskVideo(ref data, size);
                                    //DemuxVideo(ref data, size);
                                    //path = Path.Combine(outputDir, _filename[..^4] + ".m2v");
                                    //if (!fileStreams.ContainsKey(path))
                                    //{
                                    //    fileStreams.Add(path, new BinaryWriter(new FileStream(path, FileMode.Create, FileAccess.Write)));
                                    //    if (!filePaths.ContainsKey("m2v")) filePaths.Add("m2v", new List<string>{path});
                                    //    else filePaths["m2v"].Add(path);
                                    //}
                                    //fileStreams[path].Write(data);
                                    vms.Write(data);
                                }
                                break;
                            default: // Not implemented, we don't have any uses for it
                                break;
                        }
                        break;

                    case 0x40534641: // @SFA    Audio block
                        switch (info.dataType)
                        {
                            case 0:
                                if (audioExtract)
                                {
                                    // Might need some extra work if the audio has to be decrypted during the demuxing
                                    // (hello AudioMask)
                                    //path = Path.Combine(outputDir, _filename[..^4] + $"_{info.chno}.adx");
                                    //if (!fileStreams.ContainsKey(path))
                                    //{
                                    //    fileStreams.Add(path, new BinaryWriter(new FileStream(path, FileMode.Create, FileAccess.Write)));
                                    //    if (!filePaths.ContainsKey("adx")) filePaths.Add("adx", new List<string> { path });
                                    //    else filePaths["adx"].Add(path);
                                    //}
                                    MaskAudio(ref data, (uint)size);
                                    //fileStreams[path].Write(data);
                                    ams.Write(data);
                                }
                                break;
                            default: // No need to implement it, we lazy
                                break;
                        }
                        break;

                    case 0x40435545: // @CUE - Might be used to play a certain part of the video, but shouldn't be needed anyway (appears in cutscene Cs_Sumeru_AQ30161501_DT)
                        Console.WriteLine("@CUE field detected in USM, skipping as we don't need it");
                        break;
                    default:
                        Console.WriteLine("Signature {0} unknown, skipping...", info.signature);
                        break;
                }
            }
            // Closing Streams
            videoout = vms.ToArray();
            audiout = ams.ToArray();
            filePointer.Close();
            foreach (BinaryWriter stream in fileStreams.Values) stream.Close();
            return filePaths;
        }

        public Dictionary<string, List<string>> DemuxAsync(bool videoExtract, bool audioExtract,ref MemoryStream CridMS,ref MemoryStream ADXMS)
        {

            FileStream filePointer = File.OpenRead(_path);  // TODO: Use a binary reader
            long fileSize = filePointer.Length;
            Info info = new();
            MemoryStream vms = new MemoryStream();
            MemoryStream ams = new MemoryStream();

            Dictionary<string, BinaryWriter> fileStreams = new(); // File paths as keys
            Dictionary<string, List<string>> filePaths = new();
            string path;
            while (fileSize > 0)
            {

                byte[] byteBlock = new byte[32];
                filePointer.Read(byteBlock, 0, byteBlock.Length);
                fileSize -= 32;

                info.signature = Tools.Bswap(BitConverter.ToUInt32(byteBlock, 0));
                info.dataSize = Tools.Bswap(BitConverter.ToUInt32(byteBlock, 4));
                info.dataOffset = byteBlock[9];
                info.paddingSize = Tools.Bswap(BitConverter.ToUInt16(byteBlock, 10));
                info.chno = byteBlock[12];
                info.dataType = byteBlock[15];
                info.frameTime = Tools.Bswap(BitConverter.ToUInt32(byteBlock, 16));
                info.frameRate = Tools.Bswap(BitConverter.ToUInt32(byteBlock, 20));

                int size = (int)(info.dataSize - info.dataOffset - info.paddingSize);
                filePointer.Seek(info.dataOffset - 0x18, SeekOrigin.Current);
                byte[] data = new byte[size];
                filePointer.Read(data);
                filePointer.Seek(info.paddingSize, SeekOrigin.Current);
                fileSize -= info.dataSize - 0x18;

                switch (info.signature)
                {
                    case 0x43524944: // CRID

                        break;
                    case 0x40534656: // @SFV    Video block
                        switch (info.dataType)
                        {
                            case 0:
                                if (videoExtract)
                                {
                                    MaskVideo(ref data, size);
                                    //path = Path.Combine(outputDir, _filename[..^4] + ".m2v");
                                    //if (!fileStreams.ContainsKey(path))
                                    //{
                                    //    fileStreams.Add(path, new BinaryWriter(new FileStream(path, FileMode.Create, FileAccess.Write)));
                                    //    if (!filePaths.ContainsKey("m2v")) filePaths.Add("m2v", new List<string>{path});
                                    //    else filePaths["m2v"].Add(path);
                                    //}
                                    //fileStreams[path].Write(data);
                                    //vms.Write(data);
                                    CridMS.Write(data);
                                }
                                break;
                            default: // Not implemented, we don't have any uses for it
                                break;
                        }
                        break;

                    case 0x40534641: // @SFA    Audio block
                        switch (info.dataType)
                        {
                            case 0:
                                if (audioExtract)
                                {
                                    // Might need some extra work if the audio has to be decrypted during the demuxing
                                    // (hello AudioMask)
                                    //path = Path.Combine(outputDir, _filename[..^4] + $"_{info.chno}.adx");
                                    //if (!fileStreams.ContainsKey(path))
                                    //{
                                    //    fileStreams.Add(path, new BinaryWriter(new FileStream(path, FileMode.Create, FileAccess.Write)));
                                    //    if (!filePaths.ContainsKey("adx")) filePaths.Add("adx", new List<string> { path });
                                    //    else filePaths["adx"].Add(path);
                                    //}
                                    MaskAudio(ref data, (uint)size);
                                    //fileStreams[path].Write(data);
                                    //ams.Write(data);
                                    ADXMS.Write(data);
                                }
                                break;
                            default: // No need to implement it, we lazy
                                break;
                        }
                        break;

                    case 0x40435545: // @CUE - Might be used to play a certain part of the video, but shouldn't be needed anyway (appears in cutscene Cs_Sumeru_AQ30161501_DT)
                        Console.WriteLine("@CUE field detected in USM, skipping as we don't need it");
                        break;
                    default:
                        Console.WriteLine("Signature {0} unknown, skipping...", info.signature);
                        break;
                }
            }
            // Closing Streams
            filePointer.Close();
            foreach (BinaryWriter stream in fileStreams.Values) stream.Close();
            return filePaths;
        }
    }

}
