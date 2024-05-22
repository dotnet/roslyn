// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.NamingStyles;
using static Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles.SymbolSpecification;

namespace Microsoft.CodeAnalysis.Shared.Naming;

internal static class FallbackNamingRules
{
    /// <summary>
    /// Standard symbol names if the user doesn't have any existing naming rules.
    /// </summary>
    public static readonly ImmutableArray<NamingRule> Default =
    [
        new NamingRule(
            new SymbolSpecification(
                Guid.NewGuid(),
                nameof(Capitalization.CamelCase),
                [
                    new SymbolKindOrTypeKind(SymbolKind.Field),
                    new SymbolKindOrTypeKind(SymbolKind.Local),
                    new SymbolKindOrTypeKind(SymbolKind.Parameter),
                    new SymbolKindOrTypeKind(SymbolKind.RangeVariable),
                ]),
            new NamingStyle(Guid.NewGuid(), capitalizationScheme: Capitalization.CamelCase),
            enforcementLevel: ReportDiagnostic.Hidden),
        new NamingRule(
            new SymbolSpecification(
                Guid.NewGuid(),
                "CamelCaseWithUnderscore",
                [new SymbolKindOrTypeKind(SymbolKind.Field)]),
            new NamingStyle(Guid.NewGuid(), prefix: "_", capitalizationScheme: Capitalization.CamelCase),
            enforcementLevel: ReportDiagnostic.Hidden),
        new NamingRule(
            CreateDefaultSymbolSpecification(),
            new NamingStyle(Guid.NewGuid(), capitalizationScheme: Capitalization.PascalCase),
            enforcementLevel: ReportDiagnostic.Hidden),
    ];

    /// <summary>
    /// Standard name rules for name suggestion/completion utilities. These are fallback rules that run if a user
    /// hasn't provided any other naming rule matching the scenario.
    /// </summary>
    internal static readonly ImmutableArray<NamingRule> CompletionFallbackRules = [CreateCamelCaseFieldsAndParametersRule()];

    /// <summary>
    /// Standard name rules for name suggestion/completion utilities. These are supplementary rules that run in
    /// addition to any other rules defined by the user in order to provide additional valid suggestions.
    /// </summary>
    internal static readonly ImmutableArray<NamingRule> CompletionSupplementaryRules = [CreateEndWithAsyncRule(), CreateGetAsyncRule(), CreateMethodStartsWithGetRule()];

    private static NamingRule CreateGetAsyncRule()
    {
        var kinds = ImmutableArray.Create(new SymbolKindOrTypeKind(MethodKind.Ordinary));
        var modifiers = ImmutableArray.Create(new ModifierKind(ModifierKindEnum.IsAsync));
        return new NamingRule(
            new SymbolSpecification(Guid.NewGuid(), "endswithasync", kinds, accessibilityList: default, modifiers),
            new NamingStyle(Guid.NewGuid(), prefix: "Get", suffix: "Async"),
            ReportDiagnostic.Info);
    }

    private static NamingRule CreateCamelCaseFieldsAndParametersRule()
    {
        var kinds = ImmutableArray.Create(new SymbolKindOrTypeKind(SymbolKind.Field), new SymbolKindOrTypeKind(SymbolKind.Parameter), new SymbolKindOrTypeKind(SymbolKind.Local));
        return new NamingRule(
            new SymbolSpecification(Guid.NewGuid(), "camelcasefields", kinds, accessibilityList: default, modifiers: default),
            new NamingStyle(Guid.NewGuid(), capitalizationScheme: Capitalization.CamelCase),
            ReportDiagnostic.Info);
    }

    private static NamingRule CreateEndWithAsyncRule()
    {
        var kinds = ImmutableArray.Create(new SymbolKindOrTypeKind(MethodKind.Ordinary));
        var modifiers = ImmutableArray.Create(new ModifierKind(ModifierKindEnum.IsAsync));
        return new NamingRule(
            new SymbolSpecification(Guid.NewGuid(), "endswithasynct", kinds, accessibilityList: default, modifiers),
            new NamingStyle(Guid.NewGuid(), suffix: "Async"),
            ReportDiagnostic.Info);
    }

    private static NamingRule CreateMethodStartsWithGetRule()
    {
        var kinds = ImmutableArray.Create(new SymbolKindOrTypeKind(MethodKind.Ordinary));
        return new NamingRule(
            new SymbolSpecification(Guid.NewGuid(), "startswithget", kinds, accessibilityList: default, modifiers: default),
            new NamingStyle(Guid.NewGuid(), prefix: "Get"),
            ReportDiagnostic.Info);
    }
}
