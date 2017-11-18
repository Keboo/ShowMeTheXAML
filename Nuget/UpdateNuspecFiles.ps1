[CmdletBinding()]
Param([Parameter(Mandatory=$true)][string]$Version)

$regex = [regex]"(?<=\d+\.)\d+\.\d+"
$match = $regex.Match($Version)
$MaxVersion = $regex.Replace($Version, ([int]$match.Value) + 1) + ".0"

foreach($nuspecFile in Get-ChildItem "Nuget\*.nuspec" -Recurse) {
    [xml] $file = Get-Content $nuspecFile
    $metadata = $file.package.metadata
    $metadata.copyright = "Copyright " + (Get-Date).Year
    $metadata.version = $Version

    $smtxDependency = $metadata.dependencies.dependency | Where-Object { $_.id -eq "ShowMeTheXAML" } | Select-Object -First 1
    if ($smtxDependency) {
        $smtxDependency.version = "[$Version,$MaxVersion)"
    }

    $file.Save($nuspecFile)
}