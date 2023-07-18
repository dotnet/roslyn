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
    internal sealed class WorkspaceAnalyzerOptions(AnalyzerOptions options, IdeAnalyzerOptions ideOptions) : AnalyzerOptions(options.AdditionalFiles, options.AnalyzerConfigOptionsProvider)
    {
        public IdeAnalyzerOptions IdeOptions { get; } = ideOptions;

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return obj is WorkspaceAnalyzerOptions other &&
                IdeOptions == other.IdeOptions &&
                base.Equals(other);
        }

        public override int GetHashCode()
            => Hash.Combine(IdeOptions.GetHashCode(), base.GetHashCode());
    }
}
