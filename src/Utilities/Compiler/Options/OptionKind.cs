// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Analyzer.Utilities
{
    /// <summary>
    /// Kind of option to fetch from <see cref="AnalyzerOptionsExtensions"/>.
    /// </summary>
    internal enum OptionKind
    {
        /// <summary>
        /// Option prefixed with "dotnet_code_quality."
        /// Used for custom analyzer config options for analyzers in this repo.
        /// </summary>
        DotnetCodeQuality,

        /// <summary>
        /// Option prefixed with "build_property."
        /// Used for options generated for MSBuild properties.
        /// </summary>
        BuildProperty
    }
}