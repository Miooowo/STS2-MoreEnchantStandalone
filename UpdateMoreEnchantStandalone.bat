@echo off
chcp 65001 >nul
setlocal EnableDelayedExpansion
goto :MAIN

:::BEGIN_PS1
$ErrorActionPreference = 'Stop'
$ModsRoot = $env:ME_MODS
if (-not $ModsRoot -or -not (Test-Path -LiteralPath $ModsRoot)) {
    Write-Host '[错误] mods 路径无效。' -ForegroundColor Red
    exit 2
}
$repo = 'Miooowo/STS2-MoreEnchantStandalone'
$api = "https://api.github.com/repos/$repo/releases"
$ua = 'MoreEnchantStandalone-Updater/1.0'
$headers = @{ 'User-Agent' = $ua; 'Accept' = 'application/vnd.github+json' }
$localJson = Join-Path $ModsRoot 'MoreEnchantStandalone\MoreEnchantStandalone.json'
$localVerStr = '0.0.0'
if (Test-Path -LiteralPath $localJson) {
    try {
        $j = Get-Content -LiteralPath $localJson -Raw -Encoding UTF8 | ConvertFrom-Json
        if ($j.version) { $localVerStr = [string]$j.version }
    } catch { $localVerStr = '0.0.0' }
}
Write-Host ("[信息] 本地版本: {0}" -f $localVerStr)
try {
    $releasesRaw = Invoke-RestMethod -Uri $api -Headers $headers -Method Get
} catch {
    Write-Host ('[错误] 无法获取发布信息: ' + $_.Exception.Message) -ForegroundColor Red
    exit 3
}
$releases = @($releasesRaw)
if ($releases.Count -lt 1) {
    Write-Host '[错误] 仓库尚无发布版本。' -ForegroundColor Red
    exit 4
}
$rel = $releases[0]
$tag = [string]$rel.tag_name
$remoteVerStr = $tag -replace '^v', ''
Write-Host ("[信息] 最新发布: {0} ({1})" -f $remoteVerStr, $tag)
try {
    $lv = [version]$localVerStr
    $rv = [version]$remoteVerStr
} catch {
    Write-Host '[错误] 版本号格式无法解析，请检查 JSON 与发布标签。' -ForegroundColor Red
    exit 5
}
if ($rv -le $lv) {
    Write-Host '[信息] 已是最新版本，无需更新。'
    exit 0
}
$asset = @($rel.assets | Where-Object { $_.name -ieq 'MoreEnchantStandalone.zip' }) | Select-Object -First 1
if (-not $asset -or -not $asset.browser_download_url) {
    Write-Host '[错误] 发布中未找到 MoreEnchantStandalone.zip。' -ForegroundColor Red
    exit 6
}
$url = [string]$asset.browser_download_url
$tmpZip = Join-Path $env:TEMP ('MoreEnchantStandalone_' + [guid]::NewGuid().ToString('N') + '.zip')
$tmpDir = Join-Path $env:TEMP ('MoreEnchantStandalone_expand_' + [guid]::NewGuid().ToString('N'))
try {
    Write-Host '[信息] 正在下载新版本...'
    Invoke-WebRequest -Uri $url -Headers $headers -OutFile $tmpZip -UseBasicParsing
    New-Item -ItemType Directory -Path $tmpDir -Force | Out-Null
    Expand-Archive -LiteralPath $tmpZip -DestinationPath $tmpDir -Force
    $jsonHits = @(Get-ChildItem -LiteralPath $tmpDir -Recurse -File -Filter 'MoreEnchantStandalone.json' -ErrorAction SilentlyContinue)
    $pick = $jsonHits | Where-Object { $_.Directory.Name -ieq 'MoreEnchantStandalone' } | Select-Object -First 1
    if (-not $pick) { $pick = $jsonHits | Select-Object -First 1 }
    if (-not $pick) { throw '解压结果中未找到 MoreEnchantStandalone.json，压缩包结构异常。' }
    $srcDir = $pick.Directory.FullName
    $dest = Join-Path $ModsRoot 'MoreEnchantStandalone'
    if (Test-Path -LiteralPath $dest) {
        $bakName = 'MoreEnchantStandalone.bak_' + (Get-Date -Format 'yyyyMMdd_HHmmss')
        Rename-Item -LiteralPath $dest -NewName $bakName
        Write-Host ("[信息] 旧版已备份为: {0}" -f $bakName)
    }
    New-Item -ItemType Directory -Path $dest -Force | Out-Null
    Copy-Item -Path (Join-Path $srcDir '*') -Destination $dest -Recurse -Force
    Write-Host ('[信息] 更新完成，已安装到: ' + $dest) -ForegroundColor Green
} finally {
    Remove-Item -LiteralPath $tmpZip -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $tmpDir -Recurse -Force -ErrorAction SilentlyContinue
}
:::END_PS1

