namespace Common
{
    public class GraphOptions
    {
        public GraphOptions(bool generateProjectsOnly)
        {
            GenerateProjectsOnly = generateProjectsOnly;
        }

        public bool GenerateProjectsOnly { get; }
    }
}
