using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.PopulateSwitch;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.PopulateSwitch;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.PopulateSwitch
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpPopulateSwitchDiagnosticAnalyzer : AbstractPopulateSwitchDiagnosticAnalyzerBase<SyntaxKind, SwitchStatementSyntax, ExpressionSyntax>
    {
        protected override ImmutableArray<SyntaxKind> SyntaxKindsOfInterest { get; } = ImmutableArray.Create(SyntaxKind.SwitchStatement);

        protected override ExpressionSyntax GetExpression(SwitchStatementSyntax switchBlock) => switchBlock.Expression;

        protected override List<ExpressionSyntax> GetCaseLabels(SwitchStatementSyntax switchBlock, out bool hasDefaultCase)
            => CSharpPopulateSwitchHelpers.GetCaseLabels(switchBlock, out hasDefaultCase);
    }
}
