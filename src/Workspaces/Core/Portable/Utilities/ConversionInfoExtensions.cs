// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Extensions
{
    internal static class ConversionInfoExtensions
    {
        public static bool IsIdentityOrWideningReference(this ConversionInfo conversion)
            => conversion.IsIdentity ||
               (conversion.IsWidening && conversion.IsReference);

        public static bool IsImplicitUserDefinedConversion(this ConversionInfo conversion)
            => conversion.IsUserDefined &&
                conversion.MethodSymbol?.MethodKind == MethodKind.Conversion &&
                conversion.MethodSymbol.Name == WellKnownMemberNames.ImplicitConversionName;
    }
}