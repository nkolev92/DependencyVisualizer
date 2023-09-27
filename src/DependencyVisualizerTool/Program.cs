using System.CommandLine;
using Common;
using Logging;
using Microsoft.Build.Locator;
using Microsoft.Extensions.Logging;
using NuGet.Configuration;
using NuGet.ProjectModel;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using static DependencyVisualizerTool.MSBuildUtility;

namespace DependencyVisualizerTool
{
    internal class Program
    {
        private const string ToolName = "DependencyVisualizer"; // This must match the value of ToolCommandName in the project file.
        private static readonly CancellationTokenSource CancellationTokenSource = new();

        public static int Main(string[] args)
        {
            Console.CancelKeyPress += new ConsoleCancelEventHandler(CancellationHandler);
            var fileArgument = new Argument<FileInfo?>(
                name: "projectFilePath",
                description: "Project file path.",
                parse: result =>
                {
                    string? filePath = result.Tokens.Single().Value;
                    if (!File.Exists(filePath))
                    {
                        result.ErrorMessage = $"Invalid argument <projectFilePath>. The path does not exists.";
                        AppLogger.Logger.LogError(result.ErrorMessage);
                        return null;
                    }
                    return new FileInfo(filePath);
                });

            var outputOption = new Option<string?>(
                name: "--output",
                description: "Output folder path used to store generated graph file(s). By default, it's the project folder.");
            outputOption.AddAlias("-o");

            var checkVulnerabilityOption = new Option<bool?>(
                name: "--vulnerable",
                description: "Whether to visualize the vulnerable packages in your package graph");

            var checkDeprecationOption = new Option<bool?>(
                name: "--deprecated",
                description: "Whether to visualize the deprecated packages in your package graph");

            var projectsOnlyOption = new Option<bool?>(
                name: "--projects-only",
                description: "When used, generates a projects only graph.");

            var rootCommand = new RootCommand("Dependency visualizer app");
            rootCommand.Name = ToolName;
            rootCommand.AddArgument(fileArgument);
            rootCommand.AddOption(outputOption);
            rootCommand.AddOption(checkVulnerabilityOption);
            rootCommand.AddOption(checkDeprecationOption);
            rootCommand.AddOption(projectsOnlyOption);

            rootCommand.SetHandler(async (fileArgument, outputOption, checkVulnerabilityOption, checkDeprecationOption, projectsOnly) =>
            {
#if DEBUG
                System.Diagnostics.Debugger.Launch();
#endif
                await GenerateGraph(fileArgument!, outputOption, checkVulnerabilityOption, checkDeprecationOption, projectsOnly, CancellationTokenSource.Token);
            },
            fileArgument, outputOption, checkVulnerabilityOption, checkDeprecationOption, projectsOnlyOption);

            return rootCommand.InvokeAsync(args).Result;
        }

        private static async Task<int> GenerateGraph(FileInfo projectFile, string? outputFolder, bool? checkVulnerabilities, bool? checkDeprecation, bool? projectsOnly, CancellationToken cancellationToken)
        {
            MSBuildLocator.RegisterDefaults();
            string projectExtensionsPath = GetMSBuildProjectExtensionsPath(projectFile.FullName);
            LockFile? assetFile = GetAssetsFilePath(projectExtensionsPath);
            DependencyGraphSpec? dgspecFile = GetDgspecFilePath(projectExtensionsPath, projectFile);

            if (outputFolder == null)
            {
                outputFolder = Path.GetDirectoryName(projectFile.FullName);
            }
            else
            {
                CreateOutputIfNotExists(outputFolder);
            }

            if (assetFile == null || dgspecFile == null || outputFolder == null)
            {
                return 1;
            }

            var decorators = CreateDecorators(assetFile.PackageSpec, checkVulnerabilities == true, checkDeprecation == true);

            Dictionary<string, PackageDependencyGraph> dictGraph = await PackageDependencyGraph.GenerateAllDependencyGraphsFromAssetsFileAsync(
                assetFile,
                dgspecFile,
                projectsOnly == true,
                decorators,
                cancellationToken);

            var outputFiles = new List<string>(dictGraph.Count);
            foreach (var keyValuePair in dictGraph)
            {
                string projectName = Path.GetFileNameWithoutExtension(projectFile.Name);
                string tfm = keyValuePair.Key;
                string dgmlFileName = Path.Combine(outputFolder, $"{projectName}_{tfm}.dgml");
                try
                {
                    DGMLDependencyVisualizerTool.TransGraphToDGMLFile(keyValuePair.Value, dgmlFileName, populateCosts: projectsOnly != true);
                    outputFiles.Add(dgmlFileName);
                }
                catch (Exception e)
                {
                    string errorMessage = "Exception is thrown when generating the DGML file. {0}";
                    AppLogger.Logger.LogError(errorMessage, e);
                    return 1;
                }
            }
            string infoMessage = $"Successfully created dependency graph file(s): {string.Join(Environment.NewLine, outputFiles)}";
            AppLogger.Logger.LogInformation(infoMessage);
            Console.WriteLine(infoMessage);
            return 0;
        }

