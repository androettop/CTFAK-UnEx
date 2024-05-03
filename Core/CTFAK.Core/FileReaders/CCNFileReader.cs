using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.CompilerServices;
using System.Text;
using CTFAK.CCN;
using CTFAK.CCN.Chunks.Banks;
using CTFAK.FileReaders;
using CTFAK.Memory;
using CTFAK.Utils;

namespace CTFAK.EXE
{
    public class CCNFileReader:IFileReader
    {
        private const int wwww = 0x77777777;
        public string Name => "CCN";
        public GameData game;
        public GameData getGameData()
        {
            return game;
        }

        public void LoadGame(string gamePath)
        {
            CTFAKCore.currentReader = this;
            var reader = new ByteReader(gamePath, System.IO.FileMode.Open);
            PackData? packData = null;

            if (reader.PeekInt32() == wwww)
                (packData = new PackData()).Read(reader);

            game = new GameData();
            game.Read(reader);
            if (packData != null)
                game.packData = packData;
        }

        public Dictionary<int, Bitmap> getIcons()
        {
            return ApkFileReader.androidIcons;
        }

        public void PatchMethods()
        {
            //Settings.gameType = Settings.GameType.ANDROID;
        }

        public IFileReader Copy()
        {
            CCNFileReader reader = new CCNFileReader();
            reader.game = game;
            return reader;
        }
    }
}