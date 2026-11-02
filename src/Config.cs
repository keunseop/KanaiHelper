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

        public string ToDisplayString(bool emptyAsBlank = false)
        {
            if (KeyCode == 0) return emptyAsBlank ? "" : I18n.Unassigned;
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

            string rest = s.Substring(idx).Trim();
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
                if (c == '`') return 0xC0;
                if (c == '-') return 0xBD;
                if (c == '=') return 0xBB;
                if (c == '[') return 0xDB;
                if (c == ']') return 0xDD;
                if (c == ';') return 0xBA;
                if (c == '\'') return 0xDE;
                if (c == ',') return 0xBC;
                if (c == '.') return 0xBE;
                if (c == '/') return 0xBF;
                if (c == '\\') return 0xDC;
            }

            string u = keyStr.ToUpperInvariant();
            if (u == "MOUSE LEFT" || u == "MOUSELEFT" || u == "LBUTTON") return NativeMethods.VK_LBUTTON;
            if (u == "MOUSE RIGHT" || u == "MOUSERIGHT" || u == "RBUTTON") return NativeMethods.VK_RBUTTON;
            if (u == "MOUSE MIDDLE" || u == "MOUSEMIDDLE" || u == "MBUTTON") return NativeMethods.VK_MBUTTON;

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
            if (u == "CAPSLOCK") return 0x14;
            if (u == "TILDE") return 0xC0;

            if (u.StartsWith("NUMPAD") && u.Length == 7)
            {
                char d = u[6];
                if (d >= '0' && d <= '9') return 0x60 + (d - '0');
            }

            if (u.StartsWith("VK_"))
            {
                int parsedVk;
                if (int.TryParse(u.Substring(3), out parsedVk)) return parsedVk;
            }

            return 0;
        }

        private static string KeyToString(int vk)
        {
            if (vk == NativeMethods.VK_LBUTTON) return "Mouse Left";
            if (vk == NativeMethods.VK_RBUTTON) return "Mouse Right";
            if (vk == NativeMethods.VK_MBUTTON) return "Mouse Middle";

            if (vk >= '0' && vk <= '9') return ((char)vk).ToString();
            if (vk >= 'A' && vk <= 'Z') return ((char)vk).ToString();
            if (vk >= 0x70 && vk <= 0x7B) return "F" + (vk - 0x70 + 1);
            if (vk >= 0x60 && vk <= 0x69) return "Numpad" + (vk - 0x60);
            if (vk == 0x20) return "Space";
            if (vk == 0x09) return "Tab";
            if (vk == 0x14) return "CapsLock";
            if (vk == 0xC0) return "`";
            if (vk == 0xBD) return "-";
            if (vk == 0xBB) return "=";
            if (vk == 0xDB) return "[";
            if (vk == 0xDD) return "]";
            if (vk == 0xBA) return ";";
            if (vk == 0xDE) return "'";
            if (vk == 0xBC) return ",";
            if (vk == 0xBE) return ".";
            if (vk == 0xBF) return "/";
            if (vk == 0xDC) return "\\";
            return "VK_" + vk;
        }

        private static string KeyToChar(int vk)
        {
            if (vk == NativeMethods.VK_LBUTTON) return "MouseLeft";
            if (vk == NativeMethods.VK_RBUTTON) return "MouseRight";
            if (vk == NativeMethods.VK_MBUTTON) return "MouseMiddle";
            if (vk >= '0' && vk <= '9') return ((char)vk).ToString();
            if (vk >= 'A' && vk <= 'Z') return ((char)char.ToLowerInvariant((char)vk)).ToString();
            if (vk >= 0x70 && vk <= 0x7B) return "F" + (vk - 0x70 + 1);
            if (vk == 0xC0) return "Tilde";
            return KeyToString(vk);
        }
    }

    public class SkillSlotConfig
    {
        public bool Enabled { get; set; }
        public HotkeyBinding Key { get; set; }
        public int IntervalMs { get; set; }

        public SkillSlotConfig()
        {
            Enabled = true;
            Key = new HotkeyBinding();
            IntervalMs = 0;
        }

        public SkillSlotConfig(bool enabled, HotkeyBinding key, int intervalMs)
        {
            Enabled = enabled;
            Key = key ?? new HotkeyBinding();
            IntervalMs = intervalMs;
        }
    }

    public class RepeatPreset
    {
        public SkillSlotConfig[] Skills { get; set; }
        public HotkeyBinding LeftClickKey { get; set; }
        public int LeftClickIntervalMs { get; set; }
        public HotkeyBinding RightClickKey { get; set; }
        public int RightClickIntervalMs { get; set; }

        public RepeatPreset()
        {
            Skills = new SkillSlotConfig[6];
            for (int i = 0; i < 6; i++)
            {
                Skills[i] = new SkillSlotConfig();
            }
            LeftClickKey = new HotkeyBinding();
            LeftClickIntervalMs = 0;
            RightClickKey = new HotkeyBinding();
            RightClickIntervalMs = 0;
        }

        public void ResetToBlank()
        {
            for (int i = 0; i < 6; i++)
            {
                Skills[i].Enabled = true;
                Skills[i].Key = new HotkeyBinding();
                Skills[i].IntervalMs = 0;
            }
            LeftClickKey = new HotkeyBinding();
            LeftClickIntervalMs = 0;
            RightClickKey = new HotkeyBinding();
            RightClickIntervalMs = 0;
        }

        public static RepeatPreset CreateDefaultPreset1()
        {
            RepeatPreset p = new RepeatPreset();
            p.Skills[0] = new SkillSlotConfig(true, new HotkeyBinding(false, false, false, false, NativeMethods.VK_RBUTTON), 30);
            p.Skills[1] = new SkillSlotConfig(true, new HotkeyBinding(false, false, false, false, '1'), 300);
            p.Skills[2] = new SkillSlotConfig(true, new HotkeyBinding(false, false, false, false, '2'), 3000);
            p.Skills[3] = new SkillSlotConfig(true, new HotkeyBinding(false, false, false, false, '3'), 1000);
            p.Skills[4] = new SkillSlotConfig(true, new HotkeyBinding(false, false, false, false, '4'), 2000);
            p.Skills[5] = new SkillSlotConfig(true, new HotkeyBinding(false, false, false, false, 0), 0);
            return p;
        }
    }

    public class AppConfig
    {
        public int GlobalSleep { get; set; }
        public HotkeyBinding Hotkey1Slot { get; set; }
        public HotkeyBinding Hotkey2Slot { get; set; }
        public HotkeyBinding HotkeySalvage { get; set; }
        public HotkeyBinding HotkeyReforge { get; set; }
        public string Language { get; set; }
        public bool MinimizeToTray { get; set; }

        // Skill Auto-Repeat Configuration
        public HotkeyBinding RepeatToggleKey { get; set; }
        public int ActiveRepeatPreset { get; set; }
        public RepeatPreset[] RepeatPresets { get; set; }

        private readonly string _iniPath;

        public AppConfig(string iniPath)
        {
            _iniPath = iniPath;
            Language = "ko";
            GlobalSleep = 20;
            HotkeySalvage = new HotkeyBinding(true, false, false, false, '5');
            Hotkey1Slot = new HotkeyBinding(true, false, false, false, '6');
            Hotkey2Slot = new HotkeyBinding(true, false, false, false, '7');
            HotkeyReforge = new HotkeyBinding(true, false, false, false, '8');
            MinimizeToTray = true;

            RepeatToggleKey = new HotkeyBinding(false, false, false, false, 0x72); // F3
            ActiveRepeatPreset = 0;
            RepeatPresets = new RepeatPreset[4];
            RepeatPresets[0] = RepeatPreset.CreateDefaultPreset1();
            RepeatPresets[1] = new RepeatPreset();
            RepeatPresets[2] = new RepeatPreset();
            RepeatPresets[3] = new RepeatPreset();

            LoadAll();
        }

        public RepeatPreset GetActivePreset()
        {
            int idx = Math.Max(0, Math.Min(3, ActiveRepeatPreset));
            return RepeatPresets[idx];
        }

        public void LoadAll()
        {
            if (!File.Exists(_iniPath))
            {
                SaveGeneralSettings();
                SaveRepeatSettings();
                return;
            }

            LoadGeneralSettings();
            LoadRepeatSettings();
        }

        public void LoadGeneralSettings()
        {
            if (!File.Exists(_iniPath)) return;
            StringBuilder sb = new StringBuilder(256);

            NativeMethods.GetPrivateProfileString("Settings", "Language", "ko", sb, 256, _iniPath);
            string langStr = sb.ToString().Trim().ToLowerInvariant();
            Language = (langStr == "en") ? "en" : "ko";
            I18n.Language = Language;

            NativeMethods.GetPrivateProfileString("Settings", "Globalsleep", "20", sb, 256, _iniPath);
            int sleepVal;
            if (int.TryParse(sb.ToString(), out sleepVal))
            {
                GlobalSleep = Math.Max(5, Math.Min(500, sleepVal));
            }

            NativeMethods.GetPrivateProfileString("Hotkeys", "1", "^5", sb, 256, _iniPath);
            HotkeySalvage = HotkeyBinding.FromIniString(sb.ToString());

            NativeMethods.GetPrivateProfileString("Hotkeys", "2", "^6", sb, 256, _iniPath);
            Hotkey1Slot = HotkeyBinding.FromIniString(sb.ToString());

            NativeMethods.GetPrivateProfileString("Hotkeys", "3", "^7", sb, 256, _iniPath);
            Hotkey2Slot = HotkeyBinding.FromIniString(sb.ToString());

            NativeMethods.GetPrivateProfileString("Hotkeys", "4", "^8", sb, 256, _iniPath);
            HotkeyReforge = HotkeyBinding.FromIniString(sb.ToString());
        }

        public void LoadRepeatSettings()
        {
            if (!File.Exists(_iniPath)) return;
            StringBuilder sb = new StringBuilder(256);

            NativeMethods.GetPrivateProfileString("Repeat", "ToggleKey", "F3", sb, 256, _iniPath);
            RepeatToggleKey = HotkeyBinding.FromIniString(sb.ToString());

            NativeMethods.GetPrivateProfileString("Repeat", "ActivePreset", "0", sb, 256, _iniPath);
            int activeIdx;
            if (int.TryParse(sb.ToString(), out activeIdx))
            {
                ActiveRepeatPreset = Math.Max(0, Math.Min(3, activeIdx));
            }

            for (int p = 0; p < 4; p++)
            {
                string sec = "RepeatPreset" + (p + 1);
                RepeatPreset defPreset = (p == 0) ? RepeatPreset.CreateDefaultPreset1() : new RepeatPreset();
                RepeatPreset preset = new RepeatPreset();

                NativeMethods.GetPrivateProfileString(sec, "LeftClickKey", defPreset.LeftClickKey.ToIniString(), sb, 256, _iniPath);
                preset.LeftClickKey = HotkeyBinding.FromIniString(sb.ToString());

                NativeMethods.GetPrivateProfileString(sec, "LeftClickMs", defPreset.LeftClickIntervalMs.ToString(), sb, 256, _iniPath);
                int lMs;
                preset.LeftClickIntervalMs = int.TryParse(sb.ToString(), out lMs) ? Math.Max(0, lMs) : 0;

                NativeMethods.GetPrivateProfileString(sec, "RightClickKey", defPreset.RightClickKey.ToIniString(), sb, 256, _iniPath);
                preset.RightClickKey = HotkeyBinding.FromIniString(sb.ToString());

                NativeMethods.GetPrivateProfileString(sec, "RightClickMs", defPreset.RightClickIntervalMs.ToString(), sb, 256, _iniPath);
                int rMs;
                preset.RightClickIntervalMs = int.TryParse(sb.ToString(), out rMs) ? Math.Max(0, rMs) : 0;

                for (int s = 0; s < 6; s++)
                {
                    SkillSlotConfig defSlot = defPreset.Skills[s];
                    string prefix = "Skill" + (s + 1);

                    NativeMethods.GetPrivateProfileString(sec, prefix + "Enabled", defSlot.Enabled ? "1" : "0", sb, 256, _iniPath);
                    bool enabled = sb.ToString().Trim() != "0" && !sb.ToString().Trim().Equals("false", StringComparison.OrdinalIgnoreCase);

                    NativeMethods.GetPrivateProfileString(sec, prefix + "Key", defSlot.Key.ToIniString(), sb, 256, _iniPath);
                    HotkeyBinding key = HotkeyBinding.FromIniString(sb.ToString());

                    NativeMethods.GetPrivateProfileString(sec, prefix + "Ms", defSlot.IntervalMs.ToString(), sb, 256, _iniPath);
                    int ms;
                    int interval = int.TryParse(sb.ToString(), out ms) ? Math.Max(0, ms) : defSlot.IntervalMs;

                    preset.Skills[s] = new SkillSlotConfig(enabled, key, interval);
                }

                RepeatPresets[p] = preset;
            }
        }

        public void Save()
        {
            SaveGeneralSettings();
        }

        public void SaveGeneralSettings()
        {
            try
            {
                NativeMethods.WritePrivateProfileString("Settings", "Language", Language, _iniPath);
                NativeMethods.WritePrivateProfileString("Settings", "Globalsleep", GlobalSleep.ToString(), _iniPath);
                NativeMethods.WritePrivateProfileString("Hotkeys", "1", HotkeySalvage.ToIniString(), _iniPath);
                NativeMethods.WritePrivateProfileString("Hotkeys", "2", Hotkey1Slot.ToIniString(), _iniPath);
                NativeMethods.WritePrivateProfileString("Hotkeys", "3", Hotkey2Slot.ToIniString(), _iniPath);
                NativeMethods.WritePrivateProfileString("Hotkeys", "4", HotkeyReforge.ToIniString(), _iniPath);
            }
            catch { }
        }

        public void SaveRepeatSettings()
        {
            try
            {
                NativeMethods.WritePrivateProfileString("Repeat", "ToggleKey", RepeatToggleKey.ToIniString(), _iniPath);
                NativeMethods.WritePrivateProfileString("Repeat", "ActivePreset", ActiveRepeatPreset.ToString(), _iniPath);

                for (int p = 0; p < 4; p++)
                {
                    string sec = "RepeatPreset" + (p + 1);
                    RepeatPreset preset = RepeatPresets[p];
                    NativeMethods.WritePrivateProfileString(sec, "LeftClickKey", preset.LeftClickKey.ToIniString(), _iniPath);
                    NativeMethods.WritePrivateProfileString(sec, "LeftClickMs", preset.LeftClickIntervalMs.ToString(), _iniPath);
                    NativeMethods.WritePrivateProfileString(sec, "RightClickKey", preset.RightClickKey.ToIniString(), _iniPath);
                    NativeMethods.WritePrivateProfileString(sec, "RightClickMs", preset.RightClickIntervalMs.ToString(), _iniPath);

                    for (int s = 0; s < 6; s++)
                    {
                        SkillSlotConfig slot = preset.Skills[s];
                        string prefix = "Skill" + (s + 1);
                        NativeMethods.WritePrivateProfileString(sec, prefix + "Enabled", slot.Enabled ? "1" : "0", _iniPath);
                        NativeMethods.WritePrivateProfileString(sec, prefix + "Key", slot.Key.ToIniString(), _iniPath);
                        NativeMethods.WritePrivateProfileString(sec, prefix + "Ms", slot.IntervalMs.ToString(), _iniPath);
                    }
                }
            }
            catch { }
        }
    }
}
