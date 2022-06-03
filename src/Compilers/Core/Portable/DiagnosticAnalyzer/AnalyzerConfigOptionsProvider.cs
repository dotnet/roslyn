// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Provide options from an analyzer config file keyed on a source file.
    /// </summary>
    public abstract class AnalyzerConfigOptionsProvider
    {
        /// <summary>
        /// Gets global options that do not apply to any specific file
        /// </summary>
        public abstract AnalyzerConfigOptions GlobalOptions { get; }

        /// <summary>
        /// Get options for a given <paramref name="tree"/>.
        /// </summary>
        public abstract AnalyzerConfigOptions GetOptions(SyntaxTree tree);

        /// <summary>
        /// Get options for a given <see cref="AdditionalText"/>
        /// </summary>
        public abstract AnalyzerConfigOptions GetOptions(AdditionalText textFile);
    }
}
