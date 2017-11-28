
[CmdletBinding()]
Param([Parameter(Mandatory=$true)][string]$Version,
[Parameter()][System.IO.DirectoryInfo]$TargetDir)

msbuild "ShowMeTheXAML.sln" /p:SMTX_VERSION_FULL=$Version /p:Configuration=Debug

if (!(Test-Path "nuget.exe")) {
    Invoke-WebRequest -Uri https://dist.nuget.org/win-x86-commandline/latest/nuget.exe -OutFile "nuget.exe"
}
.\Nuget\UpdateNuspecFiles.ps1 -Version $Version
.\nuget pack Nuget\ShowMeTheXAML\ShowMeTheXAML.nuspec -Version $Version
.\nuget pack Nuget\ShowMeTheXAML.AvalonEdit\ShowMeTheXAML.AvalonEdit.nuspec -Version $Version
.\nuget pack Nuget\ShowMeTheXAML.MSBuild\ShowMeTheXAML.MSBuild.nuspec -Version $Version
.\nuget pack Nuget\ShowMeTheXAML.Fody\ShowMeTheXAML.Fody.nuspec -Version $Version

if ($TargetDir){
    Move-Item "ShowMeTheXAML.*.nupkg" $TargetDir -Force
    
    Write-Verbose "Moved nugets to $TargetDir"
}