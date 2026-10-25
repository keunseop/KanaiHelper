using System;

namespace KanaiHelper
{
    public static class I18n
    {
        public static string Language = "ko"; // "ko" or "en"

        public static bool IsKorean
        {
            get { return Language != "en"; }
        }

        // Header
        public static string LangToggleBtn { get { return IsKorean ? "🌐 English" : "🌐 한국어"; } }
        public static string D3Detecting { get { return IsKorean ? "● D3 감지 대기 중..." : "● Waiting for D3..."; } }
        public static string D3Detected(int w, int h) { return IsKorean ? string.Format("● D3 감지됨 ({0}x{1})", w, h) : string.Format("● D3 Detected ({0}x{1})", w, h); }
        public static string StatusIdle { get { return IsKorean ? "대기 중 (IDLE)" : "IDLE"; } }
        public static string StatusModifiers { get { return IsKorean ? "수식키 릴리즈 대기..." : "Waiting for key release..."; } }
        public static string StatusCube1 { get { return IsKorean ? "1칸 큐브 변환 중..." : "1-Slot Cube Converting..."; } }
        public static string StatusCube2 { get { return IsKorean ? "2칸 큐브 변환 중..." : "2-Slot Cube Converting..."; } }
        public static string StatusSalvage { get { return IsKorean ? "인벤토리 분해 중..." : "Salvaging Inventory..."; } }
        public static string StatusReforge { get { return IsKorean ? "재련 실행 중..." : "Reforging Item..."; } }
        public static string StatusStopped { get { return IsKorean ? "중단됨 (ESC)" : "Stopped (ESC)"; } }
        public static string StatusError { get { return IsKorean ? "오류 발생" : "Error Occurred"; } }

        // Dashboard Cards
        // Card 1: Salvage (Most frequently used)
        public static string Card1Title { get { return IsKorean ? "🔨  인벤토리 일괄 분해" : "🔨  Inventory Batch Salvage"; } }
        public static string Card1Desc { get { return IsKorean ? "대장장이 분해 버튼 클릭 후 60칸 전체 분해" : "Batch salvage all 60 inventory slots in Blacksmith window"; } }
        public static string Card1Sub { get { return IsKorean ? "가로 우선 탐색 (1행씩 순차 분해)" : "Horizontal scan (Row by row)"; } }

        // Card 2: 1-Slot Cube
        public static string Card2Title { get { return IsKorean ? "💎  1칸 아이템 큐브 변환" : "💎  1-Slot Cube Converter"; } }
        public static string Card2Desc { get { return IsKorean ? "반지, 목걸이 등 1칸 아이템 60칸 일괄 변환" : "Batch convert 1-slot items (rings, amulets, gems) across 60 slots"; } }
        public static string Card2Sub { get { return IsKorean ? "세로 우선 탐색 (열 단위 순회)" : "Vertical scan (Column by column)"; } }

        // Card 3: 2-Slot Cube
        public static string Card3Title { get { return IsKorean ? "⚔️  2칸 아이템 큐브 변환" : "⚔️  2-Slot Cube Converter"; } }
        public static string Card3Desc { get { return IsKorean ? "무기, 방어구 등 2칸 아이템 30칸 일괄 변환" : "Batch convert 2-slot items (weapons, armors) across 30 slots"; } }
        public static string Card3Sub { get { return IsKorean ? "가로 우선 탐색 (2행씩 건너뜀)" : "Horizontal scan (Skips 2 rows)"; } }

        // Card 4: Reforge
        public static string Card4Title { get { return IsKorean ? "✨  전설 아이템 재련 1회" : "✨  Legendary Reforge (1x)"; } }
        public static string Card4Desc { get { return IsKorean ? "인벤토리 맨 왼쪽 위 첫 번째 슬롯 아이템 재련" : "Reforge item in slot 1 (top-left of inventory)"; } }
        public static string Card4Sub { get { return IsKorean ? "재련 후 첫 번째 슬롯으로 마우스 복귀" : "Transmutes and returns mouse to slot 1"; } }

        // Card Buttons
        public static string RunButton { get { return IsKorean ? "▶ 실행" : "▶ Run"; } }
        public static string PressKey { get { return IsKorean ? "키 입력 대기 중..." : "Press any key..."; } }

