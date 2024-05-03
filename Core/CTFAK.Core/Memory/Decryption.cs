using CTFAK.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace CTFAK.Memory
{
    public static class Decryption
    {
        private static byte[] decodeBuffer = new byte[256];
        public static bool Valid;
        public static byte MagicChar = 54;

        // Thx LAK
        // It's 0:40, I might revisit this part again when I don't feel as crappy
        // I might even just redo it with a single byte array with 256 elements
        // Revisiting this day after, I literally did just that. Wasn't hard at all
        // But hey, at least this works ;)
        public static void MakeKey(string data1, string data2, string data3)
        {
            var MagicKey = MakeKeyCombined(Encoding.UTF8.GetBytes(data1 + data2 + data3));
            for (int i = 0; i < 256; i++)
                decodeBuffer[i] = (byte)i;

            Func<byte, byte> rotate = (byte value) => (byte)((value << 7) | (value >> 1));
            byte accum = MagicChar;
            byte hash = MagicChar;

            bool never_reset_key = true;

            byte i2 = 0;
            byte key = 0;
            for (uint i = 0; i < 256; ++i, ++key)
            {
                hash = rotate(hash);

                if (never_reset_key)
                {
                    accum += ((hash & 1) == 0) ? (byte)2 : (byte)3;
                    accum *= MagicKey[key];
                }

                if (hash == MagicKey[key])
                {
                    hash = rotate(MagicChar);
                    key = 0;

                    never_reset_key = false;
                }

                i2 += (byte)((hash ^ MagicKey[key]) + decodeBuffer[i]);

                (decodeBuffer[i2], decodeBuffer[i]) = (decodeBuffer[i], decodeBuffer[i2]);
            }
            Valid = true;
        }

        public static byte[] MakeKeyCombined(byte[] data)
        {
            int dataLen = Math.Min(128, data.Length);
            Array.Resize(ref data, 256);
            Array.Clear(data, 128, 128);

            byte accum = MagicChar;
            byte hash = MagicChar;

            for (int i = 0; i <= dataLen; ++i)
            {
                hash = (byte)((hash << 7) + (hash >> 1));
                data[i] ^= hash;
                accum += (byte)(data[i] * ((hash & 1) + 2));
            }

            data[dataLen + 1] = accum;
            return data;
        }

        public static byte[] DecodeMode3(byte[] chunkData, int chunkId, out int decompressed)
        {
            var reader = new ByteReader(chunkData);
            var decompressedSize = reader.ReadUInt32();

            var rawData = reader.ReadBytes((int)reader.Size());

            if ((chunkId & 1) == 1 && Settings.Build > 284)
                rawData[0] ^= (byte)((byte)(chunkId & 0xFF) ^ (byte)(chunkId >> 0x8));

            TransformChunk(rawData);

            using (var data = new ByteReader(rawData))
            {
                var compressedSize = data.ReadUInt32();
                decompressed = (int)decompressedSize;
                return Decompressor.DecompressBlock(data, (int)compressedSize);
            }
        }

        public static bool TransformChunk(byte[] chunk)
        {
            if (!Valid) return false;
            byte[] tempBuf = new byte[256];
            Array.Copy(decodeBuffer, tempBuf, 256);

            byte i = 0;
            byte i2 = 0;
            for (int j = 0; j < chunk.Length; j++)
            {
                ++i;
                i2 += (byte)tempBuf[i];
                (tempBuf[i2], tempBuf[i]) = (tempBuf[i], tempBuf[i2]);
                var xor = tempBuf[(byte)(tempBuf[i] + tempBuf[i2])];
                chunk[j] ^= xor;
            }
            return true;
        }
    }
}