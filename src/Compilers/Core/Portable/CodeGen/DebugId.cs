// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGen
{
    /// <summary>
    /// Unique identification of an emitted entity (method, lambda, closure) used for debugging purposes (EnC).
    /// </summary>
    /// <remarks>
    /// When used for a synthesized method the ordinal and generation numbers are included its name.
    /// For user defined methods the ordinal is included in Custom Debug Information record attached to the method.
    /// </remarks>
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    internal struct DebugId : IEquatable<DebugId>
    {
        public const int UndefinedOrdinal = -1;

        /// <summary>
        /// The index of the method in member list of the containing type, or <see cref="UndefinedOrdinal"/> if undefined.
        /// </summary>
        public readonly int Ordinal;

        /// <summary>
        /// The EnC generation the method was defined in (0 is the baseline build).
        /// </summary>
        public readonly int Generation;

        public DebugId(int ordinal, int generation)
        {
            Debug.Assert(ordinal >= 0 || ordinal == UndefinedOrdinal);
            Debug.Assert(generation >= 0);

            this.Ordinal = ordinal;
            this.Generation = generation;
        }

        public bool Equals(DebugId other)
        {
            return this.Ordinal == other.Ordinal
                && this.Generation == other.Generation;
        }

        public override bool Equals(object? obj)
        {
            return obj is DebugId && Equals((DebugId)obj);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(this.Ordinal, this.Generation);
        }

        internal string GetDebuggerDisplay()
        {
            return (Generation > 0) ? $"{Ordinal}#{Generation}" : Ordinal.ToString();
        }
    }
}
