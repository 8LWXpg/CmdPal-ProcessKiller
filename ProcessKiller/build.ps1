param(
	[switch]$skipBuild
)

Push-Location
Set-Location $PSScriptRoot

$version = Read-Host -Prompt 'New tag'

(Get-Content ./app.manifest -Raw) -replace `
	'<assemblyIdentity version="[\d\.]+"', `
	"<assemblyIdentity version=`"$version.0`"" `
| Out-File .\app.manifest -NoNewline
(Get-Content ./Package.appxmanifest -Raw) -replace `
	'(Identity[\s\S]+?)Version="[\d\.]+"', `
	"`$1Version=`"$version.0`"" `
| Out-File ./Package.appxmanifest -NoNewline

if (-not $skipBuild){
	dotnet build --configuration Release -p:Platform=x64 -p:AppxPackageDir="AppPackages\x64\"
	dotnet build --configuration Release -p:Platform=arm64 -p:AppxPackageDir="AppPackages\ARM64\"
	mkdir out -ea ig
	Remove-Item ./out/* -ea ig
	$msix = Get-ChildItem ./AppPackages/ -Recurse -Filter *.msix
	Copy-Item $msix ./out/. -Force

	Push-Location
	Import-Module 'C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\Tools\Microsoft.VisualStudio.DevShell.dll'
	Enter-VsDevShell 44033118
	Pop-Location
	$filename = $msix[0].Name
	$firstIndex = $filename.IndexOf('_')
	$secondIndex = $filename.IndexOf('_', $firstIndex+1)
	$bundle = "./out/$($filename.Substring(0, $secondIndex))_Bundle.msixbundle"
	makeappx bundle /v /d ./out/ /p $bundle
}

git add ..
git commit -m 'bump'
git tag "v$version"

Pop-Location

