using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Exceptions;
using System.Globalization;

namespace DependencyVisualizerTool
{
    internal class MSBuildUtility
    {
        private const string MSBuildProjectExtensionsPath = nameof(MSBuildProjectExtensionsPath);

        public static string GetMSBuildProjectExtensionsPath(string projectFilePath)
        {
            var project = GetProject(projectFilePath);
            return project.GetPropertyValue(MSBuildProjectExtensionsPath);
        }

        private static Project GetProject(string projectCSProjPath, IDictionary<string, string>? globalProperties = null)
        {
            try
            {
                var projectRootElement = ProjectRootElement.Open(projectCSProjPath, ProjectCollection.GlobalProjectCollection, preserveFormatting: true);
                return new Project(projectRootElement);
            }
            catch (InvalidProjectFileException e)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "Unable to open the project {0}", projectCSProjPath), e);
            }
        }

    }
}
