// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

namespace Microsoft.Cci
{
    /// <summary>
    /// Info needed when emitting ExportedType table entry.
    /// </summary>
    internal struct ExportedType
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
