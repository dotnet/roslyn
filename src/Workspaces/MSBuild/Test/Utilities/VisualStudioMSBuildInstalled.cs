// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        else
        {
            // Tests using this condition (VisualStudioMSBuildWorkspaceTests) only run on Windows and are
            // failing on the release/dev18.0 branch. Tracked by https://github.com/dotnet/roslyn/issues/82931;
            // re-enabling requires backporting https://github.com/dotnet/roslyn/pull/83477 to release/dev18.0.
            s_skipReason = "Temporarily disabled on Windows: see https://github.com/dotnet/roslyn/issues/82931 (requires backport of https://github.com/dotnet/roslyn/pull/83477).";
        }
    }

    public override bool ShouldSkip
        => s_skipReason is not null;

    public override string SkipReason
        => s_skipReason!;
}
