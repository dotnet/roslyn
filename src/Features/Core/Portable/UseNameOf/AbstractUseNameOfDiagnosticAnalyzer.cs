using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.UseNameof
{
    internal abstract class AbstractUseNameOfDiagnosticAnalyzer : AbstractCodeStyleDiagnosticAnalyzer
    {
        protected AbstractUseNameOfDiagnosticAnalyzer()
            : base(
                IDEDiagnosticIds.UseNameofDiagnosticId,
                "Use nameof.",
                "Use nameof.",
                configurable: false)
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory() => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        public override bool OpenFileOnly(Workspace workspace) => false;

        protected abstract ISyntaxFactsService GetSyntaxFactsService();

        protected override void InitializeWorker(AnalysisContext context)
        {
            context.RegisterOperationAction(Handle, OperationKind.Literal);
        }

        private void Handle(OperationAnalysisContext context)
        {
            if (context.Operation is ILiteralOperation literal &&
                literal.Parent is IArgumentOperation &&
                literal.Type.SpecialType == SpecialType.System_String &&
                literal.ConstantValue.HasValue &&
                literal.ConstantValue.Value is string value &&
                GetSyntaxFactsService().IsValidIdentifier(value))
            {
                var symbol = context.Compilation.GetSemanticModel(literal.Syntax.SyntaxTree)
                                    .LookupSymbols(literal.Syntax.SpanStart, name: value)
                                    .FirstOrDefault();
                switch (symbol)
                {
                    case IParameterSymbol _:
                        context.ReportDiagnostic(Diagnostic.Create(InfoDescriptor, literal.Syntax.GetLocation()));
                        break;
                    case IFieldSymbol _:
                    case IEventSymbol _:
                    case IPropertySymbol _:
                    case IMethodSymbol _:
                        if (Equals(symbol.ContainingType, ContainingSymbol()))
                        {
                            context.ReportDiagnostic(
                                Diagnostic.Create(
                                    InfoDescriptor,
                                    literal.Syntax.GetLocation(),
                                    IsInInstanceContext()
                                        ? ImmutableDictionary<string, string>.Empty.Add(nameof(SyntaxGenerator.ThisExpression), "true")
                                        : null));
                        }

                        break;
                    case ILocalSymbol local when IsVisible(local):
                        context.ReportDiagnostic(Diagnostic.Create(InfoDescriptor, literal.Syntax.GetLocation()));
                        break;
                }
            }

            ITypeSymbol ContainingSymbol()
            {
                return context.ContainingSymbol as ITypeSymbol ??
                       context.ContainingSymbol.ContainingType;
            }

            bool IsInInstanceContext()
            {
                var syntaxFacts = GetSyntaxFactsService();
                return !(syntaxFacts.IsInStaticContext(literal.Syntax) ||
                         syntaxFacts.IsInConstantContext(literal.Syntax));
            }

            bool IsVisible(ILocalSymbol local)
            {
                if (local.DeclaringSyntaxReferences.Length == 1 &&
                    local.DeclaringSyntaxReferences[0].Span.Start < literal.Syntax.SpanStart)
                {
                    var declaration = local.DeclaringSyntaxReferences[0]
                                           .GetSyntax(context.CancellationToken);
                    return !declaration.Contains(literal.Syntax);
                }

                return false;
            }
        }
    }
}
