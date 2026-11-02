# KanaiHelper User Guide (English)

> Reference Version: **v1.1 Native** (`KanaiHelper.exe`)  
> Diablo III must be running in **Windowed** or **Windowed (Fullscreen)** mode.

---

## 🚀 Native Windows App (`KanaiHelper.exe`)

A standalone lightweight desktop application built natively with C# and WPF, without requiring AutoHotkey.

- **Dark-Themed Dashboard**: Modern gaming aesthetic UI with top tabs separating Kanai macros and Skill auto-repeat.
- **Skill Auto-Repeat (D3 Helper)**: Toggle start/stop via F3, auto-trigger Skills 1-6 at set intervals, with Presets 1-4 for different builds.
- **Mouse Rapid-Click (Hold-to-Repeat)**: Hold down the assigned key to rapid-click Left (loot pickup) or Right (Kadala gambling) mouse button.
- **Single Instance Enforcement (Mutex)**: Prevents accidental multiple instances while gaming; brings existing window to focus.
- **Instant ESC Cancellation (0.001s)**: Low-level keyboard hook instantly aborts running macros upon pressing `ESC`.
- **System Tray Support**: Minimizes to the taskbar system tray on close (`X`), staying active in the background.
- **Instant Help Layers**: Click or hover over the `?` button next to section headers for pop-up instructions.

---

## Default Hotkeys Summary

### 1. 🔨 Kanai / Salvage Macros

| Hotkey | Function | Prerequisite In-Game Screen |
| :---: | :--- | :--- |
| **Ctrl + 5** | **🔨 Inventory Batch Salvage** (Most Popular) | Blacksmith window open with 'Salvage' clicked |
| **Ctrl + 6** | **💎 1-Slot Item Cube Batch Convert** | Kanai's Cube open with target recipe page |
| **Ctrl + 7** | **⚔️ 2-Slot Item Cube Batch Convert** | Kanai's Cube open with target recipe page |
| **Ctrl + 8** | **✨ Legendary Item Reforge (1x)** | Kanai's Cube open with Reforge recipe page |
| **ESC** | **🛑 Emergency Stop** | Press anytime during execution to abort instantly |

### 2. 🔄 Skill Auto-Repeat & Mouse Rapid-Click

| Hotkey | Function | Trigger Mode |
| :---: | :--- | :--- |
| **F3** | **⚡ Toggle Skill Auto-Repeat** | Press once to start, press again to stop (loops Skills 1-6) |
| **Custom Key** | **🖱️ Left-Click Rapid Repeat** | Rapid-clicks only while holding the key (Loot pickup) |
| **Custom Key** | **🖱️ Right-Click Rapid Repeat** | Rapid-clicks only while holding the key (Kadala gambling) |

---

## Detailed Operations

### 1. Kanai / Salvage Macros
- **Ctrl + 5 (Batch Salvage)**: Open Blacksmith salvage mode; safely salvages all 60 slots row-by-row with confirmation prompts.
- **Ctrl + 6 (1-Slot Convert)**: Loops vertically through 60 slots: Right-click -> Fill -> Transmute -> reset recipe page.
- **Ctrl + 7 (2-Slot Convert)**: Loops horizontally, skipping 2 rows to convert 30 full 2-slot weapons and armors.
- **Ctrl + 8 (Reforge 1x)**: Reforges item in slot 1 and restores cursor to let you quickly check stats.

### 2. Skill Auto-Repeat & Mouse Rapid-Click
- **Skill Auto-Repeat**:
  - Press `F3` to auto-trigger assigned keys for Skills 1-6 at their individual intervals (ms).
  - Only sends inputs while Diablo III is the active foreground window.
  - Switch between **Presets 1 to 4** to manage multiple classes and builds.
  - Click **[Save]** to persist changes to `Hotkeys.ini`, or **[Load]** to restore the last saved settings.
- **Mouse Rapid-Click**:
  - Rapid-clicks Left or Right mouse button only while you physically hold down the assigned key.
  - Works independently from the F3 toggle for safe, on-demand looting or gambling.
