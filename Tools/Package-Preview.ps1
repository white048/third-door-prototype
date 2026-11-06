param()
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$preview = Join-Path $projectRoot 'Builds\Constructivist'
$assembly = Get-Item -LiteralPath (Join-Path $preview 'TheThirdDoor_Data\Managed\Assembly-CSharp.dll')
$required = @('v2-table-cn','v2-boundary-en','v2-distance-cn','v2-loop-cn','v2-alliance-en','v2-exhaustion-cn','v2-falsepeace-en','v2-cards-cn','v2-refusal-cn','v3-dispatch-consent-cn','v3-dispatch-breach-en','v3-dispatch-return-cn','v4-office-agreement-cn','v4-office-intrusion-en','v4-office-alliance-cn','v4-save-resume-en','v4-session-nav-cn','v4-office-agreement-aspect-en','v3-aspect-en','v3-aspect-consent-en','v5-art-home-cn','v5-art-office-aspect-en','v5-art-fallback-cn','v5-art-home-store-cn','v5-art-office-store-en')
$required += @('v7-ribbon-office-agreement-aspect-en','v7-ribbon-home-store-cn','v7-ribbon-office-agreement-store-en','v7-ribbon-loop-en','v7-ribbon-fallback-cn')
$required += @('v8-dispatch-home-cn','v8-dispatch-promise-cn','v8-dispatch-late-cn','v8-dispatch-resume-cn','v8-dispatch-privacy-en','v8-dispatch-falsepeace-en','v8-dispatch-office-aspect-en','v8-dispatch-home-store-cn','v8-dispatch-office-store-en','v8-dispatch-fallback-cn','v8-dispatch-public-only-en')
$cases = @(); $nativeTotal = 0
foreach ($scenario in $required) {
    $reportPath = Join-Path $preview ('QA\' + $scenario + '\checks.txt')
    if (!(Test-Path -LiteralPath $reportPath)) {
        if ($scenario -in $required) { throw "Required verification missing: $scenario" }
        continue
    }
    $report = Get-Item -LiteralPath $reportPath
    if ($report.LastWriteTimeUtc -lt $assembly.LastWriteTimeUtc) {
        if ($scenario -in $required) { throw "Verification predates the current assembly: $scenario" }
        continue
    }
    $lines = Get-Content -LiteralPath $reportPath
    $failed = @($lines | Where-Object { $_ -match '^FAIL' }).Count
    $passed = @($lines | Where-Object { $_ -match '^PASS' }).Count
    if ($failed -gt 0 -or $passed -lt 15) { throw "Incomplete or failed verification: $scenario" }
    $logPath = Join-Path $projectRoot ('Logs\Demo-' + $scenario + '.log')
    $log = Get-Content -LiteralPath $logPath -Raw
    if ($log -notmatch 'DEMO_VALIDATION / PASS') { throw "Player completion missing: $scenario" }
    $cases += [ordered]@{ scenario=$scenario; passed=$passed; failed=$failed; rules=$(if($scenario.StartsWith('v8-')){'ordered-dispatch-v8'}else{'legacy-save-compatibility'}); resolution=$(if($scenario -match 'aspect'){'1280x960'}elseif($scenario -match 'store'){'1920x1080'}else{'1600x900'}); missingArtFallback=($scenario -match 'fallback') }
    $nativeTotal += $passed
}
$model = Get-Content -LiteralPath (Join-Path $projectRoot 'Documentation\Model-witnesses.txt') -Raw
if ($model -notmatch 'RELATIONSHIP MODEL / PASS') { throw 'Model verification missing.' }
$modelChecks = [int]([regex]::Match($model, 'Checks: (\d+)').Groups[1].Value)
$dispatchChecks=[int]([regex]::Match($model,'Dispatch V8 checks: (\d+)').Groups[1].Value)
$dispatchPackets=[int]([regex]::Match($model,'Dispatch V8 resolved packets: (\d+)').Groups[1].Value)
if($dispatchChecks -lt 1 -or $dispatchPackets -lt 1){throw 'New-rule verification missing.'}
$modelReport=Get-Item -LiteralPath (Join-Path $projectRoot 'Documentation\Model-witnesses.txt')
foreach($name in @('RelationshipRun','CorrespondenceRun','OfficeRun','RelationshipSave','DispatchRun','DispatchSave')){
    if((Get-Item -LiteralPath (Join-Path $projectRoot ('Assets\ThirdDoor\Scripts\'+$name+'.cs'))).LastWriteTimeUtc -gt $modelReport.LastWriteTimeUtc){throw ('Model source changed after verification: '+$name)}
}
$newerSource=Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Assets') -Recurse -Filter *.cs | Where-Object {$_.LastWriteTimeUtc -gt $assembly.LastWriteTimeUtc}
if($newerSource){throw 'C# source changed after this assembly was compiled.'}
$hash = (Get-FileHash -LiteralPath $assembly.FullName -Algorithm SHA256).Hash
$summary = [ordered]@{
    version='8.0'; date=[DateTime]::UtcNow.ToString('o'); engine='2022.3.62f3'
    build='Original Windows player resources with newly compiled project assembly'
    runtimeCompilation='Passed'; editorCodeCompilation='Passed with pinned Unity references'
    freshUnityBuild='Not completed in this iteration; inherited Windows preview player used'
    productStatus='Two-case gameplay slice: ordered message negotiation, recipient interruptions, a shared finite deck and provisional promises; no Steam submission or release'
    model=[ordered]@{ passed=($modelChecks+$dispatchChecks); legacyCompatibilityPassed=$modelChecks; dispatchPassed=$dispatchChecks; dispatchResolvedPackets=$dispatchPackets; dispatchRandomJourneys=600; dispatchEndingRouteCombinations=[int]([regex]::Match($model,'Dispatch V8 ending route combinations: (\d+)').Groups[1].Value); endingsReachablePerCase=7; playableCases=2 }
    cases=$cases; nativePassed=$nativeTotal
    assemblySHA256=$hash
    generatedArt=[ordered]@{ method='Built-in image_gen'; sourceImages=4; inGameCaseImages=2; cutoutAtlasObjects=9; sourcePixels=@('1254x1254','1254x1254','1672x940','1254x1254 RGBA'); originalsEdited=$false; productionBriefEntries=23; editableSVGLayouts=6; suppliedReferenceArtDistributed=$false }
    dispatch=[ordered]@{ orderedPieces=$true; recipientInterruptions=$true; sharedFiniteDeck=$true; attemptsPersist=$true; provisionalCanBeCorrected=$true; lateIndividualConfirmation=$true; fabricatedGuaranteeCannotCountAsVerified=$true; allComposerGlyphsChecked=$true; v1SavesKeepLegacyRules=$true; allSevenArchiveSummariesInBothLanguages=$true }
    collage=[ordered]@{ historicalCoreCompatibilityReportOnly=$true; physicalStation=$true; perRunRecords=$true; deliveryProof=$true; cancelIsReadOnly=$true; languageKeepsPosition=$true; offscreenSheetsDisabled=$true; continuedRunRebuilt=$true; geometryFallback=$true }
    editorial=[ordered]@{ skills=@('blader/humanizer','op7418/Humanizer-zh'); previousEditionReview='EditorialReview.txt'; aiDetectorUsed=$false }
    scope='Historical v2-v7 scenarios explicitly use legacy rules for save compatibility. Eleven v8 scenarios exercise production new-game rules, ordered packets, recipient interruption, draft translation, confirmation/cancel, finite shared deck, provisional/corrected promises, unverified-promise ending, private disclosure, late consent, public-only completion, compact current notes, all seven archive summaries through an isolated UI fixture and real Continue after partial delivery. Real CharacterController movement, physics and UI raycasts, bilingual layout and leading glyphs, cards, costs and rewind, message permission and repair, two-case endings, JSON backup recovery, Continue, case navigation, physical art and paper roll, per-run collage records, scroll and chapter jumps, language position retention, offscreen sheet culling, verbatim delivery proofs, cancel/confirm input, art fallbacks, continued-run reconstruction with the undo snapshot intact, native 1920x1080 captures. Short frame samples are not hardware certification.'
}
$summary | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $projectRoot 'Documentation\QA-results.json') -Encoding UTF8

$shots = [ordered]@{
    'Title.png'='v2-table-cn/01-title.png'; 'Exhibition.png'='v2-table-cn/02-arrival.png'
    'Cards.png'='v2-cards-cn/04-choice-2.png'; 'Notebook.png'='v2-cards-cn/06-notebook.png'; 'SentenceTrace.png'='v2-cards-cn/06b-sentence-trace.png'
    'Correspondence.png'='v3-dispatch-consent-cn/13-allowed-delivery-preview.png'; 'TrustedNo.png'='v3-dispatch-consent-cn/14-a-trusted-no-receipt.png'; 'DisclosureRepair.png'='v3-dispatch-breach-en/13-acknowledge-and-ask-again-receipt.png'
    'EndingTable.png'='v2-table-cn/08-ending.png'; 'EndingRelease.png'='v2-table-cn/08-ending.png'
    'EndingBoundary.png'='v2-boundary-en/08-ending.png'; 'EndingDistance.png'='v2-distance-cn/08-ending.png'
    'EndingLoop.png'='v2-loop-cn/08-ending.png'; 'EndingAlliance.png'='v2-alliance-en/08-ending.png'
    'EndingExhaustion.png'='v2-exhaustion-cn/08-ending.png'; 'EndingFalsePeace.png'='v2-falsepeace-en/08-ending.png'
    'OfficeTitle.png'='v4-office-agreement-cn/01c-office-title.png'; 'OfficeWorkplan.png'='v4-office-agreement-cn/02b-workplan.png'
    'OfficePublicRequest.png'='v4-office-agreement-cn/13-office-public-request-preview.png'; 'OfficeEnding.png'='v4-office-agreement-cn/08-ending.png'
    'OfficeTable.png'='v4-office-agreement-cn/08b-work-agreement-table.png'; 'Continue.png'='v4-save-resume-en/15-continue-title.png'
    'IllustratedTitle.png'='v5-art-home-cn/01-title.png'; 'IllustratedIntroduction.png'='v5-art-home-cn/01d-illustrated-introduction.png'
    'GalleryCollage.png'='v5-art-home-cn/02c-gallery-collage.png'; 'VisualNotes.png'='v5-art-home-cn/02d-visual-notes.png'
    'CollageStrip.png'='v7-ribbon-home-store-cn/18-growing-strip-response.png'; 'DispatchProof.png'='v7-ribbon-home-store-cn/16-private-delivery-proof.png'
    'DispatchComposer.png'='v8-dispatch-home-store-cn/21-correct-order-composer.png'; 'DispatchReaction.png'='v8-dispatch-home-store-cn/20-interruption-reaction.png'; 'DispatchComposerEnglish.png'='v8-dispatch-office-store-en/21-correct-order-composer.png'
    'DispatchEnding.png'='v8-dispatch-home-store-cn/25-dispatch-ending.png'; 'ProvisionalEnding.png'='v8-dispatch-falsepeace-en/25-provisional-ending.png'
    'PhysicalStrip.png'='v7-ribbon-home-store-cn/00-physical-strip.png'; 'CollageEnding.png'='v7-ribbon-office-agreement-store-en/19-ending-strip-last-page.png'
}
$gallery = Join-Path $projectRoot 'Documentation\Preview'
New-Item -ItemType Directory -Force -Path $gallery | Out-Null
foreach ($shot in $shots.GetEnumerator()) {
    Copy-Item -LiteralPath (Join-Path $preview ('QA/' + $shot.Value)) -Destination (Join-Path $gallery $shot.Key) -Force
}
# A content-versioned preview also avoids reusing a viewer's cached image from an earlier build.
Copy-Item -LiteralPath (Join-Path $gallery 'Cards.png') -Destination (Join-Path $gallery ('Cards-' + $hash.Substring(0,8) + '.png')) -Force
Copy-Item -LiteralPath (Join-Path $gallery 'Correspondence.png') -Destination (Join-Path $gallery ('Correspondence-' + $hash.Substring(0,8) + '.png')) -Force
Copy-Item -LiteralPath (Join-Path $gallery 'OfficePublicRequest.png') -Destination (Join-Path $gallery ('OfficePublicRequest-' + $hash.Substring(0,8) + '.png')) -Force
Copy-Item -LiteralPath (Join-Path $gallery 'OfficeTitle.png') -Destination (Join-Path $gallery ('OfficeTitle-' + $hash.Substring(0,8) + '.png')) -Force
Copy-Item -LiteralPath (Join-Path $gallery 'IllustratedTitle.png') -Destination (Join-Path $gallery ('IllustratedTitle-' + $hash.Substring(0,8) + '.png')) -Force
Copy-Item -LiteralPath (Join-Path $gallery 'VisualNotes.png') -Destination (Join-Path $gallery ('VisualNotes-' + $hash.Substring(0,8) + '.png')) -Force
foreach($newShot in @('CollageStrip','DispatchProof','PhysicalStrip','CollageEnding','DispatchComposer','DispatchReaction','DispatchComposerEnglish','DispatchEnding','ProvisionalEnding')){Copy-Item -LiteralPath (Join-Path $gallery ($newShot+'.png')) -Destination (Join-Path $gallery ($newShot+'-'+$hash.Substring(0,8)+'.png')) -Force}
Copy-Item -LiteralPath (Join-Path $projectRoot 'Documentation\Design-Iteration-v8.txt') -Destination (Join-Path $preview 'DesignNotes.txt') -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'Documentation\Balance-v8.txt') -Destination (Join-Path $preview 'BalanceReport.txt') -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'Documentation\NextFest-Product-Plan-v8.txt') -Destination (Join-Path $preview 'ProductPlan.txt') -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'Documentation\QA-results.json') -Destination (Join-Path $preview 'Verification.json') -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'Documentation\README_DEMO-v8.txt') -Destination (Join-Path $preview 'README_DEMO.txt') -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'Documentation\Playtest-v8.txt') -Destination (Join-Path $preview 'PlaytestGuide.txt') -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'Documentation\Editorial\Review-v6.txt') -Destination (Join-Path $preview 'EditorialReview.txt') -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'Documentation\Editorial\CopyReview-v6.json') -Destination (Join-Path $preview 'CopyReview-v6.json') -Force
if(Test-Path -LiteralPath (Join-Path $preview 'CopyReview.json')){Remove-Item -LiteralPath (Join-Path $preview 'CopyReview.json')}
Copy-Item -LiteralPath (Join-Path $projectRoot 'Documentation\ReferenceStudy-v7\Study.txt') -Destination (Join-Path $preview 'ReferenceStudy.txt') -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'Documentation\ReferenceStudy-v7\GeneratedAssetPrompt.json') -Destination (Join-Path $preview 'ArtPrompts.json') -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'Documentation\ReferenceStudy-v7\CoreCompatibility.json') -Destination (Join-Path $preview 'CoreCompatibility-v7.json') -Force

