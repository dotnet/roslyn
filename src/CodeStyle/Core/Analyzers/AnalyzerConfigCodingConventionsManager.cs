// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.CodingConventions;

namespace Microsoft.CodeAnalysis
{
    internal class AnalyzerConfigCodingConventionsManager : ICodingConventionsManager
    {
        private readonly SyntaxTree _tree;
        private readonly AnalyzerOptions _options;
        private readonly ICodingConventionsManager? _codingConventionsManager;

        public AnalyzerConfigCodingConventionsManager(SyntaxTree tree, AnalyzerOptions options)
        {
            _tree = tree;
            _options = options;
            if (options.AnalyzerConfigOptionsProvider is null)
            {
                _codingConventionsManager = CodingConventionsManagerFactory.CreateCodingConventionsManager();
            }
        }

        public Task<ICodingConventionContext> GetConventionContextAsync(string filePathContext, CancellationToken cancellationToken)
        {
            if (_codingConventionsManager is object)
            {
                return _codingConventionsManager.GetConventionContextAsync(filePathContext, cancellationToken);
            }

            var analyzerConfigOptionsProvider = _options.AnalyzerConfigOptionsProvider;
            var analyzerConfigOptions = analyzerConfigOptionsProvider.GetOptions(_tree);
            return Task.FromResult<ICodingConventionContext>(new AnalyzerConfigCodingConventionsContext(analyzerConfigOptions));
        }
    }
}
