// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers
{
    using static CodeAnalysisDiagnosticsResources;

    /// <summary>
    /// RS1001: <inheritdoc cref="MissingDiagnosticAnalyzerAttributeTitle"/>
    /// RS1004: <inheritdoc cref="AddLanguageSupportToAnalyzerTitle"/>
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class DiagnosticAnalyzerAttributeAnalyzer : DiagnosticAnalyzerCorrectnessAnalyzer
    {
        public static readonly DiagnosticDescriptor MissingDiagnosticAnalyzerAttributeRule = new(
            DiagnosticIds.MissingDiagnosticAnalyzerAttributeRuleId,
            CreateLocalizableResourceString(nameof(MissingDiagnosticAnalyzerAttributeTitle)),
            CreateLocalizableResourceString(nameof(MissingAttributeMessage), WellKnownTypeNames.MicrosoftCodeAnalysisDiagnosticsDiagnosticAnalyzerAttribute),
            DiagnosticCategory.MicrosoftCodeAnalysisCorrectness,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: CreateLocalizableResourceString(nameof(MissingDiagnosticAnalyzerAttributeDescription)),
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        public static readonly DiagnosticDescriptor AddLanguageSupportToAnalyzerRule = new(
           DiagnosticIds.AddLanguageSupportToAnalyzerRuleId,
           CreateLocalizableResourceString(nameof(AddLanguageSupportToAnalyzerTitle)),
           CreateLocalizableResourceString(nameof(AddLanguageSupportToAnalyzerMessage)),
           DiagnosticCategory.MicrosoftCodeAnalysisCorrectness,
           DiagnosticSeverity.Warning,
           isEnabledByDefault: true,
           description: CreateLocalizableResourceString(nameof(AddLanguageSupportToAnalyzerDescription)),
           customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(MissingDiagnosticAnalyzerAttributeRule, AddLanguageSupportToAnalyzerRule);

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

            private readonly INamedTypeSymbol? _attributeUsageAttribute;

            public AttributeAnalyzer(INamedTypeSymbol diagnosticAnalyzer, INamedTypeSymbol diagnosticAnalyzerAttribute, INamedTypeSymbol? attributeUsageAttribute)
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
                SyntaxNode? attributeSyntax = null;
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
                    Diagnostic diagnostic = namedType.CreateDiagnostic(MissingDiagnosticAnalyzerAttributeRule);
                    symbolContext.ReportDiagnostic(diagnostic);
                }
                else if (supportsCSharp ^ supportsVB)
                {
                    RoslynDebug.Assert(attributeSyntax != null);

                    // If the analyzer assembly doesn't reference either C# or VB CodeAnalysis assemblies,
                    // then the analyzer is pretty likely a language-agnostic analyzer.
                    Compilation compilation = symbolContext.Compilation;
                    string compilationTypeNameToCheck = supportsCSharp ? CSharpCompilationFullName : BasicCompilationFullName;
                    INamedTypeSymbol? compilationType = compilation.GetOrCreateTypeByMetadataName(compilationTypeNameToCheck);
                    if (compilationType == null)
                    {
                        string missingLanguage = supportsCSharp ? LanguageNames.VisualBasic : LanguageNames.CSharp;
                        Diagnostic diagnostic = attributeSyntax.CreateDiagnostic(AddLanguageSupportToAnalyzerRule, namedType.Name, missingLanguage);
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
                var supportedLanguage = (string?)argument.Value;
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
