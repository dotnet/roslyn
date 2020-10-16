﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.NamingStyles;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles
{
    internal abstract class NamingStyleDiagnosticAnalyzerBase<TLanguageKindEnum>
        : AbstractBuiltInCodeStyleDiagnosticAnalyzer
        where TLanguageKindEnum : struct
    {
        private static readonly LocalizableString s_localizableMessageFormat = new LocalizableResourceString(nameof(AnalyzersResources.Naming_rule_violation_0), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));
        private static readonly LocalizableString s_localizableTitleNamingStyle = new LocalizableResourceString(nameof(AnalyzersResources.Naming_Styles), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));

        protected NamingStyleDiagnosticAnalyzerBase()
            : base(IDEDiagnosticIds.NamingRuleId,
                   option: null,    // No unique option to configure the diagnosticId
                   s_localizableTitleNamingStyle,
                   s_localizableMessageFormat)
        {
        }

        // Applicable SymbolKind list is limited due to https://github.com/dotnet/roslyn/issues/8753. 
        // Locals and fields are handled by SupportedSyntaxKinds for now.
        private static readonly ImmutableArray<SymbolKind> _symbolKinds = ImmutableArray.Create(
            SymbolKind.Event,
            SymbolKind.Method,
            SymbolKind.NamedType,
            SymbolKind.Namespace,
            SymbolKind.Property);

        // Workaround: RegisterSymbolAction doesn't work with locals, local functions, parameters, or type parameters.
        // see https://github.com/dotnet/roslyn/issues/14061
        protected abstract ImmutableArray<TLanguageKindEnum> SupportedSyntaxKinds { get; }

        protected abstract bool ShouldIgnore(ISymbol symbol);

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterCompilationStartAction(CompilationStartAction);

        private void CompilationStartAction(CompilationStartAnalysisContext context)
        {
            var idToCachedResult = new ConcurrentDictionary<Guid, ConcurrentDictionary<string, string?>>(
                concurrencyLevel: 2, capacity: 0);

            context.RegisterSymbolAction(SymbolAction, _symbolKinds);
            context.RegisterSyntaxNodeAction(SyntaxNodeAction, SupportedSyntaxKinds);
            return;

            // Local functions

            void SymbolAction(SymbolAnalysisContext symbolContext)
            {
                var diagnostic = TryGetDiagnostic(
                    symbolContext.Compilation,
                    symbolContext.Symbol,
                    symbolContext.Options,
                    idToCachedResult,
                    symbolContext.CancellationToken);

                if (diagnostic != null)
                {
                    symbolContext.ReportDiagnostic(diagnostic);
                }
            }

            void SyntaxNodeAction(SyntaxNodeAnalysisContext syntaxContext)
            {
                var symbol = syntaxContext.SemanticModel.GetDeclaredSymbol(syntaxContext.Node, syntaxContext.CancellationToken);
                if (symbol == null)
                {
                    // Catch clauses don't need to have a declaration.
                    return;
                }

                var diagnostic = TryGetDiagnostic(
                    syntaxContext.Compilation,
                    symbol,
                    syntaxContext.Options,
                    idToCachedResult,
                    syntaxContext.CancellationToken);

                if (diagnostic != null)
                {
                    syntaxContext.ReportDiagnostic(diagnostic);
                }
            }
        }

        private static readonly Func<Guid, ConcurrentDictionary<string, string?>> s_createCache =
            _ => new ConcurrentDictionary<string, string?>(concurrencyLevel: 2, capacity: 0);

        private Diagnostic? TryGetDiagnostic(
            Compilation compilation,
            ISymbol symbol,
            AnalyzerOptions options,
            ConcurrentDictionary<Guid, ConcurrentDictionary<string, string?>> idToCachedResult,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(symbol.Name))
            {
                return null;
            }

            if (ShouldIgnore(symbol))
            {
                return null;
            }

            var namingPreferences = GetNamingStylePreferences(compilation, symbol, options, cancellationToken);
            if (namingPreferences == null)
            {
                return null;
            }

            var namingStyleRules = namingPreferences.Rules;

            if (!namingStyleRules.TryGetApplicableRule(symbol, out var applicableRule) ||
                applicableRule.EnforcementLevel == ReportDiagnostic.Suppress)
            {
                return null;
            }

            var cache = idToCachedResult.GetOrAdd(applicableRule.NamingStyle.ID, s_createCache);

            if (!cache.TryGetValue(symbol.Name, out var failureReason))
            {
                if (applicableRule.NamingStyle.IsNameCompliant(symbol.Name, out failureReason))
                {
                    failureReason = null;
                }

                cache.TryAdd(symbol.Name, failureReason);
            }

            if (failureReason == null)
            {
                return null;
            }

            var builder = ImmutableDictionary.CreateBuilder<string, string>();
            builder[nameof(NamingStyle)] = applicableRule.NamingStyle.CreateXElement().ToString();
            builder["OptionName"] = nameof(NamingStyleOptions.NamingPreferences);
            builder["OptionLanguage"] = compilation.Language;

            return DiagnosticHelper.Create(Descriptor, symbol.Locations.First(), applicableRule.EnforcementLevel, additionalLocations: null, builder.ToImmutable(), failureReason);
        }

        private static NamingStylePreferences? GetNamingStylePreferences(
            Compilation compilation,
            ISymbol symbol,
            AnalyzerOptions options,
            CancellationToken cancellationToken)
        {
            var sourceTree = symbol.Locations.FirstOrDefault()?.SourceTree;
            if (sourceTree == null)
            {
                return null;
            }

            return options.GetOption(NamingStyleOptions.NamingPreferences, compilation.Language, sourceTree, cancellationToken);
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;
    }
}
