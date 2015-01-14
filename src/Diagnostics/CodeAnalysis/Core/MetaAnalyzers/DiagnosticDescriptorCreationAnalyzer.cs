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
        private static LocalizableString localizableTitle = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.UseLocalizableStringsInDescriptorTitle), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static LocalizableString localizableMessage = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.UseLocalizableStringsInDescriptorMessage), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static LocalizableString localizableDescription = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.UseLocalizableStringsInDescriptorDescription), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));

        public static DiagnosticDescriptor UseLocalizableStringsInDescriptorRule = new DiagnosticDescriptor(
            DiagnosticIds.UseLocalizableStringsInDescriptorRuleId,
            localizableTitle,
            localizableMessage,
            DiagnosticCategory.AnalyzerLocalization,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: false,
            description: localizableDescription,
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
                var diagnosticDescriptorType = compilationContext.Compilation.GetTypeByMetadataName(DiagnosticAnalyzerCorrectnessAnalyzer.DiagnosticDescriptorFullName);
                if (diagnosticDescriptorType == null)
                {
                    return;
                }

                var analyzer = GetAnalyzer(compilationContext.Compilation, diagnosticDescriptorType);
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
            private readonly INamedTypeSymbol diagnosticDescriptorType;

            protected CompilationAnalyzer(INamedTypeSymbol diagnosticDescriptorType)
            {
                this.diagnosticDescriptorType = diagnosticDescriptorType;
            }

            protected abstract SyntaxNode GetObjectCreationType(TObjectCreationExpressionSyntax objectCreation);

            public void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
            {
                var objectCreation = (TObjectCreationExpressionSyntax)context.Node;
                var symbol = context.SemanticModel.GetSymbolInfo(objectCreation).Symbol;
                if (symbol == null ||
                    symbol.Kind != SymbolKind.Method ||
                    !diagnosticDescriptorType.Equals(symbol.ContainingType) ||
                    !diagnosticDescriptorType.InstanceConstructors.Any(c => c.Equals(symbol)))
                {
                    return;
                }

                var method = (IMethodSymbol)symbol;
                var title = method.Parameters.Where(p => p.Name == "title").FirstOrDefault();
                if (title != null && 
                    title.Type != null && 
                    title.Type.SpecialType == SpecialType.System_String)
                {
                    var typeName = GetObjectCreationType(objectCreation);
                    var diagnostic = Diagnostic.Create(UseLocalizableStringsInDescriptorRule, typeName.GetLocation(), DiagnosticAnalyzerCorrectnessAnalyzer.LocalizableStringFullName);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
