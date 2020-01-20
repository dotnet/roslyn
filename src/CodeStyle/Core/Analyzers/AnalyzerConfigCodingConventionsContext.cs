// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
