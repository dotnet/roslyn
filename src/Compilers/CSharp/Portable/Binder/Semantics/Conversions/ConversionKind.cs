// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        DefaultOrNullLiteral,
        ImplicitReference,
        Boxing,
        PointerToVoid,
        NullToPointer,
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
        PointerToPointer,
        IntegerToPointer,
        PointerToInteger,
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
    }
}
