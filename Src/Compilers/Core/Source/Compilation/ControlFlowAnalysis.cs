// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Provides information about statements which transfer control in and out of a region. This
    /// information is returned from a call to <see cref="M:SemanticModel.AnalyzeControlFlow" />.
    /// </summary>
    public abstract class ControlFlowAnalysis
    {
        /// <summary>
        /// An enumerator for the set of statements inside the region what are the
        /// destination of branches outside the region.
        /// </summary>
        public abstract IEnumerable<SyntaxNode> EntryPoints { get; }

        /// <summary>
        /// An enumerator for the set of statements inside a region that jump to locations outside
        /// the region.
        /// </summary>
        public abstract IEnumerable<SyntaxNode> ExitPoints { get; }

        /// <summary>
        /// Indicates whether a region completes normally. Return true if and only if the end of the
        /// last statement in a region is reachable or the region contains no statements.
        /// </summary>
        public abstract bool EndPointIsReachable { get; }

        public abstract bool StartPointIsReachable { get; }

        /// <summary>
        /// An enumerator for the set of return statements found within a region.
        /// </summary>
        // [Obsolete("The return statements in a region are now included in the result of ExitPoints.", false)]
        public abstract IEnumerable<SyntaxNode> ReturnStatements { get; }

        /// <summary>
        /// Returns true iff analysis was successful.  Analysis can fail if the region does not properly span a single expression,
        /// a single statement, or a contiguous series of statements within the enclosing block.
        /// </summary>
        public abstract bool Succeeded { get; }
    }
}
