// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Summarizes whether a conversion is allowed, and if so, which kind of conversion (and in some cases, the
    /// associated symbol).
    /// </summary>
    public struct Conversion : IEquatable<Conversion>
    {
        internal static readonly Conversion NoConversion = new Conversion(ConversionKind.NoConversion);
        internal static readonly Conversion Identity = new Conversion(ConversionKind.Identity);
        internal static readonly Conversion ImplicitNumeric = new Conversion(ConversionKind.ImplicitNumeric);
        internal static readonly Conversion ImplicitNullable = new Conversion(ConversionKind.ImplicitNullable);
        internal static readonly Conversion ImplicitReference = new Conversion(ConversionKind.ImplicitReference);
        internal static readonly Conversion ImplicitEnumeration = new Conversion(ConversionKind.ImplicitEnumeration);
        internal static readonly Conversion ImplicitThrow = new Conversion(ConversionKind.ImplicitThrow);
        internal static readonly Conversion AnonymousFunction = new Conversion(ConversionKind.AnonymousFunction);
        internal static readonly Conversion Boxing = new Conversion(ConversionKind.Boxing);
        internal static readonly Conversion NullLiteral = new Conversion(ConversionKind.NullLiteral);
        internal static readonly Conversion NullToPointer = new Conversion(ConversionKind.NullToPointer);
        internal static readonly Conversion PointerToVoid = new Conversion(ConversionKind.PointerToVoid);
        internal static readonly Conversion PointerToPointer = new Conversion(ConversionKind.PointerToPointer);
        internal static readonly Conversion PointerToInteger = new Conversion(ConversionKind.PointerToInteger);
        internal static readonly Conversion IntegerToPointer = new Conversion(ConversionKind.IntegerToPointer);
        internal static readonly Conversion Unboxing = new Conversion(ConversionKind.Unboxing);
        internal static readonly Conversion ExplicitReference = new Conversion(ConversionKind.ExplicitReference);
        internal static readonly Conversion IntPtr = new Conversion(ConversionKind.IntPtr);
        internal static readonly Conversion ExplicitEnumeration = new Conversion(ConversionKind.ExplicitEnumeration);
        internal static readonly Conversion ExplicitNullable = new Conversion(ConversionKind.ExplicitNullable);
        internal static readonly Conversion ExplicitNumeric = new Conversion(ConversionKind.ExplicitNumeric);
        internal static readonly Conversion ImplicitDynamic = new Conversion(ConversionKind.ImplicitDynamic);
        internal static readonly Conversion ExplicitDynamic = new Conversion(ConversionKind.ExplicitDynamic);
        internal static readonly Conversion InterpolatedString = new Conversion(ConversionKind.InterpolatedString);

        private readonly MethodSymbol _methodGroupConversionMethod;
        private readonly UserDefinedConversionResult _conversionResult; //no effect on Equals/GetHashCode

        internal readonly ConversionKind Kind;
        private readonly byte _flags;

        private const byte IsExtensionMethodMask = 1 << 0;
        private const byte IsArrayIndexMask = 1 << 1;

        private Conversion(ConversionKind kind, bool isExtensionMethod, bool isArrayIndex, UserDefinedConversionResult conversionResult, MethodSymbol methodGroupConversionMethod)
        {
            this.Kind = kind;
            _conversionResult = conversionResult;
            _methodGroupConversionMethod = methodGroupConversionMethod;

            _flags = isExtensionMethod ? IsExtensionMethodMask : (byte)0;
            if (isArrayIndex)
            {
                _flags |= IsArrayIndexMask;
            }
        }

        internal Conversion(UserDefinedConversionResult conversionResult, bool isImplicit)
            : this()
        {
            this.Kind = conversionResult.Kind == UserDefinedConversionResultKind.NoApplicableOperators
                ? ConversionKind.NoConversion
                : isImplicit ? ConversionKind.ImplicitUserDefined : ConversionKind.ExplicitUserDefined;
            _conversionResult = conversionResult;
        }

        internal Conversion(ConversionKind kind)
            : this()
        {
            this.Kind = kind;
        }

        internal bool IsExtensionMethod
        {
            get
            {
                return (_flags & IsExtensionMethodMask) != 0;
            }
        }

        internal bool IsArrayIndex
        {
            get
            {
                return (_flags & IsArrayIndexMask) != 0;
            }
        }


        internal Conversion ToArrayIndexConversion()
        {
            return new Conversion(this.Kind, this.IsExtensionMethod, true, _conversionResult, _methodGroupConversionMethod);
        }

        // For the method group, lambda and anonymous method conversions
        internal Conversion(ConversionKind kind, MethodSymbol methodGroupConversionMethod, bool isExtensionMethod)
            : this()
        {
            this.Kind = kind;
            _methodGroupConversionMethod = methodGroupConversionMethod;
            if (isExtensionMethod)
            {
                _flags = IsExtensionMethodMask;
            }
        }

        // CONSIDER: public?
        internal bool IsValid
        {
            get
            {
                return this.Exists && (!this.IsUserDefined || (object)this.Method != null || _conversionResult.Kind == UserDefinedConversionResultKind.Valid);
            }
        }

        /// <summary>
        /// Returns true if the conversion exists, either as an implicit or explicit conversion.
        /// </summary>
        /// <remarks>
        /// The existence of a conversion does not necessarily imply that the conversion is valid.
        /// For example, an ambiguous user-defined conversion may exist but may not be valid.
        /// </remarks>
        public bool Exists
        {
            get
            {
                return Kind != ConversionKind.NoConversion;
            }
        }

        /// <summary>
        /// Returns true if the conversion is implicit.
        /// </summary>
        /// <remarks>
        /// Implicit conversions are described in section 6.1 of the C# language specification.
        /// </remarks>
        public bool IsImplicit
        {
            get
            {
                return Kind.IsImplicitConversion();
            }
        }

        /// <summary>
        /// Returns true if the conversion is explicit.
        /// </summary>
        /// <remarks>
        /// Explicit conversions are described in section 6.2 of the C# language specification.
        /// </remarks>
        public bool IsExplicit
        {
            get
            {
                // All conversions are either implicit or explicit.
                return Exists && !IsImplicit;
            }
        }

        /// <summary>
        /// Returns true if the conversion is an identity conversion.
        /// </summary>
        /// <remarks>
        /// Identity conversions are described in section 6.1.1 of the C# language specification.
        /// </remarks>
        public bool IsIdentity
        {
            get
            {
                return Kind == ConversionKind.Identity;
            }
        }

        /// <summary>
        /// Returns true if the conversion is an implicit numeric conversion or explicit numeric conversion. 
        /// </summary>
        /// <remarks>
        /// Implicit and explicit numeric conversions are described in sections 6.1.2 and 6.2.1 of the C# language specification.
        /// </remarks>
        public bool IsNumeric
        {
            get
            {
                return Kind == ConversionKind.ImplicitNumeric || Kind == ConversionKind.ExplicitNumeric;
            }
        }

        /// <summary>
        /// Returns true if the conversion is an implicit enumeration conversion or explicit enumeration conversion.
        /// </summary>
        /// <remarks>
        /// Implicit and explicit enumeration conversions are described in sections 6.1.3 and 6.2.2 of the C# language specification.
        /// </remarks>
        public bool IsEnumeration
        {
            get
            {
                return Kind == ConversionKind.ImplicitEnumeration || Kind == ConversionKind.ExplicitEnumeration;
            }
        }

        // TODO: update the language reference section number below.
        /// <summary>
        /// Returns true if the conversion is an interpolated string conversion.
        /// </summary>
        /// <remarks>
        /// The interpolated string conversion described in section 6.1.N of the C# language specification.
        /// </remarks>
        public bool IsInterpolatedString
        {
            get
            {
                return Kind == ConversionKind.InterpolatedString;
            }
        }

        /// <summary>
        /// Returns true if the conversion is an implicit nullable conversion or explicit nullable conversion.
        /// </summary>
        /// <remarks>
        /// Implicit and explicit nullable conversions are described in sections 6.1.4 and 6.2.3 of the C# language specification.
        /// </remarks>
        public bool IsNullable
        {
            get
            {
                return Kind == ConversionKind.ImplicitNullable || Kind == ConversionKind.ExplicitNullable;
            }
        }

        /// <summary>
        /// Returns true if the conversion is an implicit reference conversion or explicit reference conversion.
        /// </summary>
        /// <remarks>
        /// Implicit and explicit reference conversions are described in sections 6.1.6 and 6.2.4 of the C# language specification.
        /// </remarks>
        public bool IsReference
        {
            get
            {
                return Kind == ConversionKind.ImplicitReference || Kind == ConversionKind.ExplicitReference;
            }
        }

        /// <summary>
        /// Returns true if the conversion is an implicit user-defined conversion or explicit user-defined conversion.
        /// </summary>
        /// <remarks>
        /// Implicit and explicit user-defined conversions are described in section 6.4 of the C# language specification.
        /// </remarks>
        public bool IsUserDefined
        {
            get
            {
                return Kind.IsUserDefinedConversion();
            }
        }

        /// <summary>
        /// Returns true if the conversion is an implicit boxing conversion.
        /// </summary>
        /// <remarks>
        /// Implicit boxing conversions are described in section 6.1.7 of the C# language specification.
        /// </remarks>
        public bool IsBoxing
        {
            get
            {
                return Kind == ConversionKind.Boxing;
            }
        }

        /// <summary>
        /// Returns true if the conversion is an explicit unboxing conversion.
        /// </summary>
        /// <remarks>
        /// Explicit unboxing conversions as described in section 6.2.5 of the C# language specification.
        /// </remarks>
        public bool IsUnboxing
        {
            get
            {
                return Kind == ConversionKind.Unboxing;
            }
        }

        /// <summary>
        /// Returns true if the conversion is an implicit null literal conversion.
        /// </summary>
        /// <remarks>
        /// Null literal conversions are described in section 6.1.5 of the C# language specification.
        /// </remarks>
        public bool IsNullLiteral
        {
            get
            {
                return Kind == ConversionKind.NullLiteral;
            }
        }

        /// <summary>
        /// Returns true if the conversion is an implicit dynamic conversion. 
        /// </summary>
        /// <remarks>
        /// Implicit dynamic conversions are described in section 6.1.8 of the C# language specification.
        /// </remarks>
        public bool IsDynamic
        {
            get
            {
                return Kind.IsDynamic();
            }
        }

        /// <summary>
        /// Returns true if the conversion is an implicit constant expression conversion.
        /// </summary>
        /// <remarks>
        /// Implicit constant expression conversions are described in section 6.1.9 of the C# language specification.
        /// </remarks>
        public bool IsConstantExpression
        {
            get
            {
                return Kind == ConversionKind.ImplicitConstant;
            }
        }

        /// <summary>
        /// Returns true if the conversion is an implicit anonymous function conversion.
        /// </summary>
        /// <remarks>
        /// Implicit anonymous function conversions are described in section 6.5 of the C# language specification.
        /// </remarks>
        public bool IsAnonymousFunction
        {
            get
            {
                return Kind == ConversionKind.AnonymousFunction;
            }
        }

        /// <summary>
        /// Returns true if the conversion is an implicit method group conversion.
        /// </summary>
        /// <remarks>
        /// Implicit method group conversions are described in section 6.6 of the C# language specification.
        /// </remarks>
        public bool IsMethodGroup
        {
            get
            {
                return Kind == ConversionKind.MethodGroup;
            }
        }

        /// <summary>
        /// Returns true if the conversion is a pointer conversion 
        /// </summary>
        /// <remarks>
        /// Pointer conversions are described in section 18.4 of the C# language specification.
        /// 
        /// Returns true if the conversion is a conversion 
        ///  a) from a pointer type to void*, 
        ///  b) from a pointer type to another pointer type (other than void*),
        ///  c) from the null literal to a pointer type,
        ///  d) from an integral numeric type to a pointer type, or
        ///  e) from a pointer type to an integral numeric type.
        /// 
        /// Does not return true for user-defined conversions to/from pointer types.
        /// Does not return true for conversions between pointer types and IntPtr/UIntPtr.
        /// </remarks>
        public bool IsPointer
        {
            get
            {
                return this.Kind.IsPointerConversion();
            }
        }

        /// <summary>
        /// Returns true if the conversion is a conversion to or from IntPtr or UIntPtr.
        /// </summary>
        /// <remarks>
        /// Returns true if the conversion is a conversion to or from IntPtr or UIntPtr.
        /// This includes:
        ///   IntPtr to/from int
        ///   IntPtr to/from long
        ///   IntPtr to/from void*
        ///   UIntPtr to/from int
        ///   UIntPtr to/from long
        ///   UIntPtr to/from void*
        /// </remarks>
        public bool IsIntPtr
        {
            get
            {
                return Kind == ConversionKind.IntPtr;
            }
        }

        internal MethodSymbol Method
        {
            get
            {
                return _methodGroupConversionMethod ?? UserDefinedConversion;
            }
        }

        /// <summary>
        /// Returns the method used to create the delegate for a method group conversion if <see cref="IsMethodGroup"/> is true 
        /// or the method used to perform the conversion for a user-defined conversion if <see cref="IsUserDefined"/> is true.
        /// Otherwise, returns null.
        /// </summary>
        /// <remarks>
        /// Method group conversions are described in section 6.6 of the C# language specification.
        /// User-defined conversions are described in section 6.4 of the C# language specification.
        /// </remarks>
        public IMethodSymbol MethodSymbol
        {
            get
            {
                return this.Method;
            }
        }

        /// <summary>
        /// Gives an indication of how successful the conversion was.
        /// Viable - found a best built-in or user-defined conversion.
        /// Empty - found no applicable built-in or user-defined conversions.
        /// OverloadResolutionFailure - found applicable conversions, but no unique best.
        /// </summary>
        internal LookupResultKind ResultKind
        {
            get
            {
                switch (_conversionResult.Kind)
                {
                    case UserDefinedConversionResultKind.Valid:
                        return LookupResultKind.Viable;
                    case UserDefinedConversionResultKind.Ambiguous:
                    case UserDefinedConversionResultKind.NoBestSourceType:
                    case UserDefinedConversionResultKind.NoBestTargetType:
                        return LookupResultKind.OverloadResolutionFailure;
                    case UserDefinedConversionResultKind.NoApplicableOperators:
                        if (_conversionResult.Results.IsDefaultOrEmpty)
                        {
                            return this.Kind == ConversionKind.NoConversion ? LookupResultKind.Empty : LookupResultKind.Viable;
                        }
                        else
                        {
                            // CONSIDER: indicating an overload resolution failure is sufficient,
                            // but it would be nice to indicate lack of accessibility or other
                            // error conditions.
                            return LookupResultKind.OverloadResolutionFailure;
                        }
                    default:
                        throw ExceptionUtilities.UnexpectedValue(_conversionResult.Kind);
                }
            }
        }

        /// <summary>
        /// Conversion applied to operand of the user-defined conversion.
        /// </summary>
        internal Conversion UserDefinedFromConversion
        {
            get
            {
                UserDefinedConversionAnalysis best = BestUserDefinedConversionAnalysis;
                return best == null ? Conversion.NoConversion : best.SourceConversion;
            }
        }

        /// <summary>
        /// Conversion applied to the result of the user-defined conversion.
        /// </summary>
        internal Conversion UserDefinedToConversion
        {
            get
            {
                UserDefinedConversionAnalysis best = BestUserDefinedConversionAnalysis;
                return best == null ? Conversion.NoConversion : best.TargetConversion;
            }
        }

        /// <summary>
        /// The user-defined operators that were considered when attempting this conversion
        /// (i.e. the arguments to overload resolution).
        /// </summary>
        internal ImmutableArray<MethodSymbol> OriginalUserDefinedConversions
        {
            get
            {
                // If overload resolution has failed then we want to stash away the original methods that we 
                // considered so that the IDE can display tooltips or other information about them.
                // However, if a method group contained a generic method that was type inferred then
                // the IDE wants information about the *inferred* method, not the original unconstructed
                // generic method.

                if (_conversionResult.Kind == UserDefinedConversionResultKind.NoApplicableOperators)
                {
                    return ImmutableArray<MethodSymbol>.Empty;
                }

                var builder = ArrayBuilder<MethodSymbol>.GetInstance();
                foreach (var analysis in _conversionResult.Results)
                {
                    builder.Add(analysis.Operator);
                }
                return builder.ToImmutableAndFree();
            }
        }

        private MethodSymbol UserDefinedConversion
        {
            get
            {
                UserDefinedConversionAnalysis best = BestUserDefinedConversionAnalysis;
                return best == null ? null : best.Operator;
            }
        }

        internal UserDefinedConversionAnalysis BestUserDefinedConversionAnalysis
        {
            get
            {
                if (_conversionResult.Kind == UserDefinedConversionResultKind.Valid)
                {
                    UserDefinedConversionAnalysis analysis = _conversionResult.Results[_conversionResult.Best];
                    return analysis;
                }

                return null;
            }
        }

        /// <summary>
        /// Returns a string that represents the <see cref="Kind"/> of the conversion.
        /// </summary>
        /// <returns>A string that represents the <see cref="Kind"/> of the conversion.</returns>
        public override string ToString()
        {
            return this.Kind.ToString();
        }

        /// <summary>
        /// Determines whether the specified <see cref="Conversion"/> object is equal to the current <see cref="Conversion"/> object.
        /// </summary>
        /// <param name="obj">The <see cref="Conversion"/> object to compare with the current <see cref="Conversion"/> object.</param>
        /// <returns>true if the specified <see cref="Conversion"/> object is equal to the current <see cref="Conversion"/> object; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return obj is Conversion && this.Equals((Conversion)obj);
        }

        /// <summary>
        /// Determines whether the specified <see cref="Conversion"/> object is equal to the current <see cref="Conversion"/> object.
        /// </summary>
        /// <param name="other">The <see cref="Conversion"/> object to compare with the current <see cref="Conversion"/> object.</param>
        /// <returns>true if the specified <see cref="Conversion"/> object is equal to the current <see cref="Conversion"/> object; otherwise, false.</returns>
        public bool Equals(Conversion other)
        {
            return this.Kind == other.Kind && this.Method == other.Method;
        }

        /// <summary>
        /// Returns a hash code for the current <see cref="Conversion"/> object.
        /// </summary>
        /// <returns>A hash code for the current <see cref="Conversion"/> object.</returns>
        public override int GetHashCode()
        {
            return Hash.Combine(this.Method, (int)this.Kind);
        }

        /// <summary>
        /// Returns true if the specified <see cref="Conversion"/> objects are equal and false otherwise.
        /// </summary>
        /// <param name="left">The first <see cref="Conversion"/> object.</param>
        /// <param name="right">The second <see cref="Conversion"/> object.</param>
        /// <returns></returns>
        public static bool operator ==(Conversion left, Conversion right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Returns false if the specified <see cref="Conversion"/> objects are equal and true otherwise.
        /// </summary>
        /// <param name="left">The first <see cref="Conversion"/> object.</param>
        /// <param name="right">The second <see cref="Conversion"/> object.</param>
        /// <returns></returns>
        public static bool operator !=(Conversion left, Conversion right)
        {
            return !(left == right);
        }
    }
}
