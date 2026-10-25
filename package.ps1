$ErrorActionPreference = "Stop"

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "  KanaiHelper 諛고룷 ?⑦궎吏(Distribution) ?앹꽦 ?쒖옉  " -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan

$rootDir = $PSScriptRoot
$version = "v1.0.0"
$releaseDir = Join-Path $rootDir "release"
$portableDirName = "KanaiHelper_${version}_Portable"
$targetDir = Join-Path $releaseDir $portableDirName
$zipFile = Join-Path $releaseDir "KanaiHelper_${version}_Portable.zip"

# 1. ?ㅽ뻾 ?뚯씪 鍮뚮뱶
Write-Host "`n[1/4] KanaiHelper.exe 理쒖떊 ?뚯뒪 而댄뙆??.." -ForegroundColor Yellow
& "$rootDir\build.ps1"
if ($LASTEXITCODE -ne 0) {
    Write-Host "[?ㅻ쪟] 鍮뚮뱶???ㅽ뙣?섏뿬 ?⑦궎吏뺤쓣 以묐떒?⑸땲??" -ForegroundColor Red
    exit 1
}

# 2. release ?대뜑 ?뺣━ 諛??앹꽦
Write-Host "`n[2/4] 諛고룷 ?대뜑 援ъ“ 以鍮? $targetDir" -ForegroundColor Yellow
if (Test-Path $targetDir) {
    Remove-Item -Path $targetDir -Recurse -Force
}
New-Item -ItemType Directory -Path $targetDir -Force | Out-Null

# 3. ?꾩닔 諛고룷 ?뚯씪 蹂듭궗 諛??앹꽦
Write-Host "`n[3/4] 諛고룷??由ъ냼??諛?媛?대뱶 臾몄꽌 ?앹꽦..." -ForegroundColor Yellow

# ?ㅽ뻾 ?뚯씪 蹂듭궗
Copy-Item -Path (Join-Path $rootDir "KanaiHelper.exe") -Destination $targetDir -Force

# 湲곕낯 Hotkeys.ini 蹂듭궗 (諛고룷??源붾걫??湲곕낯媛?
$defaultIni = @"
[Settings]
Globalsleep=20
[Hotkeys]
1=^5
2=^6
3=^7
4=^8
"@
Set-Content -Path (Join-Path $targetDir "Hotkeys.ini") -Value $defaultIni -Encoding ASCII

# 諛뷀깢?붾㈃ 諛붾줈媛湲??앹꽦 諛곗튂 ?뚯씪
$shortcutBat = @"
@echo off
chcp 65001 >nul
echo ======================================================
echo   KanaiHelper 諛뷀깢?붾㈃ 諛붾줈媛湲곕? ?앹꽦?⑸땲??..
echo ======================================================

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "`$ws = New-Object -ComObject WScript.Shell; " ^
  "`$desktop = [Environment]::GetFolderPath('Desktop'); " ^
  "`$target = Join-Path `$PSScriptRoot 'KanaiHelper.exe'; " ^
  "`$shortcut = `$ws.CreateShortcut((Join-Path `$desktop 'KanaiHelper.lnk')); " ^
  "`$shortcut.TargetPath = `$target; " ^
  "`$shortcut.WorkingDirectory = `$PSScriptRoot; " ^
  "`$shortcut.IconLocation = `$target + ',0'; " ^
  "`$shortcut.Description = 'Diablo III Kanai & Salvage Macro Helper'; " ^
  "`$shortcut.Save();"

if %errorlevel% equ 0 (
    echo.
    echo [?깃났] 諛뷀깢?붾㈃??'KanaiHelper' 諛붾줈媛湲곌? ?앹꽦?섏뿀?듬땲??
) else (
    echo.
    echo [?ㅻ쪟] 諛붾줈媛湲??앹꽦???ㅽ뙣?덉뒿?덈떎. ?섎룞?쇰줈 諛붾줈媛湲곕? 留뚮뱶?몄슂.
)
echo.
pause
"@
[System.IO.File]::WriteAllText((Join-Path $targetDir "諛뷀깢?붾㈃_諛붾줈媛湲?留뚮뱾湲?bat"), $shortcutBat, [System.Text.Encoding]::UTF8)

