$pluginName = "QuickMaths"
$pluginDir = "$env:LOCALAPPDATA\Microsoft\PowerToys\PowerToys Run\Plugins\$pluginName"

# Create plugin directory if it doesn't exist
if (!(Test-Path $pluginDir)) {
    New-Item -ItemType Directory -Force -Path $pluginDir | Out-Null
}

# Build the project
dotnet build "Community.PowerToys.Run.Plugin.QuickMaths\Community.PowerToys.Run.Plugin.QuickMaths.csproj" -c Release -p:Platform=x64

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed."
    exit 1
}

# Kill PowerToys so the plugin DLL is not locked during copy
$ptProc = Get-Process -Name "PowerToys" -ErrorAction SilentlyContinue
if ($ptProc) {
    Write-Host "Stopping PowerToys..."
    $ptProc | Stop-Process -Force
    Start-Sleep -Seconds 2
}

# Copy output files
$outputDir = "Community.PowerToys.Run.Plugin.QuickMaths\bin\x64\Release\net8.0-windows"
if (Test-Path $outputDir) {
    Copy-Item "$outputDir\*" "$pluginDir" -Recurse -Force
} else {
    Write-Host "Build output directory not found."
    exit 1
}

# Copy Images and plugin.json
if (Test-Path "Community.PowerToys.Run.Plugin.QuickMaths\Images") {
    Copy-Item "Community.PowerToys.Run.Plugin.QuickMaths\Images" "$pluginDir" -Recurse -Force
}
if (Test-Path "Community.PowerToys.Run.Plugin.QuickMaths\plugin.json") {
    Copy-Item "Community.PowerToys.Run.Plugin.QuickMaths\plugin.json" "$pluginDir" -Force
}

Write-Host "Deployed $pluginName to PowerToys Run plugins directory."

# Restart PowerToys
$ptExe = "$env:LOCALAPPDATA\PowerToys\PowerToys.exe"
if (Test-Path $ptExe) {
    Write-Host "Restarting PowerToys..."
    Start-Process $ptExe
} else {
    Write-Host "PowerToys executable not found at $ptExe - please restart PowerToys manually."
}
