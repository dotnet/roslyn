// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class PreciseAbstractFlowPass<TLocalState>
    {
        protected abstract bool IntersectWith(ref TLocalState self, ref TLocalState other);

        internal interface ILocalState
        {
            /// <summary>
            /// Produce a duplicate of this flow analysis state.
            /// </summary>
            /// <returns></returns>
            TLocalState Clone();

            /// <summary>
            /// Is the code reachable?
            /// </summary>
            bool Reachable { get; }
        }
    }
}
