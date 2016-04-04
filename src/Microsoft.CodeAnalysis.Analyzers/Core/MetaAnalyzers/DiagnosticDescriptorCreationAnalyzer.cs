// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers
{
    public abstract class DiagnosticDescriptorCreationAnalyzer<TClassDeclarationSyntax, TObjectCreationExpressionSyntax, TLanguageKindEnum> : DiagnosticAnalyzer
        where TClassDeclarationSyntax : SyntaxNode
        where TObjectCreationExpressionSyntax : SyntaxNode
        where TLanguageKindEnum : struct
    {
        private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.UseLocalizableStringsInDescriptorTitle), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.UseLocalizableStringsInDescriptorMessage), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableDescription = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.UseLocalizableStringsInDescriptorDescription), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));

        public static DiagnosticDescriptor UseLocalizableStringsInDescriptorRule = new DiagnosticDescriptor(
            DiagnosticIds.UseLocalizableStringsInDescriptorRuleId,
            s_localizableTitle,
            s_localizableMessage,
            AnalyzerDiagnosticCategory.AnalyzerLocalization,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: false,
            description: s_localizableDescription,
            customTags: WellKnownDiagnosticTags.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(UseLocalizableStringsInDescriptorRule);
            }
        }

        protected abstract ImmutableArray<TLanguageKindEnum> SyntaxKindsOfInterest { get; }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(compilationContext =>
            {
                INamedTypeSymbol diagnosticDescriptorType = compilationContext.Compilation.GetTypeByMetadataName(DiagnosticAnalyzerCorrectnessAnalyzer.DiagnosticDescriptorFullName);
                if (diagnosticDescriptorType == null)
                {
                    return;
                }

                CompilationAnalyzer analyzer = GetAnalyzer(compilationContext.Compilation, diagnosticDescriptorType);
                if (analyzer == null)
                {
                    return;
                }

                compilationContext.RegisterSyntaxNodeAction(c => analyzer.AnalyzeObjectCreation(c), SyntaxKindsOfInterest);
            });
        }

        protected abstract CompilationAnalyzer GetAnalyzer(Compilation compilation, INamedTypeSymbol diagnosticDescriptorType);

        protected abstract class CompilationAnalyzer
        {
            private readonly INamedTypeSymbol _diagnosticDescriptorType;

            protected CompilationAnalyzer(INamedTypeSymbol diagnosticDescriptorType)
            {
                _diagnosticDescriptorType = diagnosticDescriptorType;
            }

            protected abstract SyntaxNode GetObjectCreationType(TObjectCreationExpressionSyntax objectCreation);

            public void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
            {
                var objectCreation = (TObjectCreationExpressionSyntax)context.Node;
                ISymbol symbol = context.SemanticModel.GetSymbolInfo(objectCreation).Symbol;
                if (symbol == null ||
                    symbol.Kind != SymbolKind.Method ||
                    !_diagnosticDescriptorType.Equals(symbol.ContainingType) ||
                    !_diagnosticDescriptorType.InstanceConstructors.Any(c => c.Equals(symbol)))
                {
                    return;
                }

                var method = (IMethodSymbol)symbol;
                IParameterSymbol title = method.Parameters.Where(p => p.Name == "title").FirstOrDefault();
                if (title != null &&
                    title.Type != null &&
                    title.Type.SpecialType == SpecialType.System_String)
                {
                    SyntaxNode typeName = GetObjectCreationType(objectCreation);
                    Diagnostic diagnostic = Diagnostic.Create(UseLocalizableStringsInDescriptorRule, typeName.GetLocation(), DiagnosticAnalyzerCorrectnessAnalyzer.LocalizableStringFullName);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
