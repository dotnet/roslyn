// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers
{
    using static CodeAnalysisDiagnosticsResources;

    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    internal sealed class CompilerExtensionStrictApiAnalyzer : DiagnosticAnalyzer
    {
        private static readonly LocalizableString s_localizableTitle = CreateLocalizableResourceString(nameof(DoNotRegisterCompilerTypesWithBadAssemblyReferenceRuleTitle));
        private static readonly LocalizableString s_localizableDescription = CreateLocalizableResourceString(nameof(DoNotRegisterCompilerTypesWithBadAssemblyReferenceRuleDescription));

        public static readonly DiagnosticDescriptor DoNotDeclareCompilerFeatureInAssemblyWithWorkspacesReferenceStrictRule = new(
            DiagnosticIds.DoNotRegisterCompilerTypesWithBadAssemblyReferenceRuleId,
            s_localizableTitle,
            CreateLocalizableResourceString(nameof(DoNotDeclareCompilerFeatureInAssemblyWithWorkspacesReferenceMessage)),
            DiagnosticCategory.MicrosoftCodeAnalysisCorrectness,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: s_localizableDescription,
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        public static readonly DiagnosticDescriptor DoNotDeclareCSharpCompilerFeatureInAssemblyWithVisualBasicReferenceStrictRule = new(
            DiagnosticIds.DoNotRegisterCompilerTypesWithBadAssemblyReferenceRuleId,
            s_localizableTitle,
            CreateLocalizableResourceString(nameof(DoNotDeclareCSharpCompilerFeatureInAssemblyWithVisualBasicReferenceMessage)),
            DiagnosticCategory.MicrosoftCodeAnalysisCorrectness,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: s_localizableDescription,
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        public static readonly DiagnosticDescriptor DoNotDeclareVisualBasicCompilerFeatureInAssemblyWithCSharpReferenceStrictRule = new(
            DiagnosticIds.DoNotRegisterCompilerTypesWithBadAssemblyReferenceRuleId,
            s_localizableTitle,
            CreateLocalizableResourceString(nameof(DoNotDeclareVisualBasicCompilerFeatureInAssemblyWithCSharpReferenceMessage)),
            DiagnosticCategory.MicrosoftCodeAnalysisCorrectness,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: s_localizableDescription,
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            DoNotDeclareCompilerFeatureInAssemblyWithWorkspacesReferenceStrictRule,
            DoNotDeclareCSharpCompilerFeatureInAssemblyWithVisualBasicReferenceStrictRule,
            DoNotDeclareVisualBasicCompilerFeatureInAssemblyWithCSharpReferenceStrictRule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

            context.RegisterCompilationStartAction(context =>
            {
                var typeProvider = WellKnownTypeProvider.GetOrCreate(context.Compilation);
                var diagnosticAnalyzer = typeProvider.GetOrCreateTypeByMetadataName(WellKnownTypeNames.MicrosoftCodeAnalysisDiagnosticsDiagnosticAnalyzer);
                if (diagnosticAnalyzer is null)
                    return;

                var referencesWorkspaces = false;
                var referencesCSharp = false;
                var referencesVisualBasic = false;
                foreach (var assemblyName in context.Compilation.ReferencedAssemblyNames)
                {
                    if (assemblyName.Name == "Microsoft.CodeAnalysis.Workspaces")
                    {
                        referencesWorkspaces = true;
                    }
                    else if (assemblyName.Name == "Microsoft.CodeAnalysis.CSharp")
                    {
                        referencesCSharp = true;
                    }
                    else if (assemblyName.Name == "Microsoft.CodeAnalysis.VisualBasic")
                    {
                        referencesVisualBasic = true;
                    }
                }

                if (!referencesWorkspaces && !referencesCSharp && !referencesVisualBasic)
                {
                    // This compilation doesn't reference any assemblies that would produce warnings by this analyzer
                    return;
                }

                var diagnosticAnalyzerAttribute = typeProvider.GetOrCreateTypeByMetadataName(WellKnownTypeNames.MicrosoftCodeAnalysisDiagnosticsDiagnosticAnalyzerAttribute);
                var sourceGeneratorInterface = typeProvider.GetOrCreateTypeByMetadataName(WellKnownTypeNames.MicrosoftCodeAnalysisISourceGenerator);
                var incrementalGeneratorInterface = typeProvider.GetOrCreateTypeByMetadataName(WellKnownTypeNames.MicrosoftCodeAnalysisIIncrementalGenerator);
                var generatorAttribute = typeProvider.GetOrCreateTypeByMetadataName(WellKnownTypeNames.MicrosoftCodeAnalysisGeneratorAttribute);

                context.RegisterSymbolAction(
                    context =>
                    {
                        var namedType = (INamedTypeSymbol)context.Symbol;
                        if (!IsRegisteredExtension(namedType, diagnosticAnalyzer, diagnosticAnalyzerAttribute, out var applicationSyntaxReference, out var supportsCSharp, out var supportsVisualBasic)
                            && !IsRegisteredExtension(namedType, sourceGeneratorInterface, generatorAttribute, out applicationSyntaxReference, out supportsCSharp, out supportsVisualBasic)
                            && !IsRegisteredExtension(namedType, incrementalGeneratorInterface, generatorAttribute, out applicationSyntaxReference, out supportsCSharp, out supportsVisualBasic))
                        {
                            // This is not a compiler extension
                            return;
                        }

                        DiagnosticDescriptor descriptor;
                        if (referencesWorkspaces)
                        {
                            descriptor = DoNotDeclareCompilerFeatureInAssemblyWithWorkspacesReferenceStrictRule;
                        }
                        else if (supportsCSharp && referencesVisualBasic)
                        {
                            descriptor = DoNotDeclareCSharpCompilerFeatureInAssemblyWithVisualBasicReferenceStrictRule;
                        }
                        else if (supportsVisualBasic && referencesCSharp)
                        {
                            descriptor = DoNotDeclareVisualBasicCompilerFeatureInAssemblyWithCSharpReferenceStrictRule;
                        }
                        else
                        {
                            return;
                        }

                        context.ReportDiagnostic(Diagnostic.Create(descriptor, Location.Create(applicationSyntaxReference.SyntaxTree, applicationSyntaxReference.Span)));
                    },
                    SymbolKind.NamedType);
            });
        }

        private static bool IsRegisteredExtension(INamedTypeSymbol extension, [NotNullWhen(true)] INamedTypeSymbol? extensionClassOrInterface, [NotNullWhen(true)] INamedTypeSymbol? registrationAttributeType, [NotNullWhen(true)] out SyntaxReference? node, out bool supportsCSharp, out bool supportsVisualBasic)
        {
            supportsCSharp = false;
            supportsVisualBasic = false;

            if (!extension.Inherits(extensionClassOrInterface))
            {
                node = null;
                return false;
            }

            foreach (var attribute in extension.GetAttributes())
            {
                if (!attribute.AttributeClass.Inherits(registrationAttributeType))
                    continue;

                foreach (var arg in attribute.ConstructorArguments)
                {
                    CheckLanguage(arg, ref supportsCSharp, ref supportsVisualBasic);
                    if (arg.Kind == TypedConstantKind.Array)
                    {
                        foreach (var element in arg.Values)
                        {
                            CheckLanguage(element, ref supportsCSharp, ref supportsVisualBasic);
                        }
                    }
                }

                node = attribute.ApplicationSyntaxReference;
                return true;
            }

            node = null;
            return false;
        }

        private static void CheckLanguage(TypedConstant argument, ref bool supportsCSharp, ref bool supportsVisualBasic)
        {
            if (argument is { Kind: TypedConstantKind.Primitive, Type.SpecialType: SpecialType.System_String })
            {
                string supportedLanguage = (string)argument.Value;
                if (supportedLanguage == LanguageNames.CSharp)
                {
                    supportsCSharp = true;
                }
                else if (supportedLanguage == LanguageNames.VisualBasic)
                {
                    supportsVisualBasic = true;
                }
            }
        }
    }
}
