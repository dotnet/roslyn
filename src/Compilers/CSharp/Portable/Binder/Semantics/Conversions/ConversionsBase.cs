// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract partial class ConversionsBase
    {
        private const int MaximumRecursionDepth = 50;

        protected readonly AssemblySymbol corLibrary;
        protected readonly int currentRecursionDepth;

        internal readonly bool IncludeNullability;

        /// <summary>
        /// An optional clone of this instance with distinct IncludeNullability.
        /// Used to avoid unnecessary allocations when calling WithNullability() repeatedly.
        /// </summary>
        private ConversionsBase _lazyOtherNullability;

        protected ConversionsBase(AssemblySymbol corLibrary, int currentRecursionDepth, bool includeNullability, ConversionsBase otherNullabilityOpt)
        {
            Debug.Assert((object)corLibrary != null);
            Debug.Assert(otherNullabilityOpt == null || includeNullability != otherNullabilityOpt.IncludeNullability);
            Debug.Assert(otherNullabilityOpt == null || currentRecursionDepth == otherNullabilityOpt.currentRecursionDepth);

            this.corLibrary = corLibrary;
            this.currentRecursionDepth = currentRecursionDepth;
            IncludeNullability = includeNullability;
            _lazyOtherNullability = otherNullabilityOpt;
        }

        /// <summary>
        /// Returns this instance if includeNullability is correct, and returns a
        /// cached clone of this instance with distinct IncludeNullability otherwise.
        /// </summary>
        internal ConversionsBase WithNullability(bool includeNullability)
        {
            if (IncludeNullability == includeNullability)
            {
                return this;
            }
            if (_lazyOtherNullability == null)
            {
                Interlocked.CompareExchange(ref _lazyOtherNullability, WithNullabilityCore(includeNullability), null);
            }
            Debug.Assert(_lazyOtherNullability.IncludeNullability == includeNullability);
            Debug.Assert(_lazyOtherNullability._lazyOtherNullability == this);
            return _lazyOtherNullability;
        }

        protected abstract ConversionsBase WithNullabilityCore(bool includeNullability);

        public abstract Conversion GetMethodGroupDelegateConversion(BoundMethodGroup source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics);

        public abstract Conversion GetMethodGroupFunctionPointerConversion(BoundMethodGroup source, FunctionPointerTypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics);

        public abstract Conversion GetStackAllocConversion(BoundStackAllocArrayCreation sourceExpression, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics);

        protected abstract ConversionsBase CreateInstance(int currentRecursionDepth);

        protected abstract Conversion GetInterpolatedStringConversion(BoundInterpolatedString source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics);

        internal AssemblySymbol CorLibrary { get { return corLibrary; } }

        /// <summary>
        /// Determines if the source expression is convertible to the destination type via
        /// any built-in or user-defined implicit conversion.
        /// </summary>
        public Conversion ClassifyImplicitConversionFromExpression(BoundExpression sourceExpression, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert(sourceExpression != null);
            Debug.Assert((object)destination != null);

            var sourceType = sourceExpression.Type;

            //PERF: identity conversion is by far the most common implicit conversion, check for that first
            if ((object)sourceType != null && HasIdentityConversionInternal(sourceType, destination))
            {
                return Conversion.Identity;
            }

            Conversion conversion = ClassifyImplicitBuiltInConversionFromExpression(sourceExpression, sourceType, destination, ref useSiteDiagnostics);
            if (conversion.Exists)
            {
                return conversion;
            }

            if ((object)sourceType != null)
            {
                // Try using the short-circuit "fast-conversion" path.
                Conversion fastConversion = FastClassifyConversion(sourceType, destination);
                if (fastConversion.Exists)
                {
                    if (fastConversion.IsImplicit)
                    {
                        return fastConversion;
                    }
                }
                else
                {
                    conversion = ClassifyImplicitBuiltInConversionSlow(sourceType, destination, ref useSiteDiagnostics);
                    if (conversion.Exists)
                    {
                        return conversion;
                    }
                }
            }

            conversion = GetImplicitUserDefinedConversion(sourceExpression, sourceType, destination, ref useSiteDiagnostics);
            if (conversion.Exists)
            {
                return conversion;
            }

            // The switch expression conversion is "lowest priority", so that if there is a conversion from the expression's
            // type it will be preferred over the switch expression conversion.  Technically, we would want the language
            // specification to say that the switch expression conversion only "exists" if there is no implicit conversion
            // from the type, and we accomplish that by making it lowest priority.  The same is true for the conditional
            // expression conversion.
            conversion = GetSwitchExpressionConversion(sourceExpression, destination, ref useSiteDiagnostics);
            if (conversion.Exists)
            {
                return conversion;
            }
            return GetConditionalExpressionConversion(sourceExpression, destination, ref useSiteDiagnostics);
        }

        /// <summary>
        /// Determines if the source type is convertible to the destination type via
        /// any built-in or user-defined implicit conversion.
        /// </summary>
        public Conversion ClassifyImplicitConversionFromType(TypeSymbol source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert((object)source != null);
            Debug.Assert((object)destination != null);

            //PERF: identity conversions are very common, check for that first.
            if (HasIdentityConversionInternal(source, destination))
            {
                return Conversion.Identity;
            }

            // Try using the short-circuit "fast-conversion" path.
            Conversion fastConversion = FastClassifyConversion(source, destination);
            if (fastConversion.Exists)
            {
                return fastConversion.IsImplicit ? fastConversion : Conversion.NoConversion;
            }
            else
            {
                Conversion conversion = ClassifyImplicitBuiltInConversionSlow(source, destination, ref useSiteDiagnostics);
                if (conversion.Exists)
                {
                    return conversion;
                }
            }

            return GetImplicitUserDefinedConversion(null, source, destination, ref useSiteDiagnostics);
        }

        /// <summary>
        /// Determines if the source expression of given type is convertible to the destination type via
        /// any built-in or user-defined conversion.
        /// 
        /// This helper is used in rare cases involving synthesized expressions where we know the type of an expression, but do not have the actual expression.
        /// The reason for this helper (as opposed to ClassifyConversionFromType) is that conversions from expressions could be different
        /// from conversions from type. For example expressions of dynamic type are implicitly convertable to any type, while dynamic type itself is not.
        /// </summary>
        public Conversion ClassifyConversionFromExpressionType(TypeSymbol source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert((object)source != null);
            Debug.Assert((object)destination != null);

            // since we are converting from expression, we may have implicit dynamic conversion
            if (HasImplicitDynamicConversionFromExpression(source, destination))
            {
                return Conversion.ImplicitDynamic;
            }

            return ClassifyConversionFromType(source, destination, ref useSiteDiagnostics);
        }

        private static bool TryGetVoidConversion(TypeSymbol source, TypeSymbol destination, out Conversion conversion)
        {
            var sourceIsVoid = source?.SpecialType == SpecialType.System_Void;
            var destIsVoid = destination.SpecialType == SpecialType.System_Void;

            // 'void' is not supposed to be able to convert to or from anything, but in practice,
            // a lot of code depends on checking whether an expression of type 'void' is convertible to 'void'.
            // (e.g. for an expression lambda which returns void).
            // Therefore we allow an identity conversion between 'void' and 'void'.
            if (sourceIsVoid && destIsVoid)
            {
                conversion = Conversion.Identity;
                return true;
            }

            // If exactly one of source or destination is of type 'void' then no conversion may exist.
            if (sourceIsVoid || destIsVoid)
            {
                conversion = Conversion.NoConversion;
                return true;
            }

            conversion = default;
            return false;
        }

        /// <summary>
        /// Determines if the source expression is convertible to the destination type via
        /// any conversion: implicit, explicit, user-defined or built-in.
        /// </summary>
        /// <remarks>
        /// It is rare but possible for a source expression to be convertible to a destination type
        /// by both an implicit user-defined conversion and a built-in explicit conversion.
        /// In that circumstance, this method classifies the conversion as the implicit conversion or explicit depending on "forCast"
        /// </remarks>
        public Conversion ClassifyConversionFromExpression(BoundExpression sourceExpression, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics, bool forCast = false)
        {
            Debug.Assert(sourceExpression != null);
            Debug.Assert((object)destination != null);

            if (TryGetVoidConversion(sourceExpression.Type, destination, out var conversion))
            {
                return conversion;
            }

            if (forCast)
            {
                return ClassifyConversionFromExpressionForCast(sourceExpression, destination, ref useSiteDiagnostics);
            }

            var result = ClassifyImplicitConversionFromExpression(sourceExpression, destination, ref useSiteDiagnostics);
            if (result.Exists)
            {
                return result;
            }

            return ClassifyExplicitOnlyConversionFromExpression(sourceExpression, destination, ref useSiteDiagnostics, forCast: false);
        }

        /// <summary>
        /// Determines if the source type is convertible to the destination type via
        /// any conversion: implicit, explicit, user-defined or built-in.
        /// </summary>
        /// <remarks>
        /// It is rare but possible for a source type to be convertible to a destination type
        /// by both an implicit user-defined conversion and a built-in explicit conversion.
        /// In that circumstance, this method classifies the conversion as the implicit conversion or explicit depending on "forCast"
        /// </remarks>
        public Conversion ClassifyConversionFromType(TypeSymbol source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics, bool forCast = false)
        {
            Debug.Assert((object)source != null);
            Debug.Assert((object)destination != null);

            if (TryGetVoidConversion(source, destination, out var voidConversion))
            {
                return voidConversion;
            }

            if (forCast)
            {
                return ClassifyConversionFromTypeForCast(source, destination, ref useSiteDiagnostics);
            }

            // Try using the short-circuit "fast-conversion" path.
            Conversion fastConversion = FastClassifyConversion(source, destination);
            if (fastConversion.Exists)
            {
                return fastConversion;
            }
            else
            {
                Conversion conversion1 = ClassifyImplicitBuiltInConversionSlow(source, destination, ref useSiteDiagnostics);
                if (conversion1.Exists)
                {
                    return conversion1;
                }
            }

            Conversion conversion = GetImplicitUserDefinedConversion(null, source, destination, ref useSiteDiagnostics);
            if (conversion.Exists)
            {
                return conversion;
            }

            conversion = ClassifyExplicitBuiltInOnlyConversion(source, destination, ref useSiteDiagnostics, forCast: false);
            if (conversion.Exists)
            {
                return conversion;
            }

            return GetExplicitUserDefinedConversion(null, source, destination, ref useSiteDiagnostics);
        }

        /// <summary>
        /// Determines if the source expression is convertible to the destination type via
        /// any conversion: implicit, explicit, user-defined or built-in.
        /// </summary>
        /// <remarks>
        /// It is rare but possible for a source expression to be convertible to a destination type
        /// by both an implicit user-defined conversion and a built-in explicit conversion.
        /// In that circumstance, this method classifies the conversion as the built-in conversion.
        /// 
        /// An implicit conversion exists from an expression of a dynamic type to any type.
        /// An explicit conversion exists from a dynamic type to any type. 
        /// When casting we prefer the explicit conversion.
        /// </remarks>
        private Conversion ClassifyConversionFromExpressionForCast(BoundExpression source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert(source != null);
            Debug.Assert((object)destination != null);

            Conversion implicitConversion = ClassifyImplicitConversionFromExpression(source, destination, ref useSiteDiagnostics);
            if (implicitConversion.Exists && !ExplicitConversionMayDifferFromImplicit(implicitConversion))
            {
                return implicitConversion;
            }

            Conversion explicitConversion = ClassifyExplicitOnlyConversionFromExpression(source, destination, ref useSiteDiagnostics, forCast: true);
            if (explicitConversion.Exists)
            {
                return explicitConversion;
            }

            // It is possible for a user-defined conversion to be unambiguous when considered as
            // an implicit conversion and ambiguous when considered as an explicit conversion.
            // The native compiler does not check to see if a cast could be successfully bound as
            // an unambiguous user-defined implicit conversion; it goes right to the ambiguous
            // user-defined explicit conversion and produces an error. This means that in
            // C# 5 it is possible to have:
            //
            // Y y = new Y();
            // Z z1 = y;
            // 
            // succeed but
            //
            // Z z2 = (Z)y;
            //
            // fail.
            //
            // However, there is another interesting wrinkle. It is possible for both
            // an implicit user-defined conversion and an explicit user-defined conversion
            // to exist and be unambiguous. For example, if there is an implicit conversion
            // double-->C and an explicit conversion from int-->C, and the user casts a short
            // to C, then both the implicit and explicit conversions are applicable and
            // unambiguous. The native compiler in this case prefers the explicit conversion,
            // and for backwards compatibility, we match it.

            return implicitConversion;
        }

        /// <summary>
        /// Determines if the source type is convertible to the destination type via
        /// any conversion: implicit, explicit, user-defined or built-in.
        /// </summary>
        /// <remarks>
        /// It is rare but possible for a source type to be convertible to a destination type
        /// by both an implicit user-defined conversion and a built-in explicit conversion.
        /// In that circumstance, this method classifies the conversion as the built-in conversion.
        /// </remarks>
        private Conversion ClassifyConversionFromTypeForCast(TypeSymbol source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert((object)source != null);
            Debug.Assert((object)destination != null);

            // Try using the short-circuit "fast-conversion" path.
            Conversion fastConversion = FastClassifyConversion(source, destination);
            if (fastConversion.Exists)
            {
                return fastConversion;
            }

            Conversion implicitBuiltInConversion = ClassifyImplicitBuiltInConversionSlow(source, destination, ref useSiteDiagnostics);
            if (implicitBuiltInConversion.Exists && !ExplicitConversionMayDifferFromImplicit(implicitBuiltInConversion))
            {
                return implicitBuiltInConversion;
            }

            Conversion explicitBuiltInConversion = ClassifyExplicitBuiltInOnlyConversion(source, destination, ref useSiteDiagnostics, forCast: true);
            if (explicitBuiltInConversion.Exists)
            {
                return explicitBuiltInConversion;
            }

            if (implicitBuiltInConversion.Exists)
            {
                return implicitBuiltInConversion;
            }

            // It is possible for a user-defined conversion to be unambiguous when considered as
            // an implicit conversion and ambiguous when considered as an explicit conversion.
            // The native compiler does not check to see if a cast could be successfully bound as
            // an unambiguous user-defined implicit conversion; it goes right to the ambiguous
            // user-defined explicit conversion and produces an error. This means that in
            // C# 5 it is possible to have:
            //
            // Y y = new Y();
            // Z z1 = y;
            // 
            // succeed but
            //
            // Z z2 = (Z)y;
            //
            // fail.

            var conversion = GetExplicitUserDefinedConversion(null, source, destination, ref useSiteDiagnostics);
            if (conversion.Exists)
            {
                return conversion;
            }

            return GetImplicitUserDefinedConversion(null, source, destination, ref useSiteDiagnostics);
        }

        /// <summary>
        /// Attempt a quick classification of builtin conversions.  As result of "no conversion"
        /// means that there is no built-in conversion, though there still may be a user-defined
        /// conversion if compiling against a custom mscorlib.
        /// </summary>
        public static Conversion FastClassifyConversion(TypeSymbol source, TypeSymbol target)
        {
            ConversionKind convKind = ConversionEasyOut.ClassifyConversion(source, target);
            if (convKind != ConversionKind.ImplicitNullable && convKind != ConversionKind.ExplicitNullable)
            {
                return Conversion.GetTrivialConversion(convKind);
            }

            return Conversion.MakeNullableConversion(convKind, FastClassifyConversion(source.StrippedType(), target.StrippedType()));
        }

        public Conversion ClassifyBuiltInConversion(TypeSymbol source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert((object)source != null);
            Debug.Assert((object)destination != null);

            // Try using the short-circuit "fast-conversion" path.
            Conversion fastConversion = FastClassifyConversion(source, destination);
            if (fastConversion.Exists)
            {
                return fastConversion;
            }
            else
            {
                Conversion conversion = ClassifyImplicitBuiltInConversionSlow(source, destination, ref useSiteDiagnostics);
                if (conversion.Exists)
                {
                    return conversion;
                }
            }

            return ClassifyExplicitBuiltInOnlyConversion(source, destination, ref useSiteDiagnostics, forCast: false);
        }

        /// <summary>
        /// Determines if the source type is convertible to the destination type via
        /// any standard implicit or standard explicit conversion.
        /// </summary>
        /// <remarks>
        /// Not all built-in explicit conversions are standard explicit conversions.
        /// </remarks>
        public Conversion ClassifyStandardConversion(BoundExpression sourceExpression, TypeSymbol source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert(sourceExpression != null || (object)source != null);
            Debug.Assert((object)destination != null);

            // Note that the definition of explicit standard conversion does not include all explicit
            // reference conversions! There is a standard implicit reference conversion from
            // Action<Object> to Action<Exception>, thanks to contravariance. There is a standard
            // implicit reference conversion from Action<Object> to Action<String> for the same reason.
            // Therefore there is an explicit reference conversion from Action<Exception> to
            // Action<String>; a given Action<Exception> might be an Action<Object>, and hence
            // convertible to Action<String>.  However, this is not a *standard* explicit conversion. The
            // standard explicit conversions are all the standard implicit conversions and their
            // opposites. Therefore Action<Object>-->Action<String> and Action<String>-->Action<Object>
            // are both standard conversions. But Action<String>-->Action<Exception> is not a standard
            // explicit conversion because neither it nor its opposite is a standard implicit
            // conversion.
            //
            // Similarly, there is no standard explicit conversion from double to decimal, because
            // there is no standard implicit conversion between the two types.

            // SPEC: The standard explicit conversions are all standard implicit conversions plus 
            // SPEC: the subset of the explicit conversions for which an opposite standard implicit 
            // SPEC: conversion exists. In other words, if a standard implicit conversion exists from
            // SPEC: a type A to a type B, then a standard explicit conversion exists from type A to 
            // SPEC: type B and from type B to type A.

            Conversion conversion = ClassifyStandardImplicitConversion(sourceExpression, source, destination, ref useSiteDiagnostics);
            if (conversion.Exists)
            {
                return conversion;
            }

            if ((object)source != null)
            {
                return DeriveStandardExplicitFromOppositeStandardImplicitConversion(source, destination, ref useSiteDiagnostics);
            }

            return Conversion.NoConversion;
        }

        private Conversion ClassifyStandardImplicitConversion(BoundExpression sourceExpression, TypeSymbol source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert(sourceExpression != null || (object)source != null);
            Debug.Assert(sourceExpression == null || (object)sourceExpression.Type == (object)source);
            Debug.Assert((object)destination != null);

            // SPEC: The following implicit conversions are classified as standard implicit conversions:
            // SPEC: Identity conversions
            // SPEC: Implicit numeric conversions
            // SPEC: Implicit nullable conversions
            // SPEC: Implicit reference conversions
            // SPEC: Boxing conversions
            // SPEC: Implicit constant expression conversions
            // SPEC: Implicit conversions involving type parameters
            //
            // and in unsafe code:
            //
            // SPEC: From any pointer type to void*
            //
            // SPEC ERROR: 
            // The specification does not say to take into account the conversion from
            // the *expression*, only its *type*. But the expression may not have a type
            // (because it is null, a method group, or a lambda), or the expression might
            // be convertible to the destination type via a constant numeric conversion.
            // For example, the native compiler allows "C c = 1;" to work if C is a class which
            // has an implicit conversion from byte to C, despite the fact that there is
            // obviously no standard implicit conversion from *int* to *byte*. 
            // Similarly, if a struct S has an implicit conversion from string to S, then
            // "S s = null;" should be allowed. 
            // 
            // We extend the definition of standard implicit conversions to include
            // all of the implicit conversions that are allowed based on an expression,
            // with the exception of the switch expression conversion.

            Conversion conversion = ClassifyImplicitBuiltInConversionFromExpression(sourceExpression, source, destination, ref useSiteDiagnostics);
            if (conversion.Exists)
            {
                return conversion;
            }

            if ((object)source != null)
            {
                return ClassifyStandardImplicitConversion(source, destination, ref useSiteDiagnostics);
            }

            return Conversion.NoConversion;
        }

        private Conversion ClassifyStandardImplicitConversion(TypeSymbol source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert((object)source != null);
            Debug.Assert((object)destination != null);

            if (HasIdentityConversionInternal(source, destination))
            {
                return Conversion.Identity;
            }

            if (HasImplicitNumericConversion(source, destination))
            {
                return Conversion.ImplicitNumeric;
            }

            var nullableConversion = ClassifyImplicitNullableConversion(source, destination, ref useSiteDiagnostics);
            if (nullableConversion.Exists)
            {
                return nullableConversion;
            }

            if (HasImplicitReferenceConversion(source, destination, ref useSiteDiagnostics))
            {
                return Conversion.ImplicitReference;
            }

            if (HasBoxingConversion(source, destination, ref useSiteDiagnostics))
            {
                return Conversion.Boxing;
            }

            if (HasImplicitPointerToVoidConversion(source, destination))
            {
                return Conversion.PointerToVoid;
            }

            if (HasImplicitPointerConversion(source, destination, ref useSiteDiagnostics))
            {
                return Conversion.ImplicitPointer;
            }

            var tupleConversion = ClassifyImplicitTupleConversion(source, destination, ref useSiteDiagnostics);
            if (tupleConversion.Exists)
            {
                return tupleConversion;
            }

            return Conversion.NoConversion;
        }

        private Conversion ClassifyImplicitBuiltInConversionSlow(TypeSymbol source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert((object)source != null);
            Debug.Assert((object)destination != null);

            if (source.IsVoidType() || destination.IsVoidType())
            {
                return Conversion.NoConversion;
            }

            Conversion conversion = ClassifyStandardImplicitConversion(source, destination, ref useSiteDiagnostics);
            if (conversion.Exists)
            {
                return conversion;
            }

            return Conversion.NoConversion;
        }

        private Conversion GetImplicitUserDefinedConversion(BoundExpression sourceExpression, TypeSymbol source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            var conversionResult = AnalyzeImplicitUserDefinedConversions(sourceExpression, source, destination, ref useSiteDiagnostics);
            return new Conversion(conversionResult, isImplicit: true);
        }

        private Conversion ClassifyExplicitBuiltInOnlyConversion(TypeSymbol source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics, bool forCast)
        {
            Debug.Assert((object)source != null);
            Debug.Assert((object)destination != null);

            if (source.IsVoidType() || destination.IsVoidType())
            {
                return Conversion.NoConversion;
            }

            // The call to HasExplicitNumericConversion isn't necessary, because it is always tested
            // already by the "FastConversion" code.
            Debug.Assert(!HasExplicitNumericConversion(source, destination));

            //if (HasExplicitNumericConversion(source, specialTypeSource, destination, specialTypeDest))
            //{
            //    return Conversion.ExplicitNumeric;
            //}

            if (HasSpecialIntPtrConversion(source, destination))
            {
                return Conversion.IntPtr;
            }

            if (HasExplicitEnumerationConversion(source, destination))
            {
                return Conversion.ExplicitEnumeration;
            }

            var nullableConversion = ClassifyExplicitNullableConversion(source, destination, ref useSiteDiagnostics, forCast);
            if (nullableConversion.Exists)
            {
                return nullableConversion;
            }

            if (HasExplicitReferenceConversion(source, destination, ref useSiteDiagnostics))
            {
                return (source.Kind == SymbolKind.DynamicType) ? Conversion.ExplicitDynamic : Conversion.ExplicitReference;
            }

            if (HasUnboxingConversion(source, destination, ref useSiteDiagnostics))
            {
                return Conversion.Unboxing;
            }

            var tupleConversion = ClassifyExplicitTupleConversion(source, destination, ref useSiteDiagnostics, forCast);
            if (tupleConversion.Exists)
            {
                return tupleConversion;
            }

            if (HasPointerToPointerConversion(source, destination))
            {
                return Conversion.PointerToPointer;
            }

            if (HasPointerToIntegerConversion(source, destination))
            {
                return Conversion.PointerToInteger;
            }

            if (HasIntegerToPointerConversion(source, destination))
            {
                return Conversion.IntegerToPointer;
            }

            if (HasExplicitDynamicConversion(source, destination))
            {
                return Conversion.ExplicitDynamic;
            }

            return Conversion.NoConversion;
        }

        private Conversion GetExplicitUserDefinedConversion(BoundExpression sourceExpression, TypeSymbol source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            UserDefinedConversionResult conversionResult = AnalyzeExplicitUserDefinedConversions(sourceExpression, source, destination, ref useSiteDiagnostics);
            return new Conversion(conversionResult, isImplicit: false);
        }

        private Conversion DeriveStandardExplicitFromOppositeStandardImplicitConversion(TypeSymbol source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            var oppositeConversion = ClassifyStandardImplicitConversion(destination, source, ref useSiteDiagnostics);
            Conversion impliedExplicitConversion;

            switch (oppositeConversion.Kind)
            {
                case ConversionKind.Identity:
                    impliedExplicitConversion = Conversion.Identity;
                    break;
                case ConversionKind.ImplicitNumeric:
                    impliedExplicitConversion = Conversion.ExplicitNumeric;
                    break;
                case ConversionKind.ImplicitReference:
                    impliedExplicitConversion = Conversion.ExplicitReference;
                    break;
                case ConversionKind.Boxing:
                    impliedExplicitConversion = Conversion.Unboxing;
                    break;
                case ConversionKind.NoConversion:
                    impliedExplicitConversion = Conversion.NoConversion;
                    break;
                case ConversionKind.ImplicitPointerToVoid:
                    impliedExplicitConversion = Conversion.PointerToPointer;
                    break;

                case ConversionKind.ImplicitTuple:
                    // only implicit tuple conversions are standard conversions, 
                    // having implicit conversion in the other direction does not help here.
                    impliedExplicitConversion = Conversion.NoConversion;
                    break;

                case ConversionKind.ImplicitNullable:
                    var strippedSource = source.StrippedType();
                    var strippedDestination = destination.StrippedType();
                    var underlyingConversion = DeriveStandardExplicitFromOppositeStandardImplicitConversion(strippedSource, strippedDestination, ref useSiteDiagnostics);

                    // the opposite underlying conversion may not exist 
                    // for example if underlying conversion is implicit tuple
                    impliedExplicitConversion = underlyingConversion.Exists ?
                        Conversion.MakeNullableConversion(ConversionKind.ExplicitNullable, underlyingConversion) :
                        Conversion.NoConversion;

                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(oppositeConversion.Kind);
            }

            return impliedExplicitConversion;
        }

        /// <summary>
        /// IsBaseInterface returns true if baseType is on the base interface list of derivedType or
        /// any base class of derivedType. It may be on the base interface list either directly or
        /// indirectly.
        /// * baseType must be an interface.
        /// * type parameters do not have base interfaces. (They have an "effective interface list".)
        /// * an interface is not a base of itself.
        /// * this does not check for variance conversions; if a type inherits from
        ///   IEnumerable&lt;string> then IEnumerable&lt;object> is not a base interface.
        /// </summary>
        public bool IsBaseInterface(TypeSymbol baseType, TypeSymbol derivedType, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert((object)baseType != null);
            Debug.Assert((object)derivedType != null);

            if (!baseType.IsInterfaceType())
            {
                return false;
            }

            var d = derivedType as NamedTypeSymbol;
            if ((object)d == null)
            {
                return false;
            }

            foreach (var iface in d.AllInterfacesWithDefinitionUseSiteDiagnostics(ref useSiteDiagnostics))
            {
                if (HasIdentityConversionInternal(iface, baseType))
                {
                    return true;
                }
            }

            return false;
        }

        // IsBaseClass returns true if and only if baseType is a base class of derivedType, period.
        //
        // * interfaces do not have base classes. (Structs, enums and classes other than object do.)
        // * a class is not a base class of itself
        // * type parameters do not have base classes. (They have "effective base classes".)
        // * all base classes must be classes
        // * dynamics are removed; if we have class D : B<dynamic> then B<object> is a 
        //   base class of D. However, dynamic is never a base class of anything.
        public bool IsBaseClass(TypeSymbol derivedType, TypeSymbol baseType, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert((object)derivedType != null);
            Debug.Assert((object)baseType != null);

            // A base class has got to be a class. The derived type might be a struct, enum, or delegate.
            if (!baseType.IsClassType())
            {
                return false;
            }

            for (TypeSymbol b = derivedType.BaseTypeWithDefinitionUseSiteDiagnostics(ref useSiteDiagnostics); (object)b != null; b = b.BaseTypeWithDefinitionUseSiteDiagnostics(ref useSiteDiagnostics))
            {
                if (HasIdentityConversionInternal(b, baseType))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// returns true when implicit conversion is not necessarily the same as explicit conversion
        /// </summary>
        private static bool ExplicitConversionMayDifferFromImplicit(Conversion implicitConversion)
        {
            switch (implicitConversion.Kind)
            {
                case ConversionKind.ImplicitUserDefined:
                case ConversionKind.ImplicitDynamic:
                case ConversionKind.ImplicitTuple:
                case ConversionKind.ImplicitTupleLiteral:
                case ConversionKind.ImplicitNullable:
                case ConversionKind.ConditionalExpression:
                    return true;

                default:
                    return false;
            }
        }

        private Conversion ClassifyImplicitBuiltInConversionFromExpression(BoundExpression sourceExpression, TypeSymbol source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert(sourceExpression != null || (object)source != null);
            Debug.Assert(sourceExpression == null || (object)sourceExpression.Type == (object)source);
            Debug.Assert((object)destination != null);

            if (HasImplicitDynamicConversionFromExpression(source, destination))
            {
                return Conversion.ImplicitDynamic;
            }

            // The following conversions only exist for certain form of expressions, 
            // if we have no expression none if them is applicable.
            if (sourceExpression == null)
            {
                return Conversion.NoConversion;
            }

            if (HasImplicitEnumerationConversion(sourceExpression, destination))
            {
                return Conversion.ImplicitEnumeration;
            }

            var constantConversion = ClassifyImplicitConstantExpressionConversion(sourceExpression, destination);
            if (constantConversion.Exists)
            {
                return constantConversion;
            }

            switch (sourceExpression.Kind)
            {
                case BoundKind.Literal:
                    var nullLiteralConversion = ClassifyNullLiteralConversion(sourceExpression, destination);
                    if (nullLiteralConversion.Exists)
                    {
                        return nullLiteralConversion;
                    }
                    break;

                case BoundKind.DefaultLiteral:
                    return Conversion.DefaultLiteral;

                case BoundKind.ExpressionWithNullability:
                    {
                        var innerExpression = ((BoundExpressionWithNullability)sourceExpression).Expression;
                        var innerConversion = ClassifyImplicitBuiltInConversionFromExpression(innerExpression, innerExpression.Type, destination, ref useSiteDiagnostics);
                        if (innerConversion.Exists)
                        {
                            return innerConversion;
                        }
                        break;
                    }
                case BoundKind.TupleLiteral:
                    var tupleConversion = ClassifyImplicitTupleLiteralConversion((BoundTupleLiteral)sourceExpression, destination, ref useSiteDiagnostics);
                    if (tupleConversion.Exists)
                    {
                        return tupleConversion;
                    }
                    break;

                case BoundKind.UnboundLambda:
                    if (HasAnonymousFunctionConversion(sourceExpression, destination))
                    {
                        return Conversion.AnonymousFunction;
                    }
                    break;

                case BoundKind.MethodGroup:
                    Conversion methodGroupConversion = GetMethodGroupDelegateConversion((BoundMethodGroup)sourceExpression, destination, ref useSiteDiagnostics);
                    if (methodGroupConversion.Exists)
                    {
                        return methodGroupConversion;
                    }
                    break;

                case BoundKind.InterpolatedString:
                    Conversion interpolatedStringConversion = GetInterpolatedStringConversion((BoundInterpolatedString)sourceExpression, destination, ref useSiteDiagnostics);
                    if (interpolatedStringConversion.Exists)
                    {
                        return interpolatedStringConversion;
                    }
                    break;

                case BoundKind.StackAllocArrayCreation:
                    var stackAllocConversion = GetStackAllocConversion((BoundStackAllocArrayCreation)sourceExpression, destination, ref useSiteDiagnostics);
                    if (stackAllocConversion.Exists)
                    {
                        return stackAllocConversion;
                    }
                    break;

                case BoundKind.UnconvertedAddressOfOperator when destination is FunctionPointerTypeSymbol funcPtrType:
                    var addressOfConversion = GetMethodGroupFunctionPointerConversion(((BoundUnconvertedAddressOfOperator)sourceExpression).Operand, funcPtrType, ref useSiteDiagnostics);
                    if (addressOfConversion.Exists)
                    {
                        return addressOfConversion;
                    }
                    break;

                case BoundKind.ThrowExpression:
                    return Conversion.ImplicitThrow;

                case BoundKind.UnconvertedObjectCreationExpression:
                    return Conversion.ObjectCreation;
            }

            return Conversion.NoConversion;
        }

        private Conversion GetSwitchExpressionConversion(BoundExpression source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            switch (source)
            {
                case BoundConvertedSwitchExpression _:
                    // It has already been subjected to a switch expression conversion.
                    return Conversion.NoConversion;
                case BoundUnconvertedSwitchExpression switchExpression:
                    var innerConversions = ArrayBuilder<Conversion>.GetInstance(switchExpression.SwitchArms.Length);
                    foreach (var arm in switchExpression.SwitchArms)
                    {
                        var nestedConversion = this.ClassifyImplicitConversionFromExpression(arm.Value, destination, ref useSiteDiagnostics);
                        if (!nestedConversion.Exists)
                        {
                            innerConversions.Free();
                            return Conversion.NoConversion;
                        }

                        innerConversions.Add(nestedConversion);
                    }

                    return Conversion.MakeSwitchExpression(innerConversions.ToImmutableAndFree());
                default:
                    return Conversion.NoConversion;
            }
        }

        private Conversion GetConditionalExpressionConversion(BoundExpression source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            if (!(source is BoundUnconvertedConditionalOperator conditionalOperator))
                return Conversion.NoConversion;

            var trueConversion = this.ClassifyImplicitConversionFromExpression(conditionalOperator.Consequence, destination, ref useSiteDiagnostics);
            if (!trueConversion.Exists)
                return Conversion.NoConversion;

            var falseConversion = this.ClassifyImplicitConversionFromExpression(conditionalOperator.Alternative, destination, ref useSiteDiagnostics);
            if (!falseConversion.Exists)
                return Conversion.NoConversion;

            return Conversion.MakeConditionalExpression(ImmutableArray.Create(trueConversion, falseConversion));
        }

        private static Conversion ClassifyNullLiteralConversion(BoundExpression source, TypeSymbol destination)
        {
            Debug.Assert((object)source != null);
            Debug.Assert((object)destination != null);

            if (!source.IsLiteralNull())
            {
                return Conversion.NoConversion;
            }

            // SPEC: An implicit conversion exists from the null literal to any nullable type. 
            if (destination.IsNullableType())
            {
                // The spec defines a "null literal conversion" specifically as a conversion from
                // null to nullable type.
                return Conversion.NullLiteral;
            }

            // SPEC: An implicit conversion exists from the null literal to any reference type. 
            // SPEC: An implicit conversion exists from the null literal to type parameter T, 
            // SPEC: provided T is known to be a reference type. [...] The conversion [is] classified 
            // SPEC: as implicit reference conversion. 

            if (destination.IsReferenceType)
            {
                return Conversion.ImplicitReference;
            }

            // SPEC: The set of implicit conversions is extended to include...
            // SPEC: ... from the null literal to any pointer type.

            if (destination.IsPointerOrFunctionPointer())
            {
                return Conversion.NullToPointer;
            }

            return Conversion.NoConversion;
        }

        private static Conversion ClassifyImplicitConstantExpressionConversion(BoundExpression source, TypeSymbol destination)
        {
            if (HasImplicitConstantExpressionConversion(source, destination))
            {
                return Conversion.ImplicitConstant;
            }

            // strip nullable from the destination
            //
            // the following should work and it is an ImplicitNullable conversion
            //    int? x = 1;
            if (destination.Kind == SymbolKind.NamedType)
            {
                var nt = (NamedTypeSymbol)destination;
                if (nt.OriginalDefinition.GetSpecialTypeSafe() == SpecialType.System_Nullable_T &&
                    HasImplicitConstantExpressionConversion(source, nt.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[0].Type))
                {
                    return new Conversion(ConversionKind.ImplicitNullable, Conversion.ImplicitConstantUnderlying);
                }
            }

            return Conversion.NoConversion;
        }

        private Conversion ClassifyImplicitTupleLiteralConversion(BoundTupleLiteral source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            var tupleConversion = GetImplicitTupleLiteralConversion(source, destination, ref useSiteDiagnostics);
            if (tupleConversion.Exists)
            {
                return tupleConversion;
            }

            // strip nullable from the destination
            //
            // the following should work and it is an ImplicitNullable conversion
            //    (int, double)? x = (1,2);
            if (destination.Kind == SymbolKind.NamedType)
            {
                var nt = (NamedTypeSymbol)destination;
                if (nt.OriginalDefinition.GetSpecialTypeSafe() == SpecialType.System_Nullable_T)
                {
                    var underlyingTupleConversion = GetImplicitTupleLiteralConversion(source, nt.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[0].Type, ref useSiteDiagnostics);

                    if (underlyingTupleConversion.Exists)
                    {
                        return new Conversion(ConversionKind.ImplicitNullable, ImmutableArray.Create(underlyingTupleConversion));
                    }
                }
            }

            return Conversion.NoConversion;
        }

        private Conversion ClassifyExplicitTupleLiteralConversion(BoundTupleLiteral source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics, bool forCast)
        {
            var tupleConversion = GetExplicitTupleLiteralConversion(source, destination, ref useSiteDiagnostics, forCast);
            if (tupleConversion.Exists)
            {
                return tupleConversion;
            }

            // strip nullable from the destination
            //
            // the following should work and it is an ExplicitNullable conversion
            //    var x = ((byte, string)?)(1,null);
            if (destination.Kind == SymbolKind.NamedType)
            {
                var nt = (NamedTypeSymbol)destination;
                if (nt.OriginalDefinition.GetSpecialTypeSafe() == SpecialType.System_Nullable_T)
                {
                    var underlyingTupleConversion = GetExplicitTupleLiteralConversion(source, nt.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[0].Type, ref useSiteDiagnostics, forCast);

                    if (underlyingTupleConversion.Exists)
                    {
                        return new Conversion(ConversionKind.ExplicitNullable, ImmutableArray.Create(underlyingTupleConversion));
                    }
                }
            }

            return Conversion.NoConversion;
        }

        internal static bool HasImplicitConstantExpressionConversion(BoundExpression source, TypeSymbol destination)
        {
            var constantValue = source.ConstantValue;

            if (constantValue == null || (object)source.Type == null)
            {
                return false;
            }

            // An implicit constant expression conversion permits the following conversions:

            // A constant-expression of type int can be converted to type sbyte, byte, short, 
            // ushort, uint, or ulong, provided the value of the constant-expression is within the
            // range of the destination type.
            var specialSource = source.Type.GetSpecialTypeSafe();

            if (specialSource == SpecialType.System_Int32)
            {
                //if the constant value could not be computed, be generous and assume the conversion will work
                int value = constantValue.IsBad ? 0 : constantValue.Int32Value;
                switch (destination.GetSpecialTypeSafe())
                {
                    case SpecialType.System_Byte:
                        return byte.MinValue <= value && value <= byte.MaxValue;
                    case SpecialType.System_SByte:
                        return sbyte.MinValue <= value && value <= sbyte.MaxValue;
                    case SpecialType.System_Int16:
                        return short.MinValue <= value && value <= short.MaxValue;
                    case SpecialType.System_IntPtr when destination.IsNativeIntegerType:
                        return true;
                    case SpecialType.System_UInt32:
                    case SpecialType.System_UIntPtr when destination.IsNativeIntegerType:
                        return uint.MinValue <= value;
                    case SpecialType.System_UInt64:
                        return (int)ulong.MinValue <= value;
                    case SpecialType.System_UInt16:
                        return ushort.MinValue <= value && value <= ushort.MaxValue;
                    default:
                        return false;
                }
            }
            else if (specialSource == SpecialType.System_Int64 && destination.GetSpecialTypeSafe() == SpecialType.System_UInt64 && (constantValue.IsBad || 0 <= constantValue.Int64Value))
            {
                // A constant-expression of type long can be converted to type ulong, provided the
                // value of the constant-expression is not negative.
                return true;
            }

            return false;
        }

        private Conversion ClassifyExplicitOnlyConversionFromExpression(BoundExpression sourceExpression, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics, bool forCast)
        {
            Debug.Assert(sourceExpression != null);
            Debug.Assert((object)destination != null);

            var sourceType = sourceExpression.Type;

            // NB: need to check for explicit tuple literal conversion before checking for explicit conversion from type
            //     The same literal may have both explicit tuple conversion and explicit tuple literal conversion to the target type.
            //     They are, however, observably different conversions via the order of argument evaluations and element-wise conversions
            if (sourceExpression.Kind == BoundKind.TupleLiteral)
            {
                Conversion tupleConversion = ClassifyExplicitTupleLiteralConversion((BoundTupleLiteral)sourceExpression, destination, ref useSiteDiagnostics, forCast);
                if (tupleConversion.Exists)
                {
                    return tupleConversion;
                }
            }

            if ((object)sourceType != null)
            {
                // Try using the short-circuit "fast-conversion" path.
                Conversion fastConversion = FastClassifyConversion(sourceType, destination);
                if (fastConversion.Exists)
                {
                    return fastConversion;
                }
                else
                {
                    var conversion = ClassifyExplicitBuiltInOnlyConversion(sourceType, destination, ref useSiteDiagnostics, forCast);
                    if (conversion.Exists)
                    {
                        return conversion;
                    }
                }
            }

            return GetExplicitUserDefinedConversion(sourceExpression, sourceType, destination, ref useSiteDiagnostics);
        }

        private static bool HasImplicitEnumerationConversion(BoundExpression source, TypeSymbol destination)
        {
            Debug.Assert((object)source != null);
            Debug.Assert((object)destination != null);

            // SPEC: An implicit enumeration conversion permits the decimal-integer-literal 0 to be converted to any enum-type 
            // SPEC: and to any nullable-type whose underlying type is an enum-type. 
            //
            // For historical reasons we actually allow a conversion from any *numeric constant
            // zero* to be converted to any enum type, not just the literal integer zero.

            bool validType = destination.IsEnumType() ||
                destination.IsNullableType() && destination.GetNullableUnderlyingType().IsEnumType();

            if (!validType)
            {
                return false;
            }

            var sourceConstantValue = source.ConstantValue;
            return sourceConstantValue != null &&
                IsNumericType(source.Type.GetSpecialTypeSafe()) &&
                IsConstantNumericZero(sourceConstantValue);
        }

        private static LambdaConversionResult IsAnonymousFunctionCompatibleWithDelegate(UnboundLambda anonymousFunction, TypeSymbol type)
        {
            Debug.Assert((object)anonymousFunction != null);
            Debug.Assert((object)type != null);

            // SPEC: An anonymous-method-expression or lambda-expression is classified as an anonymous function. 
            // SPEC: The expression does not have a type but can be implicitly converted to a compatible delegate 
            // SPEC: type or expression tree type. Specifically, a delegate type D is compatible with an 
            // SPEC: anonymous function F provided:

            var delegateType = (NamedTypeSymbol)type;
            var invokeMethod = delegateType.DelegateInvokeMethod;

            if ((object)invokeMethod == null || invokeMethod.HasUseSiteError)
            {
                return LambdaConversionResult.BadTargetType;
            }

            var delegateParameters = invokeMethod.Parameters;

            // SPEC: If F contains an anonymous-function-signature, then D and F have the same number of parameters.
            // SPEC: If F does not contain an anonymous-function-signature, then D may have zero or more parameters 
            // SPEC: of any type, as long as no parameter of D has the out parameter modifier.

            if (anonymousFunction.HasSignature)
            {
                if (anonymousFunction.ParameterCount != invokeMethod.ParameterCount)
                {
                    return LambdaConversionResult.BadParameterCount;
                }

                // SPEC: If F has an explicitly typed parameter list, each parameter in D has the same type 
                // SPEC: and modifiers as the corresponding parameter in F.
                // SPEC: If F has an implicitly typed parameter list, D has no ref or out parameters.

                if (anonymousFunction.HasExplicitlyTypedParameterList)
                {
                    for (int p = 0; p < delegateParameters.Length; ++p)
                    {
                        if (delegateParameters[p].RefKind != anonymousFunction.RefKind(p) ||
                            !delegateParameters[p].Type.Equals(anonymousFunction.ParameterType(p), TypeCompareKind.AllIgnoreOptions))
                        {
                            return LambdaConversionResult.MismatchedParameterType;
                        }
                    }
                }
                else
                {
                    for (int p = 0; p < delegateParameters.Length; ++p)
                    {
                        if (delegateParameters[p].RefKind != RefKind.None)
                        {
                            return LambdaConversionResult.RefInImplicitlyTypedLambda;
                        }
                    }

                    // In C# it is not possible to make a delegate type
                    // such that one of its parameter types is a static type. But static types are 
                    // in metadata just sealed abstract types; there is nothing stopping someone in
                    // another language from creating a delegate with a static type for a parameter,
                    // though the only argument you could pass for that parameter is null.
                    // 
                    // In the native compiler we forbid conversion of an anonymous function that has
                    // an implicitly-typed parameter list to a delegate type that has a static type
                    // for a formal parameter type. However, we do *not* forbid it for an explicitly-
                    // typed lambda (because we already require that the explicitly typed parameter not
                    // be static) and we do not forbid it for an anonymous method with the entire
                    // parameter list missing (because the body cannot possibly have a parameter that
                    // is of static type, even though this means that we will be generating a hidden
                    // method with a parameter of static type.)
                    //
                    // We also allow more exotic situations to work in the native compiler. For example,
                    // though it is not possible to convert x=>{} to Action<GC>, it is possible to convert
                    // it to Action<List<GC>> should there be a language that allows you to construct 
                    // a variable of that type.
                    //
                    // We might consider beefing up this rule to disallow a conversion of *any* anonymous
                    // function to *any* delegate that has a static type *anywhere* in the parameter list.

                    for (int p = 0; p < delegateParameters.Length; ++p)
                    {
                        if (delegateParameters[p].TypeWithAnnotations.IsStatic)
                        {
                            return LambdaConversionResult.StaticTypeInImplicitlyTypedLambda;
                        }
                    }
                }
            }
            else
            {
                for (int p = 0; p < delegateParameters.Length; ++p)
                {
                    if (delegateParameters[p].RefKind == RefKind.Out)
                    {
                        return LambdaConversionResult.MissingSignatureWithOutParameter;
                    }
                }
            }

            // Ensure the body can be converted to that delegate type
            var bound = anonymousFunction.Bind(delegateType);
            if (ErrorFacts.PreventsSuccessfulDelegateConversion(bound.Diagnostics))
            {
                return LambdaConversionResult.BindingFailed;
            }

            return LambdaConversionResult.Success;
        }

        private static LambdaConversionResult IsAnonymousFunctionCompatibleWithExpressionTree(UnboundLambda anonymousFunction, NamedTypeSymbol type)
        {
            Debug.Assert((object)anonymousFunction != null);
            Debug.Assert((object)type != null);
            Debug.Assert(type.IsExpressionTree());

            // SPEC OMISSION:
            // 
            // The C# 3 spec said that anonymous methods and statement lambdas are *convertible* to expression tree
            // types if the anonymous method/statement lambda is convertible to its delegate type; however, actually
            // *using* such a conversion is an error. However, that is not what we implemented. In C# 3 we implemented
            // that an anonymous method is *not convertible* to an expression tree type, period. (Statement lambdas
            // used the rule described in the spec.)  
            //
            // This appears to be a spec omission; the intention is to make old-style anonymous methods not 
            // convertible to expression trees.

            var delegateType = type.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[0].Type;
            if (!delegateType.IsDelegateType())
            {
                return LambdaConversionResult.ExpressionTreeMustHaveDelegateTypeArgument;
            }

            if (anonymousFunction.Syntax.Kind() == SyntaxKind.AnonymousMethodExpression)
            {
                return LambdaConversionResult.ExpressionTreeFromAnonymousMethod;
            }

            return IsAnonymousFunctionCompatibleWithDelegate(anonymousFunction, delegateType);
        }

        public static LambdaConversionResult IsAnonymousFunctionCompatibleWithType(UnboundLambda anonymousFunction, TypeSymbol type)
        {
            Debug.Assert((object)anonymousFunction != null);
            Debug.Assert((object)type != null);

            if (type.IsDelegateType())
            {
                return IsAnonymousFunctionCompatibleWithDelegate(anonymousFunction, type);
            }
            else if (type.IsExpressionTree())
            {
                return IsAnonymousFunctionCompatibleWithExpressionTree(anonymousFunction, (NamedTypeSymbol)type);
            }

            return LambdaConversionResult.BadTargetType;
        }

        private static bool HasAnonymousFunctionConversion(BoundExpression source, TypeSymbol destination)
        {
            Debug.Assert(source != null);
            Debug.Assert((object)destination != null);

            if (source.Kind != BoundKind.UnboundLambda)
            {
                return false;
            }

            return IsAnonymousFunctionCompatibleWithType((UnboundLambda)source, destination) == LambdaConversionResult.Success;
        }

        internal Conversion ClassifyImplicitUserDefinedConversionForV6SwitchGoverningType(TypeSymbol sourceType, out TypeSymbol switchGoverningType, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            // SPEC:    The governing type of a switch statement is established by the switch expression.
            // SPEC:    1) If the type of the switch expression is sbyte, byte, short, ushort, int, uint,
            // SPEC:       long, ulong, bool, char, string, or an enum-type, or if it is the nullable type
            // SPEC:       corresponding to one of these types, then that is the governing type of the switch statement. 
            // SPEC:    2) Otherwise, exactly one user-defined implicit conversion (§6.4) must exist from the
            // SPEC:       type of the switch expression to one of the following possible governing types:
            // SPEC:       sbyte, byte, short, ushort, int, uint, long, ulong, char, string, or, a nullable type
            // SPEC:       corresponding to one of those types

            // NOTE:    We should be called only if (1) is false for source type.
            Debug.Assert((object)sourceType != null);
            Debug.Assert(!sourceType.IsValidV6SwitchGoverningType());

            UserDefinedConversionResult result = AnalyzeImplicitUserDefinedConversionForV6SwitchGoverningType(sourceType, ref useSiteDiagnostics);

            if (result.Kind == UserDefinedConversionResultKind.Valid)
            {
                UserDefinedConversionAnalysis analysis = result.Results[result.Best];

                switchGoverningType = analysis.ToType;
                Debug.Assert(switchGoverningType.IsValidV6SwitchGoverningType(isTargetTypeOfUserDefinedOp: true));
            }
            else
            {
                switchGoverningType = null;
            }

            return new Conversion(result, isImplicit: true);
        }

        internal Conversion GetCallerLineNumberConversion(TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            var greenNode = new Syntax.InternalSyntax.LiteralExpressionSyntax(SyntaxKind.NumericLiteralExpression, new Syntax.InternalSyntax.SyntaxToken(SyntaxKind.NumericLiteralToken));
            var syntaxNode = new LiteralExpressionSyntax(greenNode, null, 0);

            TypeSymbol expectedAttributeType = corLibrary.GetSpecialType(SpecialType.System_Int32);
            BoundLiteral intMaxValueLiteral = new BoundLiteral(syntaxNode, ConstantValue.Create(int.MaxValue), expectedAttributeType);
            return ClassifyStandardImplicitConversion(intMaxValueLiteral, expectedAttributeType, destination, ref useSiteDiagnostics);
        }

        internal bool HasCallerLineNumberConversion(TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            return GetCallerLineNumberConversion(destination, ref useSiteDiagnostics).Exists;
        }

        internal bool HasCallerInfoStringConversion(TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            TypeSymbol expectedAttributeType = corLibrary.GetSpecialType(SpecialType.System_String);
            Conversion conversion = ClassifyStandardImplicitConversion(expectedAttributeType, destination, ref useSiteDiagnostics);
            return conversion.Exists;
        }

        public static bool HasIdentityConversion(TypeSymbol type1, TypeSymbol type2)
        {
            return HasIdentityConversionInternal(type1, type2, includeNullability: false);
        }

        private static bool HasIdentityConversionInternal(TypeSymbol type1, TypeSymbol type2, bool includeNullability)
        {
            // Spec (6.1.1):
            // An identity conversion converts from any type to the same type. This conversion exists 
            // such that an entity that already has a required type can be said to be convertible to 
            // that type.
            //
            // Because object and dynamic are considered equivalent there is an identity conversion 
            // between object and dynamic, and between constructed types that are the same when replacing 
            // all occurrences of dynamic with object.

            Debug.Assert((object)type1 != null);
            Debug.Assert((object)type2 != null);

            // Note, when we are paying attention to nullability, we ignore oblivious mismatch.
            // See TypeCompareKind.ObliviousNullableModifierMatchesAny
            var compareKind = includeNullability ?
                TypeCompareKind.AllIgnoreOptions & ~TypeCompareKind.IgnoreNullableModifiersForReferenceTypes :
                TypeCompareKind.AllIgnoreOptions;
            return type1.Equals(type2, compareKind);
        }

        private bool HasIdentityConversionInternal(TypeSymbol type1, TypeSymbol type2)
        {
            return HasIdentityConversionInternal(type1, type2, IncludeNullability);
        }

        /// <summary>
        /// Returns true if:
        /// - Either type has no nullability information (oblivious).
        /// - Both types cannot have different nullability at the same time,
        ///   including the case of type parameters that by themselves can represent nullable and not nullable reference types.
        /// </summary>
        internal bool HasTopLevelNullabilityIdentityConversion(TypeWithAnnotations source, TypeWithAnnotations destination)
        {
            if (!IncludeNullability)
            {
                return true;
            }

            if (source.NullableAnnotation.IsOblivious() || destination.NullableAnnotation.IsOblivious())
            {
                return true;
            }

            var sourceIsPossiblyNullableTypeParameter = IsPossiblyNullableTypeTypeParameter(source);
            var destinationIsPossiblyNullableTypeParameter = IsPossiblyNullableTypeTypeParameter(destination);
            if (sourceIsPossiblyNullableTypeParameter && !destinationIsPossiblyNullableTypeParameter)
            {
                return destination.NullableAnnotation.IsAnnotated();
            }

            if (destinationIsPossiblyNullableTypeParameter && !sourceIsPossiblyNullableTypeParameter)
            {
                return source.NullableAnnotation.IsAnnotated();
            }

            return source.NullableAnnotation.IsAnnotated() == destination.NullableAnnotation.IsAnnotated();
        }

        /// <summary>
        /// Returns false if source type can be nullable at the same time when destination type can be not nullable, 
        /// including the case of type parameters that by themselves can represent nullable and not nullable reference types.
        /// When either type has no nullability information (oblivious), this method returns true.
        /// </summary>
        internal bool HasTopLevelNullabilityImplicitConversion(TypeWithAnnotations source, TypeWithAnnotations destination)
        {
            if (!IncludeNullability)
            {
                return true;
            }

            if (source.NullableAnnotation.IsOblivious() || destination.NullableAnnotation.IsOblivious() || destination.NullableAnnotation.IsAnnotated())
            {
                return true;
            }

            if (IsPossiblyNullableTypeTypeParameter(source) && !IsPossiblyNullableTypeTypeParameter(destination))
            {
                return false;
            }

            return !source.NullableAnnotation.IsAnnotated();
        }

        private static bool IsPossiblyNullableTypeTypeParameter(in TypeWithAnnotations typeWithAnnotations)
        {
            var type = typeWithAnnotations.Type;
            return type is object &&
                (type.IsPossiblyNullableReferenceTypeTypeParameter() || type.IsNullableTypeOrTypeParameter());
        }

        /// <summary>
        /// Returns false if the source does not have an implicit conversion to the destination
        /// because of either incompatible top level or nested nullability.
        /// </summary>
        public bool HasAnyNullabilityImplicitConversion(TypeWithAnnotations source, TypeWithAnnotations destination)
        {
            Debug.Assert(IncludeNullability);
            HashSet<DiagnosticInfo> discardedDiagnostics = null;
            return HasTopLevelNullabilityImplicitConversion(source, destination) &&
                ClassifyImplicitConversionFromType(source.Type, destination.Type, ref discardedDiagnostics).Kind != ConversionKind.NoConversion;
        }

        public static bool HasIdentityConversionToAny<T>(T type, ArrayBuilder<T> targetTypes)
            where T : TypeSymbol
        {
            foreach (var targetType in targetTypes)
            {
                if (HasIdentityConversionInternal(type, targetType, includeNullability: false))
                {
                    return true;
                }
            }

            return false;
        }

        public Conversion ConvertExtensionMethodThisArg(TypeSymbol parameterType, TypeSymbol thisType, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert((object)thisType != null);
            var conversion = this.ClassifyImplicitExtensionMethodThisArgConversion(null, thisType, parameterType, ref useSiteDiagnostics);
            return IsValidExtensionMethodThisArgConversion(conversion) ? conversion : Conversion.NoConversion;
        }

        // Spec 7.6.5.2: "An extension method ... is eligible if ... [an] implicit identity, reference,
        // or boxing conversion exists from expr to the type of the first parameter"
        public Conversion ClassifyImplicitExtensionMethodThisArgConversion(BoundExpression sourceExpressionOpt, TypeSymbol sourceType, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert(sourceExpressionOpt == null || (object)sourceExpressionOpt.Type == sourceType);
            Debug.Assert((object)destination != null);

            if ((object)sourceType != null)
            {
                if (HasIdentityConversionInternal(sourceType, destination))
                {
                    return Conversion.Identity;
                }

                if (HasBoxingConversion(sourceType, destination, ref useSiteDiagnostics))
                {
                    return Conversion.Boxing;
                }

                if (HasImplicitReferenceConversion(sourceType, destination, ref useSiteDiagnostics))
                {
                    return Conversion.ImplicitReference;
                }
            }

            if (sourceExpressionOpt?.Kind == BoundKind.TupleLiteral)
            {
                // GetTupleLiteralConversion is not used with IncludeNullability currently.
                // If that changes, the delegate below will need to consider top-level nullability.
                Debug.Assert(!IncludeNullability);
                var tupleConversion = GetTupleLiteralConversion(
                    (BoundTupleLiteral)sourceExpressionOpt,
                    destination,
                    ref useSiteDiagnostics,
                    ConversionKind.ImplicitTupleLiteral,
                    (ConversionsBase conversions, BoundExpression s, TypeWithAnnotations d, ref HashSet<DiagnosticInfo> u, bool a) =>
                        conversions.ClassifyImplicitExtensionMethodThisArgConversion(s, s.Type, d.Type, ref u),
                    arg: false);
                if (tupleConversion.Exists)
                {
                    return tupleConversion;
                }
            }

            if ((object)sourceType != null)
            {
                var tupleConversion = ClassifyTupleConversion(
                    sourceType,
                    destination,
                    ref useSiteDiagnostics,
                    ConversionKind.ImplicitTuple,
                    (ConversionsBase conversions, TypeWithAnnotations s, TypeWithAnnotations d, ref HashSet<DiagnosticInfo> u, bool a) =>
                    {
                        if (!conversions.HasTopLevelNullabilityImplicitConversion(s, d))
                        {
                            return Conversion.NoConversion;
                        }
                        return conversions.ClassifyImplicitExtensionMethodThisArgConversion(null, s.Type, d.Type, ref u);
                    },
                    arg: false);
                if (tupleConversion.Exists)
                {
                    return tupleConversion;
                }
            }

            return Conversion.NoConversion;
        }

        // It should be possible to remove IsValidExtensionMethodThisArgConversion
        // since ClassifyImplicitExtensionMethodThisArgConversion should only
        // return valid conversions. https://github.com/dotnet/roslyn/issues/19622

        // Spec 7.6.5.2: "An extension method ... is eligible if ... [an] implicit identity, reference,
        // or boxing conversion exists from expr to the type of the first parameter"
        public static bool IsValidExtensionMethodThisArgConversion(Conversion conversion)
        {
            switch (conversion.Kind)
            {
                case ConversionKind.Identity:
                case ConversionKind.Boxing:
                case ConversionKind.ImplicitReference:
                    return true;

                case ConversionKind.ImplicitTuple:
                case ConversionKind.ImplicitTupleLiteral:
                    // check if all element conversions satisfy the requirement
                    foreach (var elementConversion in conversion.UnderlyingConversions)
                    {
                        if (!IsValidExtensionMethodThisArgConversion(elementConversion))
                        {
                            return false;
                        }
                    }
                    return true;

                default:
                    // Caller should have not have calculated another conversion.
                    Debug.Assert(conversion.Kind == ConversionKind.NoConversion);
                    return false;
            }
        }

        private const bool F = false;
        private const bool T = true;

        // Notice that there is no implicit numeric conversion from a type to itself. That's an
        // identity conversion.
        private static readonly bool[,] s_implicitNumericConversions =
        {
            // to     sb  b  s  us i ui  l ul  c  f  d  m
            // from
            /* sb */
         { F, F, T, F, T, F, T, F, F, T, T, T },
            /*  b */
         { F, F, T, T, T, T, T, T, F, T, T, T },
            /*  s */
         { F, F, F, F, T, F, T, F, F, T, T, T },
            /* us */
         { F, F, F, F, T, T, T, T, F, T, T, T },
            /*  i */
         { F, F, F, F, F, F, T, F, F, T, T, T },
            /* ui */
         { F, F, F, F, F, F, T, T, F, T, T, T },
            /*  l */
         { F, F, F, F, F, F, F, F, F, T, T, T },
            /* ul */
         { F, F, F, F, F, F, F, F, F, T, T, T },
            /*  c */
         { F, F, F, T, T, T, T, T, F, T, T, T },
            /*  f */
         { F, F, F, F, F, F, F, F, F, F, T, F },
            /*  d */
         { F, F, F, F, F, F, F, F, F, F, F, F },
            /*  m */
         { F, F, F, F, F, F, F, F, F, F, F, F }
        };

        private static readonly bool[,] s_explicitNumericConversions =
        {
            // to     sb  b  s us  i ui  l ul  c  f  d  m
            // from
            /* sb */
         { F, T, F, T, F, T, F, T, T, F, F, F },
            /*  b */
         { T, F, F, F, F, F, F, F, T, F, F, F },
            /*  s */
         { T, T, F, T, F, T, F, T, T, F, F, F },
            /* us */
         { T, T, T, F, F, F, F, F, T, F, F, F },
            /*  i */
         { T, T, T, T, F, T, F, T, T, F, F, F },
            /* ui */
         { T, T, T, T, T, F, F, F, T, F, F, F },
            /*  l */
         { T, T, T, T, T, T, F, T, T, F, F, F },
            /* ul */
         { T, T, T, T, T, T, T, F, T, F, F, F },
            /*  c */
         { T, T, T, F, F, F, F, F, F, F, F, F },
            /*  f */
         { T, T, T, T, T, T, T, T, T, F, F, T },
            /*  d */
         { T, T, T, T, T, T, T, T, T, T, F, T },
            /*  m */
         { T, T, T, T, T, T, T, T, T, T, T, F }
        };

        private static int GetNumericTypeIndex(SpecialType specialType)
        {
            switch (specialType)
            {
                case SpecialType.System_SByte: return 0;
                case SpecialType.System_Byte: return 1;
                case SpecialType.System_Int16: return 2;
                case SpecialType.System_UInt16: return 3;
                case SpecialType.System_Int32: return 4;
                case SpecialType.System_UInt32: return 5;
                case SpecialType.System_Int64: return 6;
                case SpecialType.System_UInt64: return 7;
                case SpecialType.System_Char: return 8;
                case SpecialType.System_Single: return 9;
                case SpecialType.System_Double: return 10;
                case SpecialType.System_Decimal: return 11;
                default: return -1;
            }
        }

        private static bool HasImplicitNumericConversion(TypeSymbol source, TypeSymbol destination)
        {
            Debug.Assert((object)source != null);
            Debug.Assert((object)destination != null);

            int sourceIndex = GetNumericTypeIndex(source.SpecialType);
            if (sourceIndex < 0)
            {
                return false;
            }

            int destinationIndex = GetNumericTypeIndex(destination.SpecialType);
            if (destinationIndex < 0)
            {
                return false;
            }

            return s_implicitNumericConversions[sourceIndex, destinationIndex];
        }

        private static bool HasExplicitNumericConversion(TypeSymbol source, TypeSymbol destination)
        {
            // SPEC: The explicit numeric conversions are the conversions from a numeric-type to another 
            // SPEC: numeric-type for which an implicit numeric conversion does not already exist.
            Debug.Assert((object)source != null);
            Debug.Assert((object)destination != null);

            int sourceIndex = GetNumericTypeIndex(source.SpecialType);
            if (sourceIndex < 0)
            {
                return false;
            }

            int destinationIndex = GetNumericTypeIndex(destination.SpecialType);
            if (destinationIndex < 0)
            {
                return false;
            }

            return s_explicitNumericConversions[sourceIndex, destinationIndex];
        }

        public static bool IsConstantNumericZero(ConstantValue value)
        {
            switch (value.Discriminator)
            {
                case ConstantValueTypeDiscriminator.SByte:
                    return value.SByteValue == 0;
                case ConstantValueTypeDiscriminator.Byte:
                    return value.ByteValue == 0;
                case ConstantValueTypeDiscriminator.Int16:
                    return value.Int16Value == 0;
                case ConstantValueTypeDiscriminator.Int32:
                    return value.Int32Value == 0;
                case ConstantValueTypeDiscriminator.Int64:
                    return value.Int64Value == 0;
                case ConstantValueTypeDiscriminator.UInt16:
                    return value.UInt16Value == 0;
                case ConstantValueTypeDiscriminator.UInt32:
                    return value.UInt32Value == 0;
                case ConstantValueTypeDiscriminator.UInt64:
                    return value.UInt64Value == 0;
                case ConstantValueTypeDiscriminator.Single:
                case ConstantValueTypeDiscriminator.Double:
                    return value.DoubleValue == 0;
                case ConstantValueTypeDiscriminator.Decimal:
                    return value.DecimalValue == 0;
            }
            return false;
        }

        public static bool IsNumericType(SpecialType specialType)
        {
            switch (specialType)
            {
                case SpecialType.System_Char:
                case SpecialType.System_SByte:
                case SpecialType.System_Byte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                case SpecialType.System_Decimal:
                    return true;
                default:
                    return false;
            }
        }

        private static bool HasSpecialIntPtrConversion(TypeSymbol source, TypeSymbol target)
        {
            Debug.Assert((object)source != null);
            Debug.Assert((object)target != null);

            // There are only a total of twelve user-defined explicit conversions on IntPtr and UIntPtr:
            //
            // IntPtr  <---> int
            // IntPtr  <---> long
            // IntPtr  <---> void*
            // UIntPtr <---> uint
            // UIntPtr <---> ulong
            // UIntPtr <---> void*
            //
            // The specification says that you can put any *standard* implicit or explicit conversion
            // on "either side" of a user-defined explicit conversion, so the specification allows, say,
            // UIntPtr --> byte because the conversion UIntPtr --> uint is user-defined and the 
            // conversion uint --> byte is "standard". It is "standard" because the conversion 
            // byte --> uint is an implicit numeric conversion.

            // This means that certain conversions should be illegal. For example, IntPtr --> ulong
            // should be illegal because none of int --> ulong, long --> ulong and void* --> ulong 
            // are "standard" conversions. 

            // Similarly, some conversions involving IntPtr should be illegal because they are 
            // ambiguous. byte --> IntPtr?, for example, is ambiguous. (There are four possible
            // UD operators: int --> IntPtr and long --> IntPtr, and their lifted versions. The
            // best possible source type is int, the best possible target type is IntPtr?, and
            // there is an ambiguity between the unlifted int --> IntPtr, and the lifted 
            // int? --> IntPtr? conversions.)

            // In practice, the native compiler, and hence, the Roslyn compiler, allows all 
            // these conversions. Any conversion from a numeric type to IntPtr, or from an IntPtr
            // to a numeric type, is allowed. Also, any conversion from a pointer type to IntPtr
            // or vice versa is allowed.

            var s0 = source.StrippedType();
            var t0 = target.StrippedType();

            if (s0.SpecialType != SpecialType.System_UIntPtr &&
                s0.SpecialType != SpecialType.System_IntPtr &&
                t0.SpecialType != SpecialType.System_UIntPtr &&
                t0.SpecialType != SpecialType.System_IntPtr)
            {
                return false;
            }

            TypeSymbol otherType = (s0.SpecialType == SpecialType.System_UIntPtr || s0.SpecialType == SpecialType.System_IntPtr) ? t0 : s0;

            if (otherType.IsPointerOrFunctionPointer())
            {
                return true;
            }

            if (otherType.TypeKind == TypeKind.Enum)
            {
                return true;
            }

            switch (otherType.SpecialType)
            {
                case SpecialType.System_SByte:
                case SpecialType.System_Byte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Char:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                case SpecialType.System_Double:
                case SpecialType.System_Single:
                case SpecialType.System_Decimal:
                    return true;
            }

            return false;
        }

        private static bool HasExplicitEnumerationConversion(TypeSymbol source, TypeSymbol destination)
        {
            Debug.Assert((object)source != null);
            Debug.Assert((object)destination != null);

            // SPEC: The explicit enumeration conversions are:
            // SPEC: From sbyte, byte, short, ushort, int, uint, long, ulong, char, float, double, or decimal to any enum-type.
            // SPEC: From any enum-type to sbyte, byte, short, ushort, int, uint, long, ulong, char, float, double, or decimal.
            // SPEC: From any enum-type to any other enum-type.

            if (IsNumericType(source.SpecialType) && destination.IsEnumType())
            {
                return true;
            }

            if (IsNumericType(destination.SpecialType) && source.IsEnumType())
            {
                return true;
            }

            if (source.IsEnumType() && destination.IsEnumType())
            {
                return true;
            }

            return false;
        }

        private Conversion ClassifyImplicitNullableConversion(TypeSymbol source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert((object)source != null);
            Debug.Assert((object)destination != null);

            // SPEC: Predefined implicit conversions that operate on non-nullable value types can also be used with 
            // SPEC: nullable forms of those types. For each of the predefined implicit identity, numeric and tuple conversions
            // SPEC: that convert from a non-nullable value type S to a non-nullable value type T, the following implicit 
            // SPEC: nullable conversions exist:
            // SPEC: * An implicit conversion from S? to T?.
            // SPEC: * An implicit conversion from S to T?.
            if (!destination.IsNullableType())
            {
                return Conversion.NoConversion;
            }

            TypeSymbol unwrappedDestination = destination.GetNullableUnderlyingType();
            TypeSymbol unwrappedSource = source.StrippedType();

            if (!unwrappedSource.IsValueType)
            {
                return Conversion.NoConversion;
            }

            if (HasIdentityConversionInternal(unwrappedSource, unwrappedDestination))
            {
                return new Conversion(ConversionKind.ImplicitNullable, Conversion.IdentityUnderlying);
            }

            if (HasImplicitNumericConversion(unwrappedSource, unwrappedDestination))
            {
                return new Conversion(ConversionKind.ImplicitNullable, Conversion.ImplicitNumericUnderlying);
            }

            var tupleConversion = ClassifyImplicitTupleConversion(unwrappedSource, unwrappedDestination, ref useSiteDiagnostics);
            if (tupleConversion.Exists)
            {
                return new Conversion(ConversionKind.ImplicitNullable, ImmutableArray.Create(tupleConversion));
            }

            return Conversion.NoConversion;
        }

        private delegate Conversion ClassifyConversionFromExpressionDelegate(ConversionsBase conversions, BoundExpression sourceExpression, TypeWithAnnotations destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics, bool arg);
        private delegate Conversion ClassifyConversionFromTypeDelegate(ConversionsBase conversions, TypeWithAnnotations source, TypeWithAnnotations destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics, bool arg);

        private Conversion GetImplicitTupleLiteralConversion(BoundTupleLiteral source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            // GetTupleLiteralConversion is not used with IncludeNullability currently.
            // If that changes, the delegate below will need to consider top-level nullability.
            Debug.Assert(!IncludeNullability);
            return GetTupleLiteralConversion(
                source,
                destination,
                ref useSiteDiagnostics,
                ConversionKind.ImplicitTupleLiteral,
                (ConversionsBase conversions, BoundExpression s, TypeWithAnnotations d, ref HashSet<DiagnosticInfo> u, bool a)
                    => conversions.ClassifyImplicitConversionFromExpression(s, d.Type, ref u),
                arg: false);
        }

        private Conversion GetExplicitTupleLiteralConversion(BoundTupleLiteral source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics, bool forCast)
        {
            // GetTupleLiteralConversion is not used with IncludeNullability currently.
            // If that changes, the delegate below will need to consider top-level nullability.
            Debug.Assert(!IncludeNullability);
            return GetTupleLiteralConversion(
                source,
                destination,
                ref useSiteDiagnostics,
                ConversionKind.ExplicitTupleLiteral,
                (ConversionsBase conversions, BoundExpression s, TypeWithAnnotations d, ref HashSet<DiagnosticInfo> u, bool a) => conversions.ClassifyConversionFromExpression(s, d.Type, ref u, a),
                forCast);
        }

        private Conversion GetTupleLiteralConversion(
            BoundTupleLiteral source,
            TypeSymbol destination,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics,
            ConversionKind kind,
            ClassifyConversionFromExpressionDelegate classifyConversion,
            bool arg)
        {
            var arguments = source.Arguments;

            // check if the type is actually compatible type for a tuple of given cardinality
            if (!destination.IsTupleTypeOfCardinality(arguments.Length))
            {
                return Conversion.NoConversion;
            }

            var targetElementTypes = destination.TupleElementTypesWithAnnotations;
            Debug.Assert(arguments.Length == targetElementTypes.Length);

            // check arguments against flattened list of target element types 
            var argumentConversions = ArrayBuilder<Conversion>.GetInstance(arguments.Length);
            for (int i = 0; i < arguments.Length; i++)
            {
                var argument = arguments[i];
                var result = classifyConversion(this, argument, targetElementTypes[i], ref useSiteDiagnostics, arg);
                if (!result.Exists)
                {
                    argumentConversions.Free();
                    return Conversion.NoConversion;
                }

                argumentConversions.Add(result);
            }

            return new Conversion(kind, argumentConversions.ToImmutableAndFree());
        }

        private Conversion ClassifyImplicitTupleConversion(TypeSymbol source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            return ClassifyTupleConversion(
                source,
                destination,
                ref useSiteDiagnostics,
                ConversionKind.ImplicitTuple,
                (ConversionsBase conversions, TypeWithAnnotations s, TypeWithAnnotations d, ref HashSet<DiagnosticInfo> u, bool a) =>
                {
                    if (!conversions.HasTopLevelNullabilityImplicitConversion(s, d))
                    {
                        return Conversion.NoConversion;
                    }
                    return conversions.ClassifyImplicitConversionFromType(s.Type, d.Type, ref u);
                },
                arg: false);
        }

        private Conversion ClassifyExplicitTupleConversion(TypeSymbol source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics, bool forCast)
        {
            return ClassifyTupleConversion(
                source,
                destination,
                ref useSiteDiagnostics,
                ConversionKind.ExplicitTuple,
                (ConversionsBase conversions, TypeWithAnnotations s, TypeWithAnnotations d, ref HashSet<DiagnosticInfo> u, bool a) =>
                {
                    if (!conversions.HasTopLevelNullabilityImplicitConversion(s, d))
                    {
                        return Conversion.NoConversion;
                    }
                    return conversions.ClassifyConversionFromType(s.Type, d.Type, ref u, a);
                },
                forCast);
        }

        private Conversion ClassifyTupleConversion(
            TypeSymbol source,
            TypeSymbol destination,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics,
            ConversionKind kind,
            ClassifyConversionFromTypeDelegate classifyConversion,
            bool arg)
        {
            ImmutableArray<TypeWithAnnotations> sourceTypes;
            ImmutableArray<TypeWithAnnotations> destTypes;

            if (!source.TryGetElementTypesWithAnnotationsIfTupleType(out sourceTypes) ||
                !destination.TryGetElementTypesWithAnnotationsIfTupleType(out destTypes) ||
                sourceTypes.Length != destTypes.Length)
            {
                return Conversion.NoConversion;
            }

            var nestedConversions = ArrayBuilder<Conversion>.GetInstance(sourceTypes.Length);
            for (int i = 0; i < sourceTypes.Length; i++)
            {
                var conversion = classifyConversion(this, sourceTypes[i], destTypes[i], ref useSiteDiagnostics, arg);
                if (!conversion.Exists)
                {
                    nestedConversions.Free();
                    return Conversion.NoConversion;
                }

                nestedConversions.Add(conversion);
            }

            return new Conversion(kind, nestedConversions.ToImmutableAndFree());
        }

        private Conversion ClassifyExplicitNullableConversion(TypeSymbol source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics, bool forCast)
        {
            Debug.Assert((object)source != null);
            Debug.Assert((object)destination != null);

            // SPEC: Explicit nullable conversions permit predefined explicit conversions that operate on 
            // SPEC: non-nullable value types to also be used with nullable forms of those types. For 
            // SPEC: each of the predefined explicit conversions that convert from a non-nullable value type 
            // SPEC: S to a non-nullable value type T, the following nullable conversions exist:
            // SPEC: An explicit conversion from S? to T?.
            // SPEC: An explicit conversion from S to T?.
            // SPEC: An explicit conversion from S? to T.

            if (!source.IsNullableType() && !destination.IsNullableType())
            {
                return Conversion.NoConversion;
            }

            TypeSymbol unwrappedSource = source.StrippedType();
            TypeSymbol unwrappedDestination = destination.StrippedType();

            if (HasIdentityConversionInternal(unwrappedSource, unwrappedDestination))
            {
                return new Conversion(ConversionKind.ExplicitNullable, Conversion.IdentityUnderlying);
            }

            if (HasImplicitNumericConversion(unwrappedSource, unwrappedDestination))
            {
                return new Conversion(ConversionKind.ExplicitNullable, Conversion.ImplicitNumericUnderlying);
            }

            if (HasExplicitNumericConversion(unwrappedSource, unwrappedDestination))
            {
                return new Conversion(ConversionKind.ExplicitNullable, Conversion.ExplicitNumericUnderlying);
            }

            var tupleConversion = ClassifyExplicitTupleConversion(unwrappedSource, unwrappedDestination, ref useSiteDiagnostics, forCast);
            if (tupleConversion.Exists)
            {
                return new Conversion(ConversionKind.ExplicitNullable, ImmutableArray.Create(tupleConversion));
            }

            if (HasExplicitEnumerationConversion(unwrappedSource, unwrappedDestination))
            {
                return new Conversion(ConversionKind.ExplicitNullable, Conversion.ExplicitEnumerationUnderlying);
            }

            if (HasPointerToIntegerConversion(unwrappedSource, unwrappedDestination))
            {
                return new Conversion(ConversionKind.ExplicitNullable, Conversion.PointerToIntegerUnderlying);
            }

            return Conversion.NoConversion;
        }

        private bool HasCovariantArrayConversion(TypeSymbol source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert((object)source != null);
            Debug.Assert((object)destination != null);
            var s = source as ArrayTypeSymbol;
            var d = destination as ArrayTypeSymbol;
            if ((object)s == null || (object)d == null)
            {
                return false;
            }

            // * S and T differ only in element type. In other words, S and T have the same number of dimensions.
            if (!s.HasSameShapeAs(d))
            {
                return false;
            }

            // * Both SE and TE are reference types.
            // * An implicit reference conversion exists from SE to TE.
            return HasImplicitReferenceConversion(s.ElementTypeWithAnnotations, d.ElementTypeWithAnnotations, ref useSiteDiagnostics);
        }

        public bool HasIdentityOrImplicitReferenceConversion(TypeSymbol source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert((object)source != null);
            Debug.Assert((object)destination != null);

            if (HasIdentityConversionInternal(source, destination))
            {
                return true;
            }

            return HasImplicitReferenceConversion(source, destination, ref useSiteDiagnostics);
        }

        private static bool HasImplicitDynamicConversionFromExpression(TypeSymbol expressionType, TypeSymbol destination)
        {
            // Spec (§6.1.8)
            // An implicit dynamic conversion exists from an expression of type dynamic to any type T.

            Debug.Assert((object)destination != null);
            return expressionType?.Kind == SymbolKind.DynamicType && !destination.IsPointerOrFunctionPointer();
        }

        private static bool HasExplicitDynamicConversion(TypeSymbol source, TypeSymbol destination)
        {
            // SPEC: An explicit dynamic conversion exists from an expression of [sic] type dynamic to any type T.
            // ISSUE: The "an expression of" part of the spec is probably an error; see https://github.com/dotnet/csharplang/issues/132

            Debug.Assert((object)source != null);
            Debug.Assert((object)destination != null);
            return source.Kind == SymbolKind.DynamicType && !destination.IsPointerOrFunctionPointer();
        }

        private bool HasArrayConversionToInterface(ArrayTypeSymbol source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert((object)source != null);
            Debug.Assert((object)destination != null);

            if (!source.IsSZArray)
            {
                return false;
            }

            if (!destination.IsInterfaceType())
            {
                return false;
            }

            // The specification says that there is a conversion:

            // * From a single-dimensional array type S[] to IList<T> and its base
            //   interfaces, provided that there is an implicit identity or reference
            //   conversion from S to T.
            //
            // Newer versions of the framework also have arrays be convertible to
            // IReadOnlyList<T> and IReadOnlyCollection<T>; we honor that as well.
            //
            // Therefore we must check for:
            //
            // IList<T>
            // ICollection<T>
            // IEnumerable<T>
            // IEnumerable
            // IReadOnlyList<T>
            // IReadOnlyCollection<T>

            if (destination.SpecialType == SpecialType.System_Collections_IEnumerable)
            {
                return true;
            }

            NamedTypeSymbol destinationAgg = (NamedTypeSymbol)destination;

            if (destinationAgg.AllTypeArgumentCount() != 1)
            {
                return false;
            }

            if (!destinationAgg.IsPossibleArrayGenericInterface())
            {
                return false;
            }

            TypeWithAnnotations elementType = source.ElementTypeWithAnnotations;
            TypeWithAnnotations argument0 = destinationAgg.TypeArgumentWithDefinitionUseSiteDiagnostics(0, ref useSiteDiagnostics);

            if (IncludeNullability && !HasTopLevelNullabilityImplicitConversion(elementType, argument0))
            {
                return false;
            }

            return HasIdentityOrImplicitReferenceConversion(elementType.Type, argument0.Type, ref useSiteDiagnostics);
        }

        private bool HasImplicitReferenceConversion(TypeWithAnnotations source, TypeWithAnnotations destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            if (IncludeNullability)
            {
                if (!HasTopLevelNullabilityImplicitConversion(source, destination))
                {
                    return false;
                }
                // Check for identity conversion of underlying types if the top-level nullability is distinct.
                // (An identity conversion where nullability matches is not considered an implicit reference conversion.)
                if (source.NullableAnnotation != destination.NullableAnnotation &&
                    HasIdentityConversionInternal(source.Type, destination.Type, includeNullability: true))
                {
                    return true;
                }
            }
            return HasImplicitReferenceConversion(source.Type, destination.Type, ref useSiteDiagnostics);
        }

        internal bool HasImplicitReferenceConversion(TypeSymbol source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert((object)source != null);
            Debug.Assert((object)destination != null);

            if (source.IsErrorType())
            {
                return false;
            }

            if (!source.IsReferenceType)
            {
                return false;
            }

            // SPEC: The implicit reference conversions are:

            // SPEC: UNDONE: From any reference-type to a reference-type T if it has an implicit identity 
            // SPEC: UNDONE: or reference conversion to a reference-type T0 and T0 has an identity conversion to T.
            // UNDONE: Is the right thing to do here to strip dynamic off and check for convertibility?

            // SPEC: From any reference type to object and dynamic.
            if (destination.SpecialType == SpecialType.System_Object || destination.Kind == SymbolKind.DynamicType)
            {
                return true;
            }

            switch (source.TypeKind)
            {
                case TypeKind.Class:
                    // SPEC:  From any class type S to any class type T provided S is derived from T.
                    if (destination.IsClassType() && IsBaseClass(source, destination, ref useSiteDiagnostics))
                    {
                        return true;
                    }

                    return HasImplicitConversionToInterface(source, destination, ref useSiteDiagnostics);

                case TypeKind.Interface:
                    // SPEC: From any interface-type S to any interface-type T, provided S is derived from T.
                    // NOTE: This handles variance conversions
                    return HasImplicitConversionToInterface(source, destination, ref useSiteDiagnostics);

                case TypeKind.Delegate:
                    // SPEC: From any delegate-type to System.Delegate and the interfaces it implements.
                    // NOTE: This handles variance conversions.
                    return HasImplicitConversionFromDelegate(source, destination, ref useSiteDiagnostics);

                case TypeKind.TypeParameter:
                    return HasImplicitReferenceTypeParameterConversion((TypeParameterSymbol)source, destination, ref useSiteDiagnostics);

                case TypeKind.Array:
                    // SPEC: From an array-type S ... to an array-type T, provided ...
                    // SPEC: From any array-type to System.Array and the interfaces it implements.
                    // SPEC: From a single-dimensional array type S[] to IList<T>, provided ...
                    return HasImplicitConversionFromArray(source, destination, ref useSiteDiagnostics);
            }

            // UNDONE: Implicit conversions involving type parameters that are known to be reference types.

            return false;
        }

        private bool HasImplicitConversionToInterface(TypeSymbol source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            if (!destination.IsInterfaceType())
            {
                return false;
            }

            // * From any class type S to any interface type T provided S implements an interface
            //   convertible to T.
            if (source.IsClassType())
            {
                return HasAnyBaseInterfaceConversion(source, destination, ref useSiteDiagnostics);
            }

            // * From any interface type S to any interface type T provided S implements an interface
            //   convertible to T.
            // * From any interface type S to any interface type T provided S is not T and S is 
            //   an interface convertible to T.
            if (source.IsInterfaceType())
            {
                if (HasAnyBaseInterfaceConversion(source, destination, ref useSiteDiagnostics))
                {
                    return true;
                }

                if (!HasIdentityConversionInternal(source, destination) && HasInterfaceVarianceConversion(source, destination, ref useSiteDiagnostics))
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasImplicitConversionFromArray(TypeSymbol source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            ArrayTypeSymbol s = source as ArrayTypeSymbol;
            if ((object)s == null)
            {
                return false;
            }

            // * From an array type S with an element type SE to an array type T with element type TE
            //   provided that all of the following are true:
            //   * S and T differ only in element type. In other words, S and T have the same number of dimensions.
            //   * Both SE and TE are reference types.
            //   * An implicit reference conversion exists from SE to TE.
            if (HasCovariantArrayConversion(source, destination, ref useSiteDiagnostics))
            {
                return true;
            }

            // * From any array type to System.Array or any interface implemented by System.Array.
            if (destination.GetSpecialTypeSafe() == SpecialType.System_Array)
            {
                return true;
            }

            if (IsBaseInterface(destination, this.corLibrary.GetDeclaredSpecialType(SpecialType.System_Array), ref useSiteDiagnostics))
            {
                return true;
            }

            // * From a single-dimensional array type S[] to IList<T> and its base
            //   interfaces, provided that there is an implicit identity or reference
            //   conversion from S to T.

            if (HasArrayConversionToInterface(s, destination, ref useSiteDiagnostics))
            {
                return true;
            }

            return false;
        }

        private bool HasImplicitConversionFromDelegate(TypeSymbol source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            if (!source.IsDelegateType())
            {
                return false;
            }

            // * From any delegate type to System.Delegate
            // 
            // SPEC OMISSION:
            // 
            // The spec should actually say
            //
            // * From any delegate type to System.Delegate 
            // * From any delegate type to System.MulticastDelegate
            // * From any delegate type to any interface implemented by System.MulticastDelegate
            var specialDestination = destination.GetSpecialTypeSafe();

            if (specialDestination == SpecialType.System_MulticastDelegate ||
                specialDestination == SpecialType.System_Delegate ||
                IsBaseInterface(destination, this.corLibrary.GetDeclaredSpecialType(SpecialType.System_MulticastDelegate), ref useSiteDiagnostics))
            {
                return true;
            }

            // * From any delegate type S to a delegate type T provided S is not T and
            //   S is a delegate convertible to T

            if (HasDelegateVarianceConversion(source, destination, ref useSiteDiagnostics))
            {
                return true;
            }

            return false;
        }

        public bool HasImplicitTypeParameterConversion(TypeParameterSymbol source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            if (HasImplicitReferenceTypeParameterConversion(source, destination, ref useSiteDiagnostics))
            {
                return true;
            }

            if (HasImplicitBoxingTypeParameterConversion(source, destination, ref useSiteDiagnostics))
            {
                return true;
            }

            if ((destination.TypeKind == TypeKind.TypeParameter) &&
                source.DependsOn((TypeParameterSymbol)destination))
            {
                return true;
            }

            return false;
        }

        private bool HasImplicitReferenceTypeParameterConversion(TypeParameterSymbol source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert((object)source != null);
            Debug.Assert((object)destination != null);

            if (source.IsValueType)
            {
                return false; // Not a reference conversion.
            }

            // The following implicit conversions exist for a given type parameter T:
            //
            // * From T to its effective base class C.
            // * From T to any base class of C.
            // * From T to any interface implemented by C (or any interface variance-compatible with such)
            if (HasImplicitEffectiveBaseConversion(source, destination, ref useSiteDiagnostics))
            {
                return true;
            }

            // * From T to any interface type I in T's effective interface set, and
            //   from T to any base interface of I (or any interface variance-compatible with such)
            if (HasImplicitEffectiveInterfaceSetConversion(source, destination, ref useSiteDiagnostics))
            {
                return true;
            }

            // * From T to a type parameter U, provided T depends on U.
            if ((destination.TypeKind == TypeKind.TypeParameter) &&
                source.DependsOn((TypeParameterSymbol)destination))
            {
                return true;
            }

            return false;
        }

        // Spec 6.1.10: Implicit conversions involving type parameters
        private bool HasImplicitEffectiveBaseConversion(TypeParameterSymbol source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            // * From T to its effective base class C.
            var effectiveBaseClass = source.EffectiveBaseClass(ref useSiteDiagnostics);
            if (HasIdentityConversionInternal(effectiveBaseClass, destination))
            {
                return true;
            }

            // * From T to any base class of C.
            if (IsBaseClass(effectiveBaseClass, destination, ref useSiteDiagnostics))
            {
                return true;
            }

            // * From T to any interface implemented by C (or any interface variance-compatible with such)
            if (HasAnyBaseInterfaceConversion(effectiveBaseClass, destination, ref useSiteDiagnostics))
            {
                return true;
            }

            return false;
        }

        private bool HasImplicitEffectiveInterfaceSetConversion(TypeParameterSymbol source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            if (!destination.IsInterfaceType())
            {
                return false;
            }

            // * From T to any interface type I in T's effective interface set, and
            //   from T to any base interface of I (or any interface variance-compatible with such)
            foreach (var i in source.AllEffectiveInterfacesWithDefinitionUseSiteDiagnostics(ref useSiteDiagnostics))
            {
                if (HasInterfaceVarianceConversion(i, destination, ref useSiteDiagnostics))
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasAnyBaseInterfaceConversion(TypeSymbol derivedType, TypeSymbol baseType, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert((object)derivedType != null);
            Debug.Assert((object)baseType != null);
            if (!baseType.IsInterfaceType())
            {
                return false;
            }

            var d = derivedType as NamedTypeSymbol;
            if ((object)d == null)
            {
                return false;
            }

            foreach (var i in d.AllInterfacesWithDefinitionUseSiteDiagnostics(ref useSiteDiagnostics))
            {
                if (HasInterfaceVarianceConversion(i, baseType, ref useSiteDiagnostics))
                {
                    return true;
                }
            }

            return false;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // The rules for variant interface and delegate conversions are the same:
        //
        // An interface/delegate type S is convertible to an interface/delegate type T 
        // if and only if T is U<S1, ... Sn> and T is U<T1, ... Tn> such that for all
        // parameters of U:
        //
        // * if the ith parameter of U is invariant then Si is exactly equal to Ti.
        // * if the ith parameter of U is covariant then either Si is exactly equal
        //   to Ti, or there is an implicit reference conversion from Si to Ti.
        // * if the ith parameter of U is contravariant then either Si is exactly
        //   equal to Ti, or there is an implicit reference conversion from Ti to Si.

        private bool HasInterfaceVarianceConversion(TypeSymbol source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert((object)source != null);
            Debug.Assert((object)destination != null);
            NamedTypeSymbol s = source as NamedTypeSymbol;
            NamedTypeSymbol d = destination as NamedTypeSymbol;
            if ((object)s == null || (object)d == null)
            {
                return false;
            }

            if (!s.IsInterfaceType() || !d.IsInterfaceType())
            {
                return false;
            }

            return HasVariantConversion(s, d, ref useSiteDiagnostics);
        }

        private bool HasDelegateVarianceConversion(TypeSymbol source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert((object)source != null);
            Debug.Assert((object)destination != null);
            NamedTypeSymbol s = source as NamedTypeSymbol;
            NamedTypeSymbol d = destination as NamedTypeSymbol;
            if ((object)s == null || (object)d == null)
            {
                return false;
            }

            if (!s.IsDelegateType() || !d.IsDelegateType())
            {
                return false;
            }

            return HasVariantConversion(s, d, ref useSiteDiagnostics);
        }

        private bool HasVariantConversion(NamedTypeSymbol source, NamedTypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            // We check for overflows in HasVariantConversion, because they are only an issue
            // in the presence of contravariant type parameters, which are not involved in 
            // most conversions.
            // See VarianceTests for examples (e.g. TestVarianceConversionCycle, 
            // TestVarianceConversionInfiniteExpansion).
            //
            // CONSIDER: A more rigorous solution would mimic the CLI approach, which uses
            // a combination of requiring finite instantiation closures (see section 9.2 of
            // the CLI spec) and records previous conversion steps to check for cycles.
            if (currentRecursionDepth >= MaximumRecursionDepth)
            {
                // NOTE: The spec doesn't really address what happens if there's an overflow
                // in our conversion check.  It's sort of implied that the conversion "proof"
                // should be finite, so we'll just say that no conversion is possible.
                return false;
            }

            // Do a quick check up front to avoid instantiating a new Conversions object,
            // if possible.
            var quickResult = HasVariantConversionQuick(source, destination);
            if (quickResult.HasValue())
            {
                return quickResult.Value();
            }

            return this.CreateInstance(currentRecursionDepth + 1).
                HasVariantConversionNoCycleCheck(source, destination, ref useSiteDiagnostics);
        }

        private ThreeState HasVariantConversionQuick(NamedTypeSymbol source, NamedTypeSymbol destination)
        {
            Debug.Assert((object)source != null);
            Debug.Assert((object)destination != null);

            if (HasIdentityConversionInternal(source, destination))
            {
                return ThreeState.True;
            }

            NamedTypeSymbol typeSymbol = source.OriginalDefinition;
            if (!TypeSymbol.Equals(typeSymbol, destination.OriginalDefinition, TypeCompareKind.ConsiderEverything2))
            {
                return ThreeState.False;
            }

            return ThreeState.Unknown;
        }

        private bool HasVariantConversionNoCycleCheck(NamedTypeSymbol source, NamedTypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert((object)source != null);
            Debug.Assert((object)destination != null);

            var typeParameters = ArrayBuilder<TypeWithAnnotations>.GetInstance();
            var sourceTypeArguments = ArrayBuilder<TypeWithAnnotations>.GetInstance();
            var destinationTypeArguments = ArrayBuilder<TypeWithAnnotations>.GetInstance();

            try
            {
                source.OriginalDefinition.GetAllTypeArguments(typeParameters, ref useSiteDiagnostics);
                source.GetAllTypeArguments(sourceTypeArguments, ref useSiteDiagnostics);
                destination.GetAllTypeArguments(destinationTypeArguments, ref useSiteDiagnostics);

                Debug.Assert(TypeSymbol.Equals(source.OriginalDefinition, destination.OriginalDefinition, TypeCompareKind.AllIgnoreOptions));
                Debug.Assert(typeParameters.Count == sourceTypeArguments.Count);
                Debug.Assert(typeParameters.Count == destinationTypeArguments.Count);

                for (int paramIndex = 0; paramIndex < typeParameters.Count; ++paramIndex)
                {
                    var sourceTypeArgument = sourceTypeArguments[paramIndex];
                    var destinationTypeArgument = destinationTypeArguments[paramIndex];

                    // If they're identical then this one is automatically good, so skip it.
                    if (HasIdentityConversionInternal(sourceTypeArgument.Type, destinationTypeArgument.Type) &&
                        HasTopLevelNullabilityIdentityConversion(sourceTypeArgument, destinationTypeArgument))
                    {
                        continue;
                    }

                    TypeParameterSymbol typeParameterSymbol = (TypeParameterSymbol)typeParameters[paramIndex].Type;

                    switch (typeParameterSymbol.Variance)
                    {
                        case VarianceKind.None:
                            // System.IEquatable<T> is invariant for back compat reasons (dynamic type checks could start
                            // to succeed where they previously failed, creating different runtime behavior), but the uses
                            // require treatment specifically of nullability as contravariant, so we special case the
                            // behavior here. Normally we use GetWellKnownType for these kinds of checks, but in this
                            // case we don't want just the canonical IEquatable to be special-cased, we want all definitions
                            // to be treated as contravariant, in case there are other definitions in metadata that were
                            // compiled with that expectation.
                            if (isTypeIEquatable(destination.OriginalDefinition) &&
                                TypeSymbol.Equals(destinationTypeArgument.Type, sourceTypeArgument.Type, TypeCompareKind.AllNullableIgnoreOptions) &&
                                HasAnyNullabilityImplicitConversion(destinationTypeArgument, sourceTypeArgument))
                            {
                                return true;
                            }
                            return false;

                        case VarianceKind.Out:
                            if (!HasImplicitReferenceConversion(sourceTypeArgument, destinationTypeArgument, ref useSiteDiagnostics))
                            {
                                return false;
                            }
                            break;

                        case VarianceKind.In:
                            if (!HasImplicitReferenceConversion(destinationTypeArgument, sourceTypeArgument, ref useSiteDiagnostics))
                            {
                                return false;
                            }
                            break;

                        default:
                            throw ExceptionUtilities.UnexpectedValue(typeParameterSymbol.Variance);
                    }
                }
            }
            finally
            {
                typeParameters.Free();
                sourceTypeArguments.Free();
                destinationTypeArguments.Free();
            }

            return true;

            static bool isTypeIEquatable(NamedTypeSymbol type)
            {
                return type is
                {
                    IsInterface: true,
                    Name: "IEquatable",
                    ContainingNamespace: { Name: "System", ContainingNamespace: { IsGlobalNamespace: true } },
                    ContainingSymbol: { Kind: SymbolKind.Namespace },
                    TypeParameters: { Length: 1 }
                };
            }
        }

        // Spec 6.1.10
        private bool HasImplicitBoxingTypeParameterConversion(TypeParameterSymbol source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert((object)source != null);
            Debug.Assert((object)destination != null);

            if (source.IsReferenceType)
            {
                return false; // Not a boxing conversion; both source and destination are references.
            }

            // The following implicit conversions exist for a given type parameter T:
            //
            // * From T to its effective base class C.
            // * From T to any base class of C.
            // * From T to any interface implemented by C (or any interface variance-compatible with such)
            if (HasImplicitEffectiveBaseConversion(source, destination, ref useSiteDiagnostics))
            {
                return true;
            }

            // * From T to any interface type I in T's effective interface set, and
            //   from T to any base interface of I (or any interface variance-compatible with such)
            if (HasImplicitEffectiveInterfaceSetConversion(source, destination, ref useSiteDiagnostics))
            {
                return true;
            }

            // SPEC: From T to a type parameter U, provided T depends on U
            if ((destination.TypeKind == TypeKind.TypeParameter) &&
                source.DependsOn((TypeParameterSymbol)destination))
            {
                return true;
            }

            // SPEC: From T to a reference type I if it has an implicit conversion to a reference 
            // SPEC: type S0 and S0 has an identity conversion to S. At run-time the conversion 
            // SPEC: is executed the same way as the conversion to S0.

            // REVIEW: If T is not known to be a reference type then the only way this clause can
            // REVIEW: come into effect is if the target type is dynamic. Is that correct?

            if (destination.Kind == SymbolKind.DynamicType)
            {
                return true;
            }

            return false;
        }

        public bool HasBoxingConversion(TypeSymbol source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert((object)source != null);
            Debug.Assert((object)destination != null);

            // Certain type parameter conversions are classified as boxing conversions.
            if ((source.TypeKind == TypeKind.TypeParameter) &&
                HasImplicitBoxingTypeParameterConversion((TypeParameterSymbol)source, destination, ref useSiteDiagnostics))
            {
                return true;
            }

            // The rest of the boxing conversions only operate when going from a value type to a
            // reference type.
            if (!source.IsValueType || !destination.IsReferenceType)
            {
                return false;
            }

            // A boxing conversion exists from a nullable type to a reference type if and only if a
            // boxing conversion exists from the underlying type.
            if (source.IsNullableType())
            {
                return HasBoxingConversion(source.GetNullableUnderlyingType(), destination, ref useSiteDiagnostics);
            }

            // A boxing conversion exists from any non-nullable value type to object and dynamic, to
            // System.ValueType, and to any interface type variance-compatible with one implemented
            // by the non-nullable value type.  

            // Furthermore, an enum type can be converted to the type System.Enum.

            // We set the base class of the structs to System.ValueType, System.Enum, etc, so we can
            // just check here.

            // There are a couple of exceptions. The very special types ArgIterator, ArgumentHandle and 
            // TypedReference are not boxable: 

            if (source.IsRestrictedType())
            {
                return false;
            }

            if (destination.Kind == SymbolKind.DynamicType)
            {
                return !source.IsPointerOrFunctionPointer();
            }

            if (IsBaseClass(source, destination, ref useSiteDiagnostics))
            {
                return true;
            }

            if (HasAnyBaseInterfaceConversion(source, destination, ref useSiteDiagnostics))
            {
                return true;
            }

            return false;
        }

        internal static bool HasImplicitPointerToVoidConversion(TypeSymbol source, TypeSymbol destination)
        {
            Debug.Assert((object)source != null);
            Debug.Assert((object)destination != null);

            // SPEC: The set of implicit conversions is extended to include...
            // SPEC: ... from any pointer type to the type void*.

            return source.IsPointerOrFunctionPointer() && destination is PointerTypeSymbol { PointedAtType: { SpecialType: SpecialType.System_Void } };
        }

#nullable enable
        internal bool HasImplicitPointerConversion(TypeSymbol? source, TypeSymbol? destination, ref HashSet<DiagnosticInfo>? useSiteDiagnostics)
        {
            if (!(source is FunctionPointerTypeSymbol { Signature: { } sourceSig })
                || !(destination is FunctionPointerTypeSymbol { Signature: { } destinationSig }))
            {
                return false;
            }

            if (sourceSig.ParameterCount != destinationSig.ParameterCount ||
                sourceSig.CallingConvention != destinationSig.CallingConvention)
            {
                return false;
            }

            for (int i = 0; i < sourceSig.ParameterCount; i++)
            {
                var sourceParam = sourceSig.Parameters[i];
                var destinationParam = destinationSig.Parameters[i];

                if (sourceParam.RefKind != destinationParam.RefKind)
                {
                    return false;
                }

                if (!hasConversion(sourceParam.RefKind, destinationSig.Parameters[i].TypeWithAnnotations, sourceSig.Parameters[i].TypeWithAnnotations, ref useSiteDiagnostics))
                {
                    return false;
                }
            }

            return sourceSig.RefKind == destinationSig.RefKind
                   && hasConversion(sourceSig.RefKind, sourceSig.ReturnTypeWithAnnotations, destinationSig.ReturnTypeWithAnnotations, ref useSiteDiagnostics);

            bool hasConversion(RefKind refKind, TypeWithAnnotations sourceType, TypeWithAnnotations destinationType, ref HashSet<DiagnosticInfo>? useSiteDiagnostics)
            {
                switch (refKind)
                {
                    case RefKind.None:
                        return (!IncludeNullability || HasTopLevelNullabilityImplicitConversion(sourceType, destinationType))
                               && (HasIdentityOrImplicitReferenceConversion(sourceType.Type, destinationType.Type, ref useSiteDiagnostics)
                                   || HasImplicitPointerToVoidConversion(sourceType.Type, destinationType.Type)
                                   || HasImplicitPointerConversion(sourceType.Type, destinationType.Type, ref useSiteDiagnostics));

                    default:
                        return (!IncludeNullability || HasTopLevelNullabilityIdentityConversion(sourceType, destinationType))
                               && HasIdentityConversion(sourceType.Type, destinationType.Type);
                }
            }
        }
#nullable restore

        private bool HasIdentityOrReferenceConversion(TypeSymbol source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert((object)source != null);
            Debug.Assert((object)destination != null);

            if (HasIdentityConversionInternal(source, destination))
            {
                return true;
            }

            if (HasImplicitReferenceConversion(source, destination, ref useSiteDiagnostics))
            {
                return true;
            }

            if (HasExplicitReferenceConversion(source, destination, ref useSiteDiagnostics))
            {
                return true;
            }

            return false;
        }

        private bool HasExplicitReferenceConversion(TypeSymbol source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert((object)source != null);
            Debug.Assert((object)destination != null);

            // SPEC: The explicit reference conversions are:
            // SPEC: From object and dynamic to any other reference type.

            if (source.SpecialType == SpecialType.System_Object)
            {
                if (destination.IsReferenceType)
                {
                    return true;
                }
            }
            else if (source.Kind == SymbolKind.DynamicType && destination.IsReferenceType)
            {
                return true;
            }

            // SPEC: From any class-type S to any class-type T, provided S is a base class of T.
            if (destination.IsClassType() && IsBaseClass(destination, source, ref useSiteDiagnostics))
            {
                return true;
            }

            // SPEC: From any class-type S to any interface-type T, provided S is not sealed and provided S does not implement T.
            // ISSUE: class C : IEnumerable<Mammal> { } converting this to IEnumerable<Animal> is not an explicit conversion,
            // ISSUE: it is an implicit conversion.
            if (source.IsClassType() && destination.IsInterfaceType() && !source.IsSealed && !HasAnyBaseInterfaceConversion(source, destination, ref useSiteDiagnostics))
            {
                return true;
            }

            // SPEC: From any interface-type S to any class-type T, provided T is not sealed or provided T implements S.
            // ISSUE: What if T is sealed and implements an interface variance-convertible to S?
            // ISSUE: eg, sealed class C : IEnum<Mammal> { ... } you should be able to cast an IEnum<Animal> to C.
            if (source.IsInterfaceType() && destination.IsClassType() && (!destination.IsSealed || HasAnyBaseInterfaceConversion(destination, source, ref useSiteDiagnostics)))
            {
                return true;
            }

            // SPEC: From any interface-type S to any interface-type T, provided S is not derived from T.
            // ISSUE: This does not rule out identity conversions, which ought not to be classified as 
            // ISSUE: explicit reference conversions.
            // ISSUE: IEnumerable<Mammal> and IEnumerable<Animal> do not derive from each other but this is
            // ISSUE: not an explicit reference conversion, this is an implicit reference conversion.
            if (source.IsInterfaceType() && destination.IsInterfaceType() && !HasImplicitConversionToInterface(source, destination, ref useSiteDiagnostics))
            {
                return true;
            }

            // SPEC: UNDONE: From a reference type to a reference type T if it has an explicit reference conversion to a reference type T0 and T0 has an identity conversion T.
            // SPEC: UNDONE: From a reference type to an interface or delegate type T if it has an explicit reference conversion to an interface or delegate type T0 and either T0 is variance-convertible to T or T is variance-convertible to T0 (Â§13.1.3.2).

            if (HasExplicitArrayConversion(source, destination, ref useSiteDiagnostics))
            {
                return true;
            }

            if (HasExplicitDelegateConversion(source, destination, ref useSiteDiagnostics))
            {
                return true;
            }

            if (HasExplicitReferenceTypeParameterConversion(source, destination, ref useSiteDiagnostics))
            {
                return true;
            }

            return false;
        }

        // Spec 6.2.7 Explicit conversions involving type parameters
        private bool HasExplicitReferenceTypeParameterConversion(TypeSymbol source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert((object)source != null);
            Debug.Assert((object)destination != null);

            TypeParameterSymbol s = source as TypeParameterSymbol;
            TypeParameterSymbol t = destination as TypeParameterSymbol;

            // SPEC: The following explicit conversions exist for a given type parameter T:

            // SPEC: If T is known to be a reference type, the conversions are all classified as explicit reference conversions.
            // SPEC: If T is not known to be a reference type, the conversions are classified as unboxing conversions.

            // SPEC: From the effective base class C of T to T and from any base class of C to T. 
            if ((object)t != null && t.IsReferenceType)
            {
                for (var type = t.EffectiveBaseClass(ref useSiteDiagnostics); (object)type != null; type = type.BaseTypeWithDefinitionUseSiteDiagnostics(ref useSiteDiagnostics))
                {
                    if (HasIdentityConversionInternal(type, source))
                    {
                        return true;
                    }
                }
            }

            // SPEC: From any interface type to T. 
            if ((object)t != null && source.IsInterfaceType() && t.IsReferenceType)
            {
                return true;
            }

            // SPEC: From T to any interface-type I provided there is not already an implicit conversion from T to I.
            if ((object)s != null && s.IsReferenceType && destination.IsInterfaceType() && !HasImplicitReferenceTypeParameterConversion(s, destination, ref useSiteDiagnostics))
            {
                return true;
            }

            // SPEC: From a type parameter U to T, provided T depends on U (Â§10.1.5)
            if ((object)s != null && (object)t != null && t.IsReferenceType && t.DependsOn(s))
            {
                return true;
            }

            return false;
        }

        // Spec 6.2.7 Explicit conversions involving type parameters
        private bool HasUnboxingTypeParameterConversion(TypeSymbol source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert((object)source != null);
            Debug.Assert((object)destination != null);

            TypeParameterSymbol s = source as TypeParameterSymbol;
            TypeParameterSymbol t = destination as TypeParameterSymbol;

            // SPEC: The following explicit conversions exist for a given type parameter T:

            // SPEC: If T is known to be a reference type, the conversions are all classified as explicit reference conversions.
            // SPEC: If T is not known to be a reference type, the conversions are classified as unboxing conversions.

            // SPEC: From the effective base class C of T to T and from any base class of C to T. 
            if ((object)t != null && !t.IsReferenceType)
            {
                for (var type = t.EffectiveBaseClass(ref useSiteDiagnostics); (object)type != null; type = type.BaseTypeWithDefinitionUseSiteDiagnostics(ref useSiteDiagnostics))
                {
                    if (TypeSymbol.Equals(type, source, TypeCompareKind.ConsiderEverything2))
                    {
                        return true;
                    }
                }
            }

            // SPEC: From any interface type to T. 
            if (source.IsInterfaceType() && (object)t != null && !t.IsReferenceType)
            {
                return true;
            }

            // SPEC: From T to any interface-type I provided there is not already an implicit conversion from T to I.
            if ((object)s != null && !s.IsReferenceType && destination.IsInterfaceType() && !HasImplicitReferenceTypeParameterConversion(s, destination, ref useSiteDiagnostics))
            {
                return true;
            }

            // SPEC: From a type parameter U to T, provided T depends on U (Â§10.1.5)
            if ((object)s != null && (object)t != null && !t.IsReferenceType && t.DependsOn(s))
            {
                return true;
            }

            return false;
        }

        private bool HasExplicitDelegateConversion(TypeSymbol source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert((object)source != null);
            Debug.Assert((object)destination != null);

            // SPEC: From System.Delegate and the interfaces it implements to any delegate-type.
            // We also support System.MulticastDelegate in the implementation, in spite of it not being mentioned in the spec.
            if (destination.IsDelegateType())
            {
                if (source.SpecialType == SpecialType.System_Delegate || source.SpecialType == SpecialType.System_MulticastDelegate)
                {
                    return true;
                }

                if (HasImplicitConversionToInterface(this.corLibrary.GetDeclaredSpecialType(SpecialType.System_Delegate), source, ref useSiteDiagnostics))
                {
                    return true;
                }
            }

            // SPEC: From D<S1...Sn> to a D<T1...Tn> where D<X1...Xn> is a generic delegate type, D<S1...Sn> is not compatible with or identical to D<T1...Tn>, 
            // SPEC: and for each type parameter Xi of D the following holds:
            // SPEC: If Xi is invariant, then Si is identical to Ti.
            // SPEC: If Xi is covariant, then there is an implicit or explicit identity or reference conversion from Si to Ti.
            // SPECL If Xi is contravariant, then Si and Ti are either identical or both reference types.

            if (!source.IsDelegateType() || !destination.IsDelegateType())
            {
                return false;
            }

            if (!TypeSymbol.Equals(source.OriginalDefinition, destination.OriginalDefinition, TypeCompareKind.ConsiderEverything2))
            {
                return false;
            }

            var sourceType = (NamedTypeSymbol)source;
            var destinationType = (NamedTypeSymbol)destination;
            var original = sourceType.OriginalDefinition;

            if (HasIdentityConversionInternal(source, destination))
            {
                return false;
            }

            if (HasDelegateVarianceConversion(source, destination, ref useSiteDiagnostics))
            {
                return false;
            }

            var sourceTypeArguments = sourceType.TypeArgumentsWithDefinitionUseSiteDiagnostics(ref useSiteDiagnostics);
            var destinationTypeArguments = destinationType.TypeArgumentsWithDefinitionUseSiteDiagnostics(ref useSiteDiagnostics);

            for (int i = 0; i < sourceTypeArguments.Length; ++i)
            {
                var sourceArg = sourceTypeArguments[i].Type;
                var destinationArg = destinationTypeArguments[i].Type;

                switch (original.TypeParameters[i].Variance)
                {
                    case VarianceKind.None:
                        if (!HasIdentityConversionInternal(sourceArg, destinationArg))
                        {
                            return false;
                        }

                        break;
                    case VarianceKind.Out:
                        if (!HasIdentityOrReferenceConversion(sourceArg, destinationArg, ref useSiteDiagnostics))
                        {
                            return false;
                        }

                        break;
                    case VarianceKind.In:
                        bool hasIdentityConversion = HasIdentityConversionInternal(sourceArg, destinationArg);
                        bool bothAreReferenceTypes = sourceArg.IsReferenceType && destinationArg.IsReferenceType;
                        if (!(hasIdentityConversion || bothAreReferenceTypes))
                        {
                            return false;
                        }

                        break;
                }
            }

            return true;
        }

        private bool HasExplicitArrayConversion(TypeSymbol source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert((object)source != null);
            Debug.Assert((object)destination != null);

            var sourceArray = source as ArrayTypeSymbol;
            var destinationArray = destination as ArrayTypeSymbol;

            // SPEC: From an array-type S with an element type SE to an array-type T with an element type TE, provided all of the following are true:
            // SPEC: S and T differ only in element type. (In other words, S and T have the same number of dimensions.)
            // SPEC: Both SE and TE are reference-types.
            // SPEC: An explicit reference conversion exists from SE to TE.
            if ((object)sourceArray != null && (object)destinationArray != null)
            {
                // HasExplicitReferenceConversion checks that SE and TE are reference types so
                // there's no need for that check here. Moreover, it's not as simple as checking
                // IsReferenceType, at least not in the case of type parameters, since SE will be
                // considered a reference type implicitly in the case of "where TE : class, SE" even
                // though SE.IsReferenceType may be false. Again, HasExplicitReferenceConversion
                // already handles these cases.
                return sourceArray.HasSameShapeAs(destinationArray) &&
                    HasExplicitReferenceConversion(sourceArray.ElementType, destinationArray.ElementType, ref useSiteDiagnostics);
            }

            // SPEC: From System.Array and the interfaces it implements to any array-type.
            if ((object)destinationArray != null)
            {
                if (source.SpecialType == SpecialType.System_Array)
                {
                    return true;
                }

                foreach (var iface in this.corLibrary.GetDeclaredSpecialType(SpecialType.System_Array).AllInterfacesWithDefinitionUseSiteDiagnostics(ref useSiteDiagnostics))
                {
                    if (HasIdentityConversionInternal(iface, source))
                    {
                        return true;
                    }
                }
            }

            // SPEC: From a single-dimensional array type S[] to System.Collections.Generic.IList<T> and its base interfaces
            // SPEC: provided that there is an explicit reference conversion from S to T.

            // The framework now also allows arrays to be converted to IReadOnlyList<T> and IReadOnlyCollection<T>; we 
            // honor that as well.

            if ((object)sourceArray != null && sourceArray.IsSZArray && destination.IsPossibleArrayGenericInterface())
            {
                if (HasExplicitReferenceConversion(sourceArray.ElementType, ((NamedTypeSymbol)destination).TypeArgumentWithDefinitionUseSiteDiagnostics(0, ref useSiteDiagnostics).Type, ref useSiteDiagnostics))
                {
                    return true;
                }
            }

            // SPEC: From System.Collections.Generic.IList<S> and its base interfaces to a single-dimensional array type T[], 
            // provided that there is an explicit identity or reference conversion from S to T.

            // Similarly, we honor IReadOnlyList<S> and IReadOnlyCollection<S> in the same way.
            if ((object)destinationArray != null && destinationArray.IsSZArray)
            {
                var specialDefinition = ((TypeSymbol)source.OriginalDefinition).SpecialType;

                if (specialDefinition == SpecialType.System_Collections_Generic_IList_T ||
                    specialDefinition == SpecialType.System_Collections_Generic_ICollection_T ||
                    specialDefinition == SpecialType.System_Collections_Generic_IEnumerable_T ||
                    specialDefinition == SpecialType.System_Collections_Generic_IReadOnlyList_T ||
                    specialDefinition == SpecialType.System_Collections_Generic_IReadOnlyCollection_T)
                {
                    var sourceElement = ((NamedTypeSymbol)source).TypeArgumentWithDefinitionUseSiteDiagnostics(0, ref useSiteDiagnostics).Type;
                    var destinationElement = destinationArray.ElementType;

                    if (HasIdentityConversionInternal(sourceElement, destinationElement))
                    {
                        return true;
                    }

                    if (HasImplicitReferenceConversion(sourceElement, destinationElement, ref useSiteDiagnostics))
                    {
                        return true;
                    }

                    if (HasExplicitReferenceConversion(sourceElement, destinationElement, ref useSiteDiagnostics))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool HasUnboxingConversion(TypeSymbol source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert((object)source != null);
            Debug.Assert((object)destination != null);

            if (destination.IsPointerOrFunctionPointer())
            {
                return false;
            }

            // Ref-like types cannot be boxed or unboxed
            if (destination.IsRestrictedType())
            {
                return false;
            }

            // SPEC: An unboxing conversion permits a reference type to be explicitly converted to a value-type. 
            // SPEC: An unboxing conversion exists from the types object and System.ValueType to any non-nullable-value-type, 
            var specialTypeSource = source.SpecialType;

            if (specialTypeSource == SpecialType.System_Object || specialTypeSource == SpecialType.System_ValueType)
            {
                if (destination.IsValueType && !destination.IsNullableType())
                {
                    return true;
                }
            }

            // SPEC: and from any interface-type to any non-nullable-value-type that implements the interface-type. 

            if (source.IsInterfaceType() &&
                destination.IsValueType &&
                !destination.IsNullableType() &&
                HasBoxingConversion(destination, source, ref useSiteDiagnostics))
            {
                return true;
            }

            // SPEC: Furthermore type System.Enum can be unboxed to any enum-type.
            if (source.SpecialType == SpecialType.System_Enum && destination.IsEnumType())
            {
                return true;
            }

            // SPEC: An unboxing conversion exists from a reference type to a nullable-type if an unboxing 
            // SPEC: conversion exists from the reference type to the underlying non-nullable-value-type 
            // SPEC: of the nullable-type.
            if (source.IsReferenceType &&
                destination.IsNullableType() &&
                HasUnboxingConversion(source, destination.GetNullableUnderlyingType(), ref useSiteDiagnostics))
            {
                return true;
            }

            // SPEC: UNDONE A value type S has an unboxing conversion from an interface type I if it has an unboxing 
            // SPEC: UNDONE conversion from an interface type I0 and I0 has an identity conversion to I.

            // SPEC: UNDONE A value type S has an unboxing conversion from an interface type I if it has an unboxing conversion 
            // SPEC: UNDONE from an interface or delegate type I0 and either I0 is variance-convertible to I or I is variance-convertible to I0.

            if (HasUnboxingTypeParameterConversion(source, destination, ref useSiteDiagnostics))
            {
                return true;
            }

            return false;
        }

        private static bool HasPointerToPointerConversion(TypeSymbol source, TypeSymbol destination)
        {
            Debug.Assert((object)source != null);
            Debug.Assert((object)destination != null);

            return source.IsPointerOrFunctionPointer() && destination.IsPointerOrFunctionPointer();
        }

        private static bool HasPointerToIntegerConversion(TypeSymbol source, TypeSymbol destination)
        {
            Debug.Assert((object)source != null);
            Debug.Assert((object)destination != null);

            if (!source.IsPointerOrFunctionPointer())
            {
                return false;
            }

            // SPEC OMISSION: 
            // 
            // The spec should state that any pointer type is convertible to
            // sbyte, byte, ... etc, or any corresponding nullable type.

            switch (destination.StrippedType().SpecialType)
            {
                case SpecialType.System_SByte:
                case SpecialType.System_Byte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                    return true;
            }

            return false;
        }

        private static bool HasIntegerToPointerConversion(TypeSymbol source, TypeSymbol destination)
        {
            Debug.Assert((object)source != null);
            Debug.Assert((object)destination != null);

            if (!destination.IsPointerOrFunctionPointer())
            {
                return false;
            }

            // Note that void* is convertible to int?, but int? is not convertible to void*.

            switch (source.SpecialType)
            {
                case SpecialType.System_SByte:
                case SpecialType.System_Byte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                    return true;
            }

            return false;
        }
    }
}
