using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace KanaiHelper
{
    public class GlobalHook : IDisposable
    {
        private IntPtr _hookId = IntPtr.Zero;
        private NativeMethods.LowLevelKeyboardProc _proc;
        private readonly AppConfig _config;
        private readonly MacroEngine _engine;

        public bool IsRecordingHotkey { get; set; }
        public Action<HotkeyBinding> OnHotkeyRecorded;

        public GlobalHook(AppConfig config, MacroEngine engine)
        {
            _config = config;
            _engine = engine;
            _proc = HookCallback;
            InstallHook();
        }

        private void InstallHook()
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                IntPtr hMod = NativeMethods.GetModuleHandle(curModule.ModuleName);
                _hookId = NativeMethods.SetWindowsHookEx(NativeMethods.WH_KEYBOARD_LL, _proc, hMod, 0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int msg = wParam.ToInt32();
                if (msg == NativeMethods.WM_KEYDOWN || msg == NativeMethods.WM_SYSKEYDOWN)
                {
                    NativeMethods.KBDLLHOOKSTRUCT kb = (NativeMethods.KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(NativeMethods.KBDLLHOOKSTRUCT));
                    int vk = (int)kb.vkCode;

                    // 0. Ignore inputs injected by our own macro (e.g. ESC sent during salvage)
                    if (kb.dwExtraInfo.ToInt64() == 0x43554245L)
                    {
                        return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
                    }

                    // 1. If macro is running, only check for ESC and immediately pass through other keys
                    if (_engine.IsRunning)
                    {
                        if (vk == NativeMethods.VK_ESCAPE)
                        {
                            _engine.Stop();
                        }
                        return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
                    }

                    // 2. Hotkey recording mode
                    if (IsRecordingHotkey)
                    {
                        if (vk != NativeMethods.VK_CONTROL && vk != NativeMethods.VK_LCONTROL && vk != NativeMethods.VK_RCONTROL &&
                            vk != NativeMethods.VK_SHIFT && vk != NativeMethods.VK_LSHIFT && vk != NativeMethods.VK_RSHIFT &&
                            vk != NativeMethods.VK_MENU && vk != NativeMethods.VK_LMENU && vk != NativeMethods.VK_RMENU &&
                            vk != NativeMethods.VK_LWIN && vk != NativeMethods.VK_RWIN && vk != NativeMethods.VK_ESCAPE)
                        {
                            bool ctrl = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_CONTROL) & 0x8000) != 0;
                            bool shift = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_SHIFT) & 0x8000) != 0;
                            bool alt = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_MENU) & 0x8000) != 0;
                            bool win = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_LWIN) & 0x8000) != 0 ||
                                       (NativeMethods.GetAsyncKeyState(NativeMethods.VK_RWIN) & 0x8000) != 0;

                            HotkeyBinding recorded = new HotkeyBinding(ctrl, shift, alt, win, vk);
                            IsRecordingHotkey = false;
                            if (OnHotkeyRecorded != null)
                            {
                                OnHotkeyRecorded(recorded);
                            }
                            return (IntPtr)1; // block event
                        }
                    }

                    // 3. Check if Diablo 3 is the active foreground window
                    if (IsDiabloActive())
                    {
                        bool ctrl = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_CONTROL) & 0x8000) != 0;
                        bool shift = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_SHIFT) & 0x8000) != 0;
                        bool alt = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_MENU) & 0x8000) != 0;
                        bool win = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_LWIN) & 0x8000) != 0 ||
                                   (NativeMethods.GetAsyncKeyState(NativeMethods.VK_RWIN) & 0x8000) != 0;

                        if (MatchesBinding(_config.Hotkey1Slot, ctrl, shift, alt, win, vk))
                        {
                            _engine.Start(MacroMode.Cube1Slot);
                        }
                        else if (MatchesBinding(_config.Hotkey2Slot, ctrl, shift, alt, win, vk))
                        {
                            _engine.Start(MacroMode.Cube2Slot);
                        }
                        else if (MatchesBinding(_config.HotkeySalvage, ctrl, shift, alt, win, vk))
                        {
                            _engine.Start(MacroMode.Salvage);
                        }
                        else if (MatchesBinding(_config.HotkeyReforge, ctrl, shift, alt, win, vk))
                        {
                            _engine.Start(MacroMode.Reforge);
                        }
                    }
                }
            }

            return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        private bool MatchesBinding(HotkeyBinding binding, bool ctrl, bool shift, bool alt, bool win, int vk)
        {
            if (binding == null || binding.KeyCode == 0) return false;
            return binding.KeyCode == vk &&
                   binding.Ctrl == ctrl &&
                   binding.Shift == shift &&
                   binding.Alt == alt &&
                   binding.Win == win;
        }

        public static bool IsDiabloActive()
        {
            IntPtr fg = NativeMethods.GetForegroundWindow();
            if (fg == IntPtr.Zero) return false;

            StringBuilder sb = new StringBuilder(256);
            NativeMethods.GetClassName(fg, sb, 256);
            return sb.ToString() == MacroEngine.D3_WINDOW_CLASS;
        }

        public void Dispose()
        {
            if (_hookId != IntPtr.Zero)
            {
                NativeMethods.UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
            }
        }
    }
}
