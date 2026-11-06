param([string]$EditorRoot = 'E:\014_Unity\Editor\2022.3.62f3\Editor')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$mono = Join-Path $EditorRoot 'Data\MonoBleedingEdge\bin\mono.exe'
$csc = Join-Path $EditorRoot 'Data\MonoBleedingEdge\lib\mono\4.5\csc.exe'
$scratch = Join-Path $PSScriptRoot '.build'
New-Item -ItemType Directory -Force $scratch | Out-Null
$program = Join-Path $scratch 'ModelTests.exe'
& $mono $csc -nologo ('-out:' + $program) (Join-Path $PSScriptRoot 'ModelTests.cs') (Join-Path $PSScriptRoot 'DispatchTests.cs') (Join-Path $PSScriptRoot 'CorrespondenceTests.cs') (Join-Path $PSScriptRoot 'OfficeTests.cs') (Join-Path $PSScriptRoot 'SaveTests.cs') (Join-Path $projectRoot 'Assets\ThirdDoor\Scripts\RelationshipRun.cs') (Join-Path $projectRoot 'Assets\ThirdDoor\Scripts\CorrespondenceRun.cs') (Join-Path $projectRoot 'Assets\ThirdDoor\Scripts\OfficeRun.cs') (Join-Path $projectRoot 'Assets\ThirdDoor\Scripts\RelationshipSave.cs') (Join-Path $projectRoot 'Assets\ThirdDoor\Scripts\DispatchRun.cs') (Join-Path $projectRoot 'Assets\ThirdDoor\Scripts\DispatchSave.cs')
if ($LASTEXITCODE -ne 0) { throw 'Model test compilation failed.' }
& $mono $program (Join-Path $projectRoot 'Documentation\Model-witnesses.txt')
if ($LASTEXITCODE -ne 0) { throw 'Relationship model tests failed.' }
