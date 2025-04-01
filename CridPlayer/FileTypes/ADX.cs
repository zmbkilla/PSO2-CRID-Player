using Microsoft.VisualBasic;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AdxReader;
using static CridPlayer.FileTypes.ADX;

namespace CridPlayer.FileTypes
{
    public class ADX
    {
        byte[] adxData;
        bool isRunning = false;
        bool loop = false;
        public AdxInfo ADX_Info = new AdxInfo();

        public byte[] pcmBuffer;
        private short[] outBuffer = new short[32 * 2];
        private AdxPrev[] prev = new AdxPrev[2] { new AdxPrev(), new AdxPrev() };
        public int pcmSize = 0, wsize;
        private const int BASEVOL = 0x4000; // Equivalent to the original BASEVOL

        private byte[] adxBuffer;
        private int pcmSamples;
        private Stream adxStream;
        private int adxPosition = 0; // Replaces CurrentPosition

        // Struct to hold ADX header information
        public struct AdxInfo
        {
            public int SampleOffset;
            public int ChunkSize;
            public int Channels;
            public int Rate;
            public int Samples;
            public int LoopType;
            public int Loop;
            public int LoopSampStart;
            public int LoopStart;
            public int LoopSampEnd;
            public int LoopEnd;
            public int LoopSamples;
        }
        // Constants for ADX file offsets
        public static class Constants
        {
            public const int ADX_HDR_SIZE = 64;  // Example header size
            public const byte ADX_HDR_SIG = 0x80; // ADX file signature

            public const int ADX_ADDR_START = 2;
            public const int ADX_ADDR_CHUNK = 4;
            public const int ADX_ADDR_CHAN = 5;
            public const int ADX_ADDR_RATE = 8;
            public const int ADX_ADDR_SAMP = 12;
            public const int ADX_ADDR_TYPE = 16;
            public const int ADX_ADDR_LOOP = 20;
            public const int ADX_ADDR_SAMP_START = 24;
            public const int ADX_ADDR_BYTE_START = 28;
            public const int ADX_ADDR_SAMP_END = 32;
            public const int ADX_ADDR_BYTE_END = 36;
        }

        public int adx_parse(byte[] buf)
        {
            if (buf == null || buf.Length < Constants.ADX_HDR_SIZE)
                return -1; // Invalid buffer size

            // Check ADX file signature
            if (buf[0] != Constants.ADX_HDR_SIG)
                return -1;

            // Parse ADX file header
            ADX_Info.SampleOffset = ReadBE16(buf, Constants.ADX_ADDR_START) - 2;
            ADX_Info.ChunkSize = buf[Constants.ADX_ADDR_CHUNK];
            ADX_Info.Channels = buf[Constants.ADX_ADDR_CHAN];
            ADX_Info.Rate = ReadBE32(buf, Constants.ADX_ADDR_RATE);
            ADX_Info.Samples = ReadBE32(buf, Constants.ADX_ADDR_SAMP);
            ADX_Info.LoopType = buf[Constants.ADX_ADDR_TYPE];

            // Loop information (Type 3 and Type 4)
            if (ADX_Info.LoopType == 3)
                ADX_Info.Loop = ReadBE32(buf, Constants.ADX_ADDR_LOOP);
            else if (ADX_Info.LoopType == 4)
                ADX_Info.Loop = ReadBE32(buf, Constants.ADX_ADDR_LOOP + 0x0C);

            if (ADX_Info.Loop > 1 || ADX_Info.Loop < 0) // Invalid header check
                ADX_Info.Loop = 0;

            if (ADX_Info.Loop != 0)
            {
                int offset = ADX_Info.LoopType == 3 ? 0 : 0x0C;
                ADX_Info.LoopSampStart = ReadBE32(buf, Constants.ADX_ADDR_SAMP_START + offset);
                ADX_Info.LoopStart = ReadBE32(buf, Constants.ADX_ADDR_BYTE_START + offset);
                ADX_Info.LoopSampEnd = ReadBE32(buf, Constants.ADX_ADDR_SAMP_END + offset);
                ADX_Info.LoopEnd = ReadBE32(buf, Constants.ADX_ADDR_BYTE_END + offset);
                ADX_Info.LoopSamples = ADX_Info.LoopSampEnd - ADX_Info.LoopSampStart;
            }

            // Check for CRI file signature
            string criSignature = Encoding.ASCII.GetString(buf, ADX_Info.SampleOffset, 6);
            if (criSignature != "(c)CRI")
            {
                Console.WriteLine("Invalid ADX header!");
                return -1;
            }

            return 1;
        }

        private static int ReadBE16(byte[] buf, int offset)
        {
            return (buf[offset] << 8) | buf[offset + 1];
        }

        private static int ReadBE32(byte[] buf, int offset)
        {
            return (buf[offset] << 24) | (buf[offset + 1] << 16) | (buf[offset + 2] << 8) | buf[offset + 3];
        }

