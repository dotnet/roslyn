// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Microsoft.Build.Locator;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.MSBuild.UnitTests
{
    internal class VisualStudioMSBuildInstalled : ExecutionCondition
    {
        private static readonly VisualStudioInstance? s_instance;

        static VisualStudioMSBuildInstalled()
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

        public VisualStudioMSBuildInstalled() : this(new Version(15, 0))
        {
        }

        internal VisualStudioMSBuildInstalled(Version minimumVersion)
        {
            _minimumVersion = minimumVersion;
        }

        public override bool ShouldSkip => s_instance is null || s_instance.Version < _minimumVersion;

        public override string SkipReason
#if !NETCOREAPP
            => $"Could not locate Visual Studio with MSBuild {_minimumVersion} or higher installed";
#else
            => $"Test runs on .NET Framework only.";
#endif
    }
}
