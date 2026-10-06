param(
    [Parameter(Mandatory=$true)][string]$Version,
    [Parameter(Mandatory=$true)][string]$Zip,
    [string]$Repository = 'page-muncher/gazegallery',
    [string]$NotesFile
)
$ErrorActionPreference = 'Stop'
$asset = (Resolve-Path -LiteralPath $Zip).Path
if (-not $asset.EndsWith('.zip')) { throw 'Expected a portable ZIP.' }
$checksum = "$asset.sha256.txt"
$hash = (Get-FileHash -LiteralPath $asset -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText($checksum, "$hash  $([IO.Path]::GetFileName($asset))`n")
& gh auth status
if ($LASTEXITCODE -ne 0) { throw 'Sign in first with gh auth login.' }
$tag = "v$Version"
& gh release view $tag --repo $Repository *> $null
if ($LASTEXITCODE -eq 0) { throw "Release $tag already exists. Refusing to replace it." }
$options = @('release','create',$tag,$asset,$checksum,'--repo',$Repository,'--title',"gazegallery $Version",'--draft','--target','main')
if ($NotesFile) { $options += @('--notes-file',(Resolve-Path -LiteralPath $NotesFile).Path) }
else { $options += @('--notes',"Portable Windows x64 release. Extract the entire ZIP and run gazegallery.exe. See the bundled full guide and third-party notices.") }
& gh @options
if ($LASTEXITCODE -ne 0) { throw 'Draft creation/upload failed; inspect the draft before retrying.' }
& gh release edit $tag --repo $Repository --draft=false
if ($LASTEXITCODE -ne 0) { throw 'Uploaded draft could not be published.' }
& gh release view $tag --repo $Repository --json url --jq .url
