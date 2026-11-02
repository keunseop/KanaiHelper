# -*- coding: utf-8 -*-
import os
import sys
import shutil
import subprocess
import zipfile

def run():
    print("=" * 60)
    print("  KanaiHelper v1.1.0 Distribution Packaging")
    print("=" * 60)

    root_dir = os.path.dirname(os.path.abspath(__file__))
    version = "v1.1.0"
    release_dir = os.path.join(root_dir, "release")
    portable_dir_name = f"KanaiHelper_{version}_Portable"
    target_dir = os.path.join(release_dir, portable_dir_name)
    zip_path = os.path.join(release_dir, f"KanaiHelper_{version}_Portable.zip")

    # 0. Kill existing process
    subprocess.call(["taskkill", "/F", "/IM", "KanaiHelper.exe"],
                    stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)

    # 1. Build
    print("[1/4] Compiling KanaiHelper.exe...")
    build_bat = os.path.join(root_dir, "build.bat")
    ret = subprocess.call(["cmd.exe", "/c", build_bat], cwd=root_dir)
    if ret != 0:
        print("[Error] Build failed.")
        sys.exit(1)

    # 2. Prepare release dir
    print(f"[2/4] Preparing release folder: {target_dir}")
    os.makedirs(release_dir, exist_ok=True)
    if os.path.exists(target_dir):
        shutil.rmtree(target_dir)
    os.makedirs(target_dir, exist_ok=True)

    # 3. Copy files
    print("[3/4] Copying files and generating READMEs...")
    exe_src = os.path.join(root_dir, "KanaiHelper.exe")
    shutil.copy2(exe_src, os.path.join(target_dir, "KanaiHelper.exe"))

    ini_content = """[Settings]
Language=ko
Globalsleep=20
[Hotkeys]
1=^5
2=^6
3=^7
4=^8
[Repeat]
ToggleKey=F3
ActivePreset=0
[RepeatPreset1]
LeftClickKey=
LeftClickMs=0
RightClickKey=
RightClickMs=0
Skill1Enabled=1
Skill1Key=MouseRight
Skill1Ms=30
Skill2Enabled=1
Skill2Key=1
Skill2Ms=300
Skill3Enabled=1
Skill3Key=2
Skill3Ms=3000
Skill4Enabled=1
Skill4Key=3
Skill4Ms=1000
Skill5Enabled=1
Skill5Key=4
Skill5Ms=2000
Skill6Enabled=1
Skill6Key=
Skill6Ms=0
[RepeatPreset2]
LeftClickKey=
LeftClickMs=0
RightClickKey=
RightClickMs=0
Skill1Enabled=1
Skill1Key=
Skill1Ms=0
Skill2Enabled=1
Skill2Key=
Skill2Ms=0
Skill3Enabled=1
Skill3Key=
Skill3Ms=0
Skill4Enabled=1
Skill4Key=
Skill4Ms=0
Skill5Enabled=1
Skill5Key=
Skill5Ms=0
Skill6Enabled=1
Skill6Key=
Skill6Ms=0
[RepeatPreset3]
LeftClickKey=
LeftClickMs=0
RightClickKey=
RightClickMs=0
Skill1Enabled=1
Skill1Key=
Skill1Ms=0
Skill2Enabled=1
Skill2Key=
Skill2Ms=0
Skill3Enabled=1
Skill3Key=
Skill3Ms=0
Skill4Enabled=1
Skill4Key=
Skill4Ms=0
Skill5Enabled=1
Skill5Key=
Skill5Ms=0
Skill6Enabled=1
Skill6Key=
Skill6Ms=0
[RepeatPreset4]
LeftClickKey=
LeftClickMs=0
RightClickKey=
RightClickMs=0
Skill1Enabled=1
Skill1Key=
Skill1Ms=0
Skill2Enabled=1
Skill2Key=
Skill2Ms=0
Skill3Enabled=1
Skill3Key=
Skill3Ms=0
Skill4Enabled=1
Skill4Key=
Skill4Ms=0
Skill5Enabled=1
Skill5Key=
Skill5Ms=0
Skill6Enabled=1
Skill6Key=
Skill6Ms=0
"""
    with open(os.path.join(target_dir, "Hotkeys.ini"), "w", encoding="utf-8") as f:
        f.write(ini_content)

    shortcut_bat = r"""@echo off
chcp 65001 >nul
echo ======================================================
echo   KanaiHelper Desktop Shortcut Creator / 바로가기 생성
echo ======================================================

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$ws = New-Object -ComObject WScript.Shell; " ^
  "$desktop = [Environment]::GetFolderPath('Desktop'); " ^
  "$target = Join-Path $PSScriptRoot 'KanaiHelper.exe'; " ^
  "$shortcut = $ws.CreateShortcut((Join-Path $desktop 'KanaiHelper.lnk')); " ^
  "$shortcut.TargetPath = $target; " ^
  "$shortcut.WorkingDirectory = $PSScriptRoot; " ^
  "$shortcut.IconLocation = $target + ',0'; " ^
  "$shortcut.Description = 'Diablo III Kanai, Salvage & Skill Helper'; " ^
  "$shortcut.Save();"

if %errorlevel% equ 0 (
    echo.
    echo [SUCCESS / 성공] Desktop shortcut 'KanaiHelper' created!
    echo 바탕화면에 'KanaiHelper' 바로가기가 생성되었습니다!
) else (
    echo.
    echo [ERROR / 오류] Failed to create shortcut.
)
echo.
pause
"""
    with open(os.path.join(target_dir, "바탕화면_바로가기_만들기.bat"), "w", encoding="utf-8") as f:
        f.write(shortcut_bat)

    readme_ko = f"""======================================================================
  KanaiHelper {version} - 디아블로 III 종합 편의 유틸리티 (사용 가이드)
  [ 카나이의 함 변환 · 인벤토리 일괄 분해 · 스킬 반복키 · 마우스 연타 ]
======================================================================

[1] 프로그램 소개
- 설치 과정 없이 압축을 풀고 'KanaiHelper.exe'를 실행하시면 즉시 사용 가능한
  Windows 64-bit 고성능 초경량 매크로 유틸리티입니다.
- CPU 고정밀 하드웨어 타이머(QPC) 기반으로 어떤 PC 환경에서도 입력 밀림 없이
  정확하고 일관된 반응 속도를 보장합니다.
- 상단 탭을 통해 [카나이/분해 매크로]와 [스킬 반복키]를 언제든 전환할 수 있습니다.
- 상단 헤더의 [🌐 English] 버튼을 누르면 언제든지 영어 모드로 전환할 수 있습니다.

----------------------------------------------------------------------
[2] 주요 기능 및 기본 단축키
----------------------------------------------------------------------

1. 🔨 카나이 / 분해 매크로
* Ctrl + 5 : 인벤토리 일괄 분해 (대장장이 창에서 가로 우선 60칸)
             - 대장장이 창의 분해 모드로 진입한 후 누르면 가로 1칸 단위로
               전설 확인 승인(Enter)을 거치며 일괄 분해합니다.
* Ctrl + 6 : 1칸 아이템 카나이의 함 일괄 변환 (반지, 목걸이, 보석 등 60칸)
             - 인벤토리 60칸을 세로 우선으로 순회하며 우클릭 -> 채우기 -> 변환을 수행합니다.
* Ctrl + 7 : 2칸 아이템 카나이의 함 일괄 변환 (무기, 방어구 등 30칸)
             - 무기/방어구 규격에 맞춰 가로 우선으로 2행씩 건너뛰며 총 30칸을 수행합니다.
* Ctrl + 8 : 전설 아이템 재련 1회 (인벤토리 맨 첫 번째 칸)
             - 1번 칸의 아이템을 1회 재련 후 마우스를 다시 1번 칸에 올려 결과를 확인합니다.
* ESC      : 긴급 즉시 중단 (매크로 동작 중 언제든 누르면 0.001초 이내 안전 중단!)

2. 🔄 스킬 반복키 (D3 Helper 기능)
* F3       : 스킬 자동 반복 시작 / 정지 토글 (다시 누르면 정지, 변경 가능)
             - 기술 1~6 슬롯에 설정된 키(키보드 또는 마우스)를 설정한 주기(ms)마다 자동 반복.
             - 세팅 1 ~ 세팅 4 프리셋을 지원하여 캐릭터/빌드별 설정을 저장하고 전환 가능.
             - 디아블로 III 게임 창이 활성화되어 있을 때만 안전하게 입력됩니다.
* 마우스 연타 (Hold-to-Repeat):
             - 누르고 있는 동안만 좌클릭/우클릭을 고속 연타하며, 손을 떼면 즉시 멈춥니다.
             - F3 스킬 반복과 독립적으로 동작합니다.
             - 좌클릭 연타: 바닥 아이템 줍기 등에 유용
             - 우클릭 연타: 핏빛 파편 갬블(카달라) 등에 유용
             - 반복 간격을 0으로 설정하면 기본 50ms로 동작합니다.
* 설정 저장/불러오기:
             - 반복키 설정 변경 후 [저장하기] 버튼을 눌러야 Hotkeys.ini에 저장됩니다.
             - [불러오기]를 누르면 마지막으로 저장된 설정값으로 복구됩니다.

※ 카나이 단축키 및 스킬 반복키는 대시보드 화면의 버튼을 클릭하여 원하는 키로 자유롭게 변경 가능합니다.

----------------------------------------------------------------------
[3] 필수 게임 내 설정 (반드시 확인해 주세요!)
----------------------------------------------------------------------
1. 디아블로 III 화면 모드:
   - 게임 내 [설정] -> [화면]에서 반드시 [창 모드] 또는 [전체 화면(창)]으로 설정해 주세요.
   - (전체 화면 전용 모드에서는 윈도우 마우스 좌표 매핑이 어긋날 수 있습니다.)

2. 관리자 권한 실행 (단축키가 게임 내에서 반응하지 않을 때):
   - 배틀넷 런처나 디아블로 III가 '관리자 권한'으로 실행 중인 경우,
     Windows 보안 정책(UIPI)으로 인해 일반 권한 프로그램의 가상 입력이 차단됩니다.
   - 게임 내에서 단축키가 동작하지 않는다면 'KanaiHelper.exe'를
     [마우스 우클릭 -> 관리자 권한으로 실행]해 주세요.

3. 카나이의 함 변환 사용 시:
   - 카나이의 함 창을 열고, 인벤토리에 변환할 아이템과 재료를 준비한 뒤
     원하는 레시피 페이지(예: 3페이지 희귀 아이템 업그레이드)를 펼쳐놓고 단축키를 누르세요.

4. 대장장이 분해 사용 시:
   - 대장장이 창을 열고 '분해' 아이콘을 눌러 망치 커서 상태로 만든 뒤 Ctrl+5를 누르세요.

----------------------------------------------------------------------
[4] 유용한 팁
----------------------------------------------------------------------
- '바탕화면_바로가기_만들기.bat'을 더블 클릭하시면 바탕화면에 호라드림 큐브 아이콘 바로가기가 생성됩니다.
- 창 오른쪽 상단의 닫기(X) 버튼을 누르면 작업 표시줄 시스템 트레이로 최소화되어 백그라운드에 상주합니다.
- 각 기능 섹션 제목 옆의 '?' 버튼에 마우스를 올리거나 클릭하시면 간편 도움말 레이어가 표시됩니다.
- 카나이 매크로 동작 속도(Interval)는 하단 슬라이더로 조절할 수 있습니다 (기본 20ms 권장).
- 프로그램을 완전히 종료하려면 시스템 트레이 아이콘을 우클릭하여 [종료]를 누르시면 됩니다.

======================================================================
"""
    with open(os.path.join(target_dir, "README_KO.txt"), "w", encoding="utf-8") as f:
        f.write(readme_ko)

    readme_en = f"""======================================================================
  KanaiHelper {version} - Diablo III Utility Suite (User Guide)
  [ Kanai's Cube · Batch Salvage · Skill Auto-Repeat · Mouse Rapid-Click ]
======================================================================

[1] Introduction
- Portable, high-performance Windows 64-bit native macro utility.
- Powered by CPU hardware timer (QPC) ensuring jitter-free, consistent timing across all PCs.
- No installation required: Unzip and run 'KanaiHelper.exe' immediately.
- Use top tabs to switch between [Kanai / Salvage] and [Skill Auto-Repeat] modes.
- Click the top-right [🌐 한국어] button anytime to switch the language to Korean.

----------------------------------------------------------------------
[2] Key Features & Default Hotkeys
----------------------------------------------------------------------

1. 🔨 Kanai / Salvage Macros
* Ctrl + 5 : Inventory Batch Salvage (All 60 slots in Blacksmith window)
             - Salvages row-by-row with confirmation prompts (Enter x2).
* Ctrl + 6 : 1-Slot Item Cube Converter (Rings, Amulets, Gems - 60 slots)
             - Loops vertically across 60 slots: Right-click -> Fill -> Transmute.
* Ctrl + 7 : 2-Slot Item Cube Converter (Weapons, Armors - 30 slots)
             - Loops horizontally, skipping 2 rows to process 30 full 2-slot items.
* Ctrl + 8 : Legendary Item Reforge (Slot 1 in inventory)
             - Reforges the top-left item once and restores cursor to inspect stats.
* ESC      : Emergency Stop (Aborts macro instantly within 0.001s!)

2. 🔄 Skill Auto-Repeat (D3 Helper Functionality)
* F3       : Toggle Skill Auto-Repeat Start / Stop (Press again to stop, customizable)
             - Automatically triggers keys (keyboard or mouse) for Skills 1-6 at set intervals (ms).
             - Supports Presets 1 to 4 for managing multiple character classes and builds.
             - Only inputs when Diablo III is the active foreground window.
* Mouse Rapid-Click (Hold-to-Repeat):
             - Rapid-clicks Left or Right mouse button ONLY while you hold the assigned key.
             - Works independently of the F3 skill auto-repeat toggle.
             - Left-Click: Excellent for rapid loot/item pickup.
             - Right-Click: Excellent for blood shard Kadala gambling.
             - Setting interval to 0 uses the default 50ms rapid pace.
* Save / Load Configuration:
             - Click [Save] after adjusting repeat settings to write changes to Hotkeys.ini.
             - Click [Load] to revert back to your last saved configuration.

※ All hotkeys can be customized directly by clicking their buttons on the dashboard.

----------------------------------------------------------------------
[3] Mandatory Prerequisites (Please Check!)
----------------------------------------------------------------------
1. Diablo III Display Mode:
   - In Diablo III Video settings, set Display to [Windowed] or [Windowed (Fullscreen)].
   - (Exclusive Fullscreen mode can offset mouse coordinate calculations).

2. Run as Administrator (If inputs do not respond in-game):
   - If Battle.net or Diablo III is running with administrator privileges,
     Windows security policy (UIPI) blocks simulated inputs from standard apps.
   - If hotkeys do not respond in-game, right-click 'KanaiHelper.exe' and choose
     [Run as administrator].

3. Kanai's Cube Recipes:
   - Open Kanai's Cube, prepare items/materials, and navigate to the desired recipe page
     (e.g., Page 3: Upgrade Rare Item) before pressing Ctrl+6 or Ctrl+7.

4. Blacksmith Salvage:
   - Open the Blacksmith window, click the salvage hammer icon, and press Ctrl+5.

----------------------------------------------------------------------
[4] Handy Tips
----------------------------------------------------------------------
- Double-click '바탕화면_바로가기_만들기.bat' to create a desktop shortcut with the Horadric Cube icon.
- Closing the window (X) minimizes the app to the system tray, remaining active in the background.
- Hover or click the '?' button next to section headers to open an instant help layer.
- Adjust Kanai macro action speed via the bottom slider (Default 20ms recommended).
- Right-click the system tray icon and select [Exit] to fully terminate the app.

======================================================================
"""
    with open(os.path.join(target_dir, "README_EN.txt"), "w", encoding="utf-8") as f:
        f.write(readme_en)

    # 4. ZIP
    print("[4/4] Creating ZIP package...")
    if os.path.exists(zip_path):
        os.remove(zip_path)

    with zipfile.ZipFile(zip_path, 'w', zipfile.ZIP_DEFLATED) as zipf:
        for root, dirs, files in os.walk(target_dir):
            for file in files:
                full_path = os.path.join(root, file)
                rel_path = os.path.join(portable_dir_name, os.path.relpath(full_path, target_dir))
                zipf.write(full_path, rel_path)

    zip_size_kb = os.path.getsize(zip_path) / 1024.0
    print("=" * 60)
    print(f"  [SUCCESS] Package {version} created successfully!")
    print("=" * 60)
    print(f"1. Folder: {target_dir}")
    print(f"2. ZIP:    {zip_path} ({zip_size_kb:.1f} KB)")

if __name__ == "__main__":
    run()