        // Settings Bar
        public static string SleepLabel { get { return IsKorean ? "⏱ 클릭 간 대기 지연 (GlobalSleep): " : "⏱ Action Delay (GlobalSleep): "; } }
        public static string EscNoticePrefix { get { return IsKorean ? "취소 안내: 언제든 게임 중 " : "Emergency Stop: Press "; } }
        public static string EscNoticeSuffix { get { return IsKorean ? " 키를 누르면 즉각 중단" : " in-game to cancel immediately"; } }
        public static string TrayOption { get { return IsKorean ? "닫기(X) 시 시스템 트레이로 최소화" : "Minimize to system tray on close (X)"; } }

        // Log Console
        public static string LogHeader { get { return IsKorean ? "활동 로그 (실시간 모니터)" : "Activity Log (Live Monitor)"; } }
        public static string ClearLog { get { return IsKorean ? "로그 지우기" : "Clear Log"; } }

        // System Tray
        public static string TrayOpen { get { return IsKorean ? "KanaiHelper 열기" : "Open KanaiHelper"; } }
        public static string TrayExit { get { return IsKorean ? "종료" : "Exit"; } }
        public static string TrayBalloonTitle { get { return IsKorean ? "KanaiHelper 백그라운드 실행 중" : "KanaiHelper Running in Background"; } }
        public static string TrayBalloonText { get { return IsKorean ? "프로그램이 트레이로 최소화되었습니다. 단축키는 계속 작동합니다." : "Minimized to tray. Hotkeys remain active in-game."; } }

        // Engine Logs
        public static string LogMacroRunning { get { return IsKorean ? "이미 매크로가 실행 중입니다. 무시됨." : "Macro is already running. Ignored."; } }
        public static string LogEscAborted { get { return IsKorean ? "ESC 감지: 매크로가 즉시 중단되었습니다." : "ESC pressed: Macro aborted immediately."; } }
        public static string LogModifiersTimeout { get { return IsKorean ? "수식키를 3초 내에 떼지 않아 취소되었습니다." : "Cancelled: Modifier keys held for over 3 seconds."; } }
        public static string LogD3NotFound { get { return IsKorean ? "오류: Diablo III 창을 찾을 수 없습니다." : "Error: Diablo III window not found."; } }
        public static string LogD3NoClient { get { return IsKorean ? "오류: Diablo III 클라이언트 영역 크기를 가져오지 못했습니다." : "Error: Failed to retrieve Diablo III client dimensions."; } }
        public static string LogD3Detected(int w, int h) { return IsKorean ? string.Format("Diablo III 감지 완료 ({0}x{1})", w, h) : string.Format("Diablo III detected ({0}x{1})", w, h); }

        public static string LogCube1Start(int sleepMs) { return IsKorean ? string.Format("1칸 아이템 큐브 변환 시작 (60칸, GlobalSleep={0}ms)", sleepMs) : string.Format("1-slot cube conversion started (60 slots, GlobalSleep={0}ms)", sleepMs); }
        public static string LogCube2Start(int sleepMs) { return IsKorean ? string.Format("2칸 아이템 큐브 변환 시작 (30칸, GlobalSleep={0}ms)", sleepMs) : string.Format("2-slot cube conversion started (30 slots, GlobalSleep={0}ms)", sleepMs); }
        public static string LogSalvageStart(int sleepMs) { return IsKorean ? string.Format("인벤토리 일괄 분해 시작 (가로 우선 60칸, GlobalSleep={0}ms)", sleepMs) : string.Format("Batch salvage started (Row-major 60 slots, GlobalSleep={0}ms)", sleepMs); }
        public static string LogSalvageDelay(int sleepMs, int adjusted) { return IsKorean ? string.Format("분해 슬롯 지연 적용: GlobalSleep={0}ms ➔ 실제 분해간격={1}ms", sleepMs, adjusted) : string.Format("Salvage delay applied: GlobalSleep={0}ms ➔ Slot Interval={1}ms", sleepMs, adjusted); }
        public static string LogReforgeStart(int sleepMs) { return IsKorean ? string.Format("전설 재련 1회 시작 (첫 번째 슬롯, GlobalSleep={0}ms)", sleepMs) : string.Format("Legendary reforge started (Slot 1, GlobalSleep={0}ms)", sleepMs); }
        public static string LogSuccess { get { return IsKorean ? "작업이 성공적으로 완료되었습니다." : "Task completed successfully."; } }
        public static string LogError(string msg) { return IsKorean ? "오류 발생: " + msg : "Error: " + msg; }
    }
}
