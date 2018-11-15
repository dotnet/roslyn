using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis.CodeQuality;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.CSharp.MakeStructFieldsWritable
{
    internal sealed class CSharpMakeStructFieldsWritableDiagnosticAnalyzers : AbstractCodeQualityDiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_diagnosticDescriptor = CreateDescriptor(
            IDEDiagnosticIds.MakeStructFieldsWritable,
            new LocalizableResourceString("test", FeaturesResources.ResourceManager, typeof(FeaturesResources)),
            new LocalizableResourceString("test", FeaturesResources.ResourceManager, typeof(FeaturesResources)),
            isUnneccessary: true);

        public CSharpMakeStructFieldsWritableDiagnosticAnalyzers()
            : base(ImmutableArray.Create(s_diagnosticDescriptor), GeneratedCodeAnalysisFlags.ReportDiagnostics)
        {
        }


        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        {
            return DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;
        }

        public override bool OpenFileOnly(Workspace workspace) => true;

        protected override void InitializeWorker(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(compilationStartContext 
                => CompilationAnalyzer.CreateAndRegisterActions(compilationStartContext));
        }

        private sealed class CompilationAnalyzer
        {
            private bool _hasNonReadonlyFields = false;
            private bool _hasTypeInstanceAssigment = false;

            public static void CreateAndRegisterActions(CompilationStartAnalysisContext compilationStartContext)
            {
                var compilationAnalyzer = new CompilationAnalyzer();
                compilationAnalyzer.RegisterActions(compilationStartContext);
            }
            
            private void RegisterActions(CompilationStartAnalysisContext context)
            {
                context.RegisterSymbolStartAction(symbolStartContext =>
                {
                    var namedTypeSymbol = (INamedTypeSymbol)symbolStartContext.Symbol;
                    if (namedTypeSymbol.TypeKind != TypeKind.Struct) return;

                    symbolStartContext.RegisterSyntaxNodeAction(AnalyzeIfFieldIsReadonly, SymbolKind.Field);
                    symbolStartContext.RegisterOperationAction(AnalyzeAssignment, OperationKind.SimpleAssignment);
                }, SymbolKind.NamedType);
            }

            private void AnalyzeAssignment(OperationAnalysisContext operationContext)
            {
                var operationAssigmnent = (IAssignmentOperation)operationContext.Operation;
                _hasTypeInstanceAssigment = operationAssigmnent.Target is IInstanceReferenceOperation instance && 
                                            instance.ReferenceKind == InstanceReferenceKind.ContainingTypeInstance;
            }

            private void AnalyzeIfFieldIsReadonly(SyntaxNodeAnalysisContext nodeContext)
            {
                var fieldSymbol = (IFieldSymbol)nodeContext.ContainingSymbol;
                _hasNonReadonlyFields |= fieldSymbol.IsReadOnly;
            }
        }
    }
}
