param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$ServiceName = "PicoCompanionService",
    [string]$InstallRoot = "$env:ProgramFiles\PicoCompanion"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$serviceProject = Join-Path $repoRoot "src\PicoCompanion.Service\PicoCompanion.Service.csproj"
$uiProject = Join-Path $repoRoot "src\PicoCompanion.Ui\PicoCompanion.Ui.csproj"
$publishDir = Join-Path $InstallRoot "Service"
$uiPublishDir = Join-Path $InstallRoot "Ui"

New-Item -ItemType Directory -Force -Path $publishDir | Out-Null
New-Item -ItemType Directory -Force -Path $uiPublishDir | Out-Null
dotnet publish $serviceProject -c $Configuration -r $Runtime --self-contained false -o $publishDir
dotnet publish $uiProject -c $Configuration -r $Runtime --self-contained false -o $uiPublishDir

$serviceExe = Join-Path $publishDir "PicoCompanion.Service.exe"
$uiExe = Join-Path $uiPublishDir "PicoCompanion.Ui.exe"

if (Get-Service -Name $ServiceName -ErrorAction SilentlyContinue) {
    sc.exe stop $ServiceName | Out-Null
    sc.exe delete $ServiceName | Out-Null
    Start-Sleep -Seconds 2
}

sc.exe create $ServiceName binPath= "`"$serviceExe`"" start= auto DisplayName= "Pico Companion Service" | Out-Null
sc.exe description $ServiceName "Manages Pico 2 W DS5 dongle configuration, profiles, reboot, and UF2 firmware updates." | Out-Null
sc.exe start $ServiceName | Out-Null

Write-Host "Installed and started $ServiceName from $serviceExe"
Write-Host "Installed UI at $uiExe"
