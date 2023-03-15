// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.Cci
{
    /// <summary>
    /// A range of CLR IL operations that comprise a lexical scope.
    /// </summary>
    internal readonly struct LocalScope
    {
        /// <summary>
        /// The offset of the first operation in the scope.
        /// </summary>
        public readonly int StartOffset;

        /// <summary>
        /// The offset of the first operation outside of the scope, or the method body length.
        /// </summary>
        public readonly int EndOffset;

        private readonly ImmutableArray<ILocalDefinition> _constants;
        private readonly ImmutableArray<ILocalDefinition> _locals;

        internal LocalScope(int offset, int endOffset, ImmutableArray<ILocalDefinition> constants, ImmutableArray<ILocalDefinition> locals)
        {
            Debug.Assert(!locals.Any(l => l.Name == null));
            Debug.Assert(!constants.Any(c => c.Name == null));
            Debug.Assert(offset >= 0);
            Debug.Assert(endOffset > offset);

            StartOffset = offset;
            EndOffset = endOffset;

            _constants = constants;
            _locals = locals;
        }

        public int Length => EndOffset - StartOffset;

        /// <summary>
        /// Returns zero or more local constant definitions that are local to the given scope.
        /// </summary>
        public ImmutableArray<ILocalDefinition> Constants => _constants.NullToEmpty();

        /// <summary>
        /// Returns zero or more local variable definitions that are local to the given scope.
        /// </summary>
        public ImmutableArray<ILocalDefinition> Variables => _locals.NullToEmpty();
    }
}
