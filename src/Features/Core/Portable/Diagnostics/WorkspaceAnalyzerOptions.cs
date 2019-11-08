// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Analyzer options with workspace.
    /// These are used to fetch the workspace options by our internal analyzers (e.g. simplification analyzer).
    /// </summary>
    internal sealed class WorkspaceAnalyzerOptions : AnalyzerOptions
    {
        private readonly Solution _solution;
        private readonly OptionSet _optionSet;

        public WorkspaceAnalyzerOptions(AnalyzerOptions options, OptionSet optionSet, Solution solution)
            : base(options.AdditionalFiles, options.AnalyzerConfigOptionsProvider)
        {
            _solution = solution;
            _optionSet = optionSet;
        }

        public HostWorkspaceServices Services => _solution.Workspace.Services;

        [PerformanceSensitive("https://github.com/dotnet/roslyn/issues/23582", OftenCompletesSynchronously = true)]
        public async ValueTask<OptionSet> GetDocumentOptionSetAsync(SyntaxTree syntaxTree, CancellationToken cancellationToken)
        {
            var documentId = _solution.GetDocumentId(syntaxTree);
            if (documentId == null)
            {
                return _optionSet;
            }

            var document = _solution.GetDocument(documentId);
            if (document == null)
            {
                return _optionSet;
            }

            return await document.GetOptionsAsync(_optionSet, cancellationToken).ConfigureAwait(false);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return obj is WorkspaceAnalyzerOptions { } other && _solution is { WorkspaceVersion: other._solution.WorkspaceVersion, Workspace: other._solution.Workspace } && base.Equals(other);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(_solution.Workspace,
                Hash.Combine(_solution.WorkspaceVersion, base.GetHashCode()));
        }
    }
}
