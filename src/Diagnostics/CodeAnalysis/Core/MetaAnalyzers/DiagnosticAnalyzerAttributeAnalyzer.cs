// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class DiagnosticAnalyzerAttributeAnalyzer : DiagnosticAnalyzerCorrectnessAnalyzer
    {
        private static readonly LocalizableString s_localizableTitleMissingAttribute = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.MissingDiagnosticAnalyzerAttributeTitle), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableMessageMissingAttribute = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.MissingAttributeMessage), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources), DiagnosticAnalyzerAttributeFullName);
        private static readonly LocalizableString s_localizableDescriptionMissingAttribute = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.MissingDiagnosticAnalyzerAttributeDescription), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));

        public static DiagnosticDescriptor MissingDiagnosticAnalyzerAttributeRule = new DiagnosticDescriptor(
            DiagnosticIds.MissingDiagnosticAnalyzerAttributeRuleId,
            s_localizableTitleMissingAttribute,
            s_localizableMessageMissingAttribute,
            DiagnosticCategory.AnalyzerCorrectness,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: s_localizableDescriptionMissingAttribute,
            customTags: WellKnownDiagnosticTags.Telemetry);

        private static readonly LocalizableString s_localizableTitleAddLanguageSupportToAnalyzer = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.AddLanguageSupportToAnalyzerTitle), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableMessageAddLanguageSupportToAnalyzer = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.AddLanguageSupportToAnalyzerMessage), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableDescriptionAddLanguageSupportToAnalyzer = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.AddLanguageSupportToAnalyzerDescription), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));

        public static DiagnosticDescriptor AddLanguageSupportToAnalyzerRule = new DiagnosticDescriptor(
            DiagnosticIds.AddLanguageSupportToAnalyzerRuleId,
            s_localizableTitleAddLanguageSupportToAnalyzer,
            s_localizableMessageAddLanguageSupportToAnalyzer,
            DiagnosticCategory.AnalyzerCorrectness,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: s_localizableDescriptionAddLanguageSupportToAnalyzer,
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
            private static readonly string s_csharpCompilationFullName = @"Microsoft.CodeAnalysis.CSharp.CSharpCompilation";
            private static readonly string s_basicCompilationFullName = @"Microsoft.CodeAnalysis.VisualBasic.VisualBasicCompilation";

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
                SyntaxNode attributeSyntax = null;
                bool supportsCSharp = false;
                bool supportsVB = false;

                var namedTypeAttributes = AttributeHelpers.GetApplicableAttributes(namedType);
                foreach (var attribute in namedTypeAttributes)
                {
                    if (AttributeHelpers.DerivesFrom(attribute.AttributeClass, DiagnosticAnalyzerAttribute))
                    {
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
                    var diagnostic = Diagnostic.Create(MissingDiagnosticAnalyzerAttributeRule, namedType.Locations[0]);
                    symbolContext.ReportDiagnostic(diagnostic);
                }
                else if (supportsCSharp ^ supportsVB)
                {
                    Debug.Assert(attributeSyntax != null);

                    // If the analyzer assembly doesn't reference either C# or VB CodeAnalysis assemblies, 
                    // then the analyzer is pretty likely a language-agnostic analyzer.
                    var compilation = symbolContext.Compilation;
                    var compilationTypeNameToCheck = supportsCSharp ? s_csharpCompilationFullName : s_basicCompilationFullName;
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
