// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
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
        private const string AssemblyReferenceValidationConfigurationKey = "roslyn_correctness.assembly_reference_validation";
        private const string AssemblyReferenceValidationConfigurationRelaxedValue = "relaxed";

        private static readonly LocalizableString s_localizableTitle = CreateLocalizableResourceString(nameof(DoNotRegisterCompilerTypesWithBadAssemblyReferenceRuleTitle));
        private static readonly LocalizableString s_localizableDescription = CreateLocalizableResourceString(nameof(DoNotRegisterCompilerTypesWithBadAssemblyReferenceRuleDescription));
        private const string HelpLinkUri = "https://github.com/dotnet/roslyn/blob/main/docs/roslyn-analyzers/rules/RS1038.md";

        public static readonly DiagnosticDescriptor DoNotDeclareCompilerFeatureInAssemblyWithWorkspacesReferenceStrictRule = new(
            DiagnosticIds.DoNotRegisterCompilerTypesWithBadAssemblyReferenceRuleId,
            s_localizableTitle,
            CreateLocalizableResourceString(nameof(DoNotDeclareCompilerFeatureInAssemblyWithWorkspacesReferenceMessage)),
            DiagnosticCategory.MicrosoftCodeAnalysisCorrectness,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: s_localizableDescription,
            helpLinkUri: HelpLinkUri,
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        public static readonly DiagnosticDescriptor DoNotDeclareCSharpCompilerFeatureInAssemblyWithVisualBasicReferenceStrictRule = new(
            DiagnosticIds.DoNotRegisterCompilerTypesWithBadAssemblyReferenceRuleId,
            s_localizableTitle,
            CreateLocalizableResourceString(nameof(DoNotDeclareCSharpCompilerFeatureInAssemblyWithVisualBasicReferenceMessage)),
            DiagnosticCategory.MicrosoftCodeAnalysisCorrectness,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: s_localizableDescription,
            helpLinkUri: HelpLinkUri,
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        public static readonly DiagnosticDescriptor DoNotDeclareVisualBasicCompilerFeatureInAssemblyWithCSharpReferenceStrictRule = new(
            DiagnosticIds.DoNotRegisterCompilerTypesWithBadAssemblyReferenceRuleId,
            s_localizableTitle,
            CreateLocalizableResourceString(nameof(DoNotDeclareVisualBasicCompilerFeatureInAssemblyWithCSharpReferenceMessage)),
            DiagnosticCategory.MicrosoftCodeAnalysisCorrectness,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: s_localizableDescription,
            helpLinkUri: HelpLinkUri,
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            DoNotDeclareCompilerFeatureInAssemblyWithWorkspacesReferenceStrictRule,
            DoNotDeclareCSharpCompilerFeatureInAssemblyWithVisualBasicReferenceStrictRule,
            DoNotDeclareVisualBasicCompilerFeatureInAssemblyWithCSharpReferenceStrictRule);

        internal static bool IsStrictAnalysisEnabled(AnalyzerOptions options)
        {
            return !options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue(AssemblyReferenceValidationConfigurationKey, out var value)
                || !value.Trim().Equals(AssemblyReferenceValidationConfigurationRelaxedValue, StringComparison.Ordinal);
        }

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

            context.RegisterCompilationStartAction(context =>
            {
                // This analyzer is enabled by default via a configuration option that also applies to RS1022. It needs
                // to proceed unless .globalconfig contains the following line to enable it:
                //
                // roslyn_correctness.assembly_reference_validation = relaxed
                if (!IsStrictAnalysisEnabled(context.Options))
                {
                    // RS1022 is being applied instead of RS1038
                    return;
                }

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
                // Only examine extension registrations in source
                Debug.Assert(attribute.ApplicationSyntaxReference is not null,
                    $"Expected attributes returned by {nameof(ISymbol.GetAttributes)} (as opposed to {nameof(ITypeSymbolExtensions.GetApplicableAttributes)}) to have a non-null application.");
                if (attribute.ApplicationSyntaxReference is null)
                    continue;

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
                var supportedLanguage = (string?)argument.Value;
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
