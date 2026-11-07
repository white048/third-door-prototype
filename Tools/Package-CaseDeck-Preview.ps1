param()
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$preview = Join-Path $projectRoot 'Builds\Constructivist'
$assembly = Get-Item -LiteralPath (Join-Path $preview 'TheThirdDoor_Data\Managed\Assembly-CSharp.dll')
$matrixPath = Join-Path $PSScriptRoot 'CaseDeck-Matrix.psd1'
$matrix = Import-PowerShellDataFile $matrixPath
$cases = @(); $nativeTotal = 0
foreach ($case in $matrix.Cases) {
    $scenario = $case.Name
    $reportPath = Join-Path $preview ('QA\' + $scenario + '\checks.txt')
    $report = Get-Item -LiteralPath $reportPath
    if ($report.LastWriteTimeUtc -lt $assembly.LastWriteTimeUtc) { throw "Verification predates the current assembly: $scenario" }
    $lines = Get-Content -LiteralPath $reportPath
    $failed = @($lines | Where-Object { $_ -match '^FAIL' }).Count
    $passed = @($lines | Where-Object { $_ -match '^PASS' }).Count
    if ($failed -gt 0 -or $passed -lt 15) { throw "Incomplete or failed verification: $scenario" }
    $log = Get-Content -LiteralPath (Join-Path $projectRoot ('Logs\Demo-' + $scenario + '.log')) -Raw
    if ($log -notmatch 'DEMO_VALIDATION / PASS') { throw "Player completion missing: $scenario" }
    $rules = if ($scenario.StartsWith('v9-')) { 'case-deck-v9' } elseif ($scenario.StartsWith('v8-')) { 'ordered-dispatch-v8' } else { 'legacy-save-compatibility' }
    $cases += [ordered]@{scenario=$scenario; rules=$rules; passed=$passed; failed=$failed; resolution=($case.Width.ToString()+'x'+$case.Height); missingArtFallback=$case.Fallback}
    $nativeTotal += $passed
}
$modelPath = Join-Path $projectRoot 'Documentation\Model-witnesses.txt'
$modelReport = Get-Item -LiteralPath $modelPath
$model = Get-Content -LiteralPath $modelPath -Raw
if ($model -notmatch 'RELATIONSHIP MODEL / PASS') { throw 'Model verification missing.' }
function Read-Count([string]$pattern) {
    $match = [regex]::Match($model,$pattern)
    if (!$match.Success -or [int]$match.Groups[1].Value -lt 1) { throw ('Missing model count: '+$pattern) }
    return [int]$match.Groups[1].Value
}
$legacyChecks = Read-Count 'Legacy compatibility checks: (\d+)'
$dispatchChecks = Read-Count 'Dispatch V8 checks: (\d+)'
$deckChecks = Read-Count 'Case deck V9 checks: (\d+)'
foreach ($name in @('RelationshipRun','CorrespondenceRun','OfficeRun','RelationshipSave','DispatchRun','DispatchSave','CaseDeckRun','CaseDeckSave')) {
    if ((Get-Item -LiteralPath (Join-Path $projectRoot ('Assets\ThirdDoor\Scripts\'+$name+'.cs'))).LastWriteTimeUtc -gt $modelReport.LastWriteTimeUtc) { throw ('Model source changed after verification: '+$name) }
}
foreach ($test in Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*Tests.cs') {
    if ($test.LastWriteTimeUtc -gt $modelReport.LastWriteTimeUtc) { throw ('Model test changed after verification: '+$test.Name) }
}
$newerSource = Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Assets') -Recurse -Filter *.cs | Where-Object { $_.LastWriteTimeUtc -gt $assembly.LastWriteTimeUtc }
if ($newerSource) { throw 'C# source changed after this assembly was compiled.' }
$hash = (Get-FileHash -LiteralPath $assembly.FullName -Algorithm SHA256).Hash
$summary = [ordered]@{
    version='9.0'; date=[DateTime]::UtcNow.ToString('o'); engine='2022.3.62f3'
    build='Original Windows player resources with newly compiled project assembly'
    runtimeCompilation='Passed'; editorCodeCompilation='Passed with pinned Unity references'
    freshUnityBuild='Not completed in this iteration; inherited Windows preview player used'
    productStatus='Three-scene work-case deck experiment; home case and old saves retain previous rules. No Steam submission or player blind test.'
    assemblySHA256=$hash; nativePassed=$nativeTotal; nativeScenarioCount=$cases.Count; cases=$cases
    model=[ordered]@{passed=($legacyChecks+$dispatchChecks+$deckChecks); caseDeckPassed=$deckChecks; dispatchV8Passed=$dispatchChecks; legacyCompatibilityPassed=$legacyChecks; caseDeckRandomJourneys=250; dispatchV8RandomJourneys=600}
    deck=[ordered]@{candidateCards=9; equippedCards=5; scenes=3; usesPerScene=1; maximumSwapsBetweenScenes=1; randomDraw=$false; presets=3; basicActionsDoNotRequireCards=$true; allThreePresetsReachAgreement=$true; noStrategyCardRouteReachesAgreement=$true; tasksRequireHandoff=$true; actualEditionPreservedInHistory=$true; realContinueAfterPartialCombo=$true; oldSaveRulesPreserved=$true}
    generatedArt=[ordered]@{newImagesThisIteration=0; existingSourceImages=4; suppliedReferenceArtDistributed=$false; originalsEdited=$false}
    scope='Nine v9 real-window scenarios cover three full preset journeys, a one-card swap, an all-basic route, actual title Continue after PAUSE with saved stocks, bilingual draft preservation, proof/cancel, readonly hand, current notes, all seven archive summaries via an isolated UI fixture, original-scene collage, 4:3 and 1080p, and missing-art fallback. Four v8 and four legacy cases are selected regressions, not a rerun of the full v8 matrix. Native tests use CharacterController movement, physics sight checks and UI raycasts. Model checks include save roundtrips and preview equivalence. No human playtest, new Unity BuildPipeline build, clean-machine verification or hardware certification was completed.'
}
$summary | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $projectRoot 'Documentation\QA-results.json') -Encoding UTF8
$gallery = Join-Path $projectRoot 'Documentation\Preview'
New-Item -ItemType Directory -Force -Path $gallery | Out-Null
$shots = [ordered]@{
    DeckBuilder='v9-office-paper-cn/30-deck-scene-0.png'
    DeckBuilderEnglish='v9-office-room-en/30-deck-scene-0.png'
    DeckCombo='v9-office-room-en/33-paced-reframe-reaction.png'
    DeckHandoff='v9-office-task-cn/33-handoff-reaction.png'
    DeckEnding='v9-office-paper-cn/36-deck-ending.png'
    DeckStrip='v9-office-paper-cn/37-deck-ending-strip-last-page.png'
}
foreach ($shot in $shots.GetEnumerator()) {
    $from = Join-Path $preview ('QA/'+$shot.Value)
    if (!(Test-Path -LiteralPath $from)) { throw ('Release screenshot missing: '+$shot.Value) }
    Copy-Item -LiteralPath $from -Destination (Join-Path $gallery ($shot.Key+'.png')) -Force
    Copy-Item -LiteralPath $from -Destination (Join-Path $gallery ($shot.Key+'-'+$hash.Substring(0,8)+'.png')) -Force
}
$documents = [ordered]@{
    'Documentation\README_DEMO-v9.txt'='README_DEMO.txt'
    'Documentation\Design-Iteration-v9.txt'='DesignNotes.txt'
    'Documentation\Balance-v9.txt'='BalanceReport.txt'
    'Documentation\Playtest-v9.txt'='PlaytestGuide.txt'
    'Documentation\NextFest-Product-Plan-v9.txt'='ProductPlan.txt'
    'Documentation\QA-results.json'='Verification.json'
    'Documentation\Model-witnesses.txt'='ModelVerification.txt'
    'Tools\CaseDeck-Matrix.psd1'='VerificationMatrix.psd1'
}
foreach ($document in $documents.GetEnumerator()) { Copy-Item -LiteralPath (Join-Path $projectRoot $document.Key) -Destination (Join-Path $preview $document.Value) -Force }
$artHashes = @{}
foreach ($art in Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Assets\Resources\Design\Illustrations') -Filter *.png) {
    $artHash = (Get-FileHash -LiteralPath $art.FullName -Algorithm SHA256).Hash
    $previewArt = Join-Path $preview ('Art\'+$art.Name)
    if ((Get-FileHash -LiteralPath $previewArt -Algorithm SHA256).Hash -ne $artHash) { throw ('Preview art stale: '+$art.Name) }
    $artHashes['Art/'+$art.Name] = $artHash
}
$archivePath = Join-Path $projectRoot 'Builds\TheThirdDoor_Constructivist_v9_Preview.zip'
$temporaryArchive = Join-Path $projectRoot 'Builds\TheThirdDoor_Constructivist_v9_Preview.partial.zip'
$files = Get-ChildItem -LiteralPath $preview | Where-Object { $_.Name -ne 'QA' } | ForEach-Object { $_.FullName }
Compress-Archive -LiteralPath $files -DestinationPath $temporaryArchive -Force -CompressionLevel Optimal
$zip = [IO.Compression.ZipFile]::OpenRead($temporaryArchive)
try {
    $expected = $artHashes.Clone()
    $expected['TheThirdDoor_Data/Managed/Assembly-CSharp.dll'] = $hash
    $expected['Verification.json'] = (Get-FileHash -LiteralPath (Join-Path $preview 'Verification.json') -Algorithm SHA256).Hash
    foreach ($path in $expected.Keys) {
        $entry = $zip.Entries | Where-Object { $_.FullName.Replace('\','/') -eq $path }
        if (!$entry) { throw ('Package entry missing: '+$path) }
        $stream = $entry.Open(); $sha = [Security.Cryptography.SHA256]::Create()
        try { $entryHash = [BitConverter]::ToString($sha.ComputeHash($stream)).Replace('-','') }
        finally { $stream.Dispose(); $sha.Dispose() }
        if ($entryHash -ne $expected[$path]) { throw ('Package checksum mismatch: '+$path) }
    }
    foreach ($name in @('PlayDemo.cmd','TheThirdDoor.exe','README_DEMO.txt','DesignNotes.txt','PlaytestGuide.txt')) {
        if (!($zip.Entries | Where-Object { $_.FullName -eq $name })) { throw ('Package entry missing: '+$name) }
    }
} finally { $zip.Dispose() }
$buildRoot = [IO.Path]::GetFullPath((Join-Path $projectRoot 'Builds')) + [IO.Path]::DirectorySeparatorChar
foreach ($target in @($temporaryArchive,$archivePath)) {
    if (![IO.Path]::GetFullPath($target).StartsWith($buildRoot,[StringComparison]::OrdinalIgnoreCase)) { throw 'Archive path escaped the project Builds directory.' }
}
Move-Item -LiteralPath $temporaryArchive -Destination $archivePath -Force
$packageHash = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash
($packageHash+'  '+[IO.Path]::GetFileName($archivePath)) | Set-Content -LiteralPath ($archivePath+'.sha256') -Encoding UTF8
Write-Output ('PACKAGED / '+$archivePath)
Write-Output ('MODEL / '+$summary.model.passed+'; NATIVE / '+$nativeTotal+'; CASES / '+$cases.Count)
Write-Output ('ASSEMBLY SHA256 / '+$hash)
Write-Output ('PACKAGE SHA256 / '+$packageHash)
