// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Microsoft.Build.Locator;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.MSBuild.UnitTests
{
    internal partial class DotNetSdkMSBuildInstalled : ExecutionCondition
    {
        private static readonly VisualStudioInstance? s_instance;

        static DotNetSdkMSBuildInstalled()
        {
            s_instance = MSBuildLocator.QueryVisualStudioInstances()
                .OrderByDescending(instances => instances.Version)
                .FirstOrDefault();

            if (s_instance != null && !MSBuildLocator.IsRegistered)
            {
                MSBuildLocator.RegisterInstance(s_instance);
            }
        }

        private readonly Version _minimumVersion;

        public DotNetSdkMSBuildInstalled() : this(new Version(2, 1))
        {
        }

        internal DotNetSdkMSBuildInstalled(Version minimumVersion)
        {
            _minimumVersion = minimumVersion;
        }

        public override bool ShouldSkip => s_instance is null || s_instance.Version < _minimumVersion;

        public override string SkipReason
#if NETCOREAPP
            => $"Could not locate .NET SDK {_minimumVersion} or higher installed.";
#else
            => $"Test runs on .NET Core only.";
#endif
    }
}
