
using System.Collections.Immutable;
using System.Linq;
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
            isUnneccessary: false);

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
                => SymbolAnalyzer.CreateAndRegisterActions(compilationStartContext));
        }

        private sealed class SymbolAnalyzer
        {
            private bool _hasTypeInstanceAssigment = false;
            private INamedTypeSymbol _namedTypeSymbol;
            private SymbolAnalyzer(INamedTypeSymbol namedTypeSymbol)
            {
                _namedTypeSymbol = namedTypeSymbol;
            }

            public static void CreateAndRegisterActions(CompilationStartAnalysisContext compilationStartContext)
            {
                compilationStartContext.RegisterSymbolStartAction(symbolStartContext =>
                {
                    // We report diagnostic only if these requirements are met:
                    // 1. The type is struct
                    // 2. Struct contains at least one 'readonly' field
                    // 3. Struct contains assignment to 'this' outside the scope of constructor
                    var namedTypeSymbol = (INamedTypeSymbol)symbolStartContext.Symbol;

                    // We are only interested in struct declarations
                    if (namedTypeSymbol.TypeKind != TypeKind.Struct) return;
                    //We check if struct contains any 'readonly' fields
                    if (!HasReadonlyField(namedTypeSymbol)) return;

                    var symbolAnalyzer = new SymbolAnalyzer(namedTypeSymbol);
                    symbolAnalyzer.RegisterActions(symbolStartContext);

                }, SymbolKind.NamedType);
            }

            private static bool HasReadonlyField(INamedTypeSymbol namedTypeSymbol)
            {
                return namedTypeSymbol
                .GetMembers()
                .OfType<IFieldSymbol>()
                .Where(field => field.AssociatedSymbol == null)
                .Any(field => field.IsReadOnly);
            }

            private void RegisterActions(SymbolStartAnalysisContext symbolStartContext)
            {
                symbolStartContext.RegisterOperationAction(AnalyzeAssignment, OperationKind.SimpleAssignment);
                symbolStartContext.RegisterSymbolEndAction(SymbolEndAction);
            }

            private void AnalyzeAssignment(OperationAnalysisContext operationContext)
            {
                // We are looking for assignment to 'this' outside the constructor scope
                var operationAssigmnent = (IAssignmentOperation)operationContext.Operation;
                _hasTypeInstanceAssigment = operationAssigmnent.Target is IInstanceReferenceOperation instance &&
                                            instance.ReferenceKind == InstanceReferenceKind.ContainingTypeInstance;
            }

            private void SymbolEndAction(SymbolAnalysisContext symbolEndContext)
            {
                if (_hasTypeInstanceAssigment)
                {
                    var diagnostic = Diagnostic.Create(
                                    s_diagnosticDescriptor,
                                    _namedTypeSymbol.Locations[0]);
                    symbolEndContext.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
