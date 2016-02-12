// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Analyzer.Utilities;

namespace Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers
{
    public abstract class DiagnosticAnalyzerFieldsAnalyzer<TClassDeclarationSyntax, TFieldDeclarationSyntax, TTypeSyntax, TVariableTypeDeclarationSyntax> : DiagnosticAnalyzerCorrectnessAnalyzer
        where TClassDeclarationSyntax : SyntaxNode
        where TFieldDeclarationSyntax : SyntaxNode
        where TTypeSyntax : SyntaxNode
        where TVariableTypeDeclarationSyntax : SyntaxNode
    {
        private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.DoNotStorePerCompilationDataOntoFieldsTitle), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.DoNotStorePerCompilationDataOntoFieldsMessage), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableDescription = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.DoNotStorePerCompilationDataOntoFieldsDescription), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources), nameof(AnalysisContext), DiagnosticAnalyzerCorrectnessAnalyzer.RegisterCompilationStartActionName);
        private static readonly string s_compilationTypeFullName = typeof(Compilation).FullName;
        private static readonly string s_symbolTypeFullName = typeof(ISymbol).FullName;

        public static DiagnosticDescriptor DoNotStorePerCompilationDataOntoFieldsRule = new DiagnosticDescriptor(
            DiagnosticIds.DoNotStorePerCompilationDataOntoFieldsRuleId,
            s_localizableTitle,
            s_localizableMessage,
            AnalyzerDiagnosticCategory.AnalyzerPerformance,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: s_localizableDescription,
            customTags: WellKnownDiagnosticTags.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(DoNotStorePerCompilationDataOntoFieldsRule);
            }
        }

        [SuppressMessage("AnalyzerPerformance", "RS1012:Start action has no registered actions.", Justification = "Method returns an analyzer that is registered by the caller.")]
        protected override DiagnosticAnalyzerSymbolAnalyzer GetDiagnosticAnalyzerSymbolAnalyzer(CompilationStartAnalysisContext compilationContext, INamedTypeSymbol diagnosticAnalyzer, INamedTypeSymbol diagnosticAnalyzerAttribute)
        {
            Compilation compilation = compilationContext.Compilation;

            INamedTypeSymbol compilationType = compilation.GetTypeByMetadataName(s_compilationTypeFullName);
            if (compilationType == null)
            {
                return null;
            }

            INamedTypeSymbol symbolType = compilation.GetTypeByMetadataName(s_symbolTypeFullName);
            if (symbolType == null)
            {
                return null;
            }

            return new FieldsAnalyzer(compilationType, symbolType, diagnosticAnalyzer, diagnosticAnalyzerAttribute);
        }

        private sealed class FieldsAnalyzer : SyntaxNodeWithinAnalyzerTypeCompilationAnalyzer<TClassDeclarationSyntax, TFieldDeclarationSyntax>
        {
            private readonly INamedTypeSymbol _compilationType;
            private readonly INamedTypeSymbol _symbolType;

            public FieldsAnalyzer(INamedTypeSymbol compilationType, INamedTypeSymbol symbolType, INamedTypeSymbol diagnosticAnalyzer, INamedTypeSymbol diagnosticAnalyzerAttribute)
                : base(diagnosticAnalyzer, diagnosticAnalyzerAttribute)
            {
                _compilationType = compilationType;
                _symbolType = symbolType;
            }

            protected override void AnalyzeDiagnosticAnalyzer(SymbolAnalysisContext symbolContext)
            {
                var namedType = (INamedTypeSymbol)symbolContext.Symbol;
                if (!HasDiagnosticAnalyzerAttribute(namedType))
                {
                    // We are interested only in DiagnosticAnalyzer types with DiagnosticAnalyzerAttribute.
                    return;
                }

                base.AnalyzeDiagnosticAnalyzer(symbolContext);
            }

            protected override void AnalyzeNode(SymbolAnalysisContext symbolContext, TFieldDeclarationSyntax syntaxNode, SemanticModel semanticModel)
            {
                // Get all the type syntax nodes within the topmost type declaration nodes for field declarations.
                System.Collections.Generic.IEnumerable<TVariableTypeDeclarationSyntax> variableTypeDeclarations = syntaxNode.DescendantNodesAndSelf().OfType<TVariableTypeDeclarationSyntax>();
                System.Collections.Generic.IEnumerable<TTypeSyntax> topMostTypeNodes = variableTypeDeclarations.SelectMany(typeDecl => typeDecl.ChildNodes().OfType<TTypeSyntax>());
                System.Collections.Generic.IEnumerable<TTypeSyntax> typeNodes = topMostTypeNodes.SelectMany(t => t.DescendantNodesAndSelf().OfType<TTypeSyntax>());

                foreach (TTypeSyntax typeNode in typeNodes)
                {
                    ITypeSymbol type = semanticModel.GetTypeInfo(typeNode, symbolContext.CancellationToken).Type;
                    if (type != null)
                    {
                        foreach (ITypeSymbol innerType in type.GetBaseTypesAndThis())
                        {
                            if (innerType.Equals(_compilationType))
                            {
                                ReportDiagnostic(type, typeNode, symbolContext);
                                return;
                            }
                        }

                        foreach (INamedTypeSymbol iface in type.AllInterfaces)
                        {
                            if (iface.Equals(_symbolType))
                            {
                                ReportDiagnostic(type, typeNode, symbolContext);
                                return;
                            }
                        }
                    }
                }
            }

            private static void ReportDiagnostic(ITypeSymbol type, TTypeSyntax typeSyntax, SymbolAnalysisContext context)
            {
                Diagnostic diagnostic = Diagnostic.Create(DoNotStorePerCompilationDataOntoFieldsRule, typeSyntax.GetLocation(), type.ToDisplayString());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
