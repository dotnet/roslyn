// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    internal class AnalyzerConfigCodingConventionsManager : ICodingConventionsManager
    {
        private readonly SyntaxTree _tree;
        private readonly AnalyzerOptions _options;

        public AnalyzerConfigCodingConventionsManager(SyntaxTree tree, AnalyzerOptions options)
        {
            _tree = tree;
            _options = options;
        }

        public Task<ICodingConventionContext> GetConventionContextAsync(string filePathContext, CancellationToken cancellationToken)
        {
            var analyzerConfigOptionsProvider = _options.AnalyzerConfigOptionsProvider;
            var analyzerConfigOptions = analyzerConfigOptionsProvider.GetOptions(_tree);
            return Task.FromResult<ICodingConventionContext>(new AnalyzerConfigCodingConventionsContext(analyzerConfigOptions));
        }
    }
}
