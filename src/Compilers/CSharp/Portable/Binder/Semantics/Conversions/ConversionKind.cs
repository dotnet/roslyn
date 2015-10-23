﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


namespace Microsoft.CodeAnalysis.CSharp
{
    internal enum ConversionKind : byte
    {
        NoConversion,
        Identity,
        ImplicitNumeric,
        ImplicitEnumeration,
        ImplicitNullable,
        NullLiteral,
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
    }
}
