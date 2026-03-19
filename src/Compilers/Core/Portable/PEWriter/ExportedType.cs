// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Cci
{
    /// <summary>
    /// Info needed when emitting ExportedType table entry.
    /// </summary>
    internal readonly struct ExportedType
    {
        /// <summary>
        /// The target type reference. 
        /// </summary>
        public readonly ITypeReference Type;

        /// <summary>
        /// True if this <see cref="ExportedType"/> represents a type forwarder definition,
        /// false if it represents a type from a linked netmodule.
        /// </summary>
        public readonly bool IsForwarder;

        /// <summary>
        /// If <see cref="Type"/> is a nested type defined in a linked netmodule, 
        /// the index of the <see cref="ExportedType"/> entry that represents the enclosing type.
        /// </summary>
        public readonly int ParentIndex;

        public ExportedType(ITypeReference type, int parentIndex, bool isForwarder)
        {
            Type = type;
            IsForwarder = isForwarder;
            ParentIndex = parentIndex;
        }
    }
}
