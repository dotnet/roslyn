// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Linq;

namespace Analyzer.Utilities
{
    /// <summary>
    /// MSBuild property names that are required to be threaded as analyzer config options.
    /// </summary>
    /// <remarks>const fields in this type are automatically discovered and used to generate build_properties entries in the generated .globalconfig</remarks>
    internal static class MSBuildPropertyOptionNames
    {
        public const string TargetFramework = nameof(TargetFramework);
        public const string TargetFrameworkIdentifier = nameof(TargetFrameworkIdentifier);
        public const string TargetFrameworkVersion = nameof(TargetFrameworkVersion);
        public const string TargetPlatformMinVersion = nameof(TargetPlatformMinVersion);
        public const string UsingMicrosoftNETSdkWeb = nameof(UsingMicrosoftNETSdkWeb);
        public const string ProjectTypeGuids = nameof(ProjectTypeGuids);
        public const string InvariantGlobalization = nameof(InvariantGlobalization);
        public const string PlatformNeutralAssembly = nameof(PlatformNeutralAssembly);
        public const string EnforceExtendedAnalyzerRules = nameof(EnforceExtendedAnalyzerRules);
    }

    internal static class MSBuildPropertyOptionNamesHelpers
    {
        [Conditional("DEBUG")]
        public static void VerifySupportedPropertyOptionName(string propertyOptionName)
        {
            Debug.Assert(typeof(MSBuildPropertyOptionNames).GetFields().Single(f => f.Name == propertyOptionName) != null);
        }
    }
}
