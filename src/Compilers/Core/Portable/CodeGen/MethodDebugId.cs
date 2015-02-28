// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGen
{
    /// <summary>
    /// Unique identification of an emitted method used for debugging purposes (EnC).
    /// If the method is synthesized the id is included its name.
    /// For user defined methods the id is included in Custom Debug Information record attached to the method.
    /// </summary>
    internal struct MethodDebugId : IEquatable<MethodDebugId>
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

        public MethodDebugId(int ordinal, int generation)
        {
            Debug.Assert(ordinal >= 0 || ordinal == UndefinedOrdinal);
            Debug.Assert(generation >= 0);

            this.Ordinal = ordinal;
            this.Generation = generation;
        }

        public bool Equals(MethodDebugId other)
        {
            return this.Ordinal == other.Ordinal
                && this.Generation == other.Generation;
        }

        public override bool Equals(object obj)
        {
            return obj is MethodDebugId && Equals((MethodDebugId)obj);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(this.Ordinal, this.Generation);
        }
    }
}
