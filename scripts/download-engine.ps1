param(
    [string]$ConfigFile = "tools/engine-version.json",
    [string]$OutDir = "tools",
    [switch]$UpdateLatest
)
$ErrorActionPreference = "Stop"
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$headers = @{ "User-Agent" = "DouyiDownloadUI-build" }

if ($UpdateLatest) {
    $ytRelease = Invoke-RestMethod -Uri "https://api.github.com/repos/yt-dlp/yt-dlp/releases/latest" -Headers $headers
    $tag = $ytRelease.tag_name
    $ffRelease = Invoke-RestMethod -Uri "https://api.github.com/repos/BtbN/FFmpeg-Builds/releases/latest" -Headers $headers
    $ffAsset = $ffRelease.assets | Where-Object { $_.name -like "ffmpeg-master-latest-win64-gpl.zip" } | Select-Object -First 1
    if (-not $ffAsset) { throw "未找到 ffmpeg 下载资产" }
    $config = @{
        version      = $tag
        ytDlpUrl     = "https://github.com/yt-dlp/yt-dlp/releases/download/$tag/yt-dlp.exe"
        ffmpegZipUrl = $ffAsset.browser_download_url
    } | ConvertTo-Json
    Set-Content -Path $ConfigFile -Value $config -Encoding utf8
    Write-Host "engine-version.json 已更新到 $tag"
}

$cfg = Get-Content -Raw $ConfigFile | ConvertFrom-Json
Invoke-WebRequest -Uri $cfg.ytDlpUrl -OutFile "$OutDir/yt-dlp.exe" -Headers $headers
Invoke-WebRequest -Uri $cfg.ffmpegZipUrl -OutFile "$OutDir/ffmpeg.zip" -Headers $headers
Expand-Archive -Path "$OutDir/ffmpeg.zip" -DestinationPath "$OutDir/ffmpeg-tmp" -Force
Get-ChildItem "$OutDir/ffmpeg-tmp" -Recurse -Filter "ffmpeg.exe" | Select-Object -First 1 |
    Copy-Item -Destination "$OutDir/ffmpeg.exe" -Force
Remove-Item "$OutDir/ffmpeg.zip" -Force
Remove-Item "$OutDir/ffmpeg-tmp" -Recurse -Force
Write-Host "yt-dlp SHA256: $((Get-FileHash "$OutDir/yt-dlp.exe" -Algorithm SHA256).Hash)"
Write-Host "ffmpeg SHA256: $((Get-FileHash "$OutDir/ffmpeg.exe" -Algorithm SHA256).Hash)"
