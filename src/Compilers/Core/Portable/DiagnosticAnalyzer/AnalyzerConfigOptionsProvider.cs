// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Provide options from an analyzer config file keyed on a source file.
    /// </summary>
    public abstract class AnalyzerConfigOptionsProvider
    {
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
