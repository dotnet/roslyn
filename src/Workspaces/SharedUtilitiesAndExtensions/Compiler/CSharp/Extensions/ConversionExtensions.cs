// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CSharp.Extensions;

internal static class ConversionExtensions
{
    public static bool IsIdentityOrImplicitReference(this Conversion conversion)
    {
        return conversion.IsIdentity ||
            (conversion is { IsImplicit: true, IsReference: true });
    }

    public static bool IsImplicitUserDefinedConversion(this Conversion conversion)
        => conversion is { IsUserDefined: true, MethodSymbol: { MethodKind: MethodKind.Conversion, Name: "op_Implicit" } };
}
