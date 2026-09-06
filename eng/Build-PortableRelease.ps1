[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string] $Version,

    [Parameter(Mandatory = $true)]
    [string] $OutputDirectory
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$outputRoot = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $outputRoot) {
    throw "Output directory already exists: $outputRoot"
}

$buildRoot = Join-Path $outputRoot 'build'
$payloadDirectory = Join-Path $buildRoot 'app-publish'
$launcherDirectory = Join-Path $buildRoot 'launcher-publish'
$portableDirectory = Join-Path $outputRoot 'portable'
$filesDirectory = Join-Path $portableDirectory 'files'
$appDirectory = Join-Path $filesDirectory 'app'
$legalDirectory = Join-Path $filesDirectory 'legal'
$metadataDirectory = Join-Path $filesDirectory 'metadata'
New-Item -ItemType Directory -Path $payloadDirectory,$launcherDirectory,$appDirectory,$legalDirectory,$metadataDirectory | Out-Null

dotnet tool restore
if ($LASTEXITCODE -ne 0) { throw 'Local tool restore failed.' }

$appProject = Join-Path $repositoryRoot 'src\ThymeMe.App\ThymeMe.App.csproj'
$launcherProject = Join-Path $repositoryRoot 'src\ThymeMe.Launcher\ThymeMe.Launcher.csproj'
dotnet restore $appProject --runtime win-x64
if ($LASTEXITCODE -ne 0) { throw 'Application restore failed.' }
dotnet restore $launcherProject --runtime win-x64
if ($LASTEXITCODE -ne 0) { throw 'Launcher restore failed.' }

dotnet publish $appProject `
    --configuration Release `
    --runtime win-x64 `
    --no-restore `
    --output $payloadDirectory `
    -p:Platform=x64 `
    -p:WindowsPackageType=None
if ($LASTEXITCODE -ne 0) { throw 'Application publish failed.' }

dotnet publish $launcherProject `
    --configuration Release `
    --runtime win-x64 `
    --no-restore `
    --output $launcherDirectory `
    -p:Platform=x64
if ($LASTEXITCODE -ne 0) { throw 'Launcher publish failed.' }

$launcherPath = Join-Path $launcherDirectory 'thymeme.exe'
if (-not (Test-Path -LiteralPath $launcherPath -PathType Leaf)) {
    throw 'Single-file launcher publish did not produce thymeme.exe.'
}

Copy-Item -Path (Join-Path $payloadDirectory '*') -Destination $appDirectory -Recurse
Copy-Item -LiteralPath $launcherPath -Destination (Join-Path $portableDirectory 'thymeme.exe')
Copy-Item -LiteralPath (Join-Path $repositoryRoot 'LICENSE') -Destination (Join-Path $portableDirectory 'LICENSE.txt')
Copy-Item -LiteralPath (Join-Path $repositoryRoot 'docs\legal\NOTICE') -Destination (Join-Path $portableDirectory 'NOTICE.txt')
Copy-Item -LiteralPath (Join-Path $repositoryRoot 'docs\legal\THIRD_PARTY_NOTICES.md') -Destination $legalDirectory
Copy-Item -LiteralPath (Join-Path $repositoryRoot 'docs\legal\PROVENANCE.json') -Destination $metadataDirectory

$signature = Get-AuthenticodeSignature -LiteralPath (Join-Path $portableDirectory 'thymeme.exe')
@(
    'This Thyme-Me release is intentionally unsigned.'
    "Authenticode status: $($signature.Status)"
    'Windows may show an Unknown publisher or SmartScreen warning.'
    'Verify the SHA-256 in the GitHub Release notes and the GitHub artifact attestation before running it.'
) | Set-Content -Encoding utf8 -LiteralPath (Join-Path $metadataDirectory 'UNSIGNED_RELEASE.txt')

dotnet tool run sbom-tool generate `
    -b $appDirectory `
    -bc $repositoryRoot `
    -pn Thyme-Me `
    -pv $Version `
    -ps 'Sam Zhang' `
    -nsb 'https://github.com/samzhangubc/Thyme-Me'
if ($LASTEXITCODE -ne 0) { throw 'SBOM generation failed.' }

$sbom = Get-ChildItem -LiteralPath (Join-Path $appDirectory '_manifest') -Recurse -File -Filter 'manifest.spdx.json' | Select-Object -First 1
if ($null -eq $sbom) { throw 'SBOM generation did not produce an SPDX manifest.' }
Copy-Item -LiteralPath $sbom.FullName -Destination (Join-Path $metadataDirectory "Thyme-Me-$Version-sbom.spdx.json")
Remove-Item -LiteralPath (Join-Path $appDirectory '_manifest') -Recurse

$requiredPayload = @('thymeme.exe', 'thymeme.dll', 'thymeme.deps.json', 'thymeme.runtimeconfig.json', 'Assets\AppIcon.ico')
foreach ($relativePath in $requiredPayload) {
    if (-not (Test-Path -LiteralPath (Join-Path $appDirectory $relativePath) -PathType Leaf)) {
        throw "Portable payload is missing '$relativePath'."
    }
}

$runtimeEvidence = Get-ChildItem -LiteralPath $appDirectory -File |
    Where-Object Name -Match '^(coreclr|hostfxr|Microsoft\.WindowsAppRuntime|Microsoft\.UI\.Xaml).*\.dll$'
if (-not $runtimeEvidence) { throw 'Portable payload is not self-contained.' }

$rootFiles = @(Get-ChildItem -LiteralPath $portableDirectory -File | Sort-Object Name | Select-Object -ExpandProperty Name)
if (($rootFiles -join ',') -cne 'LICENSE.txt,NOTICE.txt,thymeme.exe') {
    throw "Unexpected portable root files: $($rootFiles -join ', ')"
}

$rootDirectories = @(Get-ChildItem -LiteralPath $portableDirectory -Directory | Sort-Object Name | Select-Object -ExpandProperty Name)
if (($rootDirectories -join ',') -cne 'files') {
    throw "Unexpected portable root directories: $($rootDirectories -join ', ')"
}

$archivePath = Join-Path $outputRoot "Thyme-Me-$Version-win-x64-portable.zip"
Compress-Archive -Path (Join-Path $portableDirectory '*') -DestinationPath $archivePath -CompressionLevel Optimal
$archiveHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $archivePath).Hash.ToLowerInvariant()

[pscustomobject]@{
    Archive = $archivePath
    Directory = $portableDirectory
    Sbom = Join-Path $metadataDirectory "Thyme-Me-$Version-sbom.spdx.json"
    Sha256 = $archiveHash
}
