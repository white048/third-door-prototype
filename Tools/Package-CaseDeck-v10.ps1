param()
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$preview = Join-Path $projectRoot 'Builds\Constructivist-v10'
$assembly = Get-Item -LiteralPath (Join-Path $preview 'TheThirdDoor_Data\Managed\Assembly-CSharp.dll')
$matrixPath = Join-Path $PSScriptRoot 'CaseDeck-v10-Matrix.psd1'
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
    $rules = if ($scenario.StartsWith('v10-')) { 'case-deck-v10' } elseif ($scenario.StartsWith('v9-')) { 'case-deck-v9' } elseif ($scenario.StartsWith('v8-')) { 'ordered-dispatch-v8' } else { 'legacy-save-compatibility' }
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
$studyRoot=Join-Path $projectRoot 'Documentation\BalanceStudy'
$study=Get-Content -LiteralPath (Join-Path $studyRoot 'summary.json') -Raw | ConvertFrom-Json
if($study.method.totalInputs -ne 1764 -or $study.method.beamWidth -ne 192 -or !$study.method.sharedCertificatesReplayed){throw 'Balance study missing or incomplete.'}
foreach($edition in @('V9-baseline','V10-tuned')){
    $csv=Get-Item -LiteralPath (Join-Path $studyRoot ($edition+'\hands.csv'))
    if(@(Import-Csv -LiteralPath $csv.FullName).Count -ne 882){throw ('Incomplete balance matrix: '+$edition)}
    foreach($modelName in @('RelationshipRun','CorrespondenceRun','OfficeRun','RelationshipSave','DispatchRun','DispatchSave','CaseDeckRun','CaseDeckSave')){
        if((Get-Item -LiteralPath (Join-Path $projectRoot ('Assets\ThirdDoor\Scripts\'+$modelName+'.cs'))).LastWriteTimeUtc -gt $csv.LastWriteTimeUtc){throw ('Balance study predates model: '+$modelName)}
    }
}
$summary = [ordered]@{
    version='10.0'; date=[DateTime]::UtcNow.ToString('o'); engine='2022.3.62f3'
    build='Original Windows player resources with newly compiled project assembly'
    runtimeCompilation='Passed'; editorCodeCompilation='Passed with pinned Unity references'
    freshUnityBuild='Not completed in this iteration; inherited Windows preview player used'
    productStatus='Three-scene work-case deck experiment with revised hearing-window balance, explicit bundled confirmation and a readable physical workplan. Previous saves retain their own rules. No player blind test or Steam submission.'
    assemblySHA256=$hash; nativePassed=$nativeTotal; nativeScenarioCount=$cases.Count; cases=$cases
    model=[ordered]@{passed=($legacyChecks+$dispatchChecks+$deckChecks); caseDeckV9AndV10Passed=$deckChecks; dispatchV8Passed=$dispatchChecks; legacyCompatibilityPassed=$legacyChecks; caseDeckRandomJourneys=250; dispatchV8RandomJourneys=600}
    deck=[ordered]@{candidateCards=9; equippedCards=5; scenes=3; usesPerScene=1; maximumSwapsBetweenScenes=1; randomDraw=$false; presets=3; basicActionsDoNotRequireCards=$true; allThreePresetsReachAgreement=$true; noStrategyCardRouteReachesAgreement=$true; tasksRequireHandoff=$true; actualEditionPreservedInHistory=$true; realContinueAfterPartialCombo=$true; oldSaveRulesPreserved=$true}
    generatedArt=[ordered]@{newImagesThisIteration=0; existingSourceImages=4; suppliedReferenceArtDistributed=$false; originalsEdited=$false}
    balanceStudy=[ordered]@{inputs=1764;hands=126;fixtures=7;beamWidth=192;depth=10;previousCommonBestMasks=@($study.v9.bestMasksAcrossEveryFixture).Count;currentCommonBestMasks=@($study.v10.bestMasksAcrossEveryFixture).Count;playerWinRates=$false;globalOptimalityProven=$false;replayedCertificates=$true}
    scope='Ten v10 production-rule scenarios cover three preset journeys, a custom mixed hand, one-card swap, no-card route, real title Continue after a partial combo, 4:3, 1080p, missing-art fallback, scalable card marks, read-only loadout, scene-dependent physical workplan, real interaction-ray focus and bilingual readonly reading, explicit handoff confirmation question in the proof, actual-question history and zero-breath closure guidance. Three v9, two v8 and two older-save scenarios are selected compatibility regressions, not a rerun of all historical matrices. Native tests use CharacterController movement, physics and UI raycasts. The balance study is bounded search through the actual model, including controlled stress states; it is not a player success-rate estimate. No human playtest, fresh Unity BuildPipeline build or clean-machine verification.'
}
$summary | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $projectRoot 'Documentation\QA-results.json') -Encoding UTF8
$gallery = Join-Path $projectRoot 'Documentation\Preview'
New-Item -ItemType Directory -Force -Path $gallery | Out-Null
$shots = [ordered]@{
    'DeckBuilder-v10'='v10-office-paper-art-cn/30-deck-scene-0.png'
    'DeckBuilderEnglish-v10'='v10-office-room-en/30-deck-scene-0.png'
    'DeckHandoffProof-v10'='v10-office-task-cn/33-handoff-proof.png'
    'DeckHandoff-v10'='v10-office-task-cn/33-handoff-reaction.png'
    'DeckEnding-v10'='v10-office-paper-art-cn/36-deck-ending.png'
    'Workplan-v10'='v10-office-paper-art-cn/40-workplan-scene-1.png'
    'WorkplanRead-v10'='v10-office-paper-art-cn/41-workplan-read-1.png'
}
foreach ($shot in $shots.GetEnumerator()) {
    $from = Join-Path $preview ('QA/'+$shot.Value)
    if (!(Test-Path -LiteralPath $from)) { throw ('Release screenshot missing: '+$shot.Value) }
    Copy-Item -LiteralPath $from -Destination (Join-Path $gallery ($shot.Key+'.png')) -Force
    Copy-Item -LiteralPath $from -Destination (Join-Path $gallery ($shot.Key+'-'+$hash.Substring(0,8)+'.png')) -Force
}
$documents = [ordered]@{
    'Documentation\README_DEMO-v10.txt'='README_DEMO.txt'
    'Documentation\Design-Iteration-v10.txt'='DesignNotes.txt'
    'Documentation\Balance-v10.txt'='BalanceReport.txt'
    'Documentation\Playtest-v10.txt'='PlaytestGuide.txt'
    'Documentation\NextFest-Product-Plan-v9.txt'='ProductPlan.txt'
    'Documentation\QA-results.json'='Verification.json'
    'Documentation\Model-witnesses.txt'='ModelVerification.txt'
    'Tools\CaseDeck-v10-Matrix.psd1'='VerificationMatrix.psd1'
    'Assets\Resources\Audio\README_FREESOUND_LIST.md'='AudioCredits.txt'
    'Assets\Resources\Design\Display-LICENSE.txt'='FontLicense.txt'
}
foreach ($document in $documents.GetEnumerator()) { Copy-Item -LiteralPath (Join-Path $projectRoot $document.Key) -Destination (Join-Path $preview $document.Value) -Force }
$launcher='@echo off'+"`r`n"+'start "The Third Door" "%~dp0TheThirdDoor.exe" -screen-fullscreen 0 -screen-width 1600 -screen-height 900'+"`r`n"
[IO.File]::WriteAllText((Join-Path $preview 'PlayDemo.cmd'),$launcher,[Text.Encoding]::ASCII)
foreach($file in Get-ChildItem -LiteralPath $studyRoot -Recurse -File){
    $relative=$file.FullName.Substring($studyRoot.Length).TrimStart('\','/')
    $target=Join-Path $preview ('BalanceStudy\'+$relative)
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $target) | Out-Null
    Copy-Item -LiteralPath $file.FullName -Destination $target -Force
}
$artHashes = @{}
foreach ($art in Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Assets\Resources\Design\Illustrations') -Filter *.png) {
    $artHash = (Get-FileHash -LiteralPath $art.FullName -Algorithm SHA256).Hash
    $previewArt = Join-Path $preview ('Art\'+$art.Name)
    if ((Get-FileHash -LiteralPath $previewArt -Algorithm SHA256).Hash -ne $artHash) { throw ('Preview art stale: '+$art.Name) }
    $artHashes['Art/'+$art.Name] = $artHash
}
$archivePath = Join-Path $projectRoot 'Builds\TheThirdDoor_Constructivist_v10_Preview.zip'
$temporaryArchive = Join-Path $projectRoot 'Builds\TheThirdDoor_Constructivist_v10_Preview.partial.zip'
$files = Get-ChildItem -LiteralPath $preview | Where-Object { $_.Name -ne 'QA' } | ForEach-Object { $_.FullName }
Compress-Archive -LiteralPath $files -DestinationPath $temporaryArchive -Force -CompressionLevel Optimal
$zip = [IO.Compression.ZipFile]::OpenRead($temporaryArchive)
try {
    $expected = $artHashes.Clone()
    $expected['TheThirdDoor_Data/Managed/Assembly-CSharp.dll'] = $hash
    $expected['Verification.json'] = (Get-FileHash -LiteralPath (Join-Path $preview 'Verification.json') -Algorithm SHA256).Hash
    $expected['BalanceStudy/index.html'] = (Get-FileHash -LiteralPath (Join-Path $studyRoot 'index.html') -Algorithm SHA256).Hash
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