:MAIN
set "ME_MODS="

REM 1) 常见 Steam 默认库路径
if exist "%ProgramFiles(x86)%\Steam\steamapps\common\Slay the Spire 2\mods\" (
  set "ME_MODS=%ProgramFiles(x86)%\Steam\steamapps\common\Slay the Spire 2\mods"
)
if not defined ME_MODS if exist "%ProgramFiles%\Steam\steamapps\common\Slay the Spire 2\mods\" (
  set "ME_MODS=%ProgramFiles%\Steam\steamapps\common\Slay the Spire 2\mods"
)

REM 2) 注册表中的 Steam 安装路径
if not defined ME_MODS (
  for /f "tokens=2*" %%A in ('reg query "HKLM\SOFTWARE\WOW6432Node\Valve\Steam" /v InstallPath 2^>nul ^| findstr /i InstallPath') do (
    set "STEAMROOT=%%B"
  )
  if defined STEAMROOT if exist "!STEAMROOT!\steamapps\common\Slay the Spire 2\mods\" (
    set "ME_MODS=!STEAMROOT!\steamapps\common\Slay the Spire 2\mods"
  )
)

REM 3) 脚本在游戏根目录：脚本同级的 mods
if not defined ME_MODS if exist "%~dp0mods\" (
  set "ME_MODS=%~dp0mods"
)

REM 4) 当前工作目录下的 mods
if not defined ME_MODS if exist "%CD%\mods\" (
  set "ME_MODS=%CD%\mods"
)

if not defined ME_MODS (
  echo [错误] 未找到「Slay the Spire 2\mods」文件夹。请将脚本放在游戏根目录，或确认已通过 Steam 安装游戏。
  pause
  exit /b 1
)

echo [信息] 使用 mods 目录: !ME_MODS!

set "ME_MODS=!ME_MODS!"
set "ME_BAT=%~f0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "$enc = New-Object System.Text.UTF8Encoding $false; $raw = [System.IO.File]::ReadAllText($env:ME_BAT, $enc); $s = ':::BEGIN_PS1'; $e = ':::END_PS1'; $i = $raw.IndexOf($s); $j = $raw.IndexOf($e); if ($i -lt 0 -or $j -lt 0) { Write-Host '[错误] 内嵌脚本标记缺失。' -ForegroundColor Red; exit 99 }; $code = $raw.Substring($i + $s.Length, $j - $i - $s.Length).Trim(); $fn = [System.IO.Path]::Combine($env:TEMP, ('meu_' + [guid]::NewGuid().ToString('N') + '.ps1')); [System.IO.File]::WriteAllText($fn, $code, (New-Object System.Text.UTF8Encoding $true)); & $fn; $c = $LASTEXITCODE; Remove-Item -LiteralPath $fn -Force -ErrorAction SilentlyContinue; exit $c"

set "PS_EXIT=%ERRORLEVEL%"
if not "%PS_EXIT%"=="0" (
  echo.
  echo [提示] 若持续无法连接 GitHub，请检查网络或代理设置。
  echo 发布页: https://github.com/Miooowo/STS2-MoreEnchantStandalone/releases
)
pause
exit /b %PS_EXIT%
