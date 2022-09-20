// See https://aka.ms/new-console-template for more information
using System.CommandLine;

var fileArgument = new Argument<FileInfo?>(
    name: "assetsFile",
    description: "The file to read and display on the console.",
    parse: result =>
    {
        string? filePath = result.Tokens.Single().Value;
        if (!File.Exists(filePath))
        {
            result.ErrorMessage = "File does not exists";
            return null;
        }
        return new FileInfo(filePath);
    });

var delayOption = new Option<int>(
    name: "--delay",
    description: "Delay between lines, specified as milliseconds per character in a line.",
    getDefaultValue: () => 42);

var lightModeOption = new Option<bool>(
    name: "--light-mode",
    description: "Background color of text displayed on the console: default is black, light mode is white.");

var rootCommand = new RootCommand("Dependency visualizer app for System.CommandLine");

var readCommand = new Command("read", "Read and display the file.")
            {
                delayOption,
                lightModeOption
            };

readCommand.AddArgument(fileArgument);
readCommand.SetHandler(async (file, delay, lightMode) =>
{
    await ReadFile(file!, delay, lightMode);
},
    fileArgument, delayOption, lightModeOption);

rootCommand.AddCommand(readCommand);

return rootCommand.InvokeAsync(args).Result;

static async Task ReadFile(
            FileInfo file, int delay, bool lightMode)
{
    Console.BackgroundColor = lightMode ? ConsoleColor.White : ConsoleColor.Black;
    List<string> lines = File.ReadLines(file.FullName).ToList();
    foreach (string line in lines)
    {
        Console.WriteLine(line);
        await Task.Delay(delay * line.Length);
    };
}