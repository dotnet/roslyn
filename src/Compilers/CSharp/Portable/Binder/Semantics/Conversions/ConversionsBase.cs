// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract partial class ConversionsBase
    {
        private const int MaximumRecursionDepth = 50;

        protected readonly AssemblySymbol corLibrary;
        private readonly int _currentRecursionDepth;

        protected ConversionsBase(AssemblySymbol corLibrary, int currentRecursionDepth)
        {
            Debug.Assert((object)corLibrary != null);
            this.corLibrary = corLibrary;
            _currentRecursionDepth = currentRecursionDepth;
        }

        protected abstract ConversionsBase CreateInstance(int currentRecursionDepth);

        public abstract Conversion GetMethodGroupConversion(BoundMethodGroup source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics);

        protected abstract Conversion GetInterpolatedStringConversion(BoundInterpolatedString source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics);

        /// <summary>
        /// Attempt a quick classification of builtin conversions.  As result of "no conversion"
        /// means that there is no built-in conversion, though there still may be a user-defined
        /// conversion if compiling against a custom mscorlib.
        /// </summary>
        public static Conversion FastClassifyConversion(TypeSymbol source, TypeSymbol target)
        {
            ConversionKind convKind = ConversionEasyOut.ClassifyConversion(source, target);
            return new Conversion(convKind);
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
        public static bool IsBaseInterface(TypeSymbol baseType, TypeSymbol derivedType, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
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
                if (HasIdentityConversion(iface, baseType))
                {
                    return true;
                }
            }

            return false;
        }

        // IsBaseClassOfClass determines whether the purported derived type is a class, and if so,
        // if the purported base type is one of its base classes. 
        public static bool IsBaseClassOfClass(TypeSymbol baseType, TypeSymbol derivedType, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert((object)baseType != null);
            Debug.Assert((object)derivedType != null);
            return derivedType.IsClassType() && IsBaseClass(derivedType, baseType, ref useSiteDiagnostics);
        }

        // IsBaseClass returns true if and only if baseType is a base class of derivedType, period.
        //
        // * interfaces do not have base classes. (Structs, enums and classes other than object do.)
        // * a class is not a base class of itself
        // * type parameters do not have base classes. (They have "effective base classes".)
        // * all base classes must be classes
        // * dynamics are removed; if we have class D : B<dynamic> then B<object> is a 
        //   base class of D. However, dynamic is never a base class of anything.
        public static bool IsBaseClass(TypeSymbol derivedType, TypeSymbol baseType, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
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
                if (HasIdentityConversion(b, baseType))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines if the source type is convertible to the destination type via
        /// any conversion: implicit, explicit, user-defined or built-in.
        /// </summary>
        /// <remarks>
        /// It is rare but possible for a source type to be convertible to a destination type
        /// by both an implicit user-defined conversion and a built-in explicit conversion.
        /// In that circumstance, this method classifies the conversion as the implicit conversion.
        /// </remarks>
        public Conversion ClassifyConversion(TypeSymbol source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics, bool builtinOnly = false)
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
                Conversion conversion1 = ClassifyImplicitBuiltInConversionSlow(source, destination, ref useSiteDiagnostics);
                if (conversion1.Exists)
                {
                    return conversion1;
                }
            }

            if (!builtinOnly)
            {
                Conversion conversion1 = GetImplicitUserDefinedConversion(null, source, destination, ref useSiteDiagnostics);
                if (conversion1.Exists)
                {
                    return conversion1;
                }
            }

            var conversion = ClassifyExplicitBuiltInOnlyConversion(source, destination, ref useSiteDiagnostics);
            if (conversion.Exists)
            {
                return conversion;
            }

            return builtinOnly ? conversion : GetExplicitUserDefinedConversion(null, source, destination, ref useSiteDiagnostics);
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
        public Conversion ClassifyConversionForCast(TypeSymbol source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
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
                Conversion conversion1 = ClassifyImplicitBuiltInConversionSlow(source, destination, ref useSiteDiagnostics);
                if (conversion1.Exists)
                {
                    return conversion1;
                }
            }

            Conversion conversion = ClassifyExplicitBuiltInOnlyConversion(source, destination, ref useSiteDiagnostics);
            if (conversion.Exists)
            {
                return conversion;
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

            conversion = GetExplicitUserDefinedConversion(null, source, destination, ref useSiteDiagnostics);
            if (conversion.Exists)
            {
                return conversion;
            }

            return GetImplicitUserDefinedConversion(null, source, destination, ref useSiteDiagnostics);
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
                conversion = ClassifyStandardImplicitConversion(destination, source, ref useSiteDiagnostics);
                switch (conversion.Kind)
                {
                    case ConversionKind.ImplicitNumeric:
                        return Conversion.ExplicitNumeric;
                    case ConversionKind.ImplicitNullable:
                        return Conversion.ExplicitNullable;
                    case ConversionKind.ImplicitReference:
                        return Conversion.ExplicitReference;
                    case ConversionKind.Boxing:
                        return Conversion.Unboxing;
                    case ConversionKind.NoConversion:
                        return Conversion.NoConversion;
                    case ConversionKind.PointerToVoid:
                        return Conversion.PointerToPointer;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(conversion.Kind);
                }
            }

            return Conversion.NoConversion;
        }

        internal Conversion ClassifyStandardImplicitConversion(BoundExpression sourceExpression, TypeSymbol source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
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
            // all of the implicit conversions that are allowed based on an expression.

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

        internal Conversion ClassifyStandardImplicitConversion(TypeSymbol source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert((object)source != null);
            Debug.Assert((object)destination != null);

            if (HasIdentityConversion(source, destination))
            {
                return Conversion.Identity;
            }

            if (HasImplicitNumericConversion(source, destination))
            {
                return Conversion.ImplicitNumeric;
            }

            if (HasImplicitNullableConversion(source, destination))
            {
                return Conversion.ImplicitNullable;
            }

            if (HasImplicitReferenceConversion(source, destination, ref useSiteDiagnostics))
            {
                return Conversion.ImplicitReference;
            }

            if (HasBoxingConversion(source, destination, ref useSiteDiagnostics))
            {
                return Conversion.Boxing;
            }

            if (HasImplicitPointerConversion(source, destination))
            {
                return Conversion.PointerToVoid;
            }

            return Conversion.NoConversion;
        }

        private Conversion ClassifyImplicitBuiltInConversionSlow(TypeSymbol source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert((object)source != null);
            Debug.Assert((object)destination != null);

            if (source.SpecialType == SpecialType.System_Void || destination.SpecialType == SpecialType.System_Void)
            {
                return Conversion.NoConversion;
            }

            Conversion conversion = ClassifyStandardImplicitConversion(source, destination, ref useSiteDiagnostics);
            if (conversion.Exists)
            {
                return conversion;
            }

            //if (HasImplicitDynamicConversion(source, destination))
            //{
            //    return Conversion.Dynamic;
            //}

            return Conversion.NoConversion;
        }

        private Conversion GetImplicitUserDefinedConversion(BoundExpression sourceExpression, TypeSymbol source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            var conversionResult = AnalyzeImplicitUserDefinedConversions(sourceExpression, source, destination, ref useSiteDiagnostics);
            return new Conversion(conversionResult, isImplicit: true);
        }

        /// <summary>
        /// Determines if the source type is convertible to the destination type via
        /// any user-defined or built-in implicit conversion.
        /// </summary>
        /// <remarks>
        /// Not all built-in explicit conversions are standard explicit conversions.
        /// </remarks>
        public Conversion ClassifyImplicitConversion(TypeSymbol source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert((object)source != null);
            Debug.Assert((object)destination != null);

            //PERF: identity conversions are very common, check for that first.
            if (HasIdentityConversion(source, destination))
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

        private Conversion ClassifyExplicitBuiltInOnlyConversion(TypeSymbol source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert((object)source != null);
            Debug.Assert((object)destination != null);

            // The call to HasExplicitNumericConversion isn't necessary, because it is always tested
            // already by the "FastConversion" code.
            Debug.Assert(!HasExplicitNumericConversion(source, destination));

            if (source.SpecialType == SpecialType.System_Void || destination.SpecialType == SpecialType.System_Void)
            {
                return Conversion.NoConversion;
            }

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

            if (HasExplicitNullableConversion(source, destination))
            {
                return Conversion.ExplicitNullable;
            }

            if (HasExplicitReferenceConversion(source, destination, ref useSiteDiagnostics))
            {
                return (source.Kind == SymbolKind.DynamicType) ? Conversion.ExplicitDynamic : Conversion.ExplicitReference;
            }

            if (HasUnboxingConversion(source, destination, ref useSiteDiagnostics))
            {
                return Conversion.Unboxing;
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

        public static bool HasIdentityConversion(TypeSymbol type1, TypeSymbol type2)
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

            return type1.Equals(type2, TypeSymbolEqualityOptions.SameType);
        }

        public static bool HasIdentityConversionToAny<T>(T type, ArrayBuilder<T> targetTypes)
            where T : TypeSymbol
        {
            for (int i = 0, n = targetTypes.Count; i < n; i++)
            {
                if (HasIdentityConversion(type, targetTypes[i]))
                {
                    return true;
                }
            }

            return false;
        }

        public Conversion ConvertExtensionMethodThisArg(TypeSymbol parameterType, TypeSymbol thisType, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert((object)thisType != null);
            var conversion = this.ClassifyImplicitConversion(thisType, parameterType, ref useSiteDiagnostics);
            return IsValidExtensionMethodThisArgConversion(conversion) ? conversion : Conversion.NoConversion;
        }

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
                default:
                    return false;
            }
        }

        private enum NumericType
        {
            SByte,
            Byte,
            Short,
            UShort,
            Int,
            UInt,
            Long,
            ULong,
            Char,
            Float,
            Double,
            Decimal
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

            if (otherType.TypeKind == TypeKind.Pointer)
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

        private static bool HasImplicitNullableConversion(TypeSymbol source, TypeSymbol destination)
        {
            Debug.Assert((object)source != null);
            Debug.Assert((object)destination != null);

            // SPEC: Predefined implicit conversions that operate on non-nullable value types can also be used with 
            // SPEC: nullable forms of those types. For each of the predefined implicit identity and numeric conversions
            // SPEC: that convert from a non-nullable value type S to a non-nullable value type T, the following implicit 
            // SPEC: nullable conversions exist:
            // SPEC: * An implicit conversion from S? to T?.
            // SPEC: * An implicit conversion from S to T?.
            if (!destination.IsNullableType())
            {
                return false;
            }

            TypeSymbol unwrappedDestination = destination.GetNullableUnderlyingType();
            TypeSymbol unwrappedSource = source.StrippedType();

            if (!unwrappedSource.IsValueType)
            {
                return false;
            }

            if (HasIdentityConversion(unwrappedSource, unwrappedDestination))
            {
                return true;
            }

            if (HasImplicitNumericConversion(unwrappedSource, unwrappedDestination))
            {
                return true;
            }

            return false;
        }

        private static bool HasExplicitNullableConversion(TypeSymbol source, TypeSymbol destination)
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
                return false;
            }

            TypeSymbol sourceUnderlying = source.StrippedType();
            TypeSymbol destinationUnderlying = destination.StrippedType();

            if (HasIdentityConversion(sourceUnderlying, destinationUnderlying))
            {
                return true;
            }

            if (HasImplicitNumericConversion(sourceUnderlying, destinationUnderlying))
            {
                return true;
            }

            if (HasExplicitNumericConversion(sourceUnderlying, destinationUnderlying))
            {
                return true;
            }

            if (HasExplicitEnumerationConversion(sourceUnderlying, destinationUnderlying))
            {
                return true;
            }

            if (HasPointerToIntegerConversion(sourceUnderlying, destinationUnderlying))
            {
                return true;
            }

            return false;
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
            // * Both SE and TE are reference types.
            // * An implicit reference conversion exists from SE to TE.
            return
                s.HasSameShapeAs(d) &&
                HasImplicitReferenceConversion(s.ElementType.TypeSymbol, d.ElementType.TypeSymbol, ref useSiteDiagnostics);
        }

        public bool HasIdentityOrImplicitReferenceConversion(TypeSymbol source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert((object)source != null);
            Debug.Assert((object)destination != null);
            if (HasIdentityConversion(source, destination))
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
            return (object)expressionType != null && expressionType.Kind == SymbolKind.DynamicType && !destination.IsPointerType();
        }

        private static bool HasExplicitDynamicConversion(TypeSymbol source, TypeSymbol destination)
        {
            // SPEC: An explicit dynamic conversion exists from an expression of type dynamic to any type T. 
            // ISSUE: Note that this is exactly the same as an implicit dynamic conversion. Conversion of
            // ISSUE: dynamic to int is both an explicit dynamic conversion and an implicit dynamic conversion.

            Debug.Assert((object)source != null);
            Debug.Assert((object)destination != null);
            return source.Kind == SymbolKind.DynamicType && !destination.IsPointerType();
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

            TypeSymbol argument0 = destinationAgg.TypeArgumentWithDefinitionUseSiteDiagnostics(0, ref useSiteDiagnostics).TypeSymbol;

            return HasIdentityOrImplicitReferenceConversion(source.ElementType.TypeSymbol, argument0, ref useSiteDiagnostics);
        }

        private bool HasImplicitReferenceConversion(TypeSymbol source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
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

                    if (HasImplicitConversionToInterface(source, destination, ref useSiteDiagnostics))
                    {
                        return true;
                    }
                    break;

                case TypeKind.Interface:
                    // SPEC: From any interface-type S to any interface-type T, provided S is derived from T.
                    // NOTE: This handles variance conversions
                    if (HasImplicitConversionToInterface(source, destination, ref useSiteDiagnostics))
                    {
                        return true;
                    }
                    break;

                case TypeKind.Delegate:
                    // SPEC: From any delegate-type to System.Delegate and the interfaces it implements.
                    // NOTE: This handles variance conversions.
                    if (HasImplicitConversionFromDelegate(source, destination, ref useSiteDiagnostics))
                    {
                        return true;
                    }
                    break;

                case TypeKind.TypeParameter:
                    if (HasImplicitReferenceTypeParameterConversion((TypeParameterSymbol)source, destination, ref useSiteDiagnostics))
                    {
                        return true;
                    }
                    break;

                case TypeKind.Array:
                    // SPEC: From an array-type S ... to an array-type T, provided ...
                    // SPEC: From any array-type to System.Array and the interfaces it implements.
                    // SPEC: From a single-dimensional array type S[] to IList<T>, provided ...
                    if (HasImplicitConversionFromArray(source, destination, ref useSiteDiagnostics))
                    {
                        return true;
                    }
                    break;
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
            // * From any interface type S to any interface type T provided S implements an interface
            //   convertible to T.
            // * From any interface type S to any interface type T provided S is not T and S is 
            //   an interface convertible to T.

            if (source.IsClassType() && HasAnyBaseInterfaceConversion(source, destination, ref useSiteDiagnostics))
            {
                return true;
            }

            if (source.IsInterfaceType() && HasAnyBaseInterfaceConversion(source, destination, ref useSiteDiagnostics))
            {
                return true;
            }

            if (source.IsInterfaceType() && source != destination && HasInterfaceVarianceConversion(source, destination, ref useSiteDiagnostics))
            {
                return true;
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
            if (HasIdentityConversion(effectiveBaseClass, destination))
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
            if (_currentRecursionDepth >= MaximumRecursionDepth)
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

            return this.CreateInstance(_currentRecursionDepth + 1).
                HasVariantConversionNoCycleCheck(source, destination, ref useSiteDiagnostics);
        }

        private static ThreeState HasVariantConversionQuick(NamedTypeSymbol source, NamedTypeSymbol destination)
        {
            Debug.Assert((object)source != null);
            Debug.Assert((object)destination != null);

            if (HasIdentityConversion(source, destination))
            {
                return ThreeState.True;
            }

            NamedTypeSymbol typeSymbol = source.OriginalDefinition;
            if (typeSymbol != destination.OriginalDefinition)
            {
                return ThreeState.False;
            }

            return ThreeState.Unknown;
        }

        private bool HasVariantConversionNoCycleCheck(NamedTypeSymbol source, NamedTypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert((object)source != null);
            Debug.Assert((object)destination != null);

            var typeParameters = ArrayBuilder<TypeSymbol>.GetInstance();
            var sourceTypeArguments = ArrayBuilder<TypeSymbol>.GetInstance();
            var destinationTypeArguments = ArrayBuilder<TypeSymbol>.GetInstance();

            try
            {
                source.OriginalDefinition.GetAllTypeArguments(typeParameters, ref useSiteDiagnostics);
                source.GetAllTypeArguments(sourceTypeArguments, ref useSiteDiagnostics);
                destination.GetAllTypeArguments(destinationTypeArguments, ref useSiteDiagnostics);

                Debug.Assert(typeParameters.Count == sourceTypeArguments.Count);
                Debug.Assert(typeParameters.Count == destinationTypeArguments.Count);

                for (int paramIndex = 0; paramIndex < typeParameters.Count; ++paramIndex)
                {
                    TypeSymbol sourceTypeArgument = sourceTypeArguments[paramIndex];
                    TypeSymbol destinationTypeArgument = destinationTypeArguments[paramIndex];

                    // If they're identical then this one is automatically good, so skip it.
                    if (HasIdentityConversion(sourceTypeArgument, destinationTypeArgument))
                    {
                        continue;
                    }

                    TypeParameterSymbol typeParameterSymbol = (TypeParameterSymbol)typeParameters[paramIndex];
                    if (typeParameterSymbol.Variance == VarianceKind.None)
                    {
                        return false;
                    }

                    if (typeParameterSymbol.Variance == VarianceKind.Out && !HasImplicitReferenceConversion(sourceTypeArgument, destinationTypeArgument, ref useSiteDiagnostics))
                    {
                        return false;
                    }

                    if (typeParameterSymbol.Variance == VarianceKind.In && !HasImplicitReferenceConversion(destinationTypeArgument, sourceTypeArgument, ref useSiteDiagnostics))
                    {
                        return false;
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
                return !source.IsPointerType();
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

        private static bool HasImplicitPointerConversion(TypeSymbol source, TypeSymbol destination)
        {
            Debug.Assert((object)source != null);
            Debug.Assert((object)destination != null);

            // SPEC: The set of implicit conversions is extended to include...
            // SPEC: ... from any pointer type to the type void*.

            PointerTypeSymbol pd = destination as PointerTypeSymbol;
            PointerTypeSymbol ps = source as PointerTypeSymbol;
            return (object)pd != null && (object)ps != null && pd.PointedAtType.SpecialType == SpecialType.System_Void;
        }

        private bool HasIdentityOrReferenceConversion(TypeSymbol source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert((object)source != null);
            Debug.Assert((object)destination != null);

            if (HasIdentityConversion(source, destination))
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
            if (IsBaseClassOfClass(source, destination, ref useSiteDiagnostics))
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
                    if (HasIdentityConversion(type, source))
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
                    if (type == source)
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

            if (source.OriginalDefinition != destination.OriginalDefinition)
            {
                return false;
            }

            var sourceType = (NamedTypeSymbol)source;
            var destinationType = (NamedTypeSymbol)destination;
            var original = sourceType.OriginalDefinition;

            if (HasIdentityConversion(source, destination))
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
                var sourceArg = sourceTypeArguments[i].TypeSymbol;
                var destinationArg = destinationTypeArguments[i].TypeSymbol;

                switch (original.TypeParameters[i].Variance)
                {
                    case VarianceKind.None:
                        if (!HasIdentityConversion(sourceArg, destinationArg))
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
                        bool hasIdentityConversion = HasIdentityConversion(sourceArg, destinationArg);
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
                    HasExplicitReferenceConversion(sourceArray.ElementType.TypeSymbol, destinationArray.ElementType.TypeSymbol, ref useSiteDiagnostics);
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
                    if (HasIdentityConversion(iface, source))
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
                if (HasExplicitReferenceConversion(sourceArray.ElementType.TypeSymbol, ((NamedTypeSymbol)destination).TypeArgumentWithDefinitionUseSiteDiagnostics(0, ref useSiteDiagnostics).TypeSymbol, ref useSiteDiagnostics))
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
                    var sourceElement = ((NamedTypeSymbol)source).TypeArgumentWithDefinitionUseSiteDiagnostics(0, ref useSiteDiagnostics).TypeSymbol;
                    var destinationElement = destinationArray.ElementType.TypeSymbol;

                    if (HasIdentityConversion(sourceElement, destinationElement))
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

            if (destination.IsPointerType())
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

            return source is PointerTypeSymbol && destination is PointerTypeSymbol;
        }

        private static bool HasPointerToIntegerConversion(TypeSymbol source, TypeSymbol destination)
        {
            Debug.Assert((object)source != null);
            Debug.Assert((object)destination != null);

            if (!(source is PointerTypeSymbol))
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

            if (!(destination is PointerTypeSymbol))
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
