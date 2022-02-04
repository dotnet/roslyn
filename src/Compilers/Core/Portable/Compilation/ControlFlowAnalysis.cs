// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Provides information about statements which transfer control in and out of a region. This
    /// information is returned from a call to <see cref="SemanticModel.AnalyzeControlFlow(SyntaxNode)" />.
    /// </summary>
    public abstract class ControlFlowAnalysis
    {
        /// <summary>
        /// The set of statements inside the region what are the
        /// destination of branches outside the region.
        /// </summary>
        public abstract ImmutableArray<SyntaxNode> EntryPoints { get; }

        /// <summary>
        /// The set of statements inside a region that jump to locations outside
        /// the region.
        /// </summary>
        public abstract ImmutableArray<SyntaxNode> ExitPoints { get; }

        /// <summary>
        /// Indicates whether a region completes normally. Return true if and only if the end of the
        /// last statement in a region is reachable or the region contains no statements.
        /// </summary>
        public abstract bool EndPointIsReachable { get; }

        public abstract bool StartPointIsReachable { get; }

        /// <summary>
        /// The set of return statements found within a region.
        /// </summary>
        public abstract ImmutableArray<SyntaxNode> ReturnStatements { get; }

        /// <summary>
        /// Returns true if and only if analysis was successful.  Analysis can fail if the region does not properly span a single expression,
        /// a single statement, or a contiguous series of statements within the enclosing block.
        /// </summary>
        public abstract bool Succeeded { get; }
    }
}
