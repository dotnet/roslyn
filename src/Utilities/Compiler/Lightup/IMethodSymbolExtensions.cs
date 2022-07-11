// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities.Lightup
{
    internal static class IMethodSymbolExtensions
    {
        private static readonly Func<IMethodSymbol, bool> s_isInitOnly
            = LightupHelpers.CreateSymbolPropertyAccessor<IMethodSymbol, bool>(typeof(IMethodSymbol), "IsInitOnly", false);

        private static readonly Func<IMethodSymbol, MethodImplAttributes> s_methodImplementationFlags
            = LightupHelpers.CreateSymbolPropertyAccessor<IMethodSymbol, MethodImplAttributes>(typeof(IMethodSymbol), "MethodImplementationFlags", MethodImplAttributes.Managed);

        private static readonly Func<IMethodSymbol, SignatureCallingConvention> s_callingConvention
            = LightupHelpers.CreateSymbolPropertyAccessor<IMethodSymbol, SignatureCallingConvention>(typeof(IMethodSymbol), "CallingConvention", SignatureCallingConvention.Default);

        public static bool IsInitOnly(this IMethodSymbol methodSymbol)
            => s_isInitOnly(methodSymbol);

        public static MethodImplAttributes MethodImplementationFlags(this IMethodSymbol methodSymbol)
            => s_methodImplementationFlags(methodSymbol);

        public static SignatureCallingConvention CallingConvention(this IMethodSymbol methodSymbol)
            => s_callingConvention(methodSymbol);
    }
}
