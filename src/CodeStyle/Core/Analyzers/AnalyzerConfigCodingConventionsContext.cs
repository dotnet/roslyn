// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    public class AnalyzerConfigCodingConventionsContext : ICodingConventionContext
    {
        private readonly AnalyzerConfigOptions _analyzerConfigOptions;

        public AnalyzerConfigCodingConventionsContext(AnalyzerConfigOptions analyzerConfigOptions)
        {
            _analyzerConfigOptions = analyzerConfigOptions;
        }

        public AnalyzerConfigOptions CurrentConventions => _analyzerConfigOptions;
    }
}
