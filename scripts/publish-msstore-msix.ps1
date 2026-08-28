$ErrorActionPreference = 'Stop'

$ProductId = '9PNHK9LDHMHS'
$PackageIdentifier = '8LWXpg.ProcessKillerforCommandPalette'

if (-not ($tag = git tag --sort=-v:refname | Select-Object -First 1)) {
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

if (-not ($link = $response.Links | Where-Object outerHTML -Like '*.msixbundle</a>' | Select-Object -Last 1)) {
	throw 'No msixbundle link found in store.rg-adguard.net response.'
}
$fileName = "${PackageIdentifier}_$version.0.Msixbundle"

Write-Host "Downloading $fileName ..."
$outFile = Join-Path ([System.IO.Path]::GetTempPath()) $fileName
Invoke-WebRequest -Uri $link.href -Headers $headers -OutFile $outFile

Write-Host "Uploading to release $tag ..."
gh release upload $tag $outFile --clobber
if ($LASTEXITCODE -ne 0) {
	throw 'gh release upload failed.'
}

if (-not ($assetUrl = (gh release view $tag --json assets | ConvertFrom-Json).assets |
			Where-Object Name -EQ $fileName |
			Select-Object -ExpandProperty url)) {
	throw 'Could not resolve uploaded asset URL.'
}

Write-Host "Updating winget manifest for $PackageIdentifier $version ..."
komac update $PackageIdentifier --version $version --urls $assetUrl
