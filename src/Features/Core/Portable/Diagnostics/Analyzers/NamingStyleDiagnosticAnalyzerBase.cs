// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.NamingStyles;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles
{
    internal abstract class NamingStyleDiagnosticAnalyzerBase<TLanguageKindEnum> :
        AbstractCodeStyleDiagnosticAnalyzer where TLanguageKindEnum : struct
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
        // Locals and fields are handled by SupportedSyntaxKinds for now.
        private static readonly ImmutableArray<SymbolKind> _symbolKinds = ImmutableArray.Create(
            SymbolKind.Event,
            SymbolKind.Method,
            SymbolKind.NamedType,
            SymbolKind.Namespace,
            SymbolKind.Property,
            SymbolKind.Parameter);

        // Workaround: RegisterSymbolAction doesn't work with locals & local functions,
        // see https://github.com/dotnet/roslyn/issues/14061
        protected abstract ImmutableArray<TLanguageKindEnum> SupportedSyntaxKinds { get; }

        public override bool OpenFileOnly(Workspace workspace) => true;

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterCompilationStartAction(CompilationStartAction);

        private void CompilationStartAction(CompilationStartAnalysisContext context)
        {
            var idToCachedResult = new ConcurrentDictionary<Guid, ConcurrentDictionary<string, string>>(
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

        private static readonly Func<Guid, ConcurrentDictionary<string, string>> s_createCache =
            _ => new ConcurrentDictionary<string, string>(concurrencyLevel: 2, capacity: 0);

        private static Diagnostic TryGetDiagnostic(
            Compilation compilation,
            ISymbol symbol,
            AnalyzerOptions options,
            ConcurrentDictionary<Guid, ConcurrentDictionary<string, string>> idToCachedResult,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(symbol.Name))
            {
                return null;
            }

            var sourceTree = symbol.Locations.FirstOrDefault()?.SourceTree;
            if (sourceTree == null)
            {
                return null;
            }

            var namingPreferences = GetNamingStylePreferencesAsync().GetAwaiter().GetResult();
            if (namingPreferences == null)
            {
                return null;
            }

            var namingStyleRules = namingPreferences.Rules;

            if (!namingStyleRules.TryGetApplicableRule(symbol, out var applicableRule) ||
                applicableRule.EnforcementLevel == DiagnosticSeverity.Hidden)
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

            var descriptor = new DiagnosticDescriptor(IDEDiagnosticIds.NamingRuleId,
                 s_localizableTitleNamingStyle,
                 string.Format(FeaturesResources.Naming_rule_violation_0, failureReason),
                 DiagnosticCategory.Style,
                 applicableRule.EnforcementLevel,
                 isEnabledByDefault: true);

            var builder = ImmutableDictionary.CreateBuilder<string, string>();
            builder[nameof(NamingStyle)] = applicableRule.NamingStyle.CreateXElement().ToString();
            builder["OptionName"] = nameof(SimplificationOptions.NamingPreferences);
            builder["OptionLanguage"] = compilation.Language;

            return Diagnostic.Create(descriptor, symbol.Locations.First(), builder.ToImmutable());

            // Local functions

            async Task<NamingStylePreferences> GetNamingStylePreferencesAsync()
            {
                var optionSet = await options.GetDocumentOptionSetAsync(sourceTree, cancellationToken).ConfigureAwait(false);
                return optionSet?.GetOption(SimplificationOptions.NamingPreferences, compilation.Language);
            }
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;
    }
}