        // Alternative using BitConverter (if you read from a stream)
        public static int ReadBE16UsingBitConverter(byte[] buf)
        {
            return BinaryPrimitives.ReverseEndianness(BitConverter.ToInt16(buf, 0));
        }

        public static int ReadBE32UsingBitConverter(byte[] buf)
        {
            return BinaryPrimitives.ReverseEndianness(BitConverter.ToInt32(buf, 0));
        }

        public bool AdxDec(byte[] fileData, bool loopEnable)
        {
            Console.WriteLine("LibADX: Checking Status");
            if (isRunning)
            {
                Console.WriteLine("LibADX: Already Running in another process!");
                return false;
            }

            if (fileData == null || fileData.Length == 0)
            {
                Console.WriteLine("LibADX: Invalid file data");
                return false;
            }

            adxData = fileData; // Store ADX file as byte array

            byte[] adxBuf = fileData; // Example buffer size for parsing
            //Array.Copy(adxData, 0, adxBuf, 0, Math.Min(adxBuf.Length, adxData.Length));

            if (adx_parse(adxBuf) < 1) // Equivalent to `adx_parse(adx_buf)`
            {
                Console.WriteLine("LibADX: Invalid File Header");
                return false;
            }

            Console.WriteLine("LibADX: Starting");
            isRunning = true;

            //Task.Run(() => AdxThread()); // Run in a separate thread
            adxBuffer = fileData;
            AdxThread();

            return true;
        }

        public void AdxThread()
        {
            pcmSamples = ADX_Info.Samples;

            if (pcmBuffer == null)
                pcmBuffer = new byte[1024*1024];

            adx_dec:
            while (pcmSamples > 0)
            {
                if (ADX_Info.Loop > 0 && adxPosition >= ADX_Info.LoopEnd)
                {
                    goto dec_finished;
                }

                if (pcmSize < pcmBuffer.Length - (16384 * ADX_Info.Channels))
                {
                    short[] tmpBuffer = new short[32 * ADX_Info.Channels];
                    Array.Copy(adxBuffer, adxPosition, adxBuffer, 0, ADX_Info.ChunkSize * ADX_Info.Channels);
                    adxPosition += ADX_Info.ChunkSize * ADX_Info.Channels;

                    for (int ch = 0; ch < ADX_Info.Channels; ch++)
                    {
                        AdxToPcm(tmpBuffer.AsSpan(ch * 32, 32), adxBuffer.AsSpan(ch * ADX_Info.ChunkSize, ADX_Info.ChunkSize), ref prev[ch]);
                    }

                    for (int i = 0; i < 32; i++)
                    {
                        for (int ch = 0; ch < ADX_Info.Channels; ch++)
                        {
                            outBuffer[i * ADX_Info.Channels + ch] = tmpBuffer[ch * 32 + i];
                        }
                    }
                    wsize = Math.Min(32, pcmSamples);
                    pcmSamples -= wsize;
                    Buffer.BlockCopy(outBuffer, 0, pcmBuffer, pcmSize, wsize * 2 * ADX_Info.Channels);
                    pcmSize += wsize * 2 * ADX_Info.Channels;
                }
            }

            dec_finished:
            if (ADX_Info.Loop > 0)
            {
                adxPosition = ADX_Info.LoopStart;
                ADX_Info.Samples = ADX_Info.LoopSamples;
                goto adx_dec;
            }
        }

        public static void AdxToPcm(Span<short> output, ReadOnlySpan<byte> input,ref AdxPrev prev)
        {
            if (input.Length < 18 || output.Length < 32)
                throw new ArgumentException("Invalid buffer size");

            int scale = (input[0] << 8) | input[1]; // Read big-endian scale
            int s0, s1 = prev.S1, s2 = prev.S2, d;

            input = input.Slice(2); // Skip first 2 bytes
            int outIndex = 0;

            for (int i = 0; i < 16; i++)
            {
                // Upper 4 bits (high nibble)
                d = input[i] >> 4;
                if ((d & 8) != 0) d -= 16;
                s0 = (BASEVOL * d * scale + 0x7298 * s1 - 0x3350 * s2) >> 14;
                s0 = Math.Clamp(s0, short.MinValue, short.MaxValue);
                output[outIndex++] = (short)s0;

                s2 = s1;
                s1 = s0;

                // Lower 4 bits (low nibble)
                d = input[i] & 0x0F;
                if ((d & 8) != 0) d -= 16;
                s0 = (BASEVOL * d * scale + 0x7298 * s1 - 0x3350 * s2) >> 14;
                s0 = Math.Clamp(s0, short.MinValue, short.MaxValue);
                output[outIndex++] = (short)s0;

                s2 = s1;
                s1 = s0;
            }

            // Store back to struct
            prev.S1 = s1;
            prev.S2 = s2;
        }
    }

    public class AdxPrev
    {
        public int S1 { get; set; }
        public int S2 { get; set; }
    }
}
