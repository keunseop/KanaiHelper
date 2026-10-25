$ErrorActionPreference = "Stop"
Write-Host "KanaiHelper v1.0 鍮뚮뱶 ?쒖옉..." -ForegroundColor Cyan

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
    Write-Host "[?깃났] KanaiHelper.exe 鍮뚮뱶媛 ?꾨즺?섏뿀?듬땲??" -ForegroundColor Green
    Get-Item $outExe | Select-Object Name, Length, LastWriteTime | Format-Table -AutoSize
} else {
    Write-Host "[?ㅻ쪟] 鍮뚮뱶 ?ㅽ뙣 (ExitCode: $LASTEXITCODE)" -ForegroundColor Red
}

