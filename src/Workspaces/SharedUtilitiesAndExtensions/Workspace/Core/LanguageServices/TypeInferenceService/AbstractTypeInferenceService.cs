// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace Microsoft.CodeAnalysis.LanguageService.TypeInferenceService;

internal abstract partial class AbstractTypeInferenceService : ITypeInferenceService
{
    protected abstract AbstractTypeInferrer CreateTypeInferrer(SemanticModel semanticModel, CancellationToken cancellationToken);

    private static ImmutableArray<ITypeSymbol> InferTypeBasedOnNameIfEmpty(
        SemanticModel semanticModel, ImmutableArray<ITypeSymbol> result, string nameOpt)
    {
        if (result.IsEmpty && nameOpt != null)
        {
            return InferTypeBasedOnName(semanticModel, nameOpt);
        }

        return result;
    }

    private static ImmutableArray<TypeInferenceInfo> InferTypeBasedOnNameIfEmpty(
        SemanticModel semanticModel, ImmutableArray<TypeInferenceInfo> result, string nameOpt)
    {
        if (result.IsEmpty && nameOpt != null)
        {
            var types = InferTypeBasedOnName(semanticModel, nameOpt);
            return types.SelectAsArray(t => new TypeInferenceInfo(t));
        }

        return result;
    }

    private static readonly ImmutableArray<string> s_booleanPrefixes =
        ["Is", "Has", "Contains", "Supports"];

    private static ImmutableArray<ITypeSymbol> InferTypeBasedOnName(
        SemanticModel semanticModel, string name)
    {
        var matchesBoolean = MatchesBoolean(name);
        return matchesBoolean
            ? [semanticModel.Compilation.GetSpecialType(SpecialType.System_Boolean)]
            : [];
    }

    private static bool MatchesBoolean(string name)
    {
        foreach (var prefix in s_booleanPrefixes)
        {
            if (Matches(name, prefix))
            {
                return true;
            }
        }

        return false;
    }

    private static bool Matches(string name, string prefix)
    {
        if (name.StartsWith(prefix))
        {
            if (name.Length == prefix.Length)
            {
                return true;
            }

            var nextChar = name[prefix.Length];
            return !char.IsLower(nextChar);
        }

        return false;
    }

    public ImmutableArray<ITypeSymbol> InferTypes(
        SemanticModel semanticModel, int position,
        string nameOpt, CancellationToken cancellationToken)
    {
        var result = CreateTypeInferrer(semanticModel, cancellationToken)
            .InferTypes(position)
            .Select(t => t.InferredType)
            .ToImmutableArray();

        return InferTypeBasedOnNameIfEmpty(semanticModel, result, nameOpt);
    }

    public ImmutableArray<ITypeSymbol> InferTypes(
        SemanticModel semanticModel, SyntaxNode expression,
        string nameOpt, CancellationToken cancellationToken)
    {
        var result = CreateTypeInferrer(semanticModel, cancellationToken)
            .InferTypes(expression)
            .Select(info => info.InferredType)
            .ToImmutableArray();

        return InferTypeBasedOnNameIfEmpty(semanticModel, result, nameOpt);
    }

    public ImmutableArray<TypeInferenceInfo> GetTypeInferenceInfo(
        SemanticModel semanticModel, int position,
        string nameOpt, CancellationToken cancellationToken)
    {
        var result = CreateTypeInferrer(semanticModel, cancellationToken).InferTypes(position);
        return InferTypeBasedOnNameIfEmpty(semanticModel, result, nameOpt);
    }

    public ImmutableArray<TypeInferenceInfo> GetTypeInferenceInfo(
        SemanticModel semanticModel, SyntaxNode expression,
        string nameOpt, CancellationToken cancellationToken)
    {
        var result = CreateTypeInferrer(semanticModel, cancellationToken).InferTypes(expression);
        return InferTypeBasedOnNameIfEmpty(semanticModel, result, nameOpt);
    }
}
