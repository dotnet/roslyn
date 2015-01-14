// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class DiagnosticAnalyzerAttributeAnalyzer : DiagnosticAnalyzerCorrectnessAnalyzer
    {
        private static LocalizableString localizableTitleMissingAttribute = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.MissingDiagnosticAnalyzerAttributeTitle), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static LocalizableString localizableMessageMissingAttribute = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.MissingAttributeMessage), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources), DiagnosticAnalyzerAttributeFullName);
        private static LocalizableString localizableDescriptionMissingAttribute = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.MissingDiagnosticAnalyzerAttributeDescription), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));

        public static DiagnosticDescriptor MissingDiagnosticAnalyzerAttributeRule = new DiagnosticDescriptor(
            DiagnosticIds.MissingDiagnosticAnalyzerAttributeRuleId,
            localizableTitleMissingAttribute,
            localizableMessageMissingAttribute,
            DiagnosticCategory.AnalyzerCorrectness,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: localizableDescriptionMissingAttribute,
            customTags: WellKnownDiagnosticTags.Telemetry);

        private static LocalizableString localizableTitleAddLanguageSupportToAnalyzer = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.AddLanguageSupportToAnalyzerTitle), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static LocalizableString localizableMessageAddLanguageSupportToAnalyzer = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.AddLanguageSupportToAnalyzerMessage), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static LocalizableString localizableDescriptionAddLanguageSupportToAnalyzer = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.AddLanguageSupportToAnalyzerDescription), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));

        public static DiagnosticDescriptor AddLanguageSupportToAnalyzerRule = new DiagnosticDescriptor(
            DiagnosticIds.AddLanguageSupportToAnalyzerRuleId,
            localizableTitleAddLanguageSupportToAnalyzer,
            localizableMessageAddLanguageSupportToAnalyzer,
            DiagnosticCategory.AnalyzerCorrectness,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: localizableDescriptionAddLanguageSupportToAnalyzer,
            customTags: WellKnownDiagnosticTags.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(MissingDiagnosticAnalyzerAttributeRule, AddLanguageSupportToAnalyzerRule);
            }
        }

        protected override CompilationAnalyzer GetCompilationAnalyzer(Compilation compilation, INamedTypeSymbol diagnosticAnalyzer, INamedTypeSymbol diagnosticAnalyzerAttribute)
        {
            return new AttributeAnalyzer(diagnosticAnalyzer, diagnosticAnalyzerAttribute);
        }

        private sealed class AttributeAnalyzer : CompilationAnalyzer
        {
            private static readonly string csharpCompilationFullName = @"Microsoft.CodeAnalysis.CSharp.CSharpCompilation";
            private static readonly string basicCompilationFullName = @"Microsoft.CodeAnalysis.VisualBasic.VisualBasicCompilation";

            public AttributeAnalyzer(INamedTypeSymbol diagnosticAnalyzer, INamedTypeSymbol diagnosticAnalyzerAttribute)
                : base(diagnosticAnalyzer, diagnosticAnalyzerAttribute)
            {
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
                var hasMultipleAttributes = false;
                SyntaxNode attributeSyntax = null;
                string supportedLanguage = null;

                var namedTypeAttributes = AttributeHelpers.GetApplicableAttributes(namedType);
                foreach (var attribute in namedTypeAttributes)
                {
                    if (AttributeHelpers.DerivesFrom(attribute.AttributeClass, DiagnosticAnalyzerAttribute))
                    {
                        hasMultipleAttributes |= hasAttribute;
                        hasAttribute = true;

                        if (!hasMultipleAttributes)
                        {
                            foreach (var arg in attribute.ConstructorArguments)
                            {
                                if (arg.Kind == TypedConstantKind.Primitive &&
                                    arg.Type != null &&
                                    arg.Type.SpecialType == SpecialType.System_String)
                                {
                                    supportedLanguage = (string)arg.Value;
                                    attributeSyntax = attribute.ApplicationSyntaxReference.GetSyntax(symbolContext.CancellationToken);
                                }
                            }
                        }
                    }
                }

                if (!hasAttribute)
                {
                    var diagnostic = Diagnostic.Create(MissingDiagnosticAnalyzerAttributeRule, namedType.Locations[0]);
                    symbolContext.ReportDiagnostic(diagnostic);
                }
                else if (!hasMultipleAttributes && supportedLanguage != null)
                {
                    Debug.Assert(attributeSyntax != null);

                    var supportsCSharp = supportedLanguage == LanguageNames.CSharp;
                    var supportsVB = supportedLanguage == LanguageNames.VisualBasic;
                    if (supportsCSharp || supportsVB)
                    {
                        // If the analyzer assembly doesn't reference either C# or VB CodeAnalysis assemblies, 
                        // then the analyzer is pretty likely a language-agnostic analyzer.
                        var compilation = symbolContext.Compilation;
                        var compilationTypeNameToCheck = supportsCSharp ? csharpCompilationFullName : basicCompilationFullName;
                        var compilationType = compilation.GetTypeByMetadataName(compilationTypeNameToCheck);
                        if (compilationType == null)
                        {
                            var missingLanguage = supportsCSharp ? LanguageNames.VisualBasic : LanguageNames.CSharp;
                            var diagnostic = Diagnostic.Create(AddLanguageSupportToAnalyzerRule, attributeSyntax.GetLocation(), namedType.Name, missingLanguage);
                            symbolContext.ReportDiagnostic(diagnostic);
                        }
                    }
                }
            }
        }
    }
}
