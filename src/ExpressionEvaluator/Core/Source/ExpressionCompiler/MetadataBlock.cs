// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    /// <summary>
    /// Module metadata block
    /// </summary>
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    internal struct MetadataBlock : IEquatable<MetadataBlock>
    {
        /// <summary>
        /// Module version id.
        /// </summary>
        internal readonly Guid ModuleVersionId;

        /// <summary>
        /// Module generation id.
        /// </summary>
        internal readonly Guid GenerationId;

        /// <summary>
        /// Pointer to memory block managed by the caller.
        /// </summary>
        internal readonly IntPtr Pointer;

        /// <summary>
        /// Size of memory block.
        /// </summary>
        internal readonly int Size;

        internal MetadataBlock(Guid moduleVersionId, Guid generationId, IntPtr pointer, int size)
        {
            this.ModuleVersionId = moduleVersionId;
            this.GenerationId = generationId;
            this.Pointer = pointer;
            this.Size = size;
        }

        public bool Equals(MetadataBlock other)
        {
            return (this.Pointer == other.Pointer) &&
                (this.Size == other.Size) &&
                (this.ModuleVersionId == other.ModuleVersionId) &&
                (this.GenerationId == other.GenerationId);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is MetadataBlock))
            {
                return false;
            }
            return Equals((MetadataBlock)obj);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(
                Hash.Combine(this.Pointer.GetHashCode(), this.Size),
                Hash.Combine(this.ModuleVersionId.GetHashCode(), this.GenerationId.GetHashCode()));
        }

        private string GetDebuggerDisplay()
        {
            return string.Format("MetadataBlock {{ Mvid = {{{0}}}, Address = {1}, Size = {2} }}", ModuleVersionId, Pointer, Size);
        }
    }
}
