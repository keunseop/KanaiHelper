$ErrorActionPreference = "Stop"
Write-Host "KanaiHelper v2.0 빌드 시작..." -ForegroundColor Cyan

$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$wpfDir = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF"
$outExe = "$PSScriptRoot\KanaiHelper.exe"
$srcFiles = (Get-ChildItem "$PSScriptRoot\src\*.cs" | Select-Object -ExpandProperty FullName)

$refs = @(
    "System.dll",
    "System.Core.dll",
    "System.Drawing.dll",
    "System.Windows.Forms.dll",
    "System.Xaml.dll",
    "$wpfDir\PresentationFramework.dll",
    "$wpfDir\PresentationCore.dll",
    "$wpfDir\WindowsBase.dll"
)

$args = @(
    "/target:winexe",
    "/optimize+",
    "/platform:x64",
    "/out:$outExe"
)

$iconFile = "$PSScriptRoot\assets\app.ico"
if (Test-Path $iconFile) {
    $args += "/win32icon:$iconFile"
}

foreach ($r in $refs) {
    $args += "/r:$r"
}
foreach ($s in $srcFiles) {
    $args += $s
}

& $csc $args

if ($LASTEXITCODE -eq 0) {
    Write-Host "[성공] KanaiHelper.exe 빌드가 완료되었습니다!" -ForegroundColor Green
    Get-Item $outExe | Select-Object Name, Length, LastWriteTime | Format-Table -AutoSize
} else {
    Write-Host "[오류] 빌드 실패 (ExitCode: $LASTEXITCODE)" -ForegroundColor Red
}
