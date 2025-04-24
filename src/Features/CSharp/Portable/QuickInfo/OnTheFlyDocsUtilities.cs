// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.QuickInfo;

internal static class OnTheFlyDocsUtilities
{
    public static ImmutableArray<OnTheFlyDocsRelevantFileInfo> GetAdditionalOnTheFlyDocsContext(Solution solution, ISymbol symbol)
    {
        var parameters = symbol.GetParameters();
        var typeArguments = symbol.GetTypeArguments();

        var parameterStrings = parameters.Select(parameter =>
        {
            var typeSymbol = parameter.Type;
            return GetOnTheFlyDocsRelevantFileInfo(typeSymbol);
        }).ToImmutableArray();

        var typeArgumentStrings = typeArguments.Select(GetOnTheFlyDocsRelevantFileInfo).ToImmutableArray();

        return parameterStrings.AddRange(typeArgumentStrings).Where(info => info != null).ToImmutableArray().Distinct();

        OnTheFlyDocsRelevantFileInfo? GetOnTheFlyDocsRelevantFileInfo(ITypeSymbol typeSymbol)
        {
            if (typeSymbol.IsTupleType && typeSymbol is INamedTypeSymbol tupleType)
            {
                foreach (var typeArgument in tupleType.TypeArguments)
                {
                    var elementInfo = GetOnTheFlyDocsRelevantFileInfo(typeArgument);
                    if (elementInfo is not null)
                    {
                        return elementInfo;
                    }
                }
            }

            if (typeSymbol is ITypeParameterSymbol typeParameterSymbol)
            {
                foreach (var constraintType in typeParameterSymbol.ConstraintTypes)
                {
                    var constraintInfo = GetOnTheFlyDocsRelevantFileInfo(constraintType);
                    if (constraintInfo is not null)
                    {
                        return constraintInfo;
                    }
                }
            }

            if (typeSymbol is IArrayTypeSymbol arrayTypeSymbol)
            {
                typeSymbol = arrayTypeSymbol.ElementType;
            }

            if (typeSymbol.IsNullable(out var underlyingType))
            {
                typeSymbol = underlyingType;
            }

            var typeSyntaxReference = typeSymbol.DeclaringSyntaxReferences.FirstOrDefault();
            if (typeSyntaxReference is not null)
            {
                var typeSpan = typeSyntaxReference.Span;
                var syntaxReferenceDocument = solution.GetDocument(typeSyntaxReference.SyntaxTree);
                if (syntaxReferenceDocument is not null)
                {
                    return new OnTheFlyDocsRelevantFileInfo(syntaxReferenceDocument, typeSpan);
                }
            }

            return null;
        }
    }
}
