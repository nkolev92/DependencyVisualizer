// See https://aka.ms/new-console-template for more information
using Microsoft.Build.Locator;
using System.CommandLine;
using Logging;
using Microsoft.Extensions.Logging;

MSBuildLocator.RegisterDefaults();

var fileArgument = new Argument<FileInfo?>(
    name: "assetsFile",
    description: "The file to read and display on the console.",
    parse: result =>
    {
        string? filePath = result.Tokens.Single().Value;
        if (!File.Exists(filePath))
        {
            result.ErrorMessage = "File does not exists";
            AppLogger.Logger.LogError("File does not exists");
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
    List<string> lines = File.ReadLines(file.FullName).ToList();
    foreach (string line in lines)
    {
        Console.WriteLine(line);
    };
}