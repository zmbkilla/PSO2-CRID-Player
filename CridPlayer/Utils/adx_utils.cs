using System;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;

public class AdxReader
{
    private const ushort ADX_KEY_MAX_TEST_FRAMES = 32768;
    private const int ADX_KEY_TEST_BUFFER_SIZE = 0x8000;

    public struct adxdata
    {
        public short[] musicdata;
        public int samplerate;
        public int channels;
        public int bitspersample;
        public ushort startoffset;
        public uint samplecount;
        public byte blocksize;
        public byte version;
        public ushort cutoff;
        public int[] cough;
        public short[][] Prev;
        public bool EOF;
    }

    public void getadxencoding(MemoryStream ms,ref adxdata mdata)
    {
        BinaryReader binaryReader = new BinaryReader(ms);
        ms.Position = 0;
        ushort check = binaryReader.ReadUInt16();
        if (check != 0x080)
        {
            return;
        }

        ushort startoffset = (ushort)(BitConverter.ToUInt16(binaryReader.ReadBytes(2).Reverse().ToArray()) +0x4);

        ms.Position = 0x4;
        byte encoding = (byte)binaryReader.ReadByte();
        if(encoding == 0x3)
        {
            Console.WriteLine("encoding: adx");
        }
        ms.Position = 0x5;
        byte framesize = (byte)binaryReader.ReadByte();
        if (framesize != 0x12)
        {
            framesize = 0;
        }
        ms.Position = 0x6;
        byte bitsPerSample = (byte)binaryReader.ReadByte();

        if (bitsPerSample != 0x4)
        {
            bitsPerSample = 0;
        }
        ms.Position = 0x7;
        byte channels = binaryReader.ReadByte();
        if(channels > 8)
        {
            Console.WriteLine("using newer encoding");
        }
        ms.Position = 0x8;
        
        uint sampleRate = BitConverter.ToUInt32(binaryReader.ReadBytes(4).Reverse().ToArray());

        ms.Position = 0xc;
        uint numberOfSamples = binaryReader.ReadUInt32();

        ms.Position = 0x10;
        ushort cutoff = binaryReader.ReadUInt16();

        ms.Position = 0x12;
        byte version = binaryReader.ReadByte();

        //only going to handle version 0x0400 for now
        if (version == 4)
        {
            int baseSize = 0x18;  // Equivalent to size_t
            int histSize;
            uint ainfSize = 0;
            int loopsSize = 0x18;
            long ainfOffset;  // Equivalent to off_t

            string headerType = "meta_ADX_04";  // Assuming it's a string or constant

            // Hist offset is always present but often blank
            int histOffset = baseSize;

            // Hist size calculation
            histSize = (channels > 1) ? 0x04 * channels : 0x04 + 0x04; // Min size is 0x8, even in 1ch files

            ainfOffset = baseSize + histSize + 0x04;
            ms.Position = ainfOffset;
            if (Encoding.UTF8.GetString(binaryReader.ReadBytes(4)) == "AINF")
            {
                ms.Position = ainfOffset + 4;
                ainfSize = binaryReader.ReadUInt32();

            }

            int loopoffset = 0;
            int loopflag = 0;
            short loopstartsample = 0;
            short loopendsample = 0;
            if (startoffset - ainfOffset - 0x6 >= histOffset + histSize + loopsSize)
            {
                loopoffset = baseSize + histSize;
                ms.Position = loopoffset + 0x4;
                loopflag = binaryReader.ReadInt32();
                ms.Position = loopoffset + 0x8;
                loopstartsample = binaryReader.ReadInt16();
                ms.Position = loopoffset + 0x10;
                loopendsample = binaryReader.ReadInt16();


            }
            //get cri string
            byte[] cri_string = new byte[6];
            ms.Position = startoffset - 6;
            cri_string =binaryReader.ReadBytes(6);
            if (Encoding.UTF8.GetString(cri_string) != "(c)CRI")
            {
                return;
            }

            //setup adxdata
            ms.Position = 0;
            byte[] setup = ms.ToArray();
            byte[] test = setup[(startoffset+4)..setup.Length];

            //for (int i = 0; i < test.Length; i += 2)
            //{
            //    byte temp = test[i];
            //    test[i] = test[i + 1];
            //    test[i + 1] = temp;
            //}
            mdata.channels = channels;
            mdata.samplerate = (int)sampleRate;
            mdata.bitspersample = bitsPerSample;
            mdata.startoffset = (ushort)(startoffset+4);
            mdata.samplecount = numberOfSamples;
            mdata.blocksize = framesize;
            mdata.version = version;
            mdata.cutoff = cutoff;
            //mdata.musicdata = DecodeAdx(test, mdata.samplerate,mdata.bitspersample,mdata.channels);
            MemoryStream bms = new MemoryStream();
            mdata.musicdata = new short[test.Length];
            byte[] result = Convert4BitTo8Bit(test);

            ProcessData(result, mdata.musicdata);
        }

    }


    


