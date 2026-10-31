param(
    [string]$Root = $PSScriptRoot,
    [string]$UnityPath = $env:UNITY_EDITOR_PATH,
    [switch]$CheckOnly
)

$ErrorActionPreference = 'Stop'
$Root = (Resolve-Path -LiteralPath $Root).Path
$backendPath = Join-Path $Root 'backend'
$frontendPath = Join-Path $Root 'frontend'
$projectPath = Join-Path $Root 'New Unity Project'
foreach ($path in @("$backendPath\go.mod", "$frontendPath\package.json", "$projectPath\Assets", "$projectPath\Packages\manifest.json", "$projectPath\ProjectSettings\ProjectVersion.txt")) {
    if (-not (Test-Path -LiteralPath $path)) { throw "缺少项目文件或目录：$path" }
}
foreach ($command in @('go.exe', 'node.exe', 'npm.cmd')) {
    if (-not (Get-Command $command -ErrorAction SilentlyContinue)) { throw "找不到 $command，请先安装对应工具并加入 PATH。" }
}
$versionLine = Get-Content -LiteralPath "$projectPath\ProjectSettings\ProjectVersion.txt" | Select-Object -First 1
$version = ($versionLine -split ':', 2)[1].Trim()
if (-not $UnityPath) {
    # 优先使用项目要求的版本，避免用其他版本打开后升级项目。
    $candidates = @(
        "${env:ProgramFiles}\Unity\Hub\Editor\$version\Editor\Unity.exe",
        "D:\Unity\Hub\Editor\$version\Editor\Unity.exe",
        "D:\u3d_editor\$version\Editor\Unity.exe",
        (Join-Path $Root "$version\Editor\Unity.exe")
    )
    $UnityPath = $candidates | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1
}
if (-not $UnityPath -or -not (Test-Path -LiteralPath $UnityPath -PathType Leaf) -or [IO.Path]::GetFileName($UnityPath) -ne 'Unity.exe') {
    throw '未找到 Unity 编辑器。请将环境变量 UNITY_EDITOR_PATH 设置为 Unity.exe 的完整路径后重试。'
}
Write-Host "项目目录：$Root"
Write-Host "Unity 编辑器：$UnityPath"
if ($CheckOnly) { Write-Host '启动前检查通过，未启动任何服务。'; return }

function Start-ServiceWindow([string]$Title, [string]$Directory, [string]$Command) {
    # 编码命令避免中文路径、空格和单引号被多层命令行解析破坏。
    $escapedDirectory = $Directory.Replace("'", "''")
    $script = "`$Host.UI.RawUI.WindowTitle = '$Title'; Set-Location -LiteralPath '$escapedDirectory'; $Command"
    $encoded = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($script))
    Start-Process -FilePath 'powershell.exe' -ArgumentList @('-NoProfile', '-NoExit', '-EncodedCommand', $encoded)
}

# 只检测端口占用，不终止已有服务，避免重复启动时影响正在使用的程序。
foreach ($port in @(18080, 5173)) {
    if (Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue) {
        throw "端口 $port 已被占用，请确认并关闭原来的服务后重试。"
    }
}
Start-ServiceWindow '巡检后端' $backendPath '$env:INSPECTION_ADDR = "127.0.0.1:18080"; $env:INSPECTION_DB = "inspection.db"; go run ./cmd/server'
Start-ServiceWindow '巡检前端' $frontendPath 'if (-not (Test-Path -LiteralPath "node_modules")) { npm.cmd ci; if ($LASTEXITCODE -ne 0) { throw "前端依赖安装失败，请检查网络和 Node.js 版本。" } }; npm.cmd run dev -- --host 127.0.0.1 --port 5173 --strictPort --open'
Start-Process -FilePath $UnityPath -ArgumentList @('-projectPath', ('"' + $projectPath + '"'))
Write-Host '已打开前后端终端并请求启动 Unity。请查看各窗口输出；Unity 打开后需要手动点击播放。'
