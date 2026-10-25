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

        public bool IsRunning
        {
            get { return _isRunning; }
        }

        public event Action<string> Log;
        public event Action<string, bool> StatusChanged;
        public event Action<int, int> ProgressChanged;

        public MacroEngine(AppConfig config)
        {
            _config = config;
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

                // 디아블로 3 창을 확실하게 전면 활성화하여 '배경 FPS 제한(30fps/8fps)' 모드로 인한 50ms 지연 원천 방지
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
                    EmitStatus(I18n.StatusIdle, false);
                }
            }
            catch (OperationCanceledException)
            {
                EmitStatus(I18n.StatusStopped, false);
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

                // 1. Right click item
                ClickMouse(clickX, clickY, false, ct);
                SleepWithCancel(sleepMs, ct);

                // 2. Click Fill
                ClickMouse(fill.X, fill.Y, true, ct);
                SleepWithCancel(sleepMs, ct);

                // 3. Click Transmute
                ClickMouse(transmute.X, transmute.Y, true, ct);
                SleepWithCancel(sleepMs + 70, ct);

                // 4. Switch Page Right
                ClickMouse(switchRight.X, switchRight.Y, true, ct);
                SleepWithCancel(sleepMs, ct);

                // 5. Switch Page Left
                ClickMouse(switchLeft.X, switchLeft.Y, true, ct);
                SleepWithCancel(sleepMs, ct);

                // Advance coordinates
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

            // 디아블로 3 클라이언트가 마지막 페이지 넘김을 완전히 처리할 수 있도록 안정화 대기
            SleepWithCancel(70, ct);
        }

        private void RunSalvageMacro(Point salvageBtn, int columns, int rows, Point topLeftInv, int slotX, int slotY,
            int sleepMs, CancellationToken ct)
        {
            // 1. Click Blacksmith Salvage button first
            ClickMouse(salvageBtn.X, salvageBtn.Y, true, ct);

            // 슬라이더 GlobalSleep 값을 정직하게 반영 (하한선 8ms 락을 풀어 5~20ms 구간에서도 속도 변화 체감)
            int adjustedSleep = Math.Max((int)Math.Round(sleepMs * 0.4), 3);
            EmitLog(I18n.LogSalvageDelay(sleepMs, adjustedSleep));
            SleepWithCancel(adjustedSleep, ct);

            int totalCycles = columns * rows; // 60 slots
            int col = 0;
            int row = 0;

            for (int cycle = 0; cycle < totalCycles; cycle++)
            {
                ct.ThrowIfCancellationRequested();
                EmitProgress(cycle + 1, totalCycles);

                int clickX = topLeftInv.X + slotX * col;
                int clickY = topLeftInv.Y + slotY * row;

                // 2. 아이템 클릭
                SleepWithCancel(adjustedSleep, ct);
                ClickMouse(clickX, clickY, true, ct);

                // 3. 전설 팝업 확인용 첫 번째 Enter
                SleepWithCancel(adjustedSleep, ct);
                SendKey((byte)NativeMethods.VK_RETURN, ct);

                // 4. 확인 팝업 승인용 두 번째 Enter
                SleepWithCancel(adjustedSleep, ct);
                SendKey((byte)NativeMethods.VK_RETURN, ct);

                // 5. 팝업 잔류 해제 및 모드 안전 복귀용 ESC
                SleepWithCancel(adjustedSleep, ct);
                SendKey((byte)NativeMethods.VK_ESCAPE, ct);

                // Salvage is strictly row-major (horizontal first, row by row)
                col++;
                if (col > 9 && row < 5)
                {
                    col = 0;
                    row += 1;
                }
            }

            // 마지막 ESC 처리가 디아블로 3 클라이언트에서 완료되어 UI가 정상 복귀할 수 있도록 안정화 대기
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

            if (position == 1) // Center
            {
                resX = x * scaleH + (dWidth - NativeDiabloWidth * scaleH) / 2.0;
            }
            else if (position == 2 || position == 4) // Left
            {
                resX = x * scaleH;
            }
            else if (position == 3) // Right
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
            // 마우스 버튼이 눌린 상태로 남아있지 않도록 마우스 좌/우 버튼 강제 릴리즈 (우클릭 먹통 방지)
            NativeMethods.mouse_event(NativeMethods.MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
            NativeMethods.mouse_event(NativeMethods.MOUSEEVENTF_RIGHTUP, 0, 0, 0, UIntPtr.Zero);

            // 단축키(Ctrl+5~8)를 누른 후 손을 떼는 과정에서 디아블로 3에 Ctrl/Shift가 눌린 채로 고정되는 현상을 완벽하게 해제.
            // (Ctrl이 남아있으면 스킬 키 및 마우스 우클릭이 Ctrl+우클릭(무반응)으로 인식되어 게임 조작이 먹통됨)
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
