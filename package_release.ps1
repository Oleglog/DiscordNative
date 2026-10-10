param(
    [string]$Version = "v0.2.4"
)

$ErrorActionPreference = 'Stop'
Write-Host "Cleaning dist/InstallerBundle..."
if (Test-Path "dist\InstallerBundle") {
    Remove-Item -Recurse -Force "dist\InstallerBundle"
}
New-Item -ItemType Directory -Force -Path "dist\InstallerBundle\Payload" | Out-Null

$appReleaseDir = "src\DiscordNative.App\bin\x64\Release\net8.0-windows10.0.19041.0"
$installerReleaseDir = "src\DiscordNative.Installer\bin\x64\Release\net8.0-windows10.0.19041.0"

Write-Host "Copying App release files..."
Copy-Item -Recurse -Force "$appReleaseDir\*" "dist\InstallerBundle\Payload\"

Write-Host "Copying Installer release files..."
Copy-Item -Force "$installerReleaseDir\DiscordNative.Installer.exe" "dist\InstallerBundle\Setup.exe"

# Also copy dependencies required by DiscordNative.Installer if any
Get-ChildItem -Path $installerReleaseDir -File | Where-Object { $_.Name -ne "DiscordNative.Installer.exe" -and $_.Name -ne "app.ico" } | ForEach-Object {
    Copy-Item -Force $_.FullName "dist\InstallerBundle\"
}

$portableZip = "dist\Diswin-$Version-win-x64-portable.zip"
$setupZip = "dist\Diswin-Setup-$Version-win-x64.zip"

Write-Host "Packaging Portable Zip ($portableZip)..."
if (Test-Path $portableZip) {
    Remove-Item -Force $portableZip
}
Compress-Archive -Path "dist\InstallerBundle\Payload\*" -DestinationPath $portableZip

Write-Host "Packaging Setup Zip ($setupZip)..."
if (Test-Path $setupZip) {
    Remove-Item -Force $setupZip
}
Compress-Archive -Path "dist\InstallerBundle\*" -DestinationPath $setupZip

Write-Host "Done! Artifacts:"
Get-ChildItem "dist\*.zip" | Select-Object Name, Length, LastWriteTime
