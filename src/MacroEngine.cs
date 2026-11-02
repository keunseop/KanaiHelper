using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace KanaiHelper
{
    public enum MacroMode
    {
        Cube1Slot,
        Cube2Slot,
        Salvage,
        Reforge
    }

    public class MacroEngine
    {
        public const string D3_WINDOW_CLASS = "D3 Main Window Class";
        private const int NativeDiabloWidth = 3440;
        private const int NativeDiabloHeight = 1440;

        private readonly AppConfig _config;
        private CancellationTokenSource _cts;
        private bool _isRunning;
        private readonly object _lock = new object();

        // Auto-Repeat Skill Helper State (Toggle via F3)
        private CancellationTokenSource _repeatCts;
        private bool _isRepeatRunning;
        private readonly object _repeatLock = new object();

        // Hold-to-Repeat Mouse Watcher State (Independent of F3)
        private volatile bool _disposed;

        public bool IsRunning
        {
            get { return _isRunning; }
        }

        public bool IsRepeatRunning
        {
            get { return _isRepeatRunning; }
        }

        public event Action<string> Log;
        public event Action<string, bool> StatusChanged;
        public event Action<int, int> ProgressChanged;
        public event Action<bool> RepeatStatusChanged;
        public event Action<bool, bool> HoldMouseStatusChanged;

        public MacroEngine(AppConfig config)
        {
            _config = config;
            StartHoldMouseWatcher();
        }

        public void Shutdown()
        {
            _disposed = true;
            StopRepeat();
            Stop();
        }

        public void EmitCustomLog(string message)
        {
            EmitLog(message);
        }

        private void EmitLog(string message)
        {
            if (Log != null)
            {
                Log(string.Format("[{0:HH:mm:ss}] {1}", DateTime.Now, message));
            }
        }

        private void EmitStatus(string status, bool running)
        {
            if (StatusChanged != null)
            {
                StatusChanged(status, running);
            }
        }

        private void EmitProgress(int current, int total)
        {
            if (ProgressChanged != null)
            {
                ProgressChanged(current, total);
            }
        }

        private void EmitRepeatStatus(bool running)
        {
            if (RepeatStatusChanged != null)
            {
                RepeatStatusChanged(running);
            }
        }

        public void ToggleRepeat()
        {
            if (_isRepeatRunning)
            {
                StopRepeat();
            }
            else
            {
                StartRepeat();
            }
        }

        public void StartRepeat()
        {
            lock (_repeatLock)
            {
                if (_isRepeatRunning) return;
                _isRepeatRunning = true;
                _repeatCts = new CancellationTokenSource();
            }

            EmitRepeatStatus(true);
            if (!_isRunning)
            {
                EmitStatus(I18n.StatusRepeatRunning(_config.ActiveRepeatPreset + 1), true);
            }
            EmitLog(I18n.LogRepeatStarted(_config.ActiveRepeatPreset + 1));

            CancellationToken token = _repeatCts.Token;
            Thread repeatThread = new Thread(delegate()
            {
                RunRepeatWorker(token);
            });
            repeatThread.IsBackground = true;
            repeatThread.Priority = ThreadPriority.AboveNormal;
            repeatThread.Start();
        }

        public void StopRepeat()
        {
            lock (_repeatLock)
            {
                if (!_isRepeatRunning) return;
                _isRepeatRunning = false;
                if (_repeatCts != null && !_repeatCts.IsCancellationRequested)
                {
                    _repeatCts.Cancel();
                }
            }

            EmitRepeatStatus(false);
            if (!_isRunning)
            {
                EmitStatus(I18n.StatusIdle, false);
            }
            EmitLog(I18n.LogRepeatStopped);
        }

        public void RefreshRepeatStatusBadge()
        {
            if (_isRepeatRunning && !_isRunning)
            {
                EmitStatus(I18n.StatusRepeatRunning(_config.ActiveRepeatPreset + 1), true);
            }
        }

        private void StartHoldMouseWatcher()
        {
            Thread holdThread = new Thread(RunHoldMouseWorker);
            holdThread.IsBackground = true;
            holdThread.Priority = ThreadPriority.AboveNormal;
            holdThread.Start();
        }

        private void RunHoldMouseWorker()
        {
            long nextLeftTick = 0;
            long nextRightTick = 0;
            bool lastLeftActive = false;
            bool lastRightActive = false;
            double ticksPerMs = Stopwatch.Frequency / 1000.0;

            while (!_disposed)
            {
                try
                {
                    if (_isRunning || !GlobalHook.IsDiabloActive())
                    {
                        nextLeftTick = 0;
                        nextRightTick = 0;
                        if (lastLeftActive || lastRightActive)
                        {
                            lastLeftActive = false;
                            lastRightActive = false;
                            if (HoldMouseStatusChanged != null) HoldMouseStatusChanged(false, false);
                        }
                        Thread.Sleep(20);
                        continue;
                    }

                    RepeatPreset preset = _config.GetActivePreset();
                    bool leftHeld = IsBindingPhysicallyHeld(preset.LeftClickKey);
                    bool rightHeld = IsBindingPhysicallyHeld(preset.RightClickKey);

                    if (leftHeld != lastLeftActive || rightHeld != lastRightActive)
                    {
                        lastLeftActive = leftHeld;
                        lastRightActive = rightHeld;
                        if (HoldMouseStatusChanged != null) HoldMouseStatusChanged(leftHeld, rightHeld);
                    }

                    if (!leftHeld && !rightHeld)
                    {
                        nextLeftTick = 0;
                        nextRightTick = 0;
                        Thread.Sleep(12);
                        continue;
                    }

                    long now = Stopwatch.GetTimestamp();

                    if (leftHeld)
                    {
                        int interval = preset.LeftClickIntervalMs > 0 ? Math.Max(10, preset.LeftClickIntervalMs) : 50; // 0 = default 50ms
                        if (now >= nextLeftTick)
                        {
                            SendRepeatMouseClick(NativeMethods.VK_LBUTTON);
                            nextLeftTick = now + (long)(interval * ticksPerMs);
                        }
                    }
                    else
                    {
                        nextLeftTick = 0;
                    }

                    if (rightHeld)
                    {
                        int interval = preset.RightClickIntervalMs > 0 ? Math.Max(10, preset.RightClickIntervalMs) : 50; // 0 = default 50ms
                        if (now >= nextRightTick)
                        {
                            SendRepeatMouseClick(NativeMethods.VK_RBUTTON);
                            nextRightTick = now + (long)(interval * ticksPerMs);
                        }
                    }
                    else
                    {
                        nextRightTick = 0;
                    }

                    Thread.Sleep(2);
                }
                catch
                {
                    Thread.Sleep(20);
                }
            }
        }

        private bool IsBindingPhysicallyHeld(HotkeyBinding b)
        {
            if (b == null || b.KeyCode == 0) return false;
            // Avoid self-feedback loop if someone sets Left/Right mouse button itself as a hold key
            if (b.KeyCode == NativeMethods.VK_LBUTTON || b.KeyCode == NativeMethods.VK_RBUTTON) return false;

            if ((NativeMethods.GetAsyncKeyState(b.KeyCode) & 0x8000) == 0) return false;
            if (b.Ctrl && (NativeMethods.GetAsyncKeyState(NativeMethods.VK_CONTROL) & 0x8000) == 0) return false;
            if (b.Shift && (NativeMethods.GetAsyncKeyState(NativeMethods.VK_SHIFT) & 0x8000) == 0) return false;
            if (b.Alt && (NativeMethods.GetAsyncKeyState(NativeMethods.VK_MENU) & 0x8000) == 0) return false;
            return true;
        }

        private void RunRepeatWorker(CancellationToken ct)
        {
            NativeMethods.timeBeginPeriod(1);
            try
            {
                long[] nextSkillTick = new long[6];
                int lastPresetIdx = -1;
                double ticksPerMs = Stopwatch.Frequency / 1000.0;

                while (!ct.IsCancellationRequested)
                {
                    // Pause automatic skill pressing while a one-shot Kanai/Salvage macro is active
                    // or when Diablo III is not the active foreground window
                    if (_isRunning || !GlobalHook.IsDiabloActive())
                    {
                        Array.Clear(nextSkillTick, 0, 6);
                        if (ct.WaitHandle.WaitOne(20)) break;
                        continue;
                    }

                    int curPresetIdx = _config.ActiveRepeatPreset;
                    if (curPresetIdx != lastPresetIdx)
                    {
                        Array.Clear(nextSkillTick, 0, 6);
                        lastPresetIdx = curPresetIdx;
                    }

                    RepeatPreset preset = _config.GetActivePreset();
                    long now = Stopwatch.GetTimestamp();

                    // Process 6 Skill Slots
                    for (int i = 0; i < 6; i++)
                    {
                        if (ct.IsCancellationRequested) break;
                        SkillSlotConfig slot = preset.Skills[i];
                        if (slot != null && slot.Enabled && slot.Key != null && slot.Key.KeyCode != 0 && slot.IntervalMs > 0)
                        {
                            int interval = Math.Max(10, slot.IntervalMs);
                            if (now >= nextSkillTick[i])
                            {
                                SendRepeatBinding(slot.Key);
                                nextSkillTick[i] = now + (long)(interval * ticksPerMs);
                            }
                        }
                    }

                    if (ct.WaitHandle.WaitOne(2)) break;
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception)
            {
            }
            finally
            {
                NativeMethods.timeEndPeriod(1);
            }
        }

        private void SendRepeatBinding(HotkeyBinding binding)
        {
            if (binding == null || binding.KeyCode == 0) return;

            int vk = binding.KeyCode;
            if (vk == NativeMethods.VK_LBUTTON || vk == NativeMethods.VK_RBUTTON || vk == NativeMethods.VK_MBUTTON)
            {
                SendRepeatMouseClick(vk);
                return;
            }

            byte bVk = (byte)vk;
            byte scan = (byte)NativeMethods.MapVirtualKey((uint)vk, 0);
            NativeMethods.keybd_event(bVk, scan, 0, NativeMethods.MACRO_INPUT_SIGNATURE);
            NativeMethods.keybd_event(bVk, scan, NativeMethods.KEYEVENTF_KEYUP, NativeMethods.MACRO_INPUT_SIGNATURE);
        }

        private void SendRepeatMouseClick(int vkMouse)
        {
            if (vkMouse == NativeMethods.VK_LBUTTON)
            {
                NativeMethods.mouse_event(NativeMethods.MOUSEEVENTF_LEFTDOWN, 0, 0, 0, NativeMethods.MACRO_INPUT_SIGNATURE);
                NativeMethods.mouse_event(NativeMethods.MOUSEEVENTF_LEFTUP, 0, 0, 0, NativeMethods.MACRO_INPUT_SIGNATURE);
            }
            else if (vkMouse == NativeMethods.VK_RBUTTON)
            {
                NativeMethods.mouse_event(NativeMethods.MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, NativeMethods.MACRO_INPUT_SIGNATURE);
                NativeMethods.mouse_event(NativeMethods.MOUSEEVENTF_RIGHTUP, 0, 0, 0, NativeMethods.MACRO_INPUT_SIGNATURE);
            }
            else if (vkMouse == NativeMethods.VK_MBUTTON)
            {
                NativeMethods.mouse_event(NativeMethods.MOUSEEVENTF_MIDDLEDOWN, 0, 0, 0, NativeMethods.MACRO_INPUT_SIGNATURE);
                NativeMethods.mouse_event(NativeMethods.MOUSEEVENTF_MIDDLEUP, 0, 0, 0, NativeMethods.MACRO_INPUT_SIGNATURE);
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                if (_isRunning && _cts != null && !_cts.IsCancellationRequested)
                {
                    _cts.Cancel();
                    EmitLog(I18n.LogEscAborted);
                }
            }
        }

        public bool Start(MacroMode mode)
        {
            lock (_lock)
            {
                if (_isRunning)
                {
                    EmitLog(I18n.LogMacroRunning);
                    return false;
                }
                _isRunning = true;
                _cts = new CancellationTokenSource();
            }

            Thread macroThread = new Thread(delegate()
            {
                RunWorker(mode, _cts.Token);
            });
            macroThread.IsBackground = true;
            macroThread.Priority = ThreadPriority.Highest;
            macroThread.Start();

            return true;
        }

        private void RunWorker(MacroMode mode, CancellationToken ct)
        {
            NativeMethods.timeBeginPeriod(1);
            try
            {
                EmitStatus(I18n.StatusModifiers, true);
                if (!WaitForModifiersReleased(3000, ct))
                {
                    EmitLog(I18n.LogModifiersTimeout);
                    return;
                }

                ReleaseStuckModifiers();
                SleepWithCancel(30, ct);

                IntPtr hWnd = NativeMethods.FindWindow(D3_WINDOW_CLASS, null);
                if (hWnd == IntPtr.Zero)
                {
                    EmitLog(I18n.LogD3NotFound);
                    EmitStatus(I18n.LogD3NotFound, false);
                    return;
                }

                NativeMethods.SetForegroundWindow(hWnd);

                NativeMethods.RECT clientRect;
                if (!NativeMethods.GetClientRect(hWnd, out clientRect) || clientRect.Width <= 0 || clientRect.Height <= 0)
                {
                    EmitLog(I18n.LogD3NoClient);
                    return;
                }

                NativeMethods.POINT origin = new NativeMethods.POINT { X = 0, Y = 0 };
                NativeMethods.ClientToScreen(hWnd, ref origin);

                int dWidth = clientRect.Width;
                int dHeight = clientRect.Height;

                EmitLog(I18n.LogD3Detected(dWidth, dHeight));

                // Base coordinates (for 3440x1440)
                Point fill = ConvertCoord(957, 1121, 2, dWidth, dHeight, origin);
                Point transmute = ConvertCoord(315, 1106, 2, dWidth, dHeight, origin);
                Point topLeftInv = ConvertCoord(2753, 748, 3, dWidth, dHeight, origin);
                Point invSize = ConvertCoord(668, 394, 4, dWidth, dHeight, origin, true);
                Point switchPages = ConvertCoord(180, 180, 4, dWidth, dHeight, origin, true);
                Point salvage = ConvertCoord(220, 390, 2, dWidth, dHeight, origin);

                int columns = 10;
                int rows = 6;
                int slotX = (int)Math.Round((double)invSize.X / columns);
                int slotY = (int)Math.Round((double)invSize.Y / rows);

                topLeftInv.X += slotX / 2;
                topLeftInv.Y += slotY / 2;

                Point switchPagesLeft = new Point(fill.X - switchPages.X, fill.Y);
                Point switchPagesRight = new Point(fill.X + switchPages.X, fill.Y);

                int sleepMs = _config.GlobalSleep;

                switch (mode)
                {
                    case MacroMode.Cube1Slot:
                        EmitStatus(I18n.StatusCube1, true);
                        EmitLog(I18n.LogCube1Start(sleepMs));
                        RunCubeMacro(1, columns, rows, topLeftInv, slotX, slotY, fill, transmute, switchPagesLeft, switchPagesRight, sleepMs, ct);
                        break;

                    case MacroMode.Cube2Slot:
                        EmitStatus(I18n.StatusCube2, true);
                        EmitLog(I18n.LogCube2Start(sleepMs));
                        RunCubeMacro(2, columns, rows, topLeftInv, slotX, slotY, fill, transmute, switchPagesLeft, switchPagesRight, sleepMs, ct);
                        break;

                    case MacroMode.Salvage:
                        EmitStatus(I18n.StatusSalvage, true);
                        EmitLog(I18n.LogSalvageStart(sleepMs));
                        RunSalvageMacro(salvage, columns, rows, topLeftInv, slotX, slotY, sleepMs, ct);
                        break;

                    case MacroMode.Reforge:
                        EmitStatus(I18n.StatusReforge, true);
                        EmitLog(I18n.LogReforgeStart(sleepMs));
                        RunReforgeMacro(topLeftInv, fill, transmute, switchPagesLeft, switchPagesRight, sleepMs, ct);
                        break;
                }

                if (!ct.IsCancellationRequested)
                {
                    EmitLog(I18n.LogSuccess);
                    if (_isRepeatRunning)
                    {
                        EmitStatus(I18n.StatusRepeatRunning(_config.ActiveRepeatPreset + 1), true);
                    }
                    else
                    {
                        EmitStatus(I18n.StatusIdle, false);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                if (_isRepeatRunning)
                {
                    EmitStatus(I18n.StatusRepeatRunning(_config.ActiveRepeatPreset + 1), true);
                }
                else
                {
                    EmitStatus(I18n.StatusStopped, false);
                }
            }
            catch (Exception ex)
            {
                EmitLog(I18n.LogError(ex.Message));
                EmitStatus(I18n.StatusError, false);
            }
            finally
            {
                NativeMethods.timeEndPeriod(1);
                ReleaseStuckModifiers();
                lock (_lock)
                {
                    _isRunning = false;
                }
                EmitProgress(0, 0);
            }
        }

        private void RunCubeMacro(int itemSize, int columns, int rows, Point topLeftInv, int slotX, int slotY,
            Point fill, Point transmute, Point switchLeft, Point switchRight, int sleepMs, CancellationToken ct)
        {
            int totalCycles = (columns * rows) / itemSize;
            int col = 0;
            int row = 0;

            for (int cycle = 0; cycle < totalCycles; cycle++)
            {
                ct.ThrowIfCancellationRequested();
                EmitProgress(cycle + 1, totalCycles);

                int clickX = topLeftInv.X + slotX * col;
                int clickY = topLeftInv.Y + slotY * row;

                ClickMouse(clickX, clickY, false, ct);
                SleepWithCancel(sleepMs, ct);

                ClickMouse(fill.X, fill.Y, true, ct);
                SleepWithCancel(sleepMs, ct);

                ClickMouse(transmute.X, transmute.Y, true, ct);
                SleepWithCancel(sleepMs + 70, ct);

                ClickMouse(switchRight.X, switchRight.Y, true, ct);
                SleepWithCancel(sleepMs, ct);

                ClickMouse(switchLeft.X, switchLeft.Y, true, ct);
                SleepWithCancel(sleepMs, ct);

                if (itemSize == 2)
                {
                    col++;
                    if (col > 9 && row < 4)
                    {
                        col = 0;
                        row += itemSize;
                    }
                }
                else
                {
                    row++;
                    if (row > 5 && col < 9)
                    {
                        row = 0;
                        col += itemSize;
                    }
                }
            }

            SleepWithCancel(70, ct);
        }

        private void RunSalvageMacro(Point salvageBtn, int columns, int rows, Point topLeftInv, int slotX, int slotY,
            int sleepMs, CancellationToken ct)
        {
            ClickMouse(salvageBtn.X, salvageBtn.Y, true, ct);

            int adjustedSleep = Math.Max((int)Math.Round(sleepMs * 0.4), 3);
            EmitLog(I18n.LogSalvageDelay(sleepMs, adjustedSleep));
            SleepWithCancel(adjustedSleep, ct);

            int totalCycles = columns * rows;
            int col = 0;
            int row = 0;

            for (int cycle = 0; cycle < totalCycles; cycle++)
            {
                ct.ThrowIfCancellationRequested();
                EmitProgress(cycle + 1, totalCycles);

                int clickX = topLeftInv.X + slotX * col;
                int clickY = topLeftInv.Y + slotY * row;

                SleepWithCancel(adjustedSleep, ct);
                ClickMouse(clickX, clickY, true, ct);

                SleepWithCancel(adjustedSleep, ct);
                SendKey((byte)NativeMethods.VK_RETURN, ct);

                SleepWithCancel(adjustedSleep, ct);
                SendKey((byte)NativeMethods.VK_RETURN, ct);

                SleepWithCancel(adjustedSleep, ct);
                SendKey((byte)NativeMethods.VK_ESCAPE, ct);

                col++;
                if (col > 9 && row < 5)
                {
                    col = 0;
                    row += 1;
                }
            }

            SleepWithCancel(70, ct);
        }

        private void RunReforgeMacro(Point topLeftInv, Point fill, Point transmute, Point switchLeft, Point switchRight,
            int sleepMs, CancellationToken ct)
        {
            EmitProgress(1, 1);

            ClickMouse(topLeftInv.X, topLeftInv.Y, false, ct);
            SleepWithCancel(sleepMs, ct);

            ClickMouse(fill.X, fill.Y, true, ct);
            SleepWithCancel(sleepMs, ct);

            ClickMouse(transmute.X, transmute.Y, true, ct);
            SleepWithCancel(sleepMs + 70, ct);

            ClickMouse(switchRight.X, switchRight.Y, true, ct);
            SleepWithCancel(sleepMs, ct);

            ClickMouse(switchLeft.X, switchLeft.Y, true, ct);
            SleepWithCancel(sleepMs, ct);

            NativeMethods.SetCursorPos(topLeftInv.X, topLeftInv.Y);
            SleepWithCancel(70, ct);
        }

        private Point ConvertCoord(int x, int y, int position, int dWidth, int dHeight, NativeMethods.POINT origin, bool isRelativeSize = false)
        {
            double scaleH = (double)dHeight / NativeDiabloHeight;
            double resX = x;
            double resY = y * scaleH;

            if (isRelativeSize)
            {
                resX = x * scaleH;
                return new Point((int)Math.Round(resX), (int)Math.Round(resY));
            }

            if (position == 1)
            {
                resX = x * scaleH + (dWidth - NativeDiabloWidth * scaleH) / 2.0;
            }
            else if (position == 2 || position == 4)
            {
                resX = x * scaleH;
            }
            else if (position == 3)
            {
                resX = dWidth - (NativeDiabloWidth - x) * scaleH;
            }

            int screenX = origin.X + (int)Math.Round(resX);
            int screenY = origin.Y + (int)Math.Round(resY);

            return new Point(screenX, screenY);
        }

        private void ClickMouse(int x, int y, bool isLeft, CancellationToken ct)
        {
            NativeMethods.SetCursorPos(x, y);
            if (isLeft)
            {
                NativeMethods.mouse_event(NativeMethods.MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
                SleepWithCancel(2, ct);
                NativeMethods.mouse_event(NativeMethods.MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
            }
            else
            {
                NativeMethods.mouse_event(NativeMethods.MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, UIntPtr.Zero);
                SleepWithCancel(2, ct);
                NativeMethods.mouse_event(NativeMethods.MOUSEEVENTF_RIGHTUP, 0, 0, 0, UIntPtr.Zero);
            }
        }

        private void SendKey(byte vkCode, CancellationToken ct)
        {
            byte scan = (byte)NativeMethods.MapVirtualKey(vkCode, 0);
            NativeMethods.keybd_event(vkCode, scan, 0, NativeMethods.MACRO_INPUT_SIGNATURE);
            SleepWithCancel(2, ct);
            NativeMethods.keybd_event(vkCode, scan, NativeMethods.KEYEVENTF_KEYUP, NativeMethods.MACRO_INPUT_SIGNATURE);
        }

        private void SleepWithCancel(int ms, CancellationToken ct)
        {
            if (ms <= 0) return;
            if (ct.IsCancellationRequested)
                throw new OperationCanceledException(ct);

            long targetTicks = (long)(ms * (Stopwatch.Frequency / 1000.0));
            long start = Stopwatch.GetTimestamp();

            while (true)
            {
                if (ct.IsCancellationRequested)
                    throw new OperationCanceledException(ct);

                long elapsed = Stopwatch.GetTimestamp() - start;
                if (elapsed >= targetTicks)
                    break;

                long remainingMs = (targetTicks - elapsed) * 1000 / Stopwatch.Frequency;
                if (remainingMs >= 2)
                {
                    Thread.Sleep(0);
                }
                else
                {
                    Thread.SpinWait(100);
                }
            }
        }

        private bool WaitForModifiersReleased(int timeoutMs, CancellationToken ct)
        {
            int[] keys = new int[] {
                NativeMethods.VK_CONTROL, NativeMethods.VK_LCONTROL, NativeMethods.VK_RCONTROL,
                NativeMethods.VK_SHIFT, NativeMethods.VK_LSHIFT, NativeMethods.VK_RSHIFT,
                NativeMethods.VK_MENU, NativeMethods.VK_LMENU, NativeMethods.VK_RMENU,
                NativeMethods.VK_LWIN, NativeMethods.VK_RWIN
            };

            int elapsed = 0;
            while (elapsed < timeoutMs)
            {
                ct.ThrowIfCancellationRequested();
                bool anyHeld = false;
                foreach (int k in keys)
                {
                    if ((NativeMethods.GetAsyncKeyState(k) & 0x8000) != 0)
                    {
                        anyHeld = true;
                        break;
                    }
                }
                if (!anyHeld) return true;
                if (ct.WaitHandle.WaitOne(10)) return false;
                elapsed += 10;
            }
            return false;
        }

        private void ReleaseStuckModifiers()
        {
            NativeMethods.mouse_event(NativeMethods.MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
            NativeMethods.mouse_event(NativeMethods.MOUSEEVENTF_RIGHTUP, 0, 0, 0, UIntPtr.Zero);

            int[] modifierVks = new int[] {
                NativeMethods.VK_LCONTROL, NativeMethods.VK_RCONTROL,
                NativeMethods.VK_CONTROL,
                NativeMethods.VK_LSHIFT, NativeMethods.VK_RSHIFT,
                NativeMethods.VK_SHIFT
            };

            foreach (int vk in modifierVks)
            {
                byte scan = (byte)NativeMethods.MapVirtualKey((uint)vk, 0);
                NativeMethods.keybd_event((byte)vk, scan, NativeMethods.KEYEVENTF_KEYUP, NativeMethods.MACRO_INPUT_SIGNATURE);
            }
        }
    }
}
