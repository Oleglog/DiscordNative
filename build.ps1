<#
    Build & run DiscordNative (Windows uniquement).
    Usage :
        .\build.ps1              # build Debug + run
        .\build.ps1 -Configuration Release
        .\build.ps1 -NoRun
#>
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [switch]$NoRun
)

$ErrorActionPreference = "Stop"

# Les tâches MSBuild PRI/Appx (requises par Windows App SDK) ne sont livrées qu'avec Visual Studio.
# On cherche une installation VS qui les contient (toutes versions confondues).
$appxTools = Get-ChildItem "$env:ProgramFiles\Microsoft Visual Studio" -Recurse -Filter Microsoft.Build.Packaging.Pri.Tasks.dll -ErrorAction SilentlyContinue |
    Select-Object -First 1 | ForEach-Object { $_.DirectoryName + "\" }
$extraArgs = @()
if ($appxTools) {
    Write-Host "Outils Appx : $appxTools" -ForegroundColor DarkGray
    $env:AppxMSBuildToolsPath = $appxTools  # via env : un '\' final dans -p:... serait mangé
} else {
    Write-Warning "Microsoft.Build.Packaging.Pri.Tasks.dll introuvable : installez la charge 'Windows App SDK' de Visual Studio."
}

Write-Host "== Restauration des paquets NuGet ==" -ForegroundColor Cyan
dotnet restore .\DiscordNative.sln

Write-Host "== Build ($Configuration, x64) ==" -ForegroundColor Cyan
dotnet build .\DiscordNative.sln -c $Configuration -p:Platform=x64 @extraArgs
if ($LASTEXITCODE -ne 0) { throw "Build échoué" }

if (-not $NoRun) {
    Write-Host "== Lancement de l'application ==" -ForegroundColor Cyan
    $exe = ".\src\DiscordNative.App\bin\x64\$Configuration\net8.0-windows10.0.19041.0\DiscordNative.App.exe"
    Start-Process $exe
}
