// See https://aka.ms/new-console-template for more information
using System.CommandLine;

Console.WriteLine("Hello, World!");

var fileOption = new Option<FileInfo?>(
            name: "--file",
            description: "The file to read and display on the console.");

var delayOption = new Option<int>(
    name: "--delay",
    description: "Delay between lines, specified as milliseconds per character in a line.",
    getDefaultValue: () => 42);

var lightModeOption = new Option<bool>(
    name: "--light-mode",
    description: "Background color of text displayed on the console: default is black, light mode is white.");

var rootCommand = new RootCommand("Sample app for System.CommandLine");

var readCommand = new Command("read", "Read and display the file.")
            {
                fileOption,
                delayOption,
                lightModeOption
            };
rootCommand.AddCommand(readCommand);
 
readCommand.SetHandler(async (file, delay, lightMode) =>
{
    await ReadFile(file!, delay, lightMode);
},
    fileOption, delayOption, lightModeOption);

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