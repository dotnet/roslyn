// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MSBuild.UnitTests;

internal sealed class VisualStudioMSBuildInstalled : ExecutionCondition
{
    private static readonly string? s_skipReason;

    static VisualStudioMSBuildInstalled()
    {
        if (!PlatformInformation.IsWindows)
        {
            s_skipReason = "Test is only supported on Windows since it looks for a Visual Studio install.";
        }
        else if (!IsVisualStudioMSBuildInstalled())
        {
            s_skipReason = "No usable Visual Studio is installed.";
        }
    }

    private static bool IsVisualStudioMSBuildInstalled()
    {
        BuildHostProcessManager? buildHostProcessManager = null;

        try
        {
            buildHostProcessManager = new BuildHostProcessManager();

            var buildHost = buildHostProcessManager.GetBuildHostAsync(BuildHostProcessKind.NetFramework, CancellationToken.None).Result;

            // HACK: for .NET Framework build hosts, we don't actually need the project path to determine whether there's a usable VS -- so we can pass any file name here.
            return buildHost.HasUsableMSBuildAsync("NonExistent.sln", CancellationToken.None).Result;
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
