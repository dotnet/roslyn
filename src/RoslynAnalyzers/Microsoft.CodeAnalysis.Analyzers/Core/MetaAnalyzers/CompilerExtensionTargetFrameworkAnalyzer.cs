// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Versioning;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers
{
    using static CodeAnalysisDiagnosticsResources;

    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    internal sealed class CompilerExtensionTargetFrameworkAnalyzer : DiagnosticAnalyzer
    {
        private static readonly LocalizableString s_localizableTitle = CreateLocalizableResourceString(nameof(DoNotDeclareCompilerFeatureInAssemblyWithUnsupportedTargetFrameworkRuleTitle));
        private static readonly LocalizableString s_localizableDescription = CreateLocalizableResourceString(nameof(DoNotDeclareCompilerFeatureInAssemblyWithUnsupportedTargetFrameworkRuleDescription));
        private const string HelpLinkUri = "https://github.com/dotnet/roslyn/blob/main/docs/roslyn-analyzers/rules/RS1041.md";

        public static readonly DiagnosticDescriptor DoNotDeclareCompilerFeatureInAssemblyWithUnsupportedTargetFrameworkStrictRule = new(
            DiagnosticIds.DoNotRegisterCompilerTypesWithBadTargetFrameworkRuleId,
            s_localizableTitle,
            CreateLocalizableResourceString(nameof(DoNotDeclareCompilerFeatureInAssemblyWithUnsupportedTargetFrameworkMessage)),
            DiagnosticCategory.MicrosoftCodeAnalysisCorrectness,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: s_localizableDescription,
            helpLinkUri: HelpLinkUri,
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            DoNotDeclareCompilerFeatureInAssemblyWithUnsupportedTargetFrameworkStrictRule);

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

                var targetFrameworkAttribute = typeProvider.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemRuntimeVersioningTargetFrameworkAttribute);
                if (targetFrameworkAttribute is null)
                    return;

                AttributeData? appliedTargetFrameworkAttribute = context.Compilation.Assembly.GetAttribute(targetFrameworkAttribute);
                if (appliedTargetFrameworkAttribute is null)
                    return;

                if (appliedTargetFrameworkAttribute.ConstructorArguments.IsEmpty)
                {
                    return;
                }

                string displayName;
                switch (appliedTargetFrameworkAttribute.ConstructorArguments[0].Value as string)
                {
                    case ".NETStandard,Version=v1.0":
                    case ".NETStandard,Version=v1.1":
                    case ".NETStandard,Version=v1.2":
                    case ".NETStandard,Version=v1.3":
                    case ".NETStandard,Version=v1.4":
                    case ".NETStandard,Version=v1.5":
                    case ".NETStandard,Version=v1.6":
                    case ".NETStandard,Version=v2.0":
                        // The compiler supports this target framework
                        return;

                    default:
                        displayName = appliedTargetFrameworkAttribute.NamedArguments.FirstOrDefault(arg => arg.Key == nameof(TargetFrameworkAttribute.FrameworkDisplayName)).Value.Value as string
                            ?? appliedTargetFrameworkAttribute.ConstructorArguments[0].Value as string
                            ?? "<unknown>";
                        break;
                }

                var diagnosticAnalyzerAttribute = typeProvider.GetOrCreateTypeByMetadataName(WellKnownTypeNames.MicrosoftCodeAnalysisDiagnosticsDiagnosticAnalyzerAttribute);
                var sourceGeneratorInterface = typeProvider.GetOrCreateTypeByMetadataName(WellKnownTypeNames.MicrosoftCodeAnalysisISourceGenerator);
                var incrementalGeneratorInterface = typeProvider.GetOrCreateTypeByMetadataName(WellKnownTypeNames.MicrosoftCodeAnalysisIIncrementalGenerator);
                var generatorAttribute = typeProvider.GetOrCreateTypeByMetadataName(WellKnownTypeNames.MicrosoftCodeAnalysisGeneratorAttribute);

                context.RegisterSymbolAction(
                    context =>
                    {
                        var namedType = (INamedTypeSymbol)context.Symbol;
                        if (!IsRegisteredExtension(namedType, diagnosticAnalyzer, diagnosticAnalyzerAttribute, out var applicationSyntaxReference)
                            && !IsRegisteredExtension(namedType, sourceGeneratorInterface, generatorAttribute, out applicationSyntaxReference)
                            && !IsRegisteredExtension(namedType, incrementalGeneratorInterface, generatorAttribute, out applicationSyntaxReference))
                        {
                            // This is not a compiler extension
                            return;
                        }

                        context.ReportDiagnostic(Diagnostic.Create(
                            DoNotDeclareCompilerFeatureInAssemblyWithUnsupportedTargetFrameworkStrictRule,
                            Location.Create(applicationSyntaxReference.SyntaxTree, applicationSyntaxReference.Span),
                            displayName));
                    },
                    SymbolKind.NamedType);
            });
        }

        private static bool IsRegisteredExtension(INamedTypeSymbol extension, [NotNullWhen(true)] INamedTypeSymbol? extensionClassOrInterface, [NotNullWhen(true)] INamedTypeSymbol? registrationAttributeType, [NotNullWhen(true)] out SyntaxReference? node)
        {
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

                node = attribute.ApplicationSyntaxReference;
                return true;
            }

            node = null;
            return false;
        }
    }
}
