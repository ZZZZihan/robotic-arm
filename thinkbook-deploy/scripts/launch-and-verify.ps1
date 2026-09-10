$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
try {
    $p = Get-Process CSharpDemo -ErrorAction SilentlyContinue | Where-Object {$_.Path -eq "$root\app\CSharpDemo.exe" -and $_.SessionId -eq (Get-Process -Id $PID).SessionId} | Select-Object -First 1
    if (!$p) { $p = Start-Process "$root\app\CSharpDemo.exe" -WorkingDirectory "$root\app" -PassThru }
    if (!$p.WaitForInputIdle(15000)) { throw 'Demo did not reach input idle.' }
    $shell = New-Object -ComObject WScript.Shell
    $null = $shell.AppActivate($p.Id)
    Start-Sleep -Seconds 2
    $p.Refresh()
    if ($p.HasExited) { throw "Demo exited: $($p.ExitCode)" }
    if ($p.MainWindowHandle -eq 0) { throw 'Demo has no main window.' }
    Add-Type -AssemblyName UIAutomationClient,UIAutomationTypes,System.Drawing
    $window = [System.Windows.Automation.AutomationElement]::FromHandle($p.MainWindowHandle)
    $all = $window.FindAll([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.Condition]::TrueCondition)
    $controls = @($all | ForEach-Object { [pscustomobject]@{Name=$_.Current.Name;Id=$_.Current.AutomationId;Enabled=$_.Current.IsEnabled} })
    $result = [pscustomobject]@{Status='window_ready';PID=$p.Id;SessionId=$p.SessionId;Title=$p.MainWindowTitle;Responding=$p.Responding;Controls=$controls;HardwareConnected=$false;MotionTested=$false;CapturedAt=(Get-Date -Format o)}
    if (!($controls | Where-Object Name -eq '未连接控制卡')) { throw 'Expected disconnected status was not found.' }
    $bounds = $window.Current.BoundingRectangle
    $bitmap = New-Object System.Drawing.Bitmap ([int]$bounds.Width),([int]$bounds.Height)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.CopyFromScreen([int]$bounds.X,[int]$bounds.Y,0,0,$bitmap.Size)
    $bitmap.Save("$root\evidence\demo-window.png",[System.Drawing.Imaging.ImageFormat]::Png)
    $graphics.Dispose(); $bitmap.Dispose()
    $result | ConvertTo-Json -Depth 5 | Set-Content "$root\evidence\desktop-verification.json" -Encoding UTF8
} catch {
    [pscustomobject]@{Status='failed';Error=$_.Exception.ToString();CapturedAt=(Get-Date -Format o)} | ConvertTo-Json | Set-Content "$root\evidence\desktop-error.json" -Encoding UTF8
    throw
}
