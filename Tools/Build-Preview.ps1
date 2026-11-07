param(
    [string]$EditorRoot = 'E:\014_Unity\Editor\2022.3.62f3\Editor',
    [switch]$CheckEditor,
    [string]$OutputDirectory = 'Builds/Constructivist'
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$sourceBuild = Join-Path $projectRoot 'Builds\Windows'
$previewBuild = [IO.Path]::GetFullPath((Join-Path $projectRoot $OutputDirectory))
$allowedBuildRoot=[IO.Path]::GetFullPath((Join-Path $projectRoot 'Builds'))+[IO.Path]::DirectorySeparatorChar
if(!$previewBuild.StartsWith($allowedBuildRoot,[StringComparison]::OrdinalIgnoreCase)){throw 'Preview output must be inside the project Builds directory.'}
$scratch = Join-Path $PSScriptRoot '.build'
$compiler = Join-Path $EditorRoot 'Data\DotNetSdkRoslyn\csc.dll'
$dotnet = Join-Path $EditorRoot 'Data\NetCoreRuntime\dotnet.exe'
if (!(Test-Path -LiteralPath $compiler)) { throw "Unity C# compiler not found: $compiler" }
if (!(Test-Path -LiteralPath (Join-Path $sourceBuild 'TheThirdDoor.exe'))) {
    throw 'The preview requires the original Builds/Windows player. Use the Unity menu for a fresh build.'
}
New-Item -ItemType Directory -Force $previewBuild, $scratch | Out-Null
& (Join-Path $PSScriptRoot 'Prepare-Glyphs.ps1')
# Compile first. A compiler error must never replace the last working preview assembly.
$managed = Join-Path $sourceBuild 'TheThirdDoor_Data\Managed'
$sources = Get-ChildItem (Join-Path $projectRoot 'Assets') -Recurse -Filter *.cs
$arguments = @('-nologo', '-target:library', '-optimize+', '-langversion:9', '-nostdlib+',
    '-define:UNITY_STANDALONE;UNITY_STANDALONE_WIN;UNITY_2022_3_OR_NEWER',
    ('-out:"' + (Join-Path $scratch 'Assembly-CSharp.dll') + '"'))
$arguments += Get-ChildItem $managed -Filter *.dll |
    Where-Object { $_.Name -ne 'Assembly-CSharp.dll' } |
    ForEach-Object { '-r:"' + $_.FullName + '"' }
$arguments += $sources | Where-Object { $_.FullName -notmatch '[\\/]Editor[\\/]' } |
    ForEach-Object { '"' + $_.FullName + '"' }
$response = Join-Path $scratch 'Preview.rsp'
[IO.File]::WriteAllLines($response, $arguments)
& $dotnet $compiler ('@' + $response)
if ($LASTEXITCODE -ne 0) { throw 'Runtime C# compilation failed.' }

if ($CheckEditor) {
    $references = @{}
    foreach ($project in @('Assembly-CSharp.csproj', 'Assembly-CSharp-Editor.csproj')) {
        $xml = [xml](Get-Content -LiteralPath (Join-Path $projectRoot $project) -Raw)
        foreach ($path in $xml.Project.ItemGroup.Reference.HintPath) {
            if ($path -and (Test-Path -LiteralPath $path)) {
                $name = [IO.Path]::GetFileName($path)
                if (!$references.ContainsKey($name)) { $references[$name] = $path }
            }
        }
    }
    $editorArguments = @('-nologo', '-target:library', '-langversion:9', '-nostdlib+',
        '-define:UNITY_EDITOR;UNITY_EDITOR_WIN;UNITY_STANDALONE_WIN;UNITY_2022_3_OR_NEWER',
        ('-out:"' + (Join-Path $scratch 'EditorCheck.dll') + '"'))
    $editorArguments += $references.Values | ForEach-Object { '-r:"' + $_ + '"' }
    $editorArguments += $sources | ForEach-Object { '"' + $_.FullName + '"' }
    $editorResponse = Join-Path $scratch 'Editor.rsp'
    [IO.File]::WriteAllLines($editorResponse, $editorArguments)
    & $dotnet $compiler ('@' + $editorResponse)
    if ($LASTEXITCODE -ne 0) { throw 'Editor C# compilation failed.' }
}
# Copy the runtime only after every requested compilation succeeds.
foreach ($file in Get-ChildItem -LiteralPath $sourceBuild -Recurse -File) {
    if ($file.Name -match '\.(zip|docx)$' -or $file.Name -eq 'Assembly-CSharp.dll') { continue }
    $relative = $file.FullName.Substring($sourceBuild.Length).TrimStart('\','/')
    $destination = Join-Path $previewBuild $relative
    New-Item -ItemType Directory -Force (Split-Path -Parent $destination) | Out-Null
    Copy-Item -LiteralPath $file.FullName -Destination $destination -Force
}
$previewManaged = Join-Path $previewBuild 'TheThirdDoor_Data\Managed'
New-Item -ItemType Directory -Force (Join-Path $previewBuild 'Art') | Out-Null
Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Assets\Resources\Design\Illustrations') -Filter *.png | ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $previewBuild ('Art\' + $_.Name)) -Force }
Copy-Item -LiteralPath (Join-Path $scratch 'Assembly-CSharp.dll') -Destination $previewManaged -Force
Write-Output "Preview ready: $previewBuild\TheThirdDoor.exe"
Write-Output 'This reuses the original player resources. Use Third Door > Build Constructivist Demo for a fresh Unity build.'
