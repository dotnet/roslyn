namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp
{
    internal static class ProjectExtensions
    {
        public static string FSharpCommandLineOptions(this Project project)
        {
            return project.CommandLineOptions;
        }
    }
}
