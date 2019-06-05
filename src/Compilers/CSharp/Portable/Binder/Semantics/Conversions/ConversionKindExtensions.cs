// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.CSharp.ConversionKind;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static class ConversionKindExtensions
    {
        public static bool IsDynamic(this ConversionKind conversionKind)
        {
            return conversionKind == ImplicitDynamic || conversionKind == ExplicitDynamic;
        }

        public static bool IsConversionFromExpression(this ConversionKind conversionKind)
        {
            switch (conversionKind)
            {
                case ImplicitEnumeration:
                case ImplicitThrow:
                case ImplicitTupleLiteral:
                case ExplicitTupleLiteral:
                case DefaultOrNullLiteral:
                case NullToPointer:
                case ImplicitDynamic:
                case ExplicitDynamic:
                case ImplicitConstant:
                case AnonymousFunction:
                case ConversionKind.MethodGroup:
                case InterpolatedString:
                case SwitchExpression:
                case StackAllocToPointerType:
                case StackAllocToSpanType:
                    // these are conversions from expression
                    return true;

                case Identity:
                case ImplicitNumeric:
                case ImplicitTuple:
                case ExplicitTuple:
                case ImplicitNullable:
                case ImplicitReference:
                case Boxing:
                case PointerToVoid:
                case ImplicitUserDefined:
                case ExplicitNumeric:
                case ExplicitEnumeration:
                case ExplicitNullable:
                case ExplicitReference:
                case Unboxing:
                case ExplicitUserDefined:
                case PointerToPointer:
                case IntegerToPointer:
                case PointerToInteger:
                case IntPtr:
                    // these are conversions from type
                    return false;

                case NoConversion:
                case Deconstruction:
                case PinnedObjectToPointer:
                default:
                    // these are not really part of the language.
                    throw ExceptionUtilities.UnexpectedValue(conversionKind);
            }
        }

        // Is the particular conversion an implicit conversion?
        public static bool IsImplicitConversion(this ConversionKind conversionKind)
        {
            switch (conversionKind)
            {
                case NoConversion:
                case UnsetConversionKind:
                    return false;

                case Identity:
                case ImplicitNumeric:
                case ImplicitTupleLiteral:
                case ImplicitTuple:
                case ImplicitEnumeration:
                case ImplicitThrow:
                case ImplicitNullable:
                case DefaultOrNullLiteral:
                case ImplicitReference:
                case Boxing:
                case ImplicitDynamic:
                case ImplicitConstant:
                case ImplicitUserDefined:
                case AnonymousFunction:
                case ConversionKind.MethodGroup:
                case PointerToVoid:
                case NullToPointer:
                case InterpolatedString:
                case SwitchExpression:
                case Deconstruction:
                case StackAllocToPointerType:
                case StackAllocToSpanType:
                    return true;

                case ExplicitNumeric:
                case ExplicitTuple:
                case ExplicitTupleLiteral:
                case ExplicitEnumeration:
                case ExplicitNullable:
                case ExplicitReference:
                case Unboxing:
                case ExplicitDynamic:
                case ExplicitUserDefined:
                case PointerToPointer:
                case PointerToInteger:
                case IntegerToPointer:
                case IntPtr:
                    return false;

                default:
                    throw ExceptionUtilities.UnexpectedValue(conversionKind);
            }
        }

        // Is the particular conversion a used-defined conversion?
        public static bool IsUserDefinedConversion(this ConversionKind conversionKind)
        {
            switch (conversionKind)
            {
                case ImplicitUserDefined:
                case ExplicitUserDefined:
                    return true;

                default:
                    return false;
            }
        }

        public static bool IsPointerConversion(this ConversionKind kind)
        {
            switch (kind)
            {
                case PointerToVoid:
                case PointerToPointer:
                case PointerToInteger:
                case IntegerToPointer:
                case NullToPointer:
                    return true;
                default:
                    return false;
            }
        }
    }
}
