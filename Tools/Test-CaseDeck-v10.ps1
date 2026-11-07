param()
$ErrorActionPreference='Stop'
$matrix=Import-PowerShellDataFile (Join-Path $PSScriptRoot 'CaseDeck-v10-Matrix.psd1')
foreach($case in $matrix.Cases){
    $arguments=@{Scenarios=@($case.Name);Width=$case.Width;Height=$case.Height;PreviewDirectory='Builds/Constructivist-v10'}
    if($case.Fallback){$arguments.ArtFallback=$true}
    & (Join-Path $PSScriptRoot 'Test-Demo.ps1') @arguments
}
