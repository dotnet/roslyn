using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.PopulateSwitch;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.PopulateSwitch;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.PopulateSwitch
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpPopulateSwitchDiagnosticAnalyzer : AbstractPopulateSwitchDiagnosticAnalyzerBase<SyntaxKind, SwitchStatementSyntax>
    {
        protected override ImmutableArray<SyntaxKind> SyntaxKindsOfInterest { get; } = ImmutableArray.Create(SyntaxKind.SwitchStatement);

        protected override SyntaxNode GetExpression(SwitchStatementSyntax switchBlock)
        {
            return switchBlock.Expression;
        }

        protected override List<SyntaxNode> GetCaseLabels(SwitchStatementSyntax switchBlock, out bool hasDefaultCase)
            => CSharpPopulateSwitchHelperClass.GetCaseLabels(switchBlock, out hasDefaultCase);
    }
}
