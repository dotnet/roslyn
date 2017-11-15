// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class DiagnosticDescriptorCreationAnalyzer : DiagnosticAnalyzer
    {
        private const string HelpLinkUriParameterName = "helpLinkUri";
        private static readonly LocalizableString s_localizableUseLocalizableStringsTitle = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.UseLocalizableStringsInDescriptorTitle), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableUseLocalizableStringsMessage = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.UseLocalizableStringsInDescriptorMessage), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableUseLocalizableStringsDescription = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.UseLocalizableStringsInDescriptorDescription), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableProvideHelpUriTitle = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.ProvideHelpUriInDescriptorTitle), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableProvideHelpUriMessage = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.ProvideHelpUriInDescriptorMessage), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableProvideHelpUriDescription = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.ProvideHelpUriInDescriptorDescription), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));

        public static readonly DiagnosticDescriptor UseLocalizableStringsInDescriptorRule = new DiagnosticDescriptor(
            DiagnosticIds.UseLocalizableStringsInDescriptorRuleId,
            s_localizableUseLocalizableStringsTitle,
            s_localizableUseLocalizableStringsMessage,
            AnalyzerDiagnosticCategory.AnalyzerLocalization,
            DiagnosticHelpers.DefaultDiagnosticSeverity,
            isEnabledByDefault: false,
            description: s_localizableUseLocalizableStringsDescription,
            customTags: WellKnownDiagnosticTags.Telemetry);

        public static readonly DiagnosticDescriptor ProvideHelpUriInDescriptorRule = new DiagnosticDescriptor(
            DiagnosticIds.ProvideHelpUriInDescriptorRuleId,
            s_localizableProvideHelpUriTitle,
            s_localizableProvideHelpUriMessage,
            AnalyzerDiagnosticCategory.AnalyzerLocalization,
            DiagnosticHelpers.DefaultDiagnosticSeverity,
            isEnabledByDefault: false,
            description: s_localizableProvideHelpUriDescription,
            customTags: WellKnownDiagnosticTags.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(UseLocalizableStringsInDescriptorRule, ProvideHelpUriInDescriptorRule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(compilationContext =>
            {
                INamedTypeSymbol diagnosticDescriptorType = compilationContext.Compilation.GetTypeByMetadataName(DiagnosticAnalyzerCorrectnessAnalyzer.DiagnosticDescriptorFullName);
                if (diagnosticDescriptorType == null)
                {
                    return;
                }

                compilationContext.RegisterOperationAction(operationAnalysisContext =>
                {
                    var objectCreation = ((IFieldInitializerOperation)operationAnalysisContext.Operation).Value as IObjectCreationOperation;
                    if (objectCreation == null)
                    {
                        return;
                    }

                    var ctor = objectCreation.Constructor;
                    if (ctor == null ||
                        !diagnosticDescriptorType.Equals(ctor.ContainingType) ||
                        !diagnosticDescriptorType.InstanceConstructors.Any(c => c.Equals(ctor)))
                    {
                        return;
                    }

                    AnalyzeTitle(operationAnalysisContext, objectCreation);
                    AnalyzeHelpLinkUri(operationAnalysisContext, objectCreation);
                }, OperationKind.FieldInitializer);
            });
        }

        private static void AnalyzeTitle(OperationAnalysisContext operationAnalysisContext, IObjectCreationOperation objectCreation)
        {
            IParameterSymbol title = objectCreation.Constructor.Parameters.Where(p => p.Name == "title").FirstOrDefault();
            if (title != null &&
                title.Type != null &&
                title.Type.SpecialType == SpecialType.System_String)
            {
                Diagnostic diagnostic = Diagnostic.Create(UseLocalizableStringsInDescriptorRule, objectCreation.Syntax.GetLocation(), DiagnosticAnalyzerCorrectnessAnalyzer.LocalizableStringFullName);
                operationAnalysisContext.ReportDiagnostic(diagnostic);
            }
        }

        private static void AnalyzeHelpLinkUri(OperationAnalysisContext operationAnalysisContext, IObjectCreationOperation objectCreation)
        {
            // Find the matching argument for helpLinkUri
            foreach (var argument in objectCreation.Arguments)
            {
                if (argument.Parameter.Name.Equals(HelpLinkUriParameterName, System.StringComparison.OrdinalIgnoreCase))
                {
                    if (argument.Value.ConstantValue.HasValue && argument.Value.ConstantValue.Value == null)
                    {
                        Diagnostic diagnostic = Diagnostic.Create(ProvideHelpUriInDescriptorRule, argument.Syntax.GetLocation());
                        operationAnalysisContext.ReportDiagnostic(diagnostic);
                    }

                    return;
                }
            }
        }
    }
}
