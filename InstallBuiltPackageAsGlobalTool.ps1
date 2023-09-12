$result = dotnet tool list --global dependencyvisualizertool
if($result.Length -gt 2)
{
    dotnet tool update --global DependencyVisualizerTool --add-source .\artifacts\ --prerelease
} else {
    dotnet tool install --global DependencyVisualizerTool --add-source .\artifacts\ --prerelease
}