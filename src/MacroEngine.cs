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
                    EmitLog("ESC 감지: 매크로가 즉시 중단되었습니다.");
                }
            }
        }

        public bool Start(MacroMode mode)
        {
            lock (_lock)
            {
                if (_isRunning)
                {
                    EmitLog("이미 매크로가 실행 중입니다. 무시됨.");
                    return false;
                }
                _isRunning = true;
                _cts = new CancellationTokenSource();
            }

            Task.Factory.StartNew(delegate
            {
                RunWorker(mode, _cts.Token);
            });

            return true;
        }

        private void RunWorker(MacroMode mode, CancellationToken ct)
        {
            NativeMethods.timeBeginPeriod(1);
            try
            {
                EmitStatus("수식키(Ctrl 등) 릴리즈 대기 중...", true);
                if (!WaitForModifiersReleased(3000, ct))
                {
                    EmitLog("수식키를 3초 내에 떼지 않아 취소되었습니다.");
                    return;
                }

                ReleaseStuckModifiers();
                SleepWithCancel(30, ct);

                IntPtr hWnd = NativeMethods.FindWindow(D3_WINDOW_CLASS, null);
                if (hWnd == IntPtr.Zero)
                {
                    EmitLog("오류: Diablo III 창을 찾을 수 없습니다.");
                    EmitStatus("Diablo III 미실행", false);
                    return;
                }

                NativeMethods.RECT clientRect;
                if (!NativeMethods.GetClientRect(hWnd, out clientRect) || clientRect.Width <= 0 || clientRect.Height <= 0)
                {
                    EmitLog("오류: Diablo III 클라이언트 영역 크기를 가져오지 못했습니다.");
                    return;
                }

                NativeMethods.POINT origin = new NativeMethods.POINT { X = 0, Y = 0 };
                NativeMethods.ClientToScreen(hWnd, ref origin);

                int dWidth = clientRect.Width;
                int dHeight = clientRect.Height;

                EmitLog(string.Format("Diablo III 감지 완료 ({0}x{1})", dWidth, dHeight));

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
                        EmitStatus("1칸 큐브 변환 실행 중...", true);
                        EmitLog("1칸 아이템 큐브 변환 시작 (60칸)");
                        RunCubeMacro(1, columns, rows, topLeftInv, slotX, slotY, fill, transmute, switchPagesLeft, switchPagesRight, sleepMs, ct);
                        break;

                    case MacroMode.Cube2Slot:
                        EmitStatus("2칸 큐브 변환 실행 중...", true);
                        EmitLog("2칸 아이템 큐브 변환 시작 (30칸)");
                        RunCubeMacro(2, columns, rows, topLeftInv, slotX, slotY, fill, transmute, switchPagesLeft, switchPagesRight, sleepMs, ct);
                        break;

                    case MacroMode.Salvage:
                        EmitStatus("인벤토리 분해 실행 중...", true);
                        EmitLog("인벤토리 일괄 분해 시작 (가로 우선 60칸)");
                        RunSalvageMacro(salvage, columns, rows, topLeftInv, slotX, slotY, sleepMs, ct);
                        break;

                    case MacroMode.Reforge:
                        EmitStatus("재련 1회 실행 중...", true);
                        EmitLog("전설 재련 1회 시작 (첫 번째 슬롯)");
                        RunReforgeMacro(topLeftInv, fill, transmute, switchPagesLeft, switchPagesRight, sleepMs, ct);
                        break;
                }

                if (!ct.IsCancellationRequested)
                {
                    EmitLog("작업이 성공적으로 완료되었습니다.");
                    EmitStatus("대기 중", false);
                }
            }
            catch (OperationCanceledException)
            {
                EmitStatus("중단됨 (ESC)", false);
            }
            catch (Exception ex)
            {
                EmitLog("오류 발생: " + ex.Message);
                EmitStatus("오류 발생", false);
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
                SleepWithCancel(sleepMs + 100, ct);

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
            SleepWithCancel(120, ct);
        }

        private void RunSalvageMacro(Point salvageBtn, int columns, int rows, Point topLeftInv, int slotX, int slotY,
            int sleepMs, CancellationToken ct)
        {
            // 1. Click Blacksmith Salvage button first
            ClickMouse(salvageBtn.X, salvageBtn.Y, true, ct);

            // AHK 원본과 완벽히 부합하는 안정적이고 빠른 분해 간격
            int adjustedSleep = Math.Max((int)Math.Round(sleepMs * 0.4), 8);
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
            SleepWithCancel(120, ct);
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
            SleepWithCancel(sleepMs + 100, ct);

            ClickMouse(switchRight.X, switchRight.Y, true, ct);
            SleepWithCancel(sleepMs, ct);

            ClickMouse(switchLeft.X, switchLeft.Y, true, ct);
            SleepWithCancel(sleepMs, ct);

            NativeMethods.SetCursorPos(topLeftInv.X, topLeftInv.Y);
            SleepWithCancel(120, ct);
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
            if (ct.WaitHandle.WaitOne(ms))
            {
                throw new OperationCanceledException(ct);
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
