// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.BannedApiAnalyzers;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Analyzers
{
    using static CodeAnalysisDiagnosticsResources;

    internal static class SymbolIsBannedInAnalyzersAnalyzer
    {
        public static readonly DiagnosticDescriptor SymbolIsBannedRule = new(
            id: DiagnosticIds.SymbolIsBannedInAnalyzersRuleId,
            title: CreateLocalizableResourceString(nameof(SymbolIsBannedInAnalyzersTitle)),
            messageFormat: CreateLocalizableResourceString(nameof(SymbolIsBannedInAnalyzersMessage)),
            category: DiagnosticCategory.MicrosoftCodeAnalysisCorrectness,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: CreateLocalizableResourceString(nameof(SymbolIsBannedInAnalyzersDescription)),
            helpLinkUri: null,
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        public static readonly DiagnosticDescriptor NoSettingSpecifiedSymbolIsBannedRule = new(
            id: DiagnosticIds.NoSettingSpecifiedSymbolIsBannedInAnalyzersRuleId,
            title: CreateLocalizableResourceString(nameof(NoSettingSpecifiedSymbolIsBannedInAnalyzersTitle)),
            messageFormat: CreateLocalizableResourceString(nameof(NoSettingSpecifiedSymbolIsBannedInAnalyzersMessage)),
            category: DiagnosticCategory.MicrosoftCodeAnalysisCorrectness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: CreateLocalizableResourceString(nameof(NoSettingSpecifiedSymbolIsBannedInAnalyzersDescription)),
            helpLinkUri: null,
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);
    }

    public abstract class SymbolIsBannedInAnalyzersAnalyzer<TSyntaxKind> : SymbolIsBannedAnalyzerBase<TSyntaxKind>
        where TSyntaxKind : struct
    {
        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(SymbolIsBannedInAnalyzersAnalyzer.SymbolIsBannedRule, SymbolIsBannedInAnalyzersAnalyzer.NoSettingSpecifiedSymbolIsBannedRule);

        protected sealed override DiagnosticDescriptor SymbolIsBannedRule => SymbolIsBannedInAnalyzersAnalyzer.SymbolIsBannedRule;

#pragma warning disable RS1025, RS1026 // Configure generated code analysis, Enable concurrent execution. Base Initialize handles these.
        public sealed override void Initialize(AnalysisContext context)
        {
            base.Initialize(context);

            context.RegisterCompilationStartAction(analyzeAnalyzersAndGeneratorsIfPropertyNotSpecified);
            void analyzeAnalyzersAndGeneratorsIfPropertyNotSpecified(CompilationStartAnalysisContext context)
            {
                var propertyValue = context.Options.GetMSBuildPropertyValue(MSBuildPropertyOptionNames.EnforceExtendedAnalyzerRules, context.Compilation);
                // Note: in absence of any value for this property in the project, the generated editorconfig will have an entry like:
                // `build_property.EnforceExtendedAnalyzerRules = `
                if (!string.IsNullOrEmpty(propertyValue))
                {
                    return;
                }

                var provider = WellKnownTypeProvider.GetOrCreate(context.Compilation);
                var diagnosticAnalyzerAttributeType = provider.GetOrCreateTypeByMetadataName(WellKnownTypeNames.MicrosoftCodeAnalysisDiagnosticsDiagnosticAnalyzerAttribute);
                var generatorAttributeType = provider.GetOrCreateTypeByMetadataName(WellKnownTypeNames.MicrosoftCodeAnalysisGeneratorAttribute);
                context.RegisterSymbolAction(analyzePossibleAnalyzerOrGenerator, SymbolKind.NamedType);

                void analyzePossibleAnalyzerOrGenerator(SymbolAnalysisContext symbolAnalysisContext)
                {
                    var symbol = symbolAnalysisContext.Symbol;

                    if (symbol.HasAnyAttribute(diagnosticAnalyzerAttributeType, generatorAttributeType))
                    {
                        symbolAnalysisContext.ReportDiagnostic(symbol.Locations.CreateDiagnostic(SymbolIsBannedInAnalyzersAnalyzer.NoSettingSpecifiedSymbolIsBannedRule, symbol));
                    }
                }
            }
        }

#pragma warning disable RS1012 // 'compilationContext' does not register any analyzer actions. Consider moving actions registered in 'Initialize' that depend on this start action to 'compilationContext'.
        protected sealed override Dictionary<(string ContainerName, string SymbolName), ImmutableArray<BanFileEntry>>? ReadBannedApis(CompilationStartAnalysisContext compilationContext)
        {
            var compilation = compilationContext.Compilation;
            var propertyValue = compilationContext.Options.GetMSBuildPropertyValue(MSBuildPropertyOptionNames.EnforceExtendedAnalyzerRules, compilation);
            if (propertyValue != "true")
                return null;

            const string fileName = "Microsoft.CodeAnalysis.AnalyzerBannedSymbols.txt";
            using var stream = typeof(SymbolIsBannedInAnalyzersAnalyzer<>).Assembly.GetManifestResourceStream(fileName);
            var source = SourceText.From(stream);

            var result = new Dictionary<(string ContainerName, string SymbolName), List<BanFileEntry>>();
            foreach (var line in source.Lines)
            {
                var text = line.ToString();
                if (string.IsNullOrWhiteSpace(text))
                    continue;

                var entry = new BanFileEntry(compilation, text, line.Span, source, fileName);
                var parsed = DocumentationCommentIdParser.ParseDeclaredSymbolId(entry.DeclarationId);
                if (parsed != null)
                {
                    if (!result.TryGetValue(parsed.Value, out var existing))
                    {
                        existing = [];
                        result.Add(parsed.Value, existing);
                    }

                    existing.Add(entry);
                }
            }

            return result.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToImmutableArray());
        }
    }
}
