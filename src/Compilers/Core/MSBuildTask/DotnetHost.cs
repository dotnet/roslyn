using System;

namespace Microsoft.CodeAnalysis.BuildTasks
{
    class DotnetHost
    {
        public string ToolName { get; }

        public string PathToTool { get; }

        public string CommandLineArgs { get; }

        public DotnetHost(string toolName, string pathToTool, string commandLineArgs)
        {
            ToolName = toolName;
            PathToTool = pathToTool;
            CommandLineArgs = commandLineArgs;
        }

        public DotnetHost(string toolNameWithoutExtension, string commandLineArgs)
        {
            string pathToTool;
            string toolName;
            if (IsCliHost(out string pathToDotnet))
            {
                toolName = $"{toolNameWithoutExtension}.dll";
                pathToTool = Utilities.GenerateFullPathToTool(toolName);
                if (pathToTool != null)
                {
                    RewriteToCliInvocation(ref pathToTool, ref commandLineArgs, pathToDotnet);
                }
            }
            else
            {
                // Desktop executes tool directly
                toolName = $"{toolNameWithoutExtension}.exe";
                pathToTool = Utilities.GenerateFullPathToTool(toolName);
            }
            PathToTool = pathToTool;
            CommandLineArgs = commandLineArgs;
        }

        private static void RewriteToCliInvocation(ref string pathToTool, ref string commandLineArgs, string pathToDotnet)
        {
            commandLineArgs = $"\"{pathToTool}\" {commandLineArgs}";
            pathToTool = pathToDotnet;
        }

        private static bool IsCliHost(out string pathToDotnet)
        {
            pathToDotnet = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
            return !string.IsNullOrEmpty(pathToDotnet);
        }
    }
}
