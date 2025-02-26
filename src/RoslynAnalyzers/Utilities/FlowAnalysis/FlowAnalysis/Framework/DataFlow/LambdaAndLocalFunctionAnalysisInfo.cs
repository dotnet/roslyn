// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    /// <summary>
    /// Contains information about escaped and analyzed lambda methods and local functions
    /// in context of analyzing its containing method.
    /// </summary>
    public sealed class LambdaAndLocalFunctionAnalysisInfo
    {
        /// <summary>
        /// Local functions that escaped analysis scope of the containing method.
        /// </summary>
        public ImmutableHashSet<IMethodSymbol> EscapedLocalFunctions { get; }

        /// <summary>
        /// Local functions for which interprocedural analysis was performed at least once during analysis of the containing method.
        /// </summary>
        public ImmutableHashSet<IMethodSymbol> AnalyzedLocalFunctions { get; }

        /// <summary>
        /// Lambda methods that escaped analysis scope of the containing method.
        /// </summary>
        public ImmutableHashSet<IFlowAnonymousFunctionOperation> EscapedLambdas { get; }

        /// <summary>
        /// Lambda methods for which interprocedural analysis was performed at least once during analysis of the containing method.
        /// </summary>
        public ImmutableHashSet<IFlowAnonymousFunctionOperation> AnalyzedLambdas { get; }

        internal LambdaAndLocalFunctionAnalysisInfo(
            ImmutableHashSet<IMethodSymbol>.Builder escapedLocalFunctions,
            ImmutableHashSet<IMethodSymbol>.Builder analyzedLocalFunctions,
            ImmutableHashSet<IFlowAnonymousFunctionOperation>.Builder escapedLambdas,
            ImmutableHashSet<IFlowAnonymousFunctionOperation>.Builder analyzedLambdas)
        {
            EscapedLocalFunctions = escapedLocalFunctions.ToImmutable();
            AnalyzedLocalFunctions = analyzedLocalFunctions.ToImmutable();
            EscapedLambdas = escapedLambdas.ToImmutable();
            AnalyzedLambdas = analyzedLambdas.ToImmutable();
        }
    }
}
