using CTFAK.Memory;
using CTFAK.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ionic.Zlib;
using System.Drawing;

namespace CTFAK.EXE
{
    public class PackData
    {
        public List<PackFile> Items = new List<PackFile>();
        public uint FormatVersion;
        public void Read(ByteReader reader)
        {
            Logger.Log("Reading PackData", false);
            long start = reader.Tell();
            reader.Skip(12);
            int dataSize = reader.ReadInt32();

            reader.Seek(start + dataSize - 32);
            var uheader = reader.ReadAscii(4);
            Settings.gameType = Settings.GameType.NORMAL;
            if (uheader == "PAMU")
            {
                Settings.Unicode = true;
            }
            else if (uheader == "PAME")
            {
                if (!Settings.Old)
                    Settings.gameType |= Settings.GameType.MMF2;
                Settings.Unicode = false;
            }
            Logger.Log($"Found {uheader} header", false);
            var runtimeVersion = reader.ReadInt16();
            if (runtimeVersion != 770)
            {
                reader.Seek(dataSize);
                uheader = reader.ReadAscii(4);
                if (uheader == "PAMU")
                {
                    Settings.Unicode = true;
                }
                else if (uheader == "PAME")
                {
                    if (!Settings.Old)
                        Settings.gameType |= Settings.GameType.MMF2;
                    Settings.Unicode = false;
                }
                reader.Skip(2);
            }
            reader.Skip(6);
            var fusionBuild = reader.ReadInt32();

            reader.Seek(start + 28);
            uint count = reader.ReadUInt32();
            bool hasBingo = fusionBuild > 231;
            for (int i = 0; i < count; i++)
            {
                var item = new PackFile();
                item.HasBingo = hasBingo;
                item.Read(reader);
                Items.Add(item);
            }
        }
    }
    public class PackFile
    {
        public string PackFilename = "ERROR";
        int _bingo = 0;
        public byte[] Data;
        public bool HasBingo;
        public bool Compressed;
        public int size;
        public void Read(ByteReader exeReader)
        {
            ushort len = exeReader.ReadUInt16();
            PackFilename = exeReader.ReadYuniversal(len);
            if (HasBingo)
                _bingo = exeReader.ReadInt32();
            size = exeReader.ReadInt32();
            if (exeReader.PeekInt16() == -9608)
            {
                Data = Decompressor.DecompressBlock(exeReader, size);
                Compressed = true;
            }
            else
                Data = exeReader.ReadBytes(size);
            Logger.Log($"New packfile: {PackFilename}, Compressed: {Compressed}", false);
        }
    }
}
