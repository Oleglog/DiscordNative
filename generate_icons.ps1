Add-Type -AssemblyName System.Drawing
$sourcePath = 'C:\Users\djell\.gemini\antigravity\brain\3cd525db-77c6-44b7-9f53-7348884a81cd\app_icon_hq_1791589589713.jpg'
$src = [System.Drawing.Image]::FromFile($sourcePath)
$bmp = New-Object System.Drawing.Bitmap $src, 256, 256
$bmp.Save('src/DiscordNative.App/Assets/AppIcon.png', [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Save('src/DiscordNative.Installer/AppIcon.png', [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Save('docs/assets/AppIcon.png', [System.Drawing.Imaging.ImageFormat]::Png)

$hIcon = $bmp.GetHicon()
$icon = [System.Drawing.Icon]::FromHandle($hIcon)
$stream = [System.IO.File]::Create('src/DiscordNative.App/app.ico')
$icon.Save($stream)
$stream.Close()
Copy-Item 'src/DiscordNative.App/app.ico' 'src/DiscordNative.Installer/app.ico' -Force
$bmp.Dispose()
$src.Dispose()
Write-Host "Icons generated successfully!"