# ?ъ슜?먯슜 媛꾪렪 ?ㅻ챸??(README_KO.txt, README_EN.txt)
$readmeKo = @"
======================================================================
  KanaiHelper $version - ?붿븘釉붾줈 III 移대굹?댁쓽 ??& 遺꾪빐 ?좏떥由ы떚 (?ъ슜 媛?대뱶)
======================================================================

[1] ?꾨줈洹몃옩 ?뚭컻
- ?ㅼ튂 怨쇱젙 ?놁씠 ?뺤텞???怨?'KanaiHelper.exe'瑜??ㅽ뻾?섏떆硫?利됱떆 ?ъ슜 媛?ν븳
  Windows 64-bit 怨좎꽦??珥덇꼍??留ㅽ겕濡??좏떥由ы떚?낅땲??
- CPU 怨좎젙諛 ?섎뱶?⑥뼱 ??대㉧(QPC) 湲곕컲?쇰줈 ?대뼡 PC ?섍꼍?먯꽌???낅젰 諛由??놁씠
  ?뺥솗?섍퀬 ?쇨???諛섏쓳 ?띾룄瑜?蹂댁옣?⑸땲??
- ?곷떒 ?ㅻ뜑??[?뙋 English] 踰꾪듉???꾨Ⅴ硫??몄젣?좎? ?곸뼱 紐⑤뱶濡??꾪솚?????덉뒿?덈떎.

