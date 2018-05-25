// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp
{
    // Deconstructions are represented internally as a tree of conversions, but
    // since they are not conversions from the language perspective we use a wrapper to
    // abstract the public API from the implementation.

    /// <summary>
    /// The representation of a deconstruction as a tree of Deconstruct methods and conversions.
    /// Methods only appear in non-terminal nodes. All terminal nodes have a Conversion.
    ///
    /// Here's an example:
    /// A deconstruction like <c>(int x1, (long x2, long x3)) = deconstructable1</c> with
    /// <c>Deconstructable1.Deconstruct(out int y1, out Deconstructable2 y2)</c> and
    /// <c>Deconstructable2.Deconstruct(out int z1, out int z2)</c> is represented as 5 DeconstructionInfo nodes.
    ///
    /// The top-level node has a <see cref="Method"/> (Deconstructable1.Deconstruct), no <see cref="Conversion"/>, but has two <see cref="Nested"/> nodes.
    /// Its first nested node has no <see cref="Method"/>, but has a <see cref="Conversion"/> (Identity).
    /// Its second nested node has a <see cref="Method"/> (Deconstructable2.Deconstruct), no <see cref="Conversion"/>, and two <see cref="Nested"/> nodes.
    /// Those last two nested nodes have no <see cref="Method"/>, but each have a <see cref="Conversion"/> (ImplicitNumeric, from int to long).
    /// </summary>
    public struct DeconstructionInfo
    {
        private readonly Conversion _conversion;

        /// <summary>
        /// The Deconstruct method (if any) for this non-terminal position in the deconstruction tree.
        /// </summary>
        public IMethodSymbol Method
        {
            get
            {
                return _conversion.Kind == ConversionKind.Deconstruction
                    ? _conversion.Method
                    : null;
            }
        }

        /// <summary>
        /// The conversion for a terminal position in the deconstruction tree.
        /// </summary>
        public Conversion? Conversion
        {
            get
            {
                return _conversion.Kind == ConversionKind.Deconstruction
                    ? null
                    : (Conversion?)_conversion;
            }
        }

        /// <summary>
        /// The children for this deconstruction node.
        /// </summary>
        public ImmutableArray<DeconstructionInfo> Nested
        {
            get
            {
                var underlyingConversions = _conversion.UnderlyingConversions;

                return underlyingConversions.IsDefault
                    ? ImmutableArray<DeconstructionInfo>.Empty
                    : underlyingConversions.SelectAsArray(c => new DeconstructionInfo(c));
            }
        }

        internal DeconstructionInfo(Conversion conversion)
        {
            _conversion = conversion;
        }
    }
}
