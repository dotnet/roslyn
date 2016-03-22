// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.SymbolCategorization;

namespace Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles
{
    internal abstract class NamingStyleDiagnosticAnalyzerBase : DiagnosticAnalyzer, IBuiltInAnalyzer
    {
        private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(FeaturesResources.NamingStylesDiagnosticTitle), FeaturesResources.ResourceManager, typeof(FeaturesResources));
        private static readonly LocalizableString s_localizableTitleNamingStyle = new LocalizableResourceString(nameof(FeaturesResources.NamingStylesDiagnosticTitle), FeaturesResources.ResourceManager, typeof(FeaturesResources));

        // Individual diagnostics have their own descriptors, so this is just used to satisfy the
        // SupportedDiagnostics API. The DiagnosticSeverity must be non-"Hidden" to run on closed
        // documents.
        private static readonly DiagnosticDescriptor s_descriptorNamingStyle = new DiagnosticDescriptor(
            IDEDiagnosticIds.NamingRuleId,
            s_localizableTitleNamingStyle,
            s_localizableMessage,
            DiagnosticCategory.Style,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        // Applicable SymbolKind list is limited due to https://github.com/dotnet/roslyn/issues/8753. 
        // We would prefer to respond to the names of all symbols.
        private static readonly ImmutableArray<SymbolKind> _symbolKinds = new[] 
            {
                SymbolKind.Event,
                SymbolKind.Field,
                SymbolKind.Method,
                SymbolKind.NamedType,
                SymbolKind.Namespace,
                SymbolKind.Property
            }.ToImmutableArray();

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_descriptorNamingStyle);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(CompilationStartAction);
        }

        private void CompilationStartAction(CompilationStartAnalysisContext context)
        {
            var workspace = (context.Options as WorkspaceAnalyzerOptions)?.Workspace;
            var categorizationService = workspace.Services.GetService<ISymbolCategorizationService>();

            var optionSet = (context.Options as WorkspaceAnalyzerOptions)?.Workspace.Options;
            var currentValue = optionSet.GetOption(SimplificationOptions.NamingPreferences, context.Compilation.Language);

            if (!string.IsNullOrEmpty(currentValue))
            {
                // Deserializing the naming preference info on every CompilationStart is expensive.
                // Instead, the diagnostic engine should listen for option changes and have the
                // ability to create the new SerializableNamingStylePreferencesInfo when it detects
                // any change. The overall system would then only deserialize & allocate when 
                // actually necessary.
                var viewModel = SerializableNamingStylePreferencesInfo.FromXElement(XElement.Parse(currentValue));
                var preferencesInfo = viewModel.GetPreferencesInfo();
                context.RegisterSymbolAction(
                    symbolContext => SymbolAction(symbolContext, preferencesInfo, categorizationService),
                    _symbolKinds);
            }
        }

        private void SymbolAction(SymbolAnalysisContext context, NamingStylePreferencesInfo preferences, ISymbolCategorizationService categorizationService)
        {
            NamingRule applicableRule;
            if (preferences.TryGetApplicableRule(context.Symbol, categorizationService, out applicableRule))
            {
                string failureReason;
                if (applicableRule.EnforcementLevel != DiagnosticSeverity.Hidden && 
                    !applicableRule.IsNameCompliant(context.Symbol.Name, out failureReason))
                {
                    var descriptor = new DiagnosticDescriptor(IDEDiagnosticIds.NamingRuleId,
                         s_localizableTitleNamingStyle,
                         string.Format(FeaturesResources.NamingViolationDescription, applicableRule.Title, failureReason),
                         DiagnosticCategory.Style,
                         applicableRule.EnforcementLevel,
                         isEnabledByDefault: true);

                    var builder = ImmutableDictionary.CreateBuilder<string, string>();
                    builder[nameof(NamingStyle)] = applicableRule.NamingStyle.CreateXElement().ToString();
                    builder["OptionName"] = nameof(SimplificationOptions.NamingPreferences);
                    builder["OptionLanguage"] = context.Compilation.Language;
                    context.ReportDiagnostic(Diagnostic.Create(descriptor, context.Symbol.Locations.First(), builder.ToImmutable()));
                }
            }
        }

        public DiagnosticAnalyzerCategory GetAnalyzerCategory()
        {
            return DiagnosticAnalyzerCategory.SemanticSpanAnalysis;
        }
    }
}
