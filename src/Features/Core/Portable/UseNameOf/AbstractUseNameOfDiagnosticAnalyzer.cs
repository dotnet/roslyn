namespace Microsoft.CodeAnalysis.UseNameof
{
    using System.Threading;
    using Microsoft.CodeAnalysis.CodeStyle;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Microsoft.CodeAnalysis.LanguageServices;
    using Microsoft.CodeAnalysis.Operations;

    internal abstract class AbstractUseNameOfDiagnosticAnalyzer : AbstractCodeStyleDiagnosticAnalyzer
    {
        protected AbstractUseNameOfDiagnosticAnalyzer()
            : base(
                IDEDiagnosticIds.UseNameofDiagnosticId,
                "Use nameof.",
                "Use nameof.",
                false)
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory() => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        public override bool OpenFileOnly(Workspace workspace) => false;

        protected abstract ISyntaxFactsService GetSyntaxFactsService();

        protected override void InitializeWorker(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.RegisterOperationAction(Handle, OperationKind.Literal);
        }

        private void Handle(OperationAnalysisContext context)
        {
            if (context.Operation is ILiteralOperation literal &&
                literal.Parent is IArgumentOperation &&
                literal.Type.SpecialType == SpecialType.System_String &&
                literal.ConstantValue.HasValue &&
                literal.ConstantValue.Value is string value &&
                this.GetSyntaxFactsService().IsValidIdentifier(value))
            {
                var semanticModel = context.Compilation.GetSemanticModel(literal.Syntax.SyntaxTree);
                foreach (var symbol in semanticModel.LookupSymbols(literal.Syntax.SpanStart, name: value))
                {
                    switch (symbol)
                    {
                        case IParameterSymbol _:
                            context.ReportDiagnostic(Diagnostic.Create(this.InfoDescriptor, literal.Syntax.GetLocation()));
                            break;
                        case IFieldSymbol _:
                        case IEventSymbol _:
                        case IPropertySymbol _:
                        case IMethodSymbol _:
                            if (context.ContainingSymbol.ContainingType == symbol.ContainingType)
                            {
                                context.ReportDiagnostic(Diagnostic.Create(this.InfoDescriptor, literal.Syntax.GetLocation()));
                            }

                            break;
                        case ILocalSymbol local when IsVisible(literal, local, context.CancellationToken):
                            context.ReportDiagnostic(Diagnostic.Create(this.InfoDescriptor, literal.Syntax.GetLocation()));
                            break;
                    }
                }
            }
        }

        private static bool IsVisible(ILiteralOperation literal, ILocalSymbol local, CancellationToken cancellationToken)
        {
            if (local.DeclaringSyntaxReferences.Length == 1 &&
                local.DeclaringSyntaxReferences[0].Span.Start < literal.Syntax.SpanStart)
            {
                var declaration = local.DeclaringSyntaxReferences[0]
                                       .GetSyntax(cancellationToken);
                return !declaration.Contains(literal.Syntax);
            }

            return false;
        }
    }
}
