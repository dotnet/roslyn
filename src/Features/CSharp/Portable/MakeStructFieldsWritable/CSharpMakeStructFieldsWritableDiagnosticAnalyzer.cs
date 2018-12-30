using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeQuality;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.CSharp.MakeStructFieldsWritable
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpMakeStructFieldsWritableDiagnosticAnalyzer : AbstractCodeQualityDiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_diagnosticDescriptor = CreateDescriptor(
            IDEDiagnosticIds.MakeStructFieldsWritable,
            new LocalizableResourceString(nameof(FeaturesResources.Make_readonly_fields_writable), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
            new LocalizableResourceString(nameof(FeaturesResources.Make_readonly_fields_writable), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
            isUnneccessary: true);

        public CSharpMakeStructFieldsWritableDiagnosticAnalyzer()
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
            private bool _hasReadonlyField = false;
            private bool _hasTypeInstanceAssigment = false;
            private SyntaxNode _typeInstanceAssigmentNode;

            public static void CreateAndRegisterActions(CompilationStartAnalysisContext compilationStartContext)
            {
                var compilationAnalyzer = new CompilationAnalyzer();
                compilationAnalyzer.RegisterActions(compilationStartContext);
            }

            private void RegisterActions(CompilationStartAnalysisContext context)
            {
                // We report diagnostic only if these requirements are met:
                // 1. The type is struct
                // 2. Struct contains at least one readonly field
                // 3. Struct contains assignment to 'this' outside the scope of constructor

                context.RegisterSymbolStartAction(symbolStartContext =>
                {
                    // We are only interested in struct declarations
                    var namedTypeSymbol = (INamedTypeSymbol)symbolStartContext.Symbol;
                    if (namedTypeSymbol.TypeKind != TypeKind.Struct) return;

                    symbolStartContext.RegisterSyntaxNodeAction(AnalyzeIfFieldIsReadonly, SyntaxKind.FieldDeclaration);
                    symbolStartContext.RegisterOperationAction(AnalyzeAssignment, OperationKind.SimpleAssignment);
                    symbolStartContext.RegisterSymbolEndAction(SymbolEndAction);
                }, SymbolKind.NamedType);
            }

            private void AnalyzeIfFieldIsReadonly(SyntaxNodeAnalysisContext nodeContext)
            {
                // We are looking for at least one 'readonly' field
                var fieldSymbol = (IFieldSymbol)nodeContext.ContainingSymbol;
                _hasReadonlyField |= fieldSymbol.IsReadOnly;
            }

            private void AnalyzeAssignment(OperationAnalysisContext operationContext)
            {
                // We are looking for assignment to 'this' outside the constructor scope
                var operationAssigmnent = (IAssignmentOperation)operationContext.Operation;
                _hasTypeInstanceAssigment = operationAssigmnent.Target is IInstanceReferenceOperation instance &&
                                            instance.ReferenceKind == InstanceReferenceKind.ContainingTypeInstance;

                if (_hasTypeInstanceAssigment)
                {
                    _typeInstanceAssigmentNode = operationAssigmnent.Syntax;
                }
            }

            private void SymbolEndAction(SymbolAnalysisContext symbolEndContext)
            {
                if (_hasTypeInstanceAssigment && _hasReadonlyField)
                {
                    var diagnostic = Diagnostic.Create(
                                    s_diagnosticDescriptor,
                                    _typeInstanceAssigmentNode.GetLocation());
                    symbolEndContext.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
