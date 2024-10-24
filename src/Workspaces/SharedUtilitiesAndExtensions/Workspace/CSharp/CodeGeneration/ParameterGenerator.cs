// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration;

using static CSharpCodeGenerationHelpers;
using static CSharpSyntaxTokens;
using static SyntaxFactory;

internal static class ParameterGenerator
{
    public static ParameterListSyntax GenerateParameterList(
        ImmutableArray<IParameterSymbol> parameterDefinitions,
        CSharpCodeGenerationContextInfo info)
    {
        var parameters = GetParameters(parameterDefinitions, info);

        return ParameterList([.. parameters]);
    }

    public static BracketedParameterListSyntax GenerateBracketedParameterList(
        ImmutableArray<IParameterSymbol> parameterDefinitions,
        CSharpCodeGenerationContextInfo info)
    {
        // Bracketed parameter lists come from indexers.  Those don't have type parameters, so we
        // could never have a typeParameterMapping.
        var parameters = GetParameters(parameterDefinitions, info);

        return BracketedParameterList([.. parameters]);
    }

    internal static ImmutableArray<ParameterSyntax> GetParameters(
        ImmutableArray<IParameterSymbol> parameterDefinitions,
        CSharpCodeGenerationContextInfo info)
    {
        var seenOptional = false;
        var isFirstParam = true;

        var result = new FixedSizeArrayBuilder<ParameterSyntax>(parameterDefinitions.Length);
        foreach (var p in parameterDefinitions)
        {
            var parameter = GetParameter(p, info, isFirstParam, seenOptional);
            result.Add(parameter);
            seenOptional = seenOptional || parameter.Default != null;
            isFirstParam = false;
        }

        return result.MoveToImmutable();
    }

    internal static ParameterSyntax GetParameter(IParameterSymbol parameter, CSharpCodeGenerationContextInfo info, bool isFirstParam, bool seenOptional)
    {
        var reusableSyntax = GetReuseableSyntaxNodeForSymbol<ParameterSyntax>(parameter, info);
        if (reusableSyntax != null)
            return reusableSyntax;

        return Parameter(parameter.Name.ToIdentifierToken())
            .WithAttributeLists(GenerateAttributes(parameter, info))
            .WithModifiers(GenerateModifiers(parameter, isFirstParam))
            .WithType(parameter.Type.GenerateTypeSyntax())
            .WithDefault(GenerateEqualsValueClause(info.Generator, parameter, seenOptional));
    }

    private static SyntaxTokenList GenerateModifiers(
        IParameterSymbol parameter, bool isFirstParam)
    {
        var list = CSharpSyntaxGeneratorInternal.GetParameterModifiers(parameter.RefKind);

        if (isFirstParam &&
            parameter.ContainingSymbol is IMethodSymbol methodSymbol &&
            methodSymbol.IsExtensionMethod)
        {
            list = list.Add(ThisKeyword);
        }

        if (parameter.IsParams)
        {
            list = list.Add(ParamsKeyword);
        }

        return list;
    }

    private static EqualsValueClauseSyntax? GenerateEqualsValueClause(
        SyntaxGenerator generator,
        IParameterSymbol parameter,
        bool seenOptional)
    {
        if (!parameter.IsParams && !parameter.IsRefOrOut())
        {
            if (parameter.HasExplicitDefaultValue || seenOptional)
            {
                var defaultValue = parameter.HasExplicitDefaultValue ? parameter.ExplicitDefaultValue : null;
                if (defaultValue is DateTime)
                    return null;

                return EqualsValueClause(
                    GenerateEqualsValueClauseWorker(generator, parameter, defaultValue));
            }
        }

        return null;
    }

    private static ExpressionSyntax GenerateEqualsValueClauseWorker(SyntaxGenerator generator, IParameterSymbol parameter, object? value)
        => ExpressionGenerator.GenerateExpression(generator, parameter.Type, value, canUseFieldReference: true);

    private static SyntaxList<AttributeListSyntax> GenerateAttributes(
        IParameterSymbol parameter, CSharpCodeGenerationContextInfo info)
    {
        var attributes = parameter.GetAttributes();
        if (attributes.Length == 0)
        {
            return default;
        }

        return AttributeGenerator.GenerateAttributeLists(attributes, info);
    }
}
