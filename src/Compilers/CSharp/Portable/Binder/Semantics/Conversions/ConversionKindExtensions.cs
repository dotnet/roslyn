// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
                case NullLiteral:
                case DefaultLiteral:
                case ImplicitReference:
                case Boxing:
                case ImplicitDynamic:
                case ImplicitConstant:
                case ImplicitUserDefined:
                case AnonymousFunction:
                case ConversionKind.MethodGroup:
                case ConversionKind.FunctionType:
                case ImplicitPointerToVoid:
                case ImplicitNullToPointer:
                case InterpolatedString:
                case InterpolatedStringHandler:
                case SwitchExpression:
                case ConditionalExpression:
                case Deconstruction:
                case StackAllocToPointerType:
                case StackAllocToSpanType:
                case ImplicitPointer:
                case ObjectCreation:
                case InlineArray:
                case CollectionExpression:
                case ImplicitSpan:
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
                case ExplicitPointerToPointer:
                case ExplicitPointerToInteger:
                case ExplicitIntegerToPointer:
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
                case ImplicitPointerToVoid:
                case ExplicitPointerToPointer:
                case ExplicitPointerToInteger:
                case ExplicitIntegerToPointer:
                case ImplicitNullToPointer:
                case ImplicitPointer:
                    return true;
                default:
                    return false;
            }
        }
    }
}