----------------------------------------------------------------------
[2] 湲곕낯 ?⑥텞??諛?湲곕뒫 ?덈궡
----------------------------------------------------------------------
* Ctrl + 5 : 1移??꾩씠??移대굹?댁쓽 ???쇨큵 蹂??(諛섏?, 紐⑷구?? 蹂댁꽍 ??60移?
             - ?몃깽?좊━ 60移몄쓣 ?몃줈 ?곗꽑?쇰줈 ?쒗쉶?섎ŉ ?고겢由?-> 梨꾩슦湲?-> 蹂?섏쓣 ?섑뻾?⑸땲??
* Ctrl + 6 : 2移??꾩씠??移대굹?댁쓽 ???쇨큵 蹂??(臾닿린, 諛⑹뼱援???30移?
             - 臾닿린/諛⑹뼱援?洹쒓꺽??留욎떠 媛濡??곗꽑?쇰줈 2?됱뵫 嫄대꼫?곕ŉ 珥?30移몄쓣 ?섑뻾?⑸땲??
* Ctrl + 7 : ?몃깽?좊━ ?쇨큵 遺꾪빐 (??μ옣??李쎌뿉??媛濡??곗꽑 60移?
             - ??μ옣??李쎌쓽 遺꾪빐 紐⑤뱶濡?吏꾩엯?????꾨Ⅴ硫?媛濡?1移??⑥쐞濡??꾩꽕 ?뺤씤 ?뱀씤(Enter)??嫄곗튂硫?遺꾪빐?⑸땲??
* Ctrl + 8 : ?꾩꽕 ?꾩씠???щ젴 1??(?몃깽?좊━ 留??쇱そ ??1踰?移?
             - 1踰?移몄쓽 ?꾩씠?쒖쓣 1???щ젴 ??留덉슦?ㅻ? ?ㅼ떆 1踰?移몄뿉 ?щ젮 寃곌낵瑜?諛붾줈 ?뺤씤?⑸땲?? (諛섎났 ?낅젰 媛??
* ESC      : 湲닿툒 以묐떒 (留ㅽ겕濡??숈옉 以??몄젣???꾨Ⅴ硫?0.001珥??대궡 利됯컖 ?덉쟾 以묐떒!)

???⑥텞?ㅻ뒗 ?꾨줈洹몃옩 ?붾㈃?먯꽌 ?먰븯??踰꾪듉???대┃?섏뿬 ?먰븯???ㅻ줈 ?몄젣?좎? ?먯쑀濡?쾶 蹂寃쏀븷 ???덉뒿?덈떎.
???⑥텞?ㅻ? ?꾨Ⅸ ????Ctrl ???먯꽌 ?먯쓣 ?쇰뒗 ?쒓컙 留ㅽ겕濡쒓? ?덉쟾?섍쾶 ?쒖옉?⑸땲??

----------------------------------------------------------------------
[3] ?꾩닔 寃뚯엫 ???ㅼ젙 (諛섎뱶???뺤씤??二쇱꽭??)
----------------------------------------------------------------------
1. ?붿븘釉붾줈 III ?붾㈃ 紐⑤뱶:
   - 寃뚯엫 ??[?ㅼ젙] -> [?붾㈃]?먯꽌 諛섎뱶??[李?紐⑤뱶] ?먮뒗 [?꾩껜 ?붾㈃(李?]?쇰줈 ?ㅼ젙??二쇱꽭??
   - (?꾩껜 ?붾㈃ ?꾩슜 紐⑤뱶?먯꽌???덈룄??留덉슦??醫뚰몴 留ㅽ븨???닿툔?????덉뒿?덈떎.)

2. 愿由ъ옄 沅뚰븳 ?ㅽ뻾 (?⑥텞?ㅺ? 寃뚯엫 ?댁뿉??諛섏쓳?섏? ?딆쓣 ??:
   - 諛고????곗쿂???붿븘釉붾줈 III媛 '愿由ъ옄 沅뚰븳'?쇰줈 ?ㅽ뻾 以묒씤 寃쎌슦,
     Windows 蹂댁븞 ?뺤콉(UIPI)?쇰줈 ?명빐 ?쇰컲 沅뚰븳 ?꾨줈洹몃옩??媛???낅젰??李⑤떒?⑸땲??
   - 寃뚯엫 ?댁뿉???⑥텞?ㅺ? ?숈옉?섏? ?딅뒗?ㅻ㈃ 'KanaiHelper.exe'瑜?     [留덉슦???고겢由?-> 愿由ъ옄 沅뚰븳?쇰줈 ?ㅽ뻾]??二쇱꽭??

3. 移대굹?댁쓽 ??蹂???ъ슜 ??
   - 移대굹?댁쓽 ??李쎌쓣 ?닿퀬, ?몃깽?좊━??蹂?섑븷 ?꾩씠?쒓낵 ?щ즺瑜?以鍮꾪븳 ??     ?먰븯???덉떆???섏씠吏(?? 3?섏씠吏 ?ш? ?꾩씠???낃렇?덉씠??瑜??쇱퀜?볤퀬 ?⑥텞?ㅻ? ?꾨Ⅴ?몄슂.

4. ??μ옣??遺꾪빐 ?ъ슜 ??
   - ??μ옣??李쎌쓣 ?닿퀬 '遺꾪빐' ?꾩씠肄섏쓣 ?뚮윭 留앹튂 而ㅼ꽌 ?곹깭濡?留뚮뱺 ??Ctrl+7???꾨Ⅴ?몄슂.

----------------------------------------------------------------------
[4] ?좎슜????----------------------------------------------------------------------
- '諛뷀깢?붾㈃_諛붾줈媛湲?留뚮뱾湲?bat'???붾툝 ?대┃?섏떆硫?諛뷀깢?붾㈃???몃씪?쒕┝ ?먮툕 ?꾩씠肄?諛붾줈媛湲곌? ?앹꽦?⑸땲??
- 李??ㅻⅨ履??곷떒???リ린(X) 踰꾪듉???꾨Ⅴ硫??묒뾽 ?쒖떆以??쒖뒪???몃젅?대줈 理쒖냼?붾릺??諛깃렇?쇱슫?쒖뿉 ?곸＜?⑸땲??
- 留ㅽ겕濡??숈옉 ?띾룄(Interval)???섎떒 ?щ씪?대뜑濡?議곗젅?????덉뒿?덈떎 (湲곕낯 20ms 沅뚯옣, PC ?ъ뼇/?ㅽ듃?뚰겕???곕씪 議곗젅).
- ?꾨줈洹몃옩???꾩쟾??醫낅즺?섎젮硫??쒖뒪???몃젅???꾩씠肄섏쓣 ?고겢由?븯??[醫낅즺]瑜??꾨Ⅴ?쒕㈃ ?⑸땲??

======================================================================
"@
[System.IO.File]::WriteAllText((Join-Path $targetDir "README_KO.txt"), $readmeKo, [System.Text.Encoding]::UTF8)

$readmeEn = @"
======================================================================
  KanaiHelper $version - Diablo III Kanai's Cube & Salvage Helper (User Guide)
======================================================================

[1] Introduction
- Portable, high-performance Windows 64-bit native macro utility.
- Powered by CPU hardware timer (QPC) ensuring jitter-free, consistent timing across all PCs.
- No installation required: Unzip and run 'KanaiHelper.exe' immediately.
- Click the top-right [?뙋 ?쒓뎅?? button anytime to switch the language to Korean.

----------------------------------------------------------------------
[2] Default Hotkeys & Features
----------------------------------------------------------------------
* Ctrl + 5 : 1-Slot Item Cube Converter (Rings, Amulets, Gems - 60 slots)
             - Loops vertically across 60 slots: Right-click -> Fill -> Transmute.
* Ctrl + 6 : 2-Slot Item Cube Converter (Weapons, Armors - 30 slots)
             - Loops horizontally, skipping 2 rows to process 30 full 2-slot items.
* Ctrl + 7 : Inventory Batch Salvage (All 60 slots in Blacksmith window)
             - Salvages row-by-row with confirmation prompts (Enter x2).
* Ctrl + 8 : Legendary Item Reforge (Slot 1 in inventory)
             - Reforges the top-left item once and restores cursor to check stats.
* ESC      : Emergency Stop (Aborts macro instantly within 0.001s!)

??Hotkeys can be customized freely by clicking the buttons on the dashboard.
??Macros start the moment modifier keys (Ctrl) are released.

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
     (e.g., Page 3: Upgrade Rare Item) before pressing Ctrl+5 or Ctrl+6.

4. Blacksmith Salvage:
   - Open the Blacksmith window, click the salvage hammer icon, and press Ctrl+7.

----------------------------------------------------------------------
[4] Handy Tips
----------------------------------------------------------------------
- Double-click '諛뷀깢?붾㈃_諛붾줈媛湲?留뚮뱾湲?bat' to create a desktop shortcut with the Horadric Cube icon.
- Closing the window (X) minimizes the app to the system tray, remaining active in the background.
- Adjust macro action speed via the bottom slider (Default 20ms recommended).
- Right-click the system tray icon and select [Exit] to fully terminate the app.

======================================================================
"@
[System.IO.File]::WriteAllText((Join-Path $targetDir "README_EN.txt"), $readmeEn, [System.Text.Encoding]::UTF8)

# 4. 臾댁꽕移?ZIP ?뺤텞 ?⑦궎吏 ?앹꽦
Write-Host "`n[4/4] 臾댁꽕移??ы꽣釉?ZIP ?뺤텞 ?뚯씪 ?앹꽦 以?.." -ForegroundColor Yellow
if (Test-Path $zipFile) {
    Remove-Item -Path $zipFile -Force
}
Compress-Archive -Path "$targetDir\*" -DestinationPath $zipFile -CompressionLevel Optimal

Write-Host "`n==================================================" -ForegroundColor Green
Write-Host "  [?깃났] 諛고룷 ?⑦궎吏 ?앹꽦???꾨즺?섏뿀?듬땲??" -ForegroundColor Green
Write-Host "==================================================" -ForegroundColor Green
Write-Host "1. 諛고룷 ?대뜑 : $targetDir" -ForegroundColor Cyan
Write-Host "2. 諛고룷 ?뺤텞 : $zipFile" -ForegroundColor Cyan
Get-Item $zipFile | Select-Object Name, @{Name="?ш린(KB)";Expression={[math]::Round($_.Length/1KB, 1)}}, LastWriteTime | Format-Table -AutoSize

