// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.NamingStyles;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles
{
    internal abstract class NamingStyleDiagnosticAnalyzerBase :
        AbstractCodeStyleDiagnosticAnalyzer
    {
        private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(FeaturesResources.Naming_Styles), FeaturesResources.ResourceManager, typeof(FeaturesResources));
        private static readonly LocalizableString s_localizableTitleNamingStyle = new LocalizableResourceString(nameof(FeaturesResources.Naming_Styles), FeaturesResources.ResourceManager, typeof(FeaturesResources));

        protected NamingStyleDiagnosticAnalyzerBase()
            : base(IDEDiagnosticIds.NamingRuleId,
                   s_localizableTitleNamingStyle, 
                   s_localizableMessage)
        {
        }

        // Applicable SymbolKind list is limited due to https://github.com/dotnet/roslyn/issues/8753. 
        // We would prefer to respond to the names of all symbols.
        private static readonly ImmutableArray<SymbolKind> _symbolKinds = ImmutableArray.Create(
            SymbolKind.Event,
            SymbolKind.Field,
            SymbolKind.Method,
            SymbolKind.NamedType,
            SymbolKind.Namespace,
            SymbolKind.Property,
            SymbolKind.Parameter);

        public override bool OpenFileOnly(Workspace workspace) => true;

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterCompilationStartAction(CompilationStartAction);

        private void CompilationStartAction(CompilationStartAnalysisContext context)
        {
            var idToCachedResult = new ConcurrentDictionary<Guid, ConcurrentDictionary<string, string>>(
                concurrencyLevel: 2, capacity: 0);

            context.RegisterSymbolAction(c => SymbolAction(c, idToCachedResult), _symbolKinds);
        }

        private static readonly Func<Guid, ConcurrentDictionary<string, string>> s_createCache =
            _ => new ConcurrentDictionary<string, string>(concurrencyLevel: 2, capacity: 0);

        private void SymbolAction(
            SymbolAnalysisContext context,
            ConcurrentDictionary<Guid, ConcurrentDictionary<string, string>> idToCachedResult)
        { 
            if (string.IsNullOrEmpty(context.Symbol.Name))
            {
                return;
            }

            var namingPreferences = context.GetNamingStylePreferencesAsync().GetAwaiter().GetResult();
            if (namingPreferences == null)
            {
                return;
            }

            var namingStyleRules = namingPreferences.Rules;

            if (!namingStyleRules.TryGetApplicableRule(context.Symbol, out var applicableRule) ||
                applicableRule.EnforcementLevel == DiagnosticSeverity.Hidden)
            {
                return;
            }

            var cache = idToCachedResult.GetOrAdd(applicableRule.NamingStyle.ID, s_createCache);

            if (!cache.TryGetValue(context.Symbol.Name, out var failureReason))
            {
                if (applicableRule.NamingStyle.IsNameCompliant(context.Symbol.Name, out failureReason))
                {
                    failureReason = null;
                }

                cache.TryAdd(context.Symbol.Name, failureReason);
            }

            if (failureReason == null)
            {
                return;
            }

            var descriptor = new DiagnosticDescriptor(IDEDiagnosticIds.NamingRuleId,
                 s_localizableTitleNamingStyle,
                 string.Format(FeaturesResources.Naming_rule_violation_0, failureReason),
                 DiagnosticCategory.Style,
                 applicableRule.EnforcementLevel,
                 isEnabledByDefault: true);

            var builder = ImmutableDictionary.CreateBuilder<string, string>();
            builder[nameof(NamingStyle)] = applicableRule.NamingStyle.CreateXElement().ToString();
            builder["OptionName"] = nameof(SimplificationOptions.NamingPreferences);
            builder["OptionLanguage"] = context.Compilation.Language;
            context.ReportDiagnostic(Diagnostic.Create(descriptor, context.Symbol.Locations.First(), builder.ToImmutable()));
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;
    }
}
