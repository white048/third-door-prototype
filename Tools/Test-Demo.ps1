param(
    [string[]]$Scenarios = @('v2-table-cn', 'v2-boundary-en', 'v2-distance-cn', 'v2-loop-cn', 'v2-alliance-en', 'v2-exhaustion-cn', 'v2-falsepeace-en', 'v2-cards-cn', 'v2-refusal-cn', 'v3-dispatch-consent-cn', 'v3-dispatch-breach-en', 'v3-dispatch-return-cn', 'v4-office-agreement-cn', 'v4-office-intrusion-en', 'v4-office-alliance-cn', 'v4-save-resume-en', 'v4-session-nav-cn'),
    [int]$Width = 1600,
    [int]$Height = 900,
    [switch]$ArtFallback
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$executable = Join-Path $projectRoot 'Builds\Constructivist\TheThirdDoor.exe'
foreach ($scenario in $Scenarios) {
    if ($scenario -notmatch '^[a-z0-9-]+$') { throw "Invalid scenario: $scenario" }
    $log = Join-Path $projectRoot ('Logs\Demo-' + $scenario + '.log')
    # Rendering requires a visible player window; hidden Windows players return black captures.
    $playerArguments = @(
        '-screen-fullscreen', '0', '-screen-width', $Width, '-screen-height', $Height,
        '-logFile', $log, ('--validate-demo=' + $scenario)
    )
    if($ArtFallback){$playerArguments+='--art-fallback'}
    $process = Start-Process -FilePath $executable -ArgumentList $playerArguments -WindowStyle Normal -PassThru -Wait
    if ($process.ExitCode -ne 0) { throw "Scenario failed: $scenario. See $log" }
    $report = Join-Path $projectRoot ('Builds\Constructivist\QA\' + $scenario + '\checks.txt')
    if (!(Test-Path -LiteralPath $report) -or (Select-String -LiteralPath $report -Pattern '^FAIL')) {
        throw "Missing or failed checks: $scenario"
    }
    Write-Output "PASS / $scenario"
}
