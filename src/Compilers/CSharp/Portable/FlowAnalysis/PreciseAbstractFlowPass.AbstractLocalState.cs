// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class PreciseAbstractFlowPass<LocalState>
    {
        protected abstract bool IntersectWith(ref LocalState self, ref LocalState other);

        internal interface AbstractLocalState
        {
            /// <summary>
            /// Produce a duplicate of this flow analysis state.
            /// </summary>
            /// <returns></returns>
            LocalState Clone();

            /// <summary>
            /// Is the code reachable?
            /// </summary>
            bool Reachable { get; }
        }
    }
}
