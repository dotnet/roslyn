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
#if NET472_OR_GREATER
        private static readonly VisualStudioInstance? s_instance;
        private readonly Version _minimumVersion;

        static VisualStudioMSBuildInstalled()
        {
            var installedVisualStudios = MSBuildLocator.QueryVisualStudioInstances().ToArray();
            foreach (var visualStudioInstall in installedVisualStudios)
            {
                if (visualStudioInstall.Version.Major == 17 &&
                    visualStudioInstall.Version.Minor == 0)
                {
                    MSBuildLocator.RegisterInstance(visualStudioInstall);
                    s_instance = visualStudioInstall;
                }
            }
        }
#endif

        public VisualStudioMSBuildInstalled()
#if NET472_OR_GREATER
            : this(new Version(16, 9))
#endif
        {
        }

#if NET472_OR_GREATER
        internal VisualStudioMSBuildInstalled(Version minimumVersion)
        {
            _minimumVersion = minimumVersion;
        }
#endif

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
