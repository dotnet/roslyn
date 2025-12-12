// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Analyzer.Utilities
{
    /// <summary>
    /// Kind of option to fetch from <see cref="ICategorizedAnalyzerConfigOptions"/>.
    /// </summary>
    internal enum OptionKind
    {
        /// <summary>
        /// Option prefixed with <c>dotnet_code_quality.</c>.
        /// <para>Used for custom analyzer config options for analyzers in this repo.</para>
        /// </summary>
        DotnetCodeQuality,

        /// <summary>
        /// Option prefixed with <c>build_property.</c>.
        /// <para>Used for options generated for MSBuild properties.</para>
        /// </summary>
        BuildProperty,
    }
}
