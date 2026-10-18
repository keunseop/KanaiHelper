using System;
using System.IO;
using System.Text;

namespace KanaiHelper
{
    public class HotkeyBinding
    {
        public bool Ctrl { get; set; }
        public bool Shift { get; set; }
        public bool Alt { get; set; }
        public bool Win { get; set; }
        public int KeyCode { get; set; } // Virtual Key Code

        public HotkeyBinding()
        {
        }

        public HotkeyBinding(bool ctrl, bool shift, bool alt, bool win, int keyCode)
        {
            Ctrl = ctrl;
            Shift = shift;
            Alt = alt;
            Win = win;
            KeyCode = keyCode;
        }

        public string ToDisplayString()
        {
            if (KeyCode == 0) return "미지정";
            StringBuilder sb = new StringBuilder();
            if (Ctrl) sb.Append("Ctrl + ");
            if (Shift) sb.Append("Shift + ");
            if (Alt) sb.Append("Alt + ");
            if (Win) sb.Append("Win + ");

            sb.Append(KeyToString(KeyCode));
            return sb.ToString();
        }

        public string ToIniString()
        {
            if (KeyCode == 0) return "";
            StringBuilder sb = new StringBuilder();
            if (Ctrl) sb.Append("^");
            if (Shift) sb.Append("+");
            if (Alt) sb.Append("!");
            if (Win) sb.Append("#");

            sb.Append(KeyToChar(KeyCode));
            return sb.ToString();
        }

        public static HotkeyBinding FromIniString(string s)
        {
            if (string.IsNullOrEmpty(s)) return new HotkeyBinding();

            bool ctrl = false, shift = false, alt = false, win = false;
            int idx = 0;
            while (idx < s.Length)
            {
                char c = s[idx];
                if (c == '^') ctrl = true;
                else if (c == '+') shift = true;
                else if (c == '!') alt = true;
                else if (c == '#') win = true;
                else if (c == '~') { /* AHK passthrough ignore */ }
                else break;
                idx++;
            }

            string rest = s.Substring(idx);
            int vk = ParseKey(rest);
            return new HotkeyBinding(ctrl, shift, alt, win, vk);
        }

        private static int ParseKey(string keyStr)
        {
            if (string.IsNullOrEmpty(keyStr)) return 0;
            if (keyStr.Length == 1)
            {
                char c = char.ToUpperInvariant(keyStr[0]);
                if (c >= '0' && c <= '9') return (int)c;
                if (c >= 'A' && c <= 'Z') return (int)c;
            }

            string u = keyStr.ToUpperInvariant();
            if (u == "F1") return 0x70;
            if (u == "F2") return 0x71;
            if (u == "F3") return 0x72;
            if (u == "F4") return 0x73;
            if (u == "F5") return 0x74;
            if (u == "F6") return 0x75;
            if (u == "F7") return 0x76;
            if (u == "F8") return 0x77;
            if (u == "F9") return 0x78;
            if (u == "F10") return 0x79;
            if (u == "F11") return 0x7A;
            if (u == "F12") return 0x7B;
            if (u == "SPACE") return 0x20;
            if (u == "TAB") return 0x09;

            return 0;
        }

        private static string KeyToString(int vk)
        {
            if (vk >= '0' && vk <= '9') return ((char)vk).ToString();
            if (vk >= 'A' && vk <= 'Z') return ((char)vk).ToString();
            if (vk >= 0x70 && vk <= 0x7B) return "F" + (vk - 0x70 + 1);
            if (vk == 0x20) return "Space";
            if (vk == 0x09) return "Tab";
            return "VK_" + vk;
        }

        private static string KeyToChar(int vk)
        {
            if (vk >= '0' && vk <= '9') return ((char)vk).ToString();
            if (vk >= 'A' && vk <= 'Z') return ((char)char.ToLowerInvariant((char)vk)).ToString();
            if (vk >= 0x70 && vk <= 0x7B) return "F" + (vk - 0x70 + 1);
            return KeyToString(vk);
        }
    }

    public class AppConfig
    {
        public int GlobalSleep { get; set; }
        public HotkeyBinding Hotkey1Slot { get; set; }
        public HotkeyBinding Hotkey2Slot { get; set; }
        public HotkeyBinding HotkeySalvage { get; set; }
        public HotkeyBinding HotkeyReforge { get; set; }
        public bool MinimizeToTray { get; set; }

        private readonly string _iniPath;

        public AppConfig(string iniPath)
        {
            _iniPath = iniPath;
            GlobalSleep = 50;
            Hotkey1Slot = new HotkeyBinding(true, false, false, false, '5');
            Hotkey2Slot = new HotkeyBinding(true, false, false, false, '6');
            HotkeySalvage = new HotkeyBinding(true, false, false, false, '7');
            HotkeyReforge = new HotkeyBinding(true, false, false, false, '8');
            MinimizeToTray = true;

            Load();
        }

        public void Load()
        {
            if (!File.Exists(_iniPath))
            {
                Save();
                return;
            }

            StringBuilder sb = new StringBuilder(256);

            NativeMethods.GetPrivateProfileString("Settings", "Globalsleep", "50", sb, 256, _iniPath);
            int sleepVal;
            if (int.TryParse(sb.ToString(), out sleepVal))
            {
                GlobalSleep = Math.Max(5, Math.Min(500, sleepVal));
            }

            NativeMethods.GetPrivateProfileString("Hotkeys", "1", "^5", sb, 256, _iniPath);
            Hotkey1Slot = HotkeyBinding.FromIniString(sb.ToString());

            NativeMethods.GetPrivateProfileString("Hotkeys", "2", "^6", sb, 256, _iniPath);
            Hotkey2Slot = HotkeyBinding.FromIniString(sb.ToString());

            NativeMethods.GetPrivateProfileString("Hotkeys", "3", "^7", sb, 256, _iniPath);
            HotkeySalvage = HotkeyBinding.FromIniString(sb.ToString());

            NativeMethods.GetPrivateProfileString("Hotkeys", "4", "^8", sb, 256, _iniPath);
            HotkeyReforge = HotkeyBinding.FromIniString(sb.ToString());
        }

        public void Save()
        {
            try
            {
                NativeMethods.WritePrivateProfileString("Settings", "Globalsleep", GlobalSleep.ToString(), _iniPath);
                NativeMethods.WritePrivateProfileString("Hotkeys", "1", Hotkey1Slot.ToIniString(), _iniPath);
                NativeMethods.WritePrivateProfileString("Hotkeys", "2", Hotkey2Slot.ToIniString(), _iniPath);
                NativeMethods.WritePrivateProfileString("Hotkeys", "3", HotkeySalvage.ToIniString(), _iniPath);
                NativeMethods.WritePrivateProfileString("Hotkeys", "4", HotkeyReforge.ToIniString(), _iniPath);
            }
            catch { }
        }
    }
}
