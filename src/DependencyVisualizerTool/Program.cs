// See https://aka.ms/new-console-template for more information
using Microsoft.Build.Locator;
using System.CommandLine;
using Logging;
using Microsoft.Extensions.Logging;
using static DependencyVisualizerTool.MSBuildUtility;
using NuGet.ProjectModel;

MSBuildLocator.RegisterDefaults();

var fileArgument = new Argument<FileInfo?>(
    name: "projectFilePath",
    description: "path to the project file",
    parse: result =>
    {
        string? filePath = result.Tokens.Single().Value;
        if (!File.Exists(filePath))
        {
            result.ErrorMessage = "Project file does not exists";
            AppLogger.Logger.LogError("Project file does not exists");
            return null;
        }
        return new FileInfo(filePath);
    });

var rootCommand = new RootCommand("Dependency visualizer app for System.CommandLine");

rootCommand.AddArgument(fileArgument);
rootCommand.SetHandler((file) =>
{
    ReadFile(file!);
},
    fileArgument);

return rootCommand.InvokeAsync(args).Result;

static void ReadFile(
            FileInfo file)
{
    try
    {
        string assetsFilePath = Path.Combine(GetMSBuildProjectExtensionsPath(file.FullName), LockFileFormat.AssetsFileName);
        List<string> lines = File.ReadLines(assetsFilePath).ToList();
        foreach (string line in lines)
        {
            Console.WriteLine(line);
        };
    }
    catch (Exception e)
    {
        AppLogger.Logger.LogError(e.Message);
        throw;
    }
}