pushd
cd $PSScriptRoot

dotnet build --configuration Release -p:GenerateAppxPackageOnBuild=true '-p:Platform=x64'
dotnet build --configuration Release -p:GenerateAppxPackageOnBuild=true '-p:Platform=arm64'
(Get-ChildItem -r *.msix -Exclude Microsoft.WindowsAppRuntime.1.6.msix).FullName | ForEach-Object { Copy-Item $_ ./out/.}

popd

