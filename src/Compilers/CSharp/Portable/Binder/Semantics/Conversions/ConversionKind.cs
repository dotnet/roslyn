﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal enum ConversionKind : byte
    {
        UnsetConversionKind = 0,
        NoConversion,
        Identity,
        ImplicitNumeric,
        ImplicitEnumeration,
        ImplicitThrow,
        ImplicitTupleLiteral,
        ImplicitTuple,
        ExplicitTupleLiteral,
        ExplicitTuple,
        ImplicitNullable,
        NullLiteral,
        ImplicitReference,
        Boxing,
        ImplicitPointerToVoid,
        ImplicitNullToPointer,
        // Any explicit conversions involving pointers not covered by PointerToVoid or NullToPointer.
        // Currently, this is just implicit function pointer conversions.
        ImplicitPointer,
        ImplicitDynamic,
        ExplicitDynamic,
        ImplicitConstant,
        ImplicitUserDefined,
        AnonymousFunction,
        MethodGroup,
        ExplicitNumeric,
        ExplicitEnumeration,
        ExplicitNullable,
        ExplicitReference,
        Unboxing,
        ExplicitUserDefined,
        ExplicitPointerToPointer,
        ExplicitIntegerToPointer,
        ExplicitPointerToInteger,
        // The IntPtr conversions are not described by the specification but we must
        // implement them for compatibility with the native compiler.
        IntPtr,
        InterpolatedString, // a conversion from an interpolated string to IFormattable or FormattableString
        SwitchExpression, // a conversion from a switch expression to a type which each arm can convert to
        Deconstruction, // The Deconstruction conversion is not part of the language, it is an implementation detail 
        StackAllocToPointerType,
        StackAllocToSpanType,

        // PinnedObjectToPointer is not directly a part of the language
        // It is used by lowering of "fixed" statements to represent conversion of an object reference (O) to an unmanaged pointer (*)
        // The conversion is unsafe and makes sense only if (O) is pinned.
        PinnedObjectToPointer,

        DefaultLiteral, // a conversion from a `default` literal to any type
        ObjectCreation, // a conversion from a `new()` expression to any type
    }
}
