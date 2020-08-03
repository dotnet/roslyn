// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    }
}
