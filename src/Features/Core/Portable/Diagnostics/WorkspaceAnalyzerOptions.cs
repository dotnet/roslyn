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
        private readonly Workspace _workspace;
        private readonly OptionSet _optionSet;

        public WorkspaceAnalyzerOptions(AnalyzerOptions options, Workspace workspace, OptionSet optionSet)
            : base(options.AdditionalFiles)
        {
            _workspace = workspace;
            _optionSet = optionSet;
        }

        public HostWorkspaceServices Services => _workspace.Services;

        public async Task<OptionSet> GetDocumentOptionSetAsync(SyntaxTree syntaxTree, CancellationToken cancellationToken)
        {
            var documentId = _workspace.CurrentSolution.GetDocumentId(syntaxTree);
            if (documentId == null)
            {
                return _optionSet;
            }

            var document = _workspace.CurrentSolution.GetDocument(documentId);
            if (document == null)
            {
                return _optionSet;
            }

            var documentOptionSet = await document.GetOptionsAsync(_optionSet, cancellationToken).ConfigureAwait(false);
            return documentOptionSet ?? _optionSet;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            var other = obj as WorkspaceAnalyzerOptions;
            return other != null &&
                _workspace == other._workspace &&
                base.Equals(other);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(_workspace, base.GetHashCode());
        }
    }
}
