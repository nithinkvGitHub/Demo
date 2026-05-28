param(
    [string]$ServiceName = "PicoCompanionService",
    [string]$InstallRoot = "$env:ProgramFiles\PicoCompanion"
)

$ErrorActionPreference = "Stop"

if (Get-Service -Name $ServiceName -ErrorAction SilentlyContinue) {
    sc.exe stop $ServiceName | Out-Null
    sc.exe delete $ServiceName | Out-Null
    Write-Host "Removed $ServiceName"
}
else {
    Write-Host "$ServiceName is not installed"
}

if (Test-Path $InstallRoot) {
    Remove-Item -Recurse -Force $InstallRoot
    Write-Host "Removed $InstallRoot"
}
