#Requires -Version 7
<#
Downloads the latest Microsoft Store msixbundle for this app from store.rg-adguard.net,
uploads it to the GitHub release matching the latest git tag, then updates the winget
manifest via komac.
#>

$ErrorActionPreference = 'Stop'

$ProductId = '9PNHK9LDHMHS'
$PackageIdentifier = '8LWXpg.ProcessKillerforCommandPalette'

$tag = git tag --sort=-v:refname | Select-Object -First 1
if (-not $tag) {
	throw 'No git tags found.'
}
$version = $tag.TrimStart('v')

$headers = @{
	'User-Agent' = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Safari/537.36'
	'Origin'     = 'https://store.rg-adguard.net'
	'Referer'    = 'https://store.rg-adguard.net/'
}
$body = @{ type = 'ProductId'; url = $ProductId; ring = 'RP' }
$response = Invoke-WebRequest -Uri 'https://store.rg-adguard.net/api/GetFiles' -Method Post -Headers $headers -Body $body

$link = $response.Links | Where-Object outerHTML -like '*.msixbundle</a>' | Select-Object -Last 1
if (-not $link) {
	throw 'No msixbundle link found in store.rg-adguard.net response.'
}
$bundle = [pscustomobject]@{
	Url  = $link.href
	Name = [regex]::Match($link.outerHTML, '>(?<name>[^<]+)</a>$').Groups['name'].Value
}

Write-Host "Downloading $($bundle.Name) ..."
$outFile = Join-Path ([System.IO.Path]::GetTempPath()) $bundle.Name
Invoke-WebRequest -Uri $bundle.Url -Headers $headers -OutFile $outFile

Write-Host "Uploading to release $tag ..."
gh release upload $tag $outFile --clobber
if ($LASTEXITCODE -ne 0) {
	throw 'gh release upload failed.'
}

$assetUrl = (gh release view $tag --json assets | ConvertFrom-Json).assets |
	Where-Object Name -eq $bundle.Name |
	Select-Object -ExpandProperty url
if (-not $assetUrl) {
	throw 'Could not resolve uploaded asset URL.'
}

Write-Host "Updating winget manifest for $PackageIdentifier $version ..."
komac update $PackageIdentifier --version $version --urls $assetUrl
