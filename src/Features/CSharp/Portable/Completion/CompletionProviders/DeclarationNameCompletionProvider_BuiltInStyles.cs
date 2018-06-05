// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles.SymbolSpecification;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal partial class DeclarationNameCompletionProvider
    {
        private static ImmutableArray<NamingRule> s_BuiltInRules;

        static DeclarationNameCompletionProvider()
        {
            s_BuiltInRules = ImmutableArray.Create(
                CreateCamelCaseFieldsAndParametersRule(),
                CreateEndWithAsyncRule(),
                CreateGetAsyncRule(),
                CreateMethodStartsWithGetRule());
        }

        private static NamingRule CreateGetAsyncRule()
        {
            var kinds = ImmutableArray.Create(new SymbolKindOrTypeKind(MethodKind.Ordinary));
            var modifiers = ImmutableArray.Create(new ModifierKind(ModifierKindEnum.IsAsync));
            return new NamingRule(
                new SymbolSpecification(Guid.NewGuid(), "endswithasync", kinds, ImmutableArray.Create<Accessibility>(), modifiers),
                new NamingStyles.NamingStyle(Guid.NewGuid(), prefix: "Get", suffix: "Async"),
                ReportDiagnostic.Info);
        }

        private static NamingRule CreateCamelCaseFieldsAndParametersRule()
        {
            var kinds = ImmutableArray.Create(new SymbolKindOrTypeKind(SymbolKind.Field), new SymbolKindOrTypeKind(SymbolKind.Parameter), new SymbolKindOrTypeKind(SymbolKind.Local));
            var modifiers = ImmutableArray.Create<ModifierKind>();
            return new NamingRule(
                new SymbolSpecification(Guid.NewGuid(), "camelcasefields", kinds, ImmutableArray.Create<Accessibility>(), modifiers),
                new NamingStyles.NamingStyle(Guid.NewGuid(), capitalizationScheme: Capitalization.CamelCase),
                ReportDiagnostic.Info);
        }

        private static NamingRule CreateEndWithAsyncRule()
        {
            var kinds = ImmutableArray.Create(new SymbolKindOrTypeKind(MethodKind.Ordinary));
            var modifiers = ImmutableArray.Create(new ModifierKind(ModifierKindEnum.IsAsync));
            return new NamingRule(
                new SymbolSpecification(Guid.NewGuid(), "endswithasynct", kinds, ImmutableArray.Create<Accessibility>(), modifiers),
                new NamingStyles.NamingStyle(Guid.NewGuid(), suffix: "Async"),
                ReportDiagnostic.Info);
        }

        private static NamingRule CreateMethodStartsWithGetRule()
        {
            var kinds = ImmutableArray.Create(new SymbolKindOrTypeKind(MethodKind.Ordinary));
            var modifiers = ImmutableArray.Create<ModifierKind>();
            return new NamingRule(
                new SymbolSpecification(Guid.NewGuid(), "startswithget", kinds, ImmutableArray.Create<Accessibility>(), modifiers),
                new NamingStyles.NamingStyle(Guid.NewGuid(), prefix: "Get"),
                ReportDiagnostic.Info);
        }
    }
}
