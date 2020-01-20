// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        public Task<AnalyzerConfigOptions> GetConventionContextAsync(string filePathContext, CancellationToken cancellationToken)
        {
            var analyzerConfigOptionsProvider = _options.AnalyzerConfigOptionsProvider;
            var analyzerConfigOptions = analyzerConfigOptionsProvider.GetOptions(_tree);
            return Task.FromResult(analyzerConfigOptions);
        }
    }
}
