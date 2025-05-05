param(
	[switch]$skipBuild
)

pushd
cd $PSScriptRoot

$version = Read-Host -Prompt 'New tag'

if (-not $skipBuild){
	dotnet build --configuration Release -p:GenerateAppxPackageOnBuild=true '-p:Platform=x64'
	dotnet build --configuration Release -p:GenerateAppxPackageOnBuild=true '-p:Platform=arm64'
	rm ./out/*
	(Get-ChildItem -r *.msix -Exclude Microsoft.WindowsAppRuntime.1.6.msix).FullName | ForEach-Object { Copy-Item $_ ./out/. -Force }
}

(cat ./app.manifest -Raw) -replace `
	'<assemblyIdentity version="[\d\.]+" name="ProcessKiller.app"/>', `
	"<assemblyIdentity version=`"$version.0`" name=`"ProcessKiller.app`"/>" `
		| Out-File .\app.manifest -NoNewline
(cat ./Package.appxmanifest -Raw) -replace `
	'Publisher="CN=8LWXpg"
    Version="0.0.1.0" />', `
	"Publisher=`"CN=8LWXpg`"
    Version=`"$version.0`" />" `
		| Out-File ./Package.appxmanifest -NoNewline

git add ..
git tag "v$version"

popd

