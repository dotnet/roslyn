// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static class ConversionExtensions
    {
        public static bool IsIdentityOrImplicitReference(this Conversion conversion)
        {
            return conversion.IsIdentity ||
                (conversion.IsImplicit && conversion.IsReference);
        }

        public static bool IsImplicitUserDefinedConversion(this Conversion conversion)
        {
            return conversion.IsUserDefined &&
                conversion.MethodSymbol != null &&
                conversion.MethodSymbol.MethodKind == MethodKind.Conversion &&
                conversion.MethodSymbol.Name == "op_Implicit";
        }
    }
}
