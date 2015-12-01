using Microsoft.CodeAnalysis.CommandLine;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.BuildTasks
{
    internal static class BuildClientShim
    {
        public static Task<BuildResponse> RunServerCompilation(
            RequestLanguage language,
            List<string> arguments,
            BuildPaths buildPaths,
            string keepAlive,
            string libEnvVariable,
            CancellationToken cancellationToken)
            => DesktopBuildClient.RunServerCompilation(
                language,
                arguments,
                buildPaths,
                keepAlive,
                libEnvVariable,
                cancellationToken);
    }
}
