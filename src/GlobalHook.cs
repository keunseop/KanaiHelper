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
        private int _heldToggleVk = 0;

        public bool IsRecordingHotkey { get; set; }
        public Action<HotkeyBinding> OnHotkeyRecorded;
        public Action OnHotkeyCancelled;

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
                NativeMethods.KBDLLHOOKSTRUCT kb = (NativeMethods.KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(NativeMethods.KBDLLHOOKSTRUCT));
                int vk = (int)kb.vkCode;

                // 0. Ignore inputs injected by our own macro
                if (kb.dwExtraInfo.ToInt64() == 0x43554245L)
                {
                    return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
                }

                if (msg == NativeMethods.WM_KEYUP || msg == NativeMethods.WM_SYSKEYUP)
                {
                    if (vk == _heldToggleVk)
                    {
                        _heldToggleVk = 0;
                    }
                    return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
                }

                if (msg == NativeMethods.WM_KEYDOWN || msg == NativeMethods.WM_SYSKEYDOWN)
                {
                    // 1. If one-shot macro is running, check for ESC and pass through
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
                        if (vk == NativeMethods.VK_ESCAPE)
                        {
                            IsRecordingHotkey = false;
                            if (OnHotkeyCancelled != null)
                            {
                                OnHotkeyCancelled();
                            }
                            return (IntPtr)1;
                        }

                        if (vk == NativeMethods.VK_DELETE || vk == NativeMethods.VK_BACK)
                        {
                            IsRecordingHotkey = false;
                            if (OnHotkeyRecorded != null)
                            {
                                OnHotkeyRecorded(new HotkeyBinding(false, false, false, false, 0));
                            }
                            return (IntPtr)1;
                        }

                        if (vk != NativeMethods.VK_CONTROL && vk != NativeMethods.VK_LCONTROL && vk != NativeMethods.VK_RCONTROL &&
                            vk != NativeMethods.VK_SHIFT && vk != NativeMethods.VK_LSHIFT && vk != NativeMethods.VK_RSHIFT &&
                            vk != NativeMethods.VK_MENU && vk != NativeMethods.VK_LMENU && vk != NativeMethods.VK_RMENU &&
                            vk != NativeMethods.VK_LWIN && vk != NativeMethods.VK_RWIN)
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
                        return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
                    }

                    bool isCtrl = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_CONTROL) & 0x8000) != 0;
                    bool isShift = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_SHIFT) & 0x8000) != 0;
                    bool isAlt = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_MENU) & 0x8000) != 0;
                    bool isWin = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_LWIN) & 0x8000) != 0 ||
                                 (NativeMethods.GetAsyncKeyState(NativeMethods.VK_RWIN) & 0x8000) != 0;

                    bool d3Active = IsDiabloActive();
                    bool selfActive = IsSelfActive();

                    // 3. Auto-Repeat Toggle Key (works in Diablo 3 or KanaiHelper window)
                    if ((d3Active || selfActive) && MatchesBinding(_config.RepeatToggleKey, isCtrl, isShift, isAlt, isWin, vk))
                    {
                        if (_heldToggleVk != vk)
                        {
                            _heldToggleVk = vk;
                            _engine.ToggleRepeat();
                        }
                        return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
                    }

                    // 4. Check if Diablo 3 is the active foreground window
                    if (d3Active)
                    {
                        if (vk == NativeMethods.VK_ESCAPE && _engine.IsRepeatRunning)
                        {
                            _engine.StopRepeat();
                        }

                        if (MatchesBinding(_config.Hotkey1Slot, isCtrl, isShift, isAlt, isWin, vk))
                        {
                            _engine.Start(MacroMode.Cube1Slot);
                        }
                        else if (MatchesBinding(_config.Hotkey2Slot, isCtrl, isShift, isAlt, isWin, vk))
                        {
                            _engine.Start(MacroMode.Cube2Slot);
                        }
                        else if (MatchesBinding(_config.HotkeySalvage, isCtrl, isShift, isAlt, isWin, vk))
                        {
                            _engine.Start(MacroMode.Salvage);
                        }
                        else if (MatchesBinding(_config.HotkeyReforge, isCtrl, isShift, isAlt, isWin, vk))
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

        public static bool IsSelfActive()
        {
            IntPtr fg = NativeMethods.GetForegroundWindow();
            if (fg == IntPtr.Zero) return false;

            StringBuilder sb = new StringBuilder(256);
            NativeMethods.GetWindowText(fg, sb, 256);
            return sb.ToString().StartsWith("KanaiHelper");
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
