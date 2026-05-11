param (
	[ValidateNotNullOrEmpty()]
	[Parameter(Mandatory)]
	[string]$InputSvg
)
Push-Location $PSScriptRoot

$inkscape = 'C:\Program Files\Inkscape\bin\inkscape.com'
$icons = @(
	@{ Name = 'SmallTile.png';             Width = 142;  Height = 142 },
	@{ Name = 'Square150x150Logo.png';     Width = 300;  Height = 300 },
	@{ Name = 'Wide310x150Logo.png';       Width = 620;  Height = 300 },
	@{ Name = 'LargeTile.png';             Width = 620;  Height = 620 },
	@{ Name = 'Square44x44Logo.png';       Width = 88;   Height = 88  },
	@{ Name = 'SplashScreen.png';          Width = 1240; Height = 600 },
	@{ Name = 'StoreLogo.scale-200.png';             Width = 100;  Height = 100 }
)
$SrcSize = 96

foreach ($icon in $icons) {
	$W = $icon.Width
	$H = $icon.Height
	$Name = $icon.Name

	Write-Host "Generating $Name ($W x $H)..."

	# Scale is driven by height (always <= width).
	# Expand x symmetrically; y stays 0:96.
	$svgW = $SrcSize * $W / $H
	$x0   = -($svgW - $SrcSize) / 2
	$x1   = $SrcSize + ($svgW - $SrcSize) / 2

	Write-Host --export-area="$x0`:0`:$x1`:$SrcSize"

    & $inkscape $InputSvg `
        --export-type=png `
        --export-filename=$Name `
        --export-width=$W `
        --export-height=$H `
        --export-area="$x0`:0`:$x1`:$SrcSize" 2>&1 | Out-Null

	Write-Host "-> " $icon.Name
}

Pop-Location
