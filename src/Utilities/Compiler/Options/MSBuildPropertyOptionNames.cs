// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Analyzer.Utilities
{
    /// <summary>
    /// MSBuild property names that are required to be threaded as analyzer config options.
    /// </summary>
    internal static partial class MSBuildPropertyOptionNames
    {
        public const string TargetFramework = "TargetFramework";
        public const string TargetPlatformMinVersion = "TargetPlatformMinVersion";
    }
}
