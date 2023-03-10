// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Build.Locator;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.MSBuild.UnitTests
{
    internal class VisualStudioMSBuildInstalled : ExecutionCondition
    {
#if NET472_OR_GREATER

        private static readonly VisualStudioInstance? s_instance;
        private readonly Version _minimumVersion;

        static VisualStudioMSBuildInstalled()
        {
            var latestInstalledInstance = (VisualStudioInstance?)null;
            foreach (var visualStudioInstance in MSBuildLocator.QueryVisualStudioInstances())
            {
                if (latestInstalledInstance == null || visualStudioInstance.Version > latestInstalledInstance.Version)
                {
                    latestInstalledInstance = visualStudioInstance;
                }
            }

            if (latestInstalledInstance != null)
            {
                MSBuildLocator.RegisterInstance(latestInstalledInstance);
                s_instance = latestInstalledInstance;
            }
        }

#endif

        public VisualStudioMSBuildInstalled()
            : this(new Version(17, 0))
        {
        }

        internal VisualStudioMSBuildInstalled(Version minimumVersion)
        {
#if NET472_OR_GREATER
            _minimumVersion = minimumVersion;
#endif
        }

        public override bool ShouldSkip
#if NET472_OR_GREATER
            => s_instance is null || s_instance.Version < _minimumVersion;
#else
            => true;
#endif

        public override string SkipReason
#if NET472_OR_GREATER
            => $"Could not locate Visual Studio with MSBuild {_minimumVersion} or higher installed";
#else
            => $"Test runs on .NET Framework only.";
#endif
    }
}
