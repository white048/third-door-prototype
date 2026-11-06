param()
$ErrorActionPreference='Stop'
# V8 production rules first; v2-v7 are explicitly legacy-save compatibility fixtures.
& (Join-Path $PSScriptRoot 'Test-Demo.ps1') -Scenarios v8-dispatch-office-aspect-en -Width 1280 -Height 960
& (Join-Path $PSScriptRoot 'Test-Demo.ps1') -Scenarios v8-dispatch-home-cn,v8-dispatch-promise-cn,v8-dispatch-late-cn,v8-dispatch-resume-cn,v8-dispatch-privacy-en,v8-dispatch-falsepeace-en,v8-dispatch-public-only-en
& (Join-Path $PSScriptRoot 'Test-Demo.ps1') -Scenarios v8-dispatch-home-store-cn,v8-dispatch-office-store-en -Width 1920 -Height 1080
& (Join-Path $PSScriptRoot 'Test-Demo.ps1') -Scenarios v8-dispatch-fallback-cn -ArtFallback
& (Join-Path $PSScriptRoot 'Test-Demo.ps1')
& (Join-Path $PSScriptRoot 'Test-Demo.ps1') -Scenarios v3-aspect-en,v3-aspect-consent-en,v4-office-agreement-aspect-en,v5-art-office-aspect-en,v7-ribbon-office-agreement-aspect-en -Width 1280 -Height 960
& (Join-Path $PSScriptRoot 'Test-Demo.ps1') -Scenarios v5-art-home-cn,v7-ribbon-loop-en
& (Join-Path $PSScriptRoot 'Test-Demo.ps1') -Scenarios v5-art-fallback-cn,v7-ribbon-fallback-cn -ArtFallback
& (Join-Path $PSScriptRoot 'Test-Demo.ps1') -Scenarios v5-art-home-store-cn,v5-art-office-store-en,v7-ribbon-home-store-cn,v7-ribbon-office-agreement-store-en -Width 1920 -Height 1080
