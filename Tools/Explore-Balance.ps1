param([string]$EditorRoot = 'E:\014_Unity\Editor\2022.3.62f3\Editor')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$monoRoot = Join-Path $EditorRoot 'Data\MonoBleedingEdge'
$mono = Join-Path $monoRoot 'bin\mono.exe'
$csc = Join-Path $monoRoot 'lib\mono\4.5\csc.exe'
$scratch = Join-Path $PSScriptRoot '.build'
New-Item -ItemType Directory -Force $scratch | Out-Null
$program = Join-Path $scratch 'BalanceReport.exe'
& $mono $csc -nologo ('-out:' + $program) (Join-Path $PSScriptRoot 'BalanceReport.cs') (Join-Path $projectRoot 'Assets\ThirdDoor\Scripts\RelationshipRun.cs') (Join-Path $projectRoot 'Assets\ThirdDoor\Scripts\CorrespondenceRun.cs') (Join-Path $projectRoot 'Assets\ThirdDoor\Scripts\OfficeRun.cs')
if ($LASTEXITCODE -ne 0) { throw 'Balance report compilation failed.' }
& $mono $program (Join-Path $projectRoot 'Documentation\Balance-v4.txt')
if ($LASTEXITCODE -ne 0) { throw 'Balance exploration failed.' }
