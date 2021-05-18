// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

#if CODEANALYSIS_V3_OR_BETTER
using System.Linq;
#endif

namespace Analyzer.Utilities
{
    /// <summary>
    /// MSBuild property names that are required to be threaded as analyzer config options.
    /// </summary>
    internal static class MSBuildPropertyOptionNames
    {
        public const string TargetFramework = nameof(TargetFramework);
        public const string TargetPlatformMinVersion = nameof(TargetPlatformMinVersion);
        public const string UsingMicrosoftNETSdkWeb = nameof(UsingMicrosoftNETSdkWeb);
        public const string ProjectTypeGuids = nameof(ProjectTypeGuids);
        public const string PublishSingleFile = nameof(PublishSingleFile);
        public const string IncludeAllContentForSelfExtract = nameof(IncludeAllContentForSelfExtract);
        public const string InvariantGlobalization = nameof(InvariantGlobalization);
    }

    internal static class MSBuildPropertyOptionNamesHelpers
    {
        [Conditional("DEBUG")]
        public static void VerifySupportedPropertyOptionName(string propertyOptionName)
        {
#if CODEANALYSIS_V3_OR_BETTER
            Debug.Assert(typeof(MSBuildPropertyOptionNames).GetFields().Single(f => f.Name == propertyOptionName) != null);
#endif
        }
    }
}
