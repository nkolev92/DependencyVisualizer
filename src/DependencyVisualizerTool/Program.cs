// See https://aka.ms/new-console-template for more information
using Microsoft.Build.Locator;
using System.CommandLine;
using Logging;
using Microsoft.Extensions.Logging;
using static DependencyVisualizerTool.MSBuildUtility;
using NuGet.ProjectModel;
using static NuGet.Packaging.PackagingConstants;
using Newtonsoft.Json;
using System.Globalization;
using Common;

namespace DependencyVisualizerTool
{
    internal class Program
    {
        public static int Main(string[] args)
        {
            MSBuildLocator.RegisterDefaults();

            var fileArgument = new Argument<FileInfo>(
                name: "projectFilePath",
                description: "path to the project file to be used for finding the package.assets.file",
                parse: result =>
                {
                    string? filePath = result.Tokens.Single().Value;
                    if (!File.Exists(filePath))
                    {
                        result.ErrorMessage = "Invalid argument projectFilePath. The path does not exists.";
                        AppLogger.Logger.LogError(result.ErrorMessage);
                        throw new ArgumentException(result.ErrorMessage);
                    }
                    return new FileInfo(filePath);
                });

            var outputOption = new Option<string?>(
                name: "--outputFolder",
                description: "A folder path used to store generated DGML files. By default, it's the project folder.");

            var rootCommand = new RootCommand("Dependency visualizer app");
            rootCommand.AddArgument(fileArgument);
            rootCommand.AddOption(outputOption);

            rootCommand.SetHandler((fileArgument, outputOption) =>
            {
                GenerateGraph(fileArgument, outputOption);
            },
            fileArgument, outputOption);

            return rootCommand.InvokeAsync(args).Result;
        }

        private static void GenerateGraph(FileInfo projectFile, string? outputFolder)
        {
            ArgumentNullException.ThrowIfNull(projectFile);
            string projectExtensionsPath = GetMSBuildProjectExtensionsPath(projectFile.FullName);
            LockFile assetFile = GetAssetsFilePath(projectExtensionsPath);
            DependencyGraphSpec dgspecFile = GetDgspecFilePath(projectExtensionsPath, projectFile);
   
            if (outputFolder == null)
            {
                outputFolder = Path.GetDirectoryName(projectFile.FullName);
            }
            else
            {
                CreateOutputIfNotExist(outputFolder);
            }

            Dictionary<string, PackageDependencyGraph> dictGraph = PackageDependencyGraph.GenerateAllDependencyGraphsFromAssetsFile(assetFile, dgspecFile);
            foreach (var keyValuePair in dictGraph)
            {
                string projectName = Path.GetFileNameWithoutExtension(projectFile.Name);
                string tfm = keyValuePair.Key;
                string dgmlFileName = Path.Combine(outputFolder, $"{projectName}_{tfm}.dgml");
                DGMLDependencyVisualizerTool.TransGraphToDGMLFile(keyValuePair.Value, dgmlFileName);
            }
        }
        private static LockFile GetAssetsFilePath(string projectExtensionsPath)
        {
            ArgumentNullException.ThrowIfNull(projectExtensionsPath);
            try
            {
                string assetsFilePath = Path.Combine(projectExtensionsPath, LockFileFormat.AssetsFileName);
                if (!File.Exists(assetsFilePath))
                {
                    AppLogger.Logger.LogError($"Assets file: {assetsFilePath} doesn't exist. Please make sure restore is done on this project before running this command.");
                }
                LockFile assetsFile = new LockFileFormat().Read(assetsFilePath);
                return assetsFile;
            }
            catch (Exception e)
            {
                AppLogger.Logger.LogError($"Failed to read the assets file: \n {e.Message}");
                throw;
            }

        }
        private static DependencyGraphSpec GetDgspecFilePath(string projectExtensionsPath, FileInfo projectFile)
        {
            ArgumentNullException.ThrowIfNull(projectExtensionsPath);
            ArgumentNullException.ThrowIfNull(projectFile);
            try
            {
                string dgspecFileName = DependencyGraphSpec.GetDGSpecFileName(projectFile.Name);
                string dgspecFileFullPath = Path.Combine(projectExtensionsPath, dgspecFileName);
                if (!File.Exists(dgspecFileFullPath))
                {
                    AppLogger.Logger.LogError($"Dgspec file: {dgspecFileFullPath} doesn't exist. Please make sure restore is done on this project before running this command.");
                }
                DependencyGraphSpec dgspecFile = DependencyGraphSpec.Load(dgspecFileFullPath);
                return dgspecFile;
            }
            catch (Exception e)
            {
                AppLogger.Logger.LogError(e.Message);
                throw;
            }
        }

        private static void CreateOutputIfNotExist(string outputFolder)
        {
            if (!Directory.Exists(outputFolder))
            {
                try
                {
                    Directory.CreateDirectory(outputFolder);
                }
                catch (Exception e)
                {
                    var errorMessage = $"Failed in creating output folder. \n{e.Message}";
                    AppLogger.Logger.LogError(errorMessage);
                    throw;
                }
            }
        }

    }

}