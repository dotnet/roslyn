// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.Cci
{
    /// <summary>
    /// A range of CLR IL operations that comprise a lexical scope, specified as an IL offset and a length.
    /// </summary>
    internal struct LocalScope
    {
        private readonly uint offset;
        private readonly uint length;
        private readonly ImmutableArray<ILocalDefinition> constants;
        private readonly ImmutableArray<ILocalDefinition> locals;

        internal LocalScope(uint offset, uint length, ImmutableArray<ILocalDefinition> constants, ImmutableArray<ILocalDefinition> locals)
        {
            // We should not create 0-length scopes as they are useless.
            // however we will allow the case of "begin == end" as that is how edge inclusive scopes of length 1 are represented.
            
            this.offset = offset;
            this.length = length;
            this.constants = constants;
            this.locals = locals;
        }

        /// <summary>
        /// The offset of the first operation in the scope.
        /// </summary>
        public uint Offset
        {
            get { return offset; }
        }

        /// <summary>
        /// The length of the scope. Offset+Length equals the offset of the first operation outside the scope, or equals the method body length.
        /// </summary>
        public uint Length
        {
            get { return length; }
        }

        /// <summary>
        /// Returns zero or more local constant definitions that are local to the given scope.
        /// </summary>
        public ImmutableArray<ILocalDefinition> Constants
        {
            get { return constants.IsDefault ? ImmutableArray<ILocalDefinition>.Empty : constants; }
        }

        /// <summary>
        /// Returns zero or more local variable definitions that are local to the given scope.
        /// </summary>
        public ImmutableArray<ILocalDefinition> Variables
        {
            get { return locals.IsDefault ? ImmutableArray<ILocalDefinition>.Empty : locals; }
        }
    }
}
