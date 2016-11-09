// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles
{
    internal abstract class NamingStyleDiagnosticAnalyzerBase : 
        AbstractCodeStyleDiagnosticAnalyzer, IBuiltInAnalyzer
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
        private static readonly ImmutableArray<SymbolKind> _symbolKinds = new[]
            {
                SymbolKind.Event,
                SymbolKind.Field,
                SymbolKind.Method,
                SymbolKind.NamedType,
                SymbolKind.Namespace,
                SymbolKind.Property
            }.ToImmutableArray();

        public bool OpenFileOnly(Workspace workspace) => true;

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterCompilationStartAction(CompilationStartAction);

        private void CompilationStartAction(CompilationStartAnalysisContext context)
        {
            var workspace = (context.Options as WorkspaceAnalyzerOptions)?.Workspace;
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
                    symbolContext => SymbolAction(symbolContext, preferencesInfo),
                    _symbolKinds);
            }
        }

        private void SymbolAction(SymbolAnalysisContext context, NamingStylePreferencesInfo preferences)
        {
            NamingRule applicableRule;
            if (preferences.TryGetApplicableRule(context.Symbol, out applicableRule))
            {
                string failureReason;
                if (applicableRule.EnforcementLevel != DiagnosticSeverity.Hidden &&
                    !applicableRule.IsNameCompliant(context.Symbol.Name, out failureReason))
                {
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
            }
        }

        public DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;
    }
}