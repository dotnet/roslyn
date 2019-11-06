// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;

namespace Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class DiagnosticAnalyzerAttributeAnalyzer : DiagnosticAnalyzerCorrectnessAnalyzer
    {
        private static readonly LocalizableString s_localizableTitleMissingAttribute = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.MissingDiagnosticAnalyzerAttributeTitle), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableMessageMissingAttribute = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.MissingAttributeMessage), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources), DiagnosticAnalyzerAttributeFullName);
        private static readonly LocalizableString s_localizableDescriptionMissingAttribute = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.MissingDiagnosticAnalyzerAttributeDescription), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));

        public static readonly DiagnosticDescriptor MissingDiagnosticAnalyzerAttributeRule = new DiagnosticDescriptor(
            DiagnosticIds.MissingDiagnosticAnalyzerAttributeRuleId,
            s_localizableTitleMissingAttribute,
            s_localizableMessageMissingAttribute,
            DiagnosticCategory.MicrosoftCodeAnalysisCorrectness,
            DiagnosticHelpers.DefaultDiagnosticSeverity,
            isEnabledByDefault: DiagnosticHelpers.EnabledByDefaultIfNotBuildingVSIX,
            description: s_localizableDescriptionMissingAttribute,
            customTags: WellKnownDiagnosticTags.Telemetry);

        private static readonly LocalizableString s_localizableTitleAddLanguageSupportToAnalyzer = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.AddLanguageSupportToAnalyzerTitle), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableMessageAddLanguageSupportToAnalyzer = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.AddLanguageSupportToAnalyzerMessage), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableDescriptionAddLanguageSupportToAnalyzer = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.AddLanguageSupportToAnalyzerDescription), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));

        public static readonly DiagnosticDescriptor AddLanguageSupportToAnalyzerRule = new DiagnosticDescriptor(
            DiagnosticIds.AddLanguageSupportToAnalyzerRuleId,
            s_localizableTitleAddLanguageSupportToAnalyzer,
            s_localizableMessageAddLanguageSupportToAnalyzer,
            DiagnosticCategory.MicrosoftCodeAnalysisCorrectness,
            DiagnosticHelpers.DefaultDiagnosticSeverity,
            isEnabledByDefault: DiagnosticHelpers.EnabledByDefaultIfNotBuildingVSIX,
            description: s_localizableDescriptionAddLanguageSupportToAnalyzer,
            customTags: WellKnownDiagnosticTags.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(MissingDiagnosticAnalyzerAttributeRule, AddLanguageSupportToAnalyzerRule);

#pragma warning disable RS1025 // Configure generated code analysis
        public override void Initialize(AnalysisContext context)
#pragma warning restore RS1025 // Configure generated code analysis
        {
            context.EnableConcurrentExecution();

            base.Initialize(context);
        }

        [SuppressMessage("AnalyzerPerformance", "RS1012:Start action has no registered actions.", Justification = "Method returns an analyzer that is registered by the caller.")]
        protected override DiagnosticAnalyzerSymbolAnalyzer GetDiagnosticAnalyzerSymbolAnalyzer(CompilationStartAnalysisContext compilationContext, INamedTypeSymbol diagnosticAnalyzer, INamedTypeSymbol diagnosticAnalyzerAttribute)
        {
            var attributeUsageAttribute = compilationContext.Compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemAttributeUsageAttribute);
            return new AttributeAnalyzer(diagnosticAnalyzer, diagnosticAnalyzerAttribute, attributeUsageAttribute);
        }

        private sealed class AttributeAnalyzer : DiagnosticAnalyzerSymbolAnalyzer
        {
            private const string CSharpCompilationFullName = @"Microsoft.CodeAnalysis.CSharp.CSharpCompilation";
            private const string BasicCompilationFullName = @"Microsoft.CodeAnalysis.VisualBasic.VisualBasicCompilation";

            private readonly INamedTypeSymbol _attributeUsageAttribute;

            public AttributeAnalyzer(INamedTypeSymbol diagnosticAnalyzer, INamedTypeSymbol diagnosticAnalyzerAttribute, INamedTypeSymbol attributeUsageAttribute)
                : base(diagnosticAnalyzer, diagnosticAnalyzerAttribute)
            {
                _attributeUsageAttribute = attributeUsageAttribute;
            }

            protected override void AnalyzeDiagnosticAnalyzer(SymbolAnalysisContext symbolContext)
            {
                var namedType = (INamedTypeSymbol)symbolContext.Symbol;
                if (namedType.IsAbstract)
                {
                    return;
                }

                // 1) MissingDiagnosticAnalyzerAttributeRule: DiagnosticAnalyzer has no DiagnosticAnalyzerAttribute.
                // 2) AddLanguageSupportToAnalyzerRule: For analyzer supporting only one of C# or VB languages, detect if it can support the other language.

                var hasAttribute = false;
                SyntaxNode attributeSyntax = null;
                bool supportsCSharp = false;
                bool supportsVB = false;

                var namedTypeAttributes = namedType.GetApplicableAttributes(_attributeUsageAttribute);

                foreach (AttributeData attribute in namedTypeAttributes)
                {
                    if (attribute.AttributeClass.DerivesFrom(DiagnosticAnalyzerAttribute))
                    {
                        // Bail out for the case where analyzer type derives from a sub-type in different assembly, and the sub-type has the diagnostic analyzer attribute.
                        if (attribute.ApplicationSyntaxReference == null)
                        {
                            return;
                        }

                        hasAttribute = true;

                        // The attribute constructor's signature is "(string, params string[])",
                        // so process both string arguments and string[] arguments.
                        foreach (TypedConstant arg in attribute.ConstructorArguments)
                        {
                            CheckLanguage(arg, ref supportsCSharp, ref supportsVB);

                            if (arg.Kind == TypedConstantKind.Array)
                            {
                                foreach (TypedConstant element in arg.Values)
                                {
                                    CheckLanguage(element, ref supportsCSharp, ref supportsVB);
                                }
                            }
                        }

                        attributeSyntax = attribute.ApplicationSyntaxReference.GetSyntax(symbolContext.CancellationToken);
                    }
                }

                if (!hasAttribute)
                {
                    Diagnostic diagnostic = Diagnostic.Create(MissingDiagnosticAnalyzerAttributeRule, namedType.Locations[0]);
                    symbolContext.ReportDiagnostic(diagnostic);
                }
                else if (supportsCSharp ^ supportsVB)
                {
                    Debug.Assert(attributeSyntax != null);

                    // If the analyzer assembly doesn't reference either C# or VB CodeAnalysis assemblies,
                    // then the analyzer is pretty likely a language-agnostic analyzer.
                    Compilation compilation = symbolContext.Compilation;
                    string compilationTypeNameToCheck = supportsCSharp ? CSharpCompilationFullName : BasicCompilationFullName;
                    INamedTypeSymbol compilationType = compilation.GetOrCreateTypeByMetadataName(compilationTypeNameToCheck);
                    if (compilationType == null)
                    {
                        string missingLanguage = supportsCSharp ? LanguageNames.VisualBasic : LanguageNames.CSharp;
                        Diagnostic diagnostic = Diagnostic.Create(AddLanguageSupportToAnalyzerRule, attributeSyntax.GetLocation(), namedType.Name, missingLanguage);
                        symbolContext.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }

        private static void CheckLanguage(TypedConstant argument, ref bool supportsCSharp, ref bool supportsVB)
        {
            if (argument.Kind == TypedConstantKind.Primitive &&
                argument.Type != null &&
                argument.Type.SpecialType == SpecialType.System_String)
            {
                string supportedLanguage = (string)argument.Value;
                if (supportedLanguage == LanguageNames.CSharp)
                {
                    supportsCSharp = true;
                }
                else if (supportedLanguage == LanguageNames.VisualBasic)
                {
                    supportsVB = true;
                }
            }
        }
    }
}
