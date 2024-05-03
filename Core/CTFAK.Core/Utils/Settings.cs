using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CTFAK.Utils.Settings;

namespace CTFAK.Utils
{
    public class Settings
    {
        [Flags]
        public enum GameType:byte
        {
            NORMAL = 0b00000001,
            MMF2 = 0b00000010,
            MMF15 = 0b00000100,
            ANDROID = 0b00001000,
            TWOFIVEPLUS = 0b00010000,
            F3 = 0b00100000,
            IOS = 0b01000000,
            UNKNOWN = 0b00000000
        }

        public static int Build;
        public static byte BuildType;
        public static bool Unicode;
        public static bool isMFA;
        public static bool Fusion3Seed;
        public static GameType gameType = GameType.NORMAL;
        public static bool Old => gameType.HasFlag(GameType.MMF15);
        public static bool MMF2 => gameType.HasFlag(GameType.MMF2);
        public static bool TwoFivePlus => gameType.HasFlag(GameType.TWOFIVEPLUS);
        public static bool Android => gameType.HasFlag(GameType.ANDROID);
        public static bool F3 => gameType.HasFlag(GameType.F3);
        public static bool iOS => gameType.HasFlag(GameType.IOS);
        public static bool Normal => gameType == GameType.NORMAL;

        public static string GetGameTypeStr()
        {
            string str;

            if (Old)
                str = "MMF 1.5";
            else if (MMF2)
                str = "MMF 2";
            else if (F3)
                str = "Fusion 3";
            else if (TwoFivePlus)
                str = "2.5+";
            else
                str = "Normal";

            if (Android)
                str += " Android";
            else if (iOS)
                str += " iOS";
            else if (Fusion3Seed)
                str += " Seeded";
            else if (isMFA)
                str += " MFA";

            return str + " (" + Convert.ToString((byte)gameType, 2).PadLeft(8, '0') + ")";
        }

        public static string GetExporterStr()
        {
            if (Exporter.ContainsKey(BuildType))
                return Exporter[BuildType];
            return "Unknown";
        }

        public static Dictionary<int, string> Exporter = new Dictionary<int, string>()
        {
            { 0, "Windows EXE Application" },
            { 1, "Windows Screen Saver" },
            { 2, "Sub-Application" },
            { 3, "Java Sub-Application" },
            { 4, "Java Application" },
            { 5, "Java Internet Applet" },
            { 6, "Java Web Start" },
            { 7, "Java for Mobile Devices" },
            { 9, "Java Mac Application" },
            { 10, "Adobe Flash" },
            { 11, "Java for BlackBerry" },
            { 12, "Android / OUYA Application" },
            { 13, "iOS Application" },
            { 14, "iOS Xcode Project" },
            { 15, "Final iOS Xcode Project" },
            { 17, "MAC application" },
            { 18, "XNA Windows Project" },
            { 19, "XNA Xbox Project" },
            { 20, "XNA Phone Project" },
            { 27, "HTML5 Development" },
            { 28, "HTML5 Final Project" },
            { 30, "MAC application file" },
            { 31, "MAC Xcode project" },
            { 33, "UWP Project" },
            { 34, "Android App Bundle" },
            { 74, "Nintendo Switch" },
            { 75, "Xbox One" },
            { 78, "Playstation" }
        };
    }
}
