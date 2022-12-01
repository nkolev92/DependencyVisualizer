namespace Common
{
    public class GraphOptions
    {
        public GraphOptions(bool checkVulnerabilities, bool generateProjectsOnly)
        {
            CheckVulnerabilities = checkVulnerabilities;
            GenerateProjectsOnly = generateProjectsOnly;
        }

        public bool CheckVulnerabilities { get; }
        public bool GenerateProjectsOnly { get; }
    }
}
