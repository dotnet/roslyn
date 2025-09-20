// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Threading;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.MSBuild.UnitTests;

internal sealed partial class DotNetSdkMSBuildInstalled : ExecutionCondition
{
    private static readonly string? s_skipReason;

    static DotNetSdkMSBuildInstalled()
    {
        // We'll use the same .NET as we use for our own build, since that's the only SDK guaranteed to be installed
        var solution = Path.Combine(GetSolutionFolder(), "Roslyn.sln");

        if (!HasNetCoreSdkForSolution(solution))
        {
            s_skipReason = "No usable .NET SDK could be found.";
        }

        static string GetSolutionFolder()
        {
            // Expected assembly path:
            //  <solutionFolder>\artifacts\bin\Microsoft.CodeAnalysis.Workspaces.MSBuild.UnitTests\<Configuration>\<TFM>\Microsoft.CodeAnalysis.Workspaces.MSBuild.UnitTests.dll
            var assemblyLocation = typeof(DotNetSdkMSBuildInstalled).Assembly.Location;
            var solutionFolder = Directory.GetParent(assemblyLocation)
                ?.Parent?.Parent?.Parent?.Parent?.Parent?.FullName;
            Assumes.NotNull(solutionFolder);
            return solutionFolder;
        }
    }

    private static bool HasNetCoreSdkForSolution(string solution)
    {
        BuildHostProcessManager? buildHostProcessManager = null;

        try
        {
            buildHostProcessManager = new BuildHostProcessManager();

            var buildHost = buildHostProcessManager.GetBuildHostAsync(BuildHostProcessManager.BuildHostProcessKind.NetCore, CancellationToken.None).Result;

            return buildHost.HasUsableMSBuildAsync(solution, CancellationToken.None).Result;
        }
        finally
        {
            buildHostProcessManager?.DisposeAsync().AsTask().Wait();
        }
    }

    public override bool ShouldSkip
        => s_skipReason is not null;

    public override string SkipReason
        => s_skipReason!;
}
