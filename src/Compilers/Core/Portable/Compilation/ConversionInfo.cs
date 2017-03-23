// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Semantics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Summarizes whether a conversion is allowed, and if so, which kind of conversion (and in 
    /// some cases, the associated symbol).
    /// </summary>
    public struct ConversionInfo : IEquatable<ConversionInfo>
    {
        private enum ConversionKind
        {
            Exists =            0b0000000000000001,
            IsIdentity =        0b0000000000000010,
            IsWidening =        0b0000000000000100,
            IsNarrowing =       0b0000000000001000,
            IsNumeric =         0b0000000000010000,
            IsNullable =        0b0000000000100000,
            IsReference =       0b0000000001000000,
            IsDefaultLiteral =  0b0000000010000000,
            IsUserDefined =     0b0000000100000000,
        }

        private readonly ConversionKind _kind;

        internal ConversionInfo(
            bool exists, bool isIdentity, bool isWidening, bool isNarrowing, bool isNumeric,
            bool isNullable, bool isReference, bool isDefaultLiteral, bool isUserDefined,
            IMethodSymbol methodSymbol)
        {
            _kind =
                (exists ? ConversionKind.Exists : 0) |
                (isIdentity ? ConversionKind.IsIdentity : 0) |
                (isWidening ? ConversionKind.IsWidening : 0) |
                (isNarrowing ? ConversionKind.IsNarrowing : 0) |
                (isNumeric ? ConversionKind.IsNumeric : 0) |
                (isNullable ? ConversionKind.IsNullable : 0) |
                (isReference ? ConversionKind.IsReference : 0) |
                (isDefaultLiteral ? ConversionKind.IsDefaultLiteral : 0) |
                (isUserDefined ? ConversionKind.IsUserDefined : 0);

            MethodSymbol = methodSymbol;
        }

        public override bool Equals(object obj)
            => obj is ConversionInfo && Equals((ConversionInfo)obj);

        public bool Equals(ConversionInfo other)
            => _kind == other._kind &&
               EqualityComparer<IMethodSymbol>.Default.Equals(this.MethodSymbol, other.MethodSymbol);

        public override int GetHashCode()
            => Hash.Combine(this.MethodSymbol, (int)_kind);

        public static bool operator ==(ConversionInfo info1, ConversionInfo info2)
            => info1.Equals(info2);

        public static bool operator !=(ConversionInfo info1, ConversionInfo info2)
            => !(info1 == info2);

        private bool HasFlag(ConversionKind kind)
            => (_kind & kind) == kind;

        /// <summary>
        /// Returns true if the conversion exists, either as an <see cref="IsWidening"/> or 
        /// an <see cref="IsNarrowing"/> conversion.
        /// </summary>
        public bool Exists => HasFlag(ConversionKind.Exists);

        /// <summary>
        /// Returns true if the conversion is an identity conversion.
        /// </summary>
        public bool IsIdentity => HasFlag(ConversionKind.IsIdentity);

        /// <summary>
        /// Returns true if the conversion widens from some narrower type to some broader
        /// type.  For example, a conversion from <see cref="System.String"/> to <see cref="System.Object"/>.
        /// A widening conversion may generally appear with or without a
        /// <see cref="IConversionExpression.IsExplicit"/>.  For example, a user in C# could
        /// write both:
        /// 
        /// <code>
        ///     object o1 = ""; // and
        ///     object o2 = (object)"";
        /// </code>
        /// 
        /// These would be both be <see cref="IsWidening"/> conversions, but only the second would
        /// have an <see cref="IConversionExpression.IsExplicit"/>.
        /// 
        /// <see cref="IsWidening"/> conversions can appear with other
        /// conversion types.  For example there are <see cref="IsWidening"/> <see cref="IsReference"/>
        /// conversions, and <see cref="IsWidening"/> <see cref="IsNumeric"/> conversions.
        /// </summary>
        public bool IsWidening => HasFlag(ConversionKind.IsWidening);

        /// <summary>
        /// True if and only if the conversion narrows from some broader type to some 
        /// narrower type.  For example, a conversion from <see cref="System.Object"/> to
        /// <see cref="System.String"/> In general, narrowing conversions appear when there is an
        /// <see cref="IConversionExpression.IsExplicit"/> present.
        /// 
        /// <see cref="IsNarrowing"/> conversions can appear with other
        /// conversion types.  For example there are <see cref="IsNarrowing"/> <see cref="IsReference"/>
        /// conversions, and <see cref="IsNarrowing"/> <see cref="IsNumeric"/> conversions.
        /// </summary>
        public bool IsNarrowing => HasFlag(ConversionKind.IsNarrowing);

        /// <summary>
        /// Returns true if the conversion is an implicit numeric conversion or explicit numeric conversion. 
        /// </summary>
        public bool IsNumeric => HasFlag(ConversionKind.IsNumeric);

        /// <summary>
        /// Returns true if the conversion is an implicit nullable conversion or explicit nullable conversion.
        /// </summary>
        public bool IsNullable => HasFlag(ConversionKind.IsNullable);

        /// <summary>
        /// Returns true if the conversion is an implicit reference conversion or explicit reference conversion.
        /// </summary>
        public bool IsReference => HasFlag(ConversionKind.IsReference);

        /// <summary>
        /// Returns true if the conversion is an implicit default ('null' in C#, 'nothing' in VB) literal conversion.
        /// </summary>
        public bool IsDefaultLiteral => HasFlag(ConversionKind.IsDefaultLiteral);

        /// <summary>
        /// Returns true if the conversion is an implicit user-defined conversion or explicit user-defined conversion.
        /// </summary>
        public bool IsUserDefined => HasFlag(ConversionKind.IsUserDefined);

        /// <summary>
        /// Returns the method that defines the user defined conversion, if any..
        /// </summary>
        public IMethodSymbol MethodSymbol { get; }
    }
}