// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.MSBuild.UnitTests
{
    internal class MSBuildInstalled : ExecutionCondition
    {
        private readonly ExecutionCondition _msBuildInstalled;

        public MSBuildInstalled()
            : this(minimumVsVersion: new Version(15, 0), minimumSdkVersion: new Version(2, 1))
        {
        }

        protected MSBuildInstalled(Version minimumVsVersion, Version minimumSdkVersion)
        {
            _msBuildInstalled =
#if NETCOREAPP
                new DotNetSdkMSBuildInstalled(minimumSdkVersion);
#else
                new VisualStudioMSBuildInstalled(minimumVsVersion);
#endif
        }

        public override bool ShouldSkip => _msBuildInstalled.ShouldSkip;

        public override string SkipReason => _msBuildInstalled.SkipReason;
    }

    internal class MSBuild16_2OrHigherInstalled : MSBuildInstalled
    {
        public MSBuild16_2OrHigherInstalled()
            : base(minimumVsVersion: new Version(16, 2), minimumSdkVersion: new Version(3, 1))
        {
        }
    }

    internal class MSBuild16_9OrHigherInstalled : MSBuildInstalled
    {
        public MSBuild16_9OrHigherInstalled()
            : base(minimumVsVersion: new Version(16, 9), minimumSdkVersion: new Version(5, 0, 201))
        {
        }
    }
}
