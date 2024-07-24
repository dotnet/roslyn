// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static partial class ITypeSymbolExtensions
{
    public static IList<ITypeParameterSymbol> GetReferencedMethodTypeParameters(
        this ITypeSymbol? type, IList<ITypeParameterSymbol>? result = null)
    {
        result ??= [];
        type?.Accept(new CollectTypeParameterSymbolsVisitor(result, onlyMethodTypeParameters: true));
        return result;
    }

    public static IList<ITypeParameterSymbol> GetReferencedTypeParameters(
        this ITypeSymbol? type, IList<ITypeParameterSymbol>? result = null)
    {
        result ??= [];
        type?.Accept(new CollectTypeParameterSymbolsVisitor(result, onlyMethodTypeParameters: false));
        return result;
    }

    [return: NotNullIfNotNull(parameterName: nameof(type))]
    public static ITypeSymbol? SubstituteTypes<TType1, TType2>(
        this ITypeSymbol? type,
        IDictionary<TType1, TType2> mapping,
        Compilation compilation)
        where TType1 : ITypeSymbol
        where TType2 : ITypeSymbol
    {
        return type.SubstituteTypes(mapping, new CompilationTypeGenerator(compilation));
    }

    [return: NotNullIfNotNull(parameterName: nameof(type))]
    public static ITypeSymbol? SubstituteTypes<TType1, TType2>(
        this ITypeSymbol? type,
        IDictionary<TType1, TType2> mapping,
        ITypeGenerator typeGenerator)
        where TType1 : ITypeSymbol
        where TType2 : ITypeSymbol
    {
        return type?.Accept(new SubstituteTypesVisitor<TType1, TType2>(mapping, typeGenerator));
    }
}
