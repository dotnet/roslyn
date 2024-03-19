// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using static Microsoft.CodeAnalysis.CSharp.CodeGeneration.CSharpCodeGenerationHelpers;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration;

internal static class ParameterGenerator
{
    public static ParameterListSyntax GenerateParameterList(
        ImmutableArray<IParameterSymbol> parameterDefinitions,
        bool isExplicit,
        CSharpCodeGenerationContextInfo info)
    {
        var parameters = GetParameters(parameterDefinitions, isExplicit, info);

        return SyntaxFactory.ParameterList([.. parameters]);
    }

    public static BracketedParameterListSyntax GenerateBracketedParameterList(
        ImmutableArray<IParameterSymbol> parameterDefinitions,
        bool isExplicit,
        CSharpCodeGenerationContextInfo info)
    {
        // Bracketed parameter lists come from indexers.  Those don't have type parameters, so we
        // could never have a typeParameterMapping.
        var parameters = GetParameters(parameterDefinitions, isExplicit, info);

        return SyntaxFactory.BracketedParameterList([.. parameters]);
    }

    internal static ImmutableArray<ParameterSyntax> GetParameters(
        ImmutableArray<IParameterSymbol> parameterDefinitions,
        bool isExplicit,
        CSharpCodeGenerationContextInfo info)
    {
        using var _ = ArrayBuilder<ParameterSyntax>.GetInstance(out var result);
        var seenOptional = false;
        var isFirstParam = true;

        foreach (var p in parameterDefinitions)
        {
            var parameter = GetParameter(p, info, isExplicit, isFirstParam, seenOptional);
            result.Add(parameter);
            seenOptional = seenOptional || parameter.Default != null;
            isFirstParam = false;
        }

        return result.ToImmutable();
    }

    internal static ParameterSyntax GetParameter(IParameterSymbol parameter, CSharpCodeGenerationContextInfo info, bool isExplicit, bool isFirstParam, bool seenOptional)
    {
        var reusableSyntax = GetReuseableSyntaxNodeForSymbol<ParameterSyntax>(parameter, info);
        if (reusableSyntax != null)
            return reusableSyntax;

        return SyntaxFactory.Parameter(parameter.Name.ToIdentifierToken())
            .WithAttributeLists(GenerateAttributes(parameter, isExplicit, info))
            .WithModifiers(GenerateModifiers(parameter, isFirstParam))
            .WithType(parameter.Type.GenerateTypeSyntax())
            .WithDefault(GenerateEqualsValueClause(info.Generator, parameter, isExplicit, seenOptional));
    }

    private static SyntaxTokenList GenerateModifiers(
        IParameterSymbol parameter, bool isFirstParam)
    {
        var list = CSharpSyntaxGeneratorInternal.GetParameterModifiers(parameter.RefKind);

        if (isFirstParam &&
            parameter.ContainingSymbol is IMethodSymbol methodSymbol &&
            methodSymbol.IsExtensionMethod)
        {
            list = list.Add(SyntaxFactory.Token(SyntaxKind.ThisKeyword));
        }

        if (parameter.IsParams)
        {
            list = list.Add(SyntaxFactory.Token(SyntaxKind.ParamsKeyword));
        }

        return list;
    }

    private static EqualsValueClauseSyntax? GenerateEqualsValueClause(
        SyntaxGenerator generator,
        IParameterSymbol parameter,
        bool isExplicit,
        bool seenOptional)
    {
        if (!parameter.IsParams && !isExplicit && !parameter.IsRefOrOut())
        {
            if (parameter.HasExplicitDefaultValue || seenOptional)
            {
                var defaultValue = parameter.HasExplicitDefaultValue ? parameter.ExplicitDefaultValue : null;
                if (defaultValue is DateTime)
                    return null;

                return SyntaxFactory.EqualsValueClause(
                    GenerateEqualsValueClauseWorker(generator, parameter, defaultValue));
            }
        }

        return null;
    }

    private static ExpressionSyntax GenerateEqualsValueClauseWorker(SyntaxGenerator generator, IParameterSymbol parameter, object? value)
        => ExpressionGenerator.GenerateExpression(generator, parameter.Type, value, canUseFieldReference: true);

    private static SyntaxList<AttributeListSyntax> GenerateAttributes(
        IParameterSymbol parameter, bool isExplicit, CSharpCodeGenerationContextInfo info)
    {
        if (isExplicit)
        {
            return default;
        }

        var attributes = parameter.GetAttributes();
        if (attributes.Length == 0)
        {
            return default;
        }

        return AttributeGenerator.GenerateAttributeLists(attributes, info);
    }
}
