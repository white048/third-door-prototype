param()
$ErrorActionPreference = 'Stop'
# This matrix is shared with Package-CaseDeck-Preview.ps1. Each case runs in a real player window.
$matrix = Import-PowerShellDataFile (Join-Path $PSScriptRoot 'CaseDeck-Matrix.psd1')
foreach ($case in $matrix.Cases) {
    $arguments = @{ Scenarios = @($case.Name); Width = $case.Width; Height = $case.Height }
    if ($case.Fallback) { $arguments.ArtFallback = $true }
    & (Join-Path $PSScriptRoot 'Test-Demo.ps1') @arguments
}
