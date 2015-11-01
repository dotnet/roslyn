// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static class ConversionKindExtensions
    {
        public static bool IsDynamic(this ConversionKind conversionKind)
        {
            return conversionKind == ConversionKind.ImplicitDynamic || conversionKind == ConversionKind.ExplicitDynamic;
        }

        // Is the particular conversion an implicit conversion?
        public static bool IsImplicitConversion(this ConversionKind conversionKind)
        {
            switch (conversionKind)
            {
                case ConversionKind.NoConversion:
                    return false;

                case ConversionKind.Identity:
                case ConversionKind.ImplicitNumeric:
                case ConversionKind.ImplicitEnumeration:
                case ConversionKind.ImplicitThrow:
                case ConversionKind.ImplicitNullable:
                case ConversionKind.NullLiteral:
                case ConversionKind.ImplicitReference:
                case ConversionKind.Boxing:
                case ConversionKind.ImplicitDynamic:
                case ConversionKind.ImplicitConstant:
                case ConversionKind.ImplicitUserDefined:
                case ConversionKind.AnonymousFunction:
                case ConversionKind.MethodGroup:
                case ConversionKind.PointerToVoid:
                case ConversionKind.NullToPointer:
                case ConversionKind.InterpolatedString:
                    return true;

                case ConversionKind.ExplicitNumeric:
                case ConversionKind.ExplicitEnumeration:
                case ConversionKind.ExplicitNullable:
                case ConversionKind.ExplicitReference:
                case ConversionKind.Unboxing:
                case ConversionKind.ExplicitDynamic:
                case ConversionKind.ExplicitUserDefined:
                case ConversionKind.PointerToPointer:
                case ConversionKind.PointerToInteger:
                case ConversionKind.IntegerToPointer:
                case ConversionKind.IntPtr:
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
                case ConversionKind.ImplicitUserDefined:
                case ConversionKind.ExplicitUserDefined:
                    return true;

                default:
                    return false;
            }
        }

        public static bool IsPointerConversion(this ConversionKind kind)
        {
            switch (kind)
            {
                case ConversionKind.PointerToVoid:
                case ConversionKind.PointerToPointer:
                case ConversionKind.PointerToInteger:
                case ConversionKind.IntegerToPointer:
                case ConversionKind.NullToPointer:
                    return true;
                default:
                    return false;
            }
        }
    }
}
