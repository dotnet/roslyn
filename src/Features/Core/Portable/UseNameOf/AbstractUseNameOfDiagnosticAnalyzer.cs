using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.UseNameof
{
    internal abstract class AbstractUseNameOfDiagnosticAnalyzer : AbstractCodeStyleDiagnosticAnalyzer
    {
        private static readonly ImmutableDictionary<string, string> UseThisProperties = ImmutableDictionary<string, string>.Empty.Add(nameof(SyntaxGenerator.ThisExpression), "true");

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
                foreach (var symbol in context.Compilation.GetSemanticModel(literal.Syntax.SyntaxTree)
                                                .LookupSymbols(literal.Syntax.SpanStart, name: value))
                {
                    switch (symbol)
                    {
                        case IParameterSymbol _:
                            context.ReportDiagnostic(Diagnostic.Create(InfoDescriptor, literal.Syntax.GetLocation()));
                            return;
                        case IFieldSymbol _:
                        case IEventSymbol _:
                        case IPropertySymbol _:
                        case IMethodSymbol _:
                            if (Equals(symbol.ContainingType, ContainingType()))
                            {
                                context.ReportDiagnostic(
                                    Diagnostic.Create(
                                        InfoDescriptor,
                                        literal.Syntax.GetLocation(),
                                        GetProperties(symbol)));
                            }

                            return;
                        case ILocalSymbol local when IsVisible(local):
                            context.ReportDiagnostic(Diagnostic.Create(InfoDescriptor, literal.Syntax.GetLocation()));
                            return;
                    }
                }
            }

            ITypeSymbol ContainingType()
            {
                return context.ContainingSymbol as ITypeSymbol ??
                       context.ContainingSymbol.ContainingType;
            }

            ImmutableDictionary<string, string> GetProperties(ISymbol symbol)
            {
                if (symbol.IsStatic || context.ContainingSymbol.IsStatic)
                {
                    return null;
                }

                var syntaxFacts = GetSyntaxFactsService();
                if (syntaxFacts.IsInStaticContext(literal.Syntax) ||
                    syntaxFacts.IsInConstantContext(literal.Syntax))
                {
                    return null;
                }

                return UseThisProperties;
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