    byte[] Convert4BitTo8Bit(byte[] input)
    {
        byte[] output = new byte[input.Length * 2]; // 4 bits to 8 bits
        for (int i = 0; i < input.Length; i++)
        {
            output[i * 2] = (byte)(input[i] << 4); // Shift the 4-bit value into 8-bit space
            output[i * 2 + 1] = (byte)(input[i] & 0xF); // Handle low nibble if needed
        }
        return output;
    }

    // Converts the getword macro to a method in C#
    static int GetWord(byte[] inArray, ref int index)
    {
        int word = (inArray[index] * 256) + inArray[index + 1];
        index += 2; // Equivalent to "in++" in C
        return word;
    }

    static void ProcessData(byte[] inArray, short[] outArray)
    {
        int n, a, x, y = 0, z = 0, ao = 0;
        int i = 0;
        int index = 0;

        // Use GetWord function to extract the first word
        n = GetWord(inArray, ref index) * 8 + 7;

        // Process data
        for (i = 0; i < 16; i++)
        {
            a = (inArray[index] << 8) & 0xf000;
            if (a >= 0x8000)
                a -= 0x10000;

            x = (n * (a + ao) >> 16);
            outArray[i] = (short)((0x11e0 * x + 0x7298 * y - 0x3350 * z) * 2 / 0x8000);
            z = y; y = x;

            x = (n * a * 2 >> 16);
            outArray[i + 1] = (short)((0x11e0 * x + 0x7298 * y - 0x3350 * z) * 2 / 0x8000);
            z = y; y = x;

            ao = a;

            a = (inArray[index] << 12) & 0xf000;
            if (a >= 0x8000)
                a -= 0x10000;

            x = (n * (a + ao) >> 16);
            outArray[i + 2] = (short)((0x11e0 * x + 0x7298 * y - 0x3350 * z) * 2 / 0x8000);
            z = y; y = x;

            x = (n * a * 2 >> 16);
            outArray[i + 3] = (short)((0x11e0 * x + 0x7298 * y - 0x3350 * z) * 2 / 0x8000);
            z = y; y = x;

            ao = a;
            index++; // Move to the next byte in the array
        }
    }



    public static short[] DecodeAdx(byte[] adxData, int sampleRate, int bitsPerSample, int channels)
    {
        List<short> decodedSamples = new List<short>();

        int blockSize = 18; // ADX standard block size
        int index = 0;

        while (index + blockSize <= adxData.Length)
        {
            for (int ch = 0; ch < channels; ch++)
            {
                int header = adxData[index++]; // Read header (Unused in this version, but may be needed)
                if (index + 2 > adxData.Length) break;
                int scale = BitConverter.ToInt16(adxData, index);
                index += 2;

                short sample1 = 0;
                short sample2 = 0;

                for (int i = 0; i < (blockSize - 2) / 2; i++)
                {
                    byte data = adxData[index++];
                    short decoded1 = (short)((data >> 4) * scale); // Decode high nibble
                    short decoded2 = (short)((data & 0xF) * scale); // Decode low nibble

                    decoded1 = Clamp(decoded1 + Predict(sample1, sample2));
                    decoded2 = Clamp(decoded2 + Predict(decoded1, sample1));

                    sample2 = sample1;
                    sample1 = decoded1;

                    decodedSamples.Add(decoded1);
                    decodedSamples.Add(decoded2);
                }
            }
        }

        return decodedSamples.ToArray();
    }

    private static short Predict(short sample1, short sample2)
    {
        // A more detailed prediction model may be needed
        return (short)((sample1 * 0.9375) - (sample2 * 0.28125)); // Typical ADX prediction
    }

    private static short Clamp(int value)
    {
        return (short)Math.Max(short.MinValue, Math.Min(short.MaxValue, value));
    }

}

public class VgmStream
{
    public int SampleRate { get; set; }
    public int NumSamples { get; set; }
    public int LoopStartSample { get; set; }
    public int LoopEndSample { get; set; }
    public ushort CodecConfig { get; set; }
    public CodingType CodingType { get; set; }

    public VgmStream(int channels, int loopFlag)
    {
        // Constructor logic to initialize the VGMStream
    }
}



public enum CodingType
{
    CriAdxFixed,
    CriAdx,
    CriAdxExp,
    CriAdxEnc8,
    CriAdxEnc9
}

public enum MetaType
{
    Adx03,
    Adx04,
    Adx05
}