if(Test-Path -LiteralPath (Join-Path $preview 'CoreCompatibility.json')){Remove-Item -LiteralPath (Join-Path $preview 'CoreCompatibility.json')}
Copy-Item -LiteralPath (Join-Path $projectRoot 'Documentation\Model-witnesses.txt') -Destination (Join-Path $preview 'ModelVerification.txt') -Force
$artHashes = @{}
foreach($art in Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Assets\Resources\Design\Illustrations') -Filter *.png) {
    $sourceHash=(Get-FileHash -LiteralPath $art.FullName -Algorithm SHA256).Hash
    $previewArt=Join-Path $preview ('Art\'+$art.Name)
    if(!(Test-Path -LiteralPath $previewArt) -or (Get-FileHash -LiteralPath $previewArt -Algorithm SHA256).Hash -ne $sourceHash){throw ('Preview art missing or stale: '+$art.Name)}
    $artHashes[$art.Name]=$sourceHash
}

$archivePath = Join-Path $projectRoot 'Builds\TheThirdDoor_Constructivist_v8_Preview.zip'
$temporaryArchive = Join-Path $projectRoot 'Builds\TheThirdDoor_Constructivist_v8_Preview.partial.zip'
$files = Get-ChildItem -LiteralPath $preview | Where-Object { $_.Name -ne 'QA' } | ForEach-Object { $_.FullName }
Compress-Archive -LiteralPath $files -DestinationPath $temporaryArchive -Force -CompressionLevel Optimal
$zip = [IO.Compression.ZipFile]::OpenRead($temporaryArchive)
try {
    $entry = $zip.Entries | Where-Object { $_.FullName.Replace('\','/') -eq 'TheThirdDoor_Data/Managed/Assembly-CSharp.dll' }
    if (!$entry) { throw 'The compiled assembly is missing from the package.' }
    $stream = $entry.Open()
    $sha = [Security.Cryptography.SHA256]::Create()
    try { $archiveHash = [BitConverter]::ToString($sha.ComputeHash($stream)).Replace('-','') }
    finally { $stream.Dispose(); $sha.Dispose() }
    if ($archiveHash -ne $hash) { throw 'Package assembly checksum mismatch.' }
    foreach($artName in $artHashes.Keys) {
        $artEntry=$zip.Entries | Where-Object { $_.FullName.Replace('\','/') -eq ('Art/'+$artName) }
        if(!$artEntry){throw ('Package art missing: '+$artName)}
        $artStream=$artEntry.Open();$artSha=[Security.Cryptography.SHA256]::Create()
        try{$artHash=[BitConverter]::ToString($artSha.ComputeHash($artStream)).Replace('-','')}finally{$artStream.Dispose();$artSha.Dispose()}
        if($artHash -ne $artHashes[$artName]){throw ('Package art checksum mismatch: '+$artName)}
    }
    if (!($zip.Entries | Where-Object { $_.FullName -eq 'PlayDemo.cmd' })) { throw 'Launcher missing from package.' }
} finally { $zip.Dispose() }
Move-Item -LiteralPath $temporaryArchive -Destination $archivePath -Force
$packageHash=(Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash
($packageHash+'  '+[IO.Path]::GetFileName($archivePath)) | Set-Content -LiteralPath ($archivePath+'.sha256') -Encoding UTF8
Write-Output ('PACKAGED / ' + $archivePath)
Write-Output ('MODEL / ' + ($modelChecks+$dispatchChecks) + ' assertions (' + $dispatchChecks + ' v8, ' + $modelChecks + ' legacy); NATIVE / ' + $summary.nativePassed + ' checks; CASES / ' + $cases.Count)
Write-Output ('ASSEMBLY SHA256 / ' + $hash)
