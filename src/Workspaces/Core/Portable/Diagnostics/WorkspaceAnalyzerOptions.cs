// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
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

        public IdeAnalyzerOptions IdeOptions { get; }

        public WorkspaceAnalyzerOptions(AnalyzerOptions options, Solution solution, IdeAnalyzerOptions ideOptions)
            : base(options.AdditionalFiles, options.AnalyzerConfigOptionsProvider)
        {
            _solution = solution;
            IdeOptions = ideOptions;
        }

        public SolutionServices Services => _solution.Services;

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return obj is WorkspaceAnalyzerOptions other &&
                _solution.WorkspaceVersion == other._solution.WorkspaceVersion &&
                _solution.Workspace == other._solution.Workspace &&
                base.Equals(other);
        }

        public override int GetHashCode()
            => Hash.Combine(_solution.Workspace,
               Hash.Combine(_solution.WorkspaceVersion, base.GetHashCode()));
    }
}
