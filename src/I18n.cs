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
        public static string PressKeyOrMouse { get { return IsKorean ? "키 입력 / 우클릭(MouseR) / 좌클릭(MouseL)" : "Press Key / Right-Click / Left-Click"; } }
        public static string Unassigned { get { return IsKorean ? "미지정" : "None"; } }
        public static string HoldKeyPlaceholder { get { return IsKorean ? "클릭하여 홀드 키 지정" : "Click to set Hold Key"; } }

        // Mode Tabs
        public static string TabKanai { get { return IsKorean ? "🔨 카나이 / 분해 매크로" : "🔨 Kanai / Salvage"; } }
        public static string TabRepeat { get { return IsKorean ? "🔄 스킬 반복키 (D3 Helper)" : "🔄 Skill Auto-Repeat"; } }

        // Skill Auto-Repeat UI (Redesigned UX)
        public static string RepeatBtnNew { get { return IsKorean ? "새로 만들기" : "Reset"; } }
        public static string RepeatBtnSave { get { return IsKorean ? "저장하기" : "Save"; } }
        public static string RepeatBtnSaveDirty { get { return IsKorean ? "저장하기 *" : "Save *"; } }
        public static string RepeatBtnLoad { get { return IsKorean ? "불러오기" : "Load"; } }
        public static string RepeatPresetTab(int idx) { return IsKorean ? "세팅 " + idx : "Preset " + idx; }

        // Section 1: Toggle Skill Auto-Repeat
        public static string RepeatSkillsSection { get { return IsKorean ? "⚡ 자동 스킬 반복" : "⚡ Skill Auto-Repeat"; } }

        // Help (?) Popups
        public static string HelpSkillsTitle { get { return IsKorean ? "자동 스킬 반복" : "Skill Auto-Repeat"; } }
        public static string HelpSkillsBody { get { return IsKorean
            ? "시작/종료 키(기본 F3)를 누르면 체크된 기술을 설정한 간격마다 자동으로 눌러줍니다. 다시 누르면 정지합니다.\n\n• 키 지정: 칸 클릭 → 키보드 입력\n• 마우스 지정: 칸 클릭 → 다시 좌클릭 / 우클릭\n• 해제: ✕ 버튼, Del 키, 또는 칸 우클릭\n• 디아블로 3 창이 활성화된 경우에만 동작합니다."
            : "Press the Start/Stop key (default F3) to auto-press checked skills at their intervals. Press again to stop.\n\n• Set key: click box → press a key\n• Set mouse: click box → left / right click again\n• Clear: ✕ button, Del key, or right-click the box\n• Works only while Diablo III is the active window."; } }
        public static string HelpMouseTitle { get { return IsKorean ? "마우스 연타" : "Mouse Rapid-Click"; } }
        public static string HelpMouseBody { get { return IsKorean
            ? "지정한 키를 누르고 있는 동안만 좌/우클릭을 연타하고, 떼면 바로 멈춥니다.\n\n• F3 스킬 반복과 별개로 동작합니다.\n• 좌클릭: 아이템 줍기 / 우클릭: 핏빛 파편 갬블\n• 간격을 0으로 두면 기본 50ms로 연타합니다.\n• 지정한 키 입력은 게임에도 전달되므로, 게임에서 쓰지 않는 키를 권장합니다."
            : "Rapid-clicks left / right while you hold the assigned key, and stops as soon as you release it.\n\n• Works independently of F3 skill repeat.\n• Left: loot pickup / Right: Kadala gambling\n• Interval 0 = default 50ms.\n• The key is also sent to the game, so pick an unused key."; } }
        public static string RepeatStartStopLabel { get { return IsKorean ? "시작/종료 단축키:" : "Start/Stop Key:"; } }
        public static string RepeatPressAgainHint { get { return IsKorean ? "(한 번 누르면 계속 반복 · 다시 누르면 정지)" : "(Press once to start · Press again to stop)"; } }
        public static string RepeatSkillsHint { get { return IsKorean ? "💡 반복키 칸 클릭 후 키보드 입력, 또는 다시 좌클릭(Mouse Left) / 우클릭(Mouse Right) 지정" : "💡 Click box then press key, or Left-Click (Mouse Left) / Right-Click (Mouse Right)"; } }
        public static string RepeatColEnable { get { return IsKorean ? "사용" : "Active"; } }
        public static string RepeatColSkill { get { return IsKorean ? "슬롯" : "Slot"; } }
        public static string RepeatColKey { get { return IsKorean ? "자동으로 눌러줄 키 (키보드 / 마우스)" : "Key to Auto-Press (Keyboard / Mouse)"; } }
        public static string RepeatColInterval { get { return IsKorean ? "반복 간격 (ms)" : "Interval (ms)"; } }
        public static string RepeatSkillRowLabel(int idx) { return IsKorean ? "기술 " + idx : "Skill " + idx; }

        // Section 2: Hold Mouse Rapid-Click
        public static string RepeatMouseHoldSection { get { return IsKorean ? "🖱️ 마우스 연타 (누르는 동안)" : "🖱️ Mouse Rapid-Click (Hold)"; } }
        public static string RepeatMouseHoldBadge { get { return IsKorean ? "F3 스킬 반복과 별개 동작 · 지정한 키를 꾹 누르는 동안만 연타" : "Independent of F3 · Rapid-clicks only while holding the key"; } }
        public static string RepeatLeftClickTitle { get { return IsKorean ? "좌클릭 연타 (아이템 줍기 등)" : "Left-Click Repeat (Loot Pickup)"; } }
        public static string RepeatRightClickTitle { get { return IsKorean ? "우클릭 연타 (핏빛 파편 갬블 등)" : "Right-Click Repeat (Kadala Gamble)"; } }
        public static string RepeatHoldKeyLabel { get { return IsKorean ? "누르고 있을 키" : "Hold Key"; } }
        public static string RepeatHoldIntervalLabel { get { return IsKorean ? "간격" : "Delay"; } }

        public static string StatusRepeatRunning(int presetIdx) { return IsKorean ? string.Format("반복 실행 중 (세팅 {0})", presetIdx) : string.Format("REPEAT ON (P{0})", presetIdx); }
        public static string LogRepeatStarted(int presetIdx) { return IsKorean ? string.Format("스킬 반복키 시작됨 [세팅 {0}] (디아블로 3 활성 시 자동 입력)", presetIdx) : string.Format("Skill Auto-Repeat started [Preset {0}]", presetIdx); }
        public static string LogRepeatStopped { get { return IsKorean ? "스킬 반복키가 정지되었습니다." : "Skill Auto-Repeat stopped."; } }
        public static string LogPresetReset(int presetIdx) { return IsKorean ? string.Format("[세팅 {0}] 화면 값이 초기화되었습니다. ([저장하기]를 누르면 파일에 반영됩니다)", presetIdx) : string.Format("[Preset {0}] cleared in UI. (Click [Save] to persist)", presetIdx); }
        public static string LogPresetSaved { get { return IsKorean ? "반복키 설정이 Hotkeys.ini 파일에 저장되었습니다." : "Repeat settings saved to Hotkeys.ini."; } }
        public static string LogPresetLoaded { get { return IsKorean ? "Hotkeys.ini 파일에서 마지막 저장된 반복키 설정을 불러왔습니다." : "Loaded last saved repeat settings from Hotkeys.ini."; } }

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
