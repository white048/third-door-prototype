param()
$ErrorActionPreference='Stop'
$projectRoot=Split-Path -Parent $PSScriptRoot
$brief=Join-Path $projectRoot 'Documentation\ArtDirection'
$manifest=Get-Content -LiteralPath (Join-Path $brief 'AssetManifest.json') -Raw | ConvertFrom-Json
if(@($manifest.assets).Count -ne 23){throw 'Art brief entry count is incomplete.'}
if(@($manifest.nativeScreenshots).Count -ne 10){throw 'Ten verified native gameplay frames are required.'}
$expected=@{}
foreach($asset in $manifest.assets){
    if($asset.filename -match '\.png$'){$expected['images/'+$asset.filename]=$asset.sha256}
}
foreach($shot in $manifest.nativeScreenshots){$expected[$shot.file]=$shot.sha256}
foreach($entryName in $expected.Keys){
    $file=Join-Path $brief $entryName
    if(!(Test-Path -LiteralPath $file) -or (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash -ne $expected[$entryName]){throw ('Art brief source checksum mismatch: '+$entryName)}
}
foreach($svg in Get-ChildItem -LiteralPath (Join-Path $brief 'layouts') -Filter *.svg){
    $document=[xml](Get-Content -LiteralPath $svg.FullName -Raw)
    if($document.DocumentElement.LocalName -ne 'svg'){throw ('Invalid SVG: '+$svg.Name)}
}
$destination=Join-Path $projectRoot 'Builds\TheThirdDoor_ArtDirection_v3.zip'
$temporary=Join-Path $projectRoot 'Builds\TheThirdDoor_ArtDirection_v3.partial.zip'
$files=Get-ChildItem -LiteralPath $brief | ForEach-Object {$_.FullName}
Compress-Archive -LiteralPath $files -DestinationPath $temporary -Force -CompressionLevel Optimal
$zip=[IO.Compression.ZipFile]::OpenRead($temporary)
try{
    foreach($name in @('index.html','ArtPlacement.png','AssetManifest.json','FontLicense.txt')){
        if(!($zip.Entries | Where-Object {$_.FullName -eq $name})){throw ('Brief package missing: '+$name)}
    }
    foreach($name in $expected.Keys){
        $entry=$zip.Entries | Where-Object {$_.FullName.Replace('\','/') -eq $name}
        if(!$entry){throw ('Brief image missing: '+$name)}
        $stream=$entry.Open();$sha=[Security.Cryptography.SHA256]::Create()
        try{$digest=[BitConverter]::ToString($sha.ComputeHash($stream)).Replace('-','')}finally{$stream.Dispose();$sha.Dispose()}
        if($digest -ne $expected[$name]){throw ('Brief package image mismatch: '+$name)}
    }
}finally{$zip.Dispose()}
Move-Item -LiteralPath $temporary -Destination $destination -Force
Write-Output ('ART HANDOFF / '+$destination)
Write-Output '23 entries; 4 original images including the nine-object atlas; 6 SVG layouts; 10 unchanged native 1920x1080 frames.'
