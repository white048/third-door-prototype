param(
    [string]$Output = 'Documentation/BalanceStudy/V9-baseline',
    [int]$Width = 48,
    [switch]$PreviousEdition,
    [string]$EditorRoot = 'E:\014_Unity\Editor\2022.3.62f3\Editor'
)
$ErrorActionPreference='Stop'
$projectRoot=Split-Path -Parent $PSScriptRoot
$mono=Join-Path $EditorRoot 'Data/MonoBleedingEdge/bin/mono.exe'
$csc=Join-Path $EditorRoot 'Data/MonoBleedingEdge/lib/mono/4.5/csc.exe'
$scratch=Join-Path $PSScriptRoot '.build'
New-Item -ItemType Directory -Force -Path $scratch | Out-Null
$program=Join-Path $scratch 'CaseDeckBalance.exe'
$model=@('RelationshipRun','CorrespondenceRun','OfficeRun','RelationshipSave','DispatchRun','DispatchSave','CaseDeckRun','CaseDeckSave') | ForEach-Object {Join-Path $projectRoot ('Assets/ThirdDoor/Scripts/'+$_+'.cs')}
& $mono $csc -nologo ('-out:'+$program) (Join-Path $PSScriptRoot 'CaseDeckBalance.cs') @model
if($LASTEXITCODE -ne 0){throw 'Deck planner compilation failed.'}
& $mono $program (Join-Path $projectRoot $Output) $Width $(if($PreviousEdition){'previous'}else{'tuned'})
if($LASTEXITCODE -ne 0){throw 'Deck planner failed.'}
