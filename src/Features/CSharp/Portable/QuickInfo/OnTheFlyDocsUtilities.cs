// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.QuickInfo;

internal static class OnTheFlyDocsUtilities
{
    public static ImmutableArray<OnTheFlyDocsRelevantFileInfo> GetAdditionalOnTheFlyDocsContext(Solution solution, ISymbol symbol)
    {
        using var _ = PooledHashSet<OnTheFlyDocsRelevantFileInfo>.GetInstance(out var results);

        if (symbol is IPropertySymbol propertySymbol)
        {
            results.AddRange(GetOnTheFlyDocsRelevantFileInfos(propertySymbol.Type).Where(info => info != null).Cast<OnTheFlyDocsRelevantFileInfo>());
        }
        else if (symbol is IEventSymbol eventSymbol)
        {
            if (eventSymbol.Type is INamedTypeSymbol delegateType && delegateType.TypeKind == TypeKind.Delegate)
            {
                ProcessDelegateTypeSymbol(delegateType, results);
            }
        }
        else if (symbol is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.TypeKind == TypeKind.Delegate)
        {
            ProcessDelegateTypeSymbol(namedTypeSymbol, results);
        }

        var parameters = symbol.GetParameters();
        var typeArguments = symbol.GetTypeArguments();

        results.AddRange(parameters.SelectMany(parameter =>
        {
            var typeSymbol = parameter.Type;
            return GetOnTheFlyDocsRelevantFileInfos(typeSymbol);
        }).Where(info => info != null).Cast<OnTheFlyDocsRelevantFileInfo>());

        results.AddRange(typeArguments.SelectMany(type => GetOnTheFlyDocsRelevantFileInfos(type)).Where(info => info != null).Cast<OnTheFlyDocsRelevantFileInfo>());

        return [.. results];

        void ProcessDelegateTypeSymbol(INamedTypeSymbol delegateType, PooledHashSet<OnTheFlyDocsRelevantFileInfo> resultSet)
        {
            var invokeMethod = delegateType.DelegateInvokeMethod;
            if (invokeMethod != null)
            {
                foreach (var parameter in invokeMethod.Parameters)
                {
                    resultSet.AddRange(GetOnTheFlyDocsRelevantFileInfos(parameter.Type)
                        .Where(info => info != null)
                        .Cast<OnTheFlyDocsRelevantFileInfo>());
                }

                if (!invokeMethod.ReturnsVoid)
                {
                    resultSet.AddRange(GetOnTheFlyDocsRelevantFileInfos(invokeMethod.ReturnType)
                        .Where(info => info != null)
                        .Cast<OnTheFlyDocsRelevantFileInfo>());
                }
            }
        }

        ImmutableArray<OnTheFlyDocsRelevantFileInfo?> GetOnTheFlyDocsRelevantFileInfos(ITypeSymbol typeSymbol)
        {
            var results = ImmutableArray.CreateBuilder<OnTheFlyDocsRelevantFileInfo?>();
            if (typeSymbol.IsTupleType && typeSymbol is INamedTypeSymbol tupleType)
            {
                foreach (var typeArgument in tupleType.TypeArguments)
                {
                    var elementInfos = GetOnTheFlyDocsRelevantFileInfos(typeArgument);
                    results.AddRange(elementInfos);
                }

                if (results.Count > 0)
                {
                    return results.ToImmutable();
                }
            }

            if (typeSymbol is ITypeParameterSymbol typeParameterSymbol)
            {
                foreach (var constraintType in typeParameterSymbol.ConstraintTypes)
                {
                    var constraintInfos = GetOnTheFlyDocsRelevantFileInfos(constraintType);
                    results.AddRange(constraintInfos);
                }

                if (results.Count > 0)
                {
                    return results.ToImmutable();
                }
            }

            if (typeSymbol is IArrayTypeSymbol arrayTypeSymbol)
            {
                typeSymbol = arrayTypeSymbol.ElementType;
            }

            if (typeSymbol is IPointerTypeSymbol pointerTypeSymbol)
            {
                typeSymbol = pointerTypeSymbol.PointedAtType;
            }

            if (typeSymbol.IsNullable(out var underlyingType))
            {
                typeSymbol = underlyingType;
            }

            foreach (var typeSyntaxReference in typeSymbol.DeclaringSyntaxReferences)
            {
                var typeSpan = typeSyntaxReference.Span;
                var syntaxReferenceDocument = solution.GetDocument(typeSyntaxReference.SyntaxTree);
                if (syntaxReferenceDocument is not null)
                {
                    results.Add(new OnTheFlyDocsRelevantFileInfo(syntaxReferenceDocument, typeSpan));
                }
            }

            return results.ToImmutable();
        }
    }
}
