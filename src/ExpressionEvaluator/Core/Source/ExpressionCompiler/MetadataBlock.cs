// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    /// <summary>
    /// Module metadata block
    /// </summary>
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    internal readonly struct MetadataBlock : IEquatable<MetadataBlock>
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
            ModuleVersionId = moduleVersionId;
            GenerationId = generationId;
            Pointer = pointer;
            Size = size;
        }

        public bool Equals(MetadataBlock other)
        {
            return Pointer == other.Pointer &&
                   Size == other.Size &&
                   ModuleVersionId == other.ModuleVersionId &&
                   GenerationId == other.GenerationId;
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
                Hash.Combine(Pointer.GetHashCode(), Size),
                Hash.Combine(ModuleVersionId.GetHashCode(), GenerationId.GetHashCode()));
        }

        private string GetDebuggerDisplay()
        {
            return string.Format("MetadataBlock {{ Mvid = {{{0}}}, Address = {1}, Size = {2} }}", ModuleVersionId, Pointer, Size);
        }
    }
}
