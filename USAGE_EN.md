# KanaiHelper User Guide (English)

> Reference Version: **v1.0 Native** (`KanaiHelper.exe`)  
> Diablo III must be running in **Windowed** or **Windowed (Fullscreen)** mode.

---

## 🚀 Native Windows App (`KanaiHelper.exe`)

A standalone lightweight desktop application built natively with C# and WPF, without requiring AutoHotkey.

- **Dark-Themed Dashboard**: Modern gaming aesthetic UI with interactive hotkey configuration buttons, live progress bar, and activity log console.
- **Single Instance Enforcement (Mutex)**: Prevents accidental multiple instances while gaming; brings existing window to focus.
- **Instant ESC Cancellation (0.001s)**: Low-level keyboard hook instantly aborts any running macro upon pressing `ESC`.
- **System Tray Support**: Minimizes to the taskbar system tray on close (`X`), staying active in the background.
- **Bilingual Interface**: Seamlessly switch between **English** and **Korean** at any time with a single click.
- **High-Precision QPC Hardware Timer**: Powered by `QueryPerformanceCounter` for consistent, jitter-free execution regardless of Windows power throttling or background processes.

---

## Default Hotkeys Summary

| Hotkey | Function | Prerequisite In-Game Screen |
| :---: | :--- | :--- |
| **Ctrl + 5** | **🔨 Inventory Batch Salvage** (Most Popular) | Blacksmith window open with 'Salvage' clicked |
| **Ctrl + 6** | **💎 1-Slot Item Cube Batch Convert** | Kanai's Cube open with target recipe page |
| **Ctrl + 7** | **⚔️ 2-Slot Item Cube Batch Convert** | Kanai's Cube open with target recipe page |
| **Ctrl + 8** | **✨ Legendary Item Reforge (1x)** | Kanai's Cube open with Reforge recipe page |
| **ESC** | **🛑 Emergency Stop** | Press anytime during execution to abort instantly |

> Hotkeys can be customized directly by clicking the hotkey buttons on the dashboard or by editing `Hotkeys.ini`.

---

## Detailed Macro Operations

### 1. Ctrl + 5 — Inventory Batch Salvage (Most Popular)
- Open the Blacksmith window and click the **Salvage** hammer button.
- Iterates horizontally across all 60 inventory slots (Row 1 left-to-right -> Row 2 ...).
- For each slot:
  1. **Left-Click** item
  2. Send **Enter twice** to confirm legendary item prompts
  3. Send **ESC** to dismiss remaining dialogues
- Safely handles normal, magic, rare, and legendary items while ignoring empty slots.

### 2. Ctrl + 6 — 1-Slot Item Cube Converter
- Iterates vertically across all 60 inventory slots (Column-by-column: Col 1 Top-to-Bottom -> Col 2 ...).
- For each slot:
  1. **Right-Click** item -> places into Kanai's Cube
  2. Click **Fill** (Recipe components)
  3. Click **Transmute**
  4. Flips recipe page **Right -> Left** to reset the recipe
- Recommended for rings, amulets, gems, and 1-slot crafting recipes (e.g., Upgrade Rare Item).

### 3. Ctrl + 7 — 2-Slot Item Cube Converter
- Operates similarly to 1-Slot mode, but tailored for 2-slot items (weapons, armor, helmets, pants).
- Iterates **horizontally skipping 2 rows** to process 30 item slots without misclicking.

### 4. Ctrl + 8 — Legendary Reforge (1x)
- Targets the item placed in the **very first inventory slot (top-left)**.
- Executes: Right-click -> Fill -> Transmute -> Reset Page -> Returns cursor to slot 1.
- Press repeatedly until you obtain your desired rolls.

---

## ⚙️ Configuration (`Hotkeys.ini`)

Settings are stored in `Hotkeys.ini` in the application directory:

```ini
[Settings]
Language=en      ; en for English, ko for Korean
Globalsleep=20   ; Action delay in milliseconds (adjust via slider in dashboard)
[Hotkeys]
1=^5             ; Inventory salvage
2=^6             ; 1-slot item cube conversion
3=^7             ; 2-slot item cube conversion
4=^8             ; Legendary reforge
```

*Modifiers: `^` = Ctrl, `+` = Shift, `!` = Alt, `#` = Windows key.*

---

## ⚠️ Important Prerequisites & Troubleshooting

1. **Window Mode (Mandatory)**:
   - Diablo III must be set to **Windowed** or **Windowed (Fullscreen)**. Exclusive fullscreen mode can offset mouse coordinate calculations.
2. **Run as Administrator**:
   - If Battle.net or Diablo III is running with administrator privileges, Windows security policy (UIPI) blocks simulated input from standard-privilege programs.
   - If hotkeys do not respond in-game, right-click `KanaiHelper.exe` and select **Run as Administrator**.
3. **Emergency Stop**:
   - Press **ESC** at any time to immediately interrupt and terminate macro execution.
