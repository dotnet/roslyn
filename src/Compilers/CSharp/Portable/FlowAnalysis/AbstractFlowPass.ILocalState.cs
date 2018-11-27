// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class AbstractFlowPass<TLocalState>
    {
        /// <summary>
        /// This is the "top" state of the data flow lattice. Generally,
        /// it is considered the state which is reachable, but no information
        /// is yet available. This is the state used at the start of method
        /// bodies.
        /// </summary>
        protected abstract TLocalState TopState();

        /// <summary>
        /// This is the absolute "bottom" state of the data flow lattice. C#
        /// does not specify a difference beteween unreachable states, so there
        /// must only be one. This is state used for unreachable code, like
        /// statements after a "return" or "throw" statement.
        /// </summary>
        protected abstract TLocalState UnreachableState();

        /// <summary>
        /// Nontrivial implementation is required for DataFlowsOutWalker or any flow analysis pass that "tracks
        /// unassignments" like the nullable walker. The result should be a state, for each variable, that is
        /// the strongest result possible (i.e. definitely assigned for the data flow passes, or not null for
        /// the nullable analysis).  Slightly more formally, this should be a reachable state that won't affect
        /// another reachable state in a "join" operation.
        /// </summary>
        protected virtual TLocalState ReachableBottomState() => default;

        /// <summary>
        /// The "Join" operation is used when two separate control flow paths converge at a single statment.
        /// This operation is used to combine the if/else paths of a conditional, or two "goto" statements
        /// to the same label, for example.
        /// 
        /// According to convention, Join moves "up" the lattice, so the following equations must hold:
        /// 1. Join(Unreachable(), X) = X
        /// 2. Join(Top, X) = Top
        /// </summary>
        protected abstract bool Join(ref TLocalState self, ref TLocalState other);

        /// <summary>
        /// The Meet operation is the inverse of <see cref="Join(ref TLocalState, ref TLocalState)"/>. 
        /// It's used when combining state additively, like when the state from a return statement
        /// inside a 'try' clause is combined with the end state of a 'finally' clause.
        ///
        /// This moves "down" our flow lattice, by convention.
        /// </summary>
        protected abstract void Meet(ref TLocalState self, ref TLocalState other);

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