        private static List<IPackageDependencyNodeDecorator> CreateDecorators(PackageSpec packageSpec, bool visualizeVulnerabilities, bool visualizeDeprecation)
        {
            List<IPackageDependencyNodeDecorator> decorators = new();
            List<SourceRepository>? repositories = visualizeVulnerabilities || visualizeDeprecation ? GetHTTPSourceRepositories(packageSpec) : null;
            SourceCacheContext sourceCacheContext = new();
            if (visualizeVulnerabilities)
            {
                decorators.Add(new VulnerabilityInfoDecorator(repositories!, sourceCacheContext));
            }

            if (visualizeDeprecation)
            {
                decorators.Add(new DeprecationInfoDecorator(repositories!, sourceCacheContext));
            }

            return decorators;

            static List<SourceRepository> GetHTTPSourceRepositories(PackageSpec projectPackageSpec)
            {
                using var settingsLoadContext = new SettingsLoadingContext();

                Dictionary<PackageSource, SourceRepository> sourceRepositoryCache = new();

                var settings = Settings.LoadImmutableSettingsGivenConfigPaths(projectPackageSpec.RestoreMetadata.ConfigFilePaths, settingsLoadContext);
                var sources = projectPackageSpec.RestoreMetadata.Sources;

                IEnumerable<Lazy<INuGetResourceProvider>> providers = Repository.Provider.GetCoreV3();

                foreach (PackageSource source in sources)
                {
                    if (source.IsHttp)
                    {
                        SourceRepository sourceRepository = Repository.CreateSource(providers, source, FeedType.Undefined);
                        sourceRepositoryCache[source] = sourceRepository;
                    }
                }

                return sourceRepositoryCache.Values.ToList();
            }
        }

        private static LockFile? GetAssetsFilePath(string projectExtensionsPath)
        {
            ArgumentNullException.ThrowIfNull(projectExtensionsPath);
            string assetsFilePath = Path.Combine(projectExtensionsPath, LockFileFormat.AssetsFileName);
            if (!File.Exists(assetsFilePath))
            {
                AppLogger.Logger.LogError($"Assets file: {assetsFilePath} doesn't exist. Please make sure restore is done on this project before running this command.");
                return null;
            }
            try
            {
                LockFile assetsFile = new LockFileFormat().Read(assetsFilePath);
                return assetsFile;
            }
            catch (Exception e)
            {
                string errorMessage = $"Exception is thrown when reading the assets file at {assetsFilePath}.";
                AppLogger.Logger.LogError(errorMessage);
                AppLogger.Logger.LogDebug(e.Message);
                AppLogger.Logger.LogDebug(e.StackTrace);
                return null;
            }
        }

        private static DependencyGraphSpec? GetDgspecFilePath(string projectExtensionsPath, FileInfo projectFile)
        {
            ArgumentNullException.ThrowIfNull(projectExtensionsPath);

            string dgspecFileName = DependencyGraphSpec.GetDGSpecFileName(projectFile.Name);
            string dgspecFileFullPath = Path.Combine(projectExtensionsPath, dgspecFileName);
            if (!File.Exists(dgspecFileFullPath))
            {
                AppLogger.Logger.LogError($"Dgspec file: {dgspecFileFullPath} doesn't exist. Please make sure restore is done on this project before running this command.");
                return null;
            }
            try
            {
                DependencyGraphSpec dgspecFile = DependencyGraphSpec.Load(dgspecFileFullPath);
                return dgspecFile;
            }
            catch (Exception e)
            {
                string errorMessage = $"Exception is thrown when reading the dgspec file at {dgspecFileFullPath}.";
                AppLogger.Logger.LogError(errorMessage);
                AppLogger.Logger.LogDebug(e.Message);
                AppLogger.Logger.LogDebug(e.StackTrace);
                return null;
            }
        }

        private static void CreateOutputIfNotExists(string outputFolder)
        {
            if (!Directory.Exists(outputFolder))
            {
                try
                {
                    Directory.CreateDirectory(outputFolder);
                }
                catch (Exception e)
                {
                    var errorMessage = $"Exception is thrown when creating the outputFolder at: {outputFolder}";
                    AppLogger.Logger.LogError(errorMessage);
                    AppLogger.Logger.LogDebug(e.Message);
                    AppLogger.Logger.LogDebug(e.StackTrace);
                }
            }
        }

        protected static void CancellationHandler(object? sender, ConsoleCancelEventArgs args)
        {
            args.Cancel = true;
            CancellationTokenSource.Cancel();
        }
    }
}