// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.NamingStyles;
using static Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles.SymbolSpecification;

namespace Microsoft.CodeAnalysis.Shared.Naming
{
    internal static class FallbackNamingRules
    {
        /// <summary>
        /// Standard field/property names a refactoring look for given a named symbol that is the subject of refactoring. 
        /// The refactoring will try to find existing matching symbol and if not found, it will generate one.
        /// </summary>
        internal static readonly ImmutableArray<NamingRule> RefactoringMatchLookupRules = ImmutableArray.Create(
            new NamingRule(
                new SymbolSpecification(Guid.NewGuid(), "Property", ImmutableArray.Create(new SymbolKindOrTypeKind(SymbolKind.Property))),
                new NamingStyle(Guid.NewGuid(), capitalizationScheme: Capitalization.PascalCase),
                enforcementLevel: ReportDiagnostic.Hidden),
            new NamingRule(
                new SymbolSpecification(Guid.NewGuid(), "Field", ImmutableArray.Create(new SymbolKindOrTypeKind(SymbolKind.Field), new SymbolKindOrTypeKind(SymbolKind.Parameter))),
                new NamingStyle(Guid.NewGuid(), capitalizationScheme: Capitalization.CamelCase),
                enforcementLevel: ReportDiagnostic.Hidden),
            new NamingRule(
                new SymbolSpecification(Guid.NewGuid(), "FieldWithUnderscore", ImmutableArray.Create(new SymbolKindOrTypeKind(SymbolKind.Field))),
                new NamingStyle(Guid.NewGuid(), prefix: "_", capitalizationScheme: Capitalization.CamelCase),
                enforcementLevel: ReportDiagnostic.Hidden));

        /// <summary>
        /// Standard name rules for name suggestion/completion utilities.
        /// </summary>
        internal static readonly ImmutableArray<NamingRule> CompletionOfferingRules = ImmutableArray.Create(
            CreateCamelCaseFieldsAndParametersRule(),
            CreateEndWithAsyncRule(),
            CreateGetAsyncRule(),
            CreateMethodStartsWithGetRule());

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
}
