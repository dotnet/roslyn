using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.PopulateSwitch;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.PopulateSwitch
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpPopulateSwitchDiagnosticAnalyzer : AbstractPopulateSwitchDiagnosticAnalyzerBase<SyntaxKind, SwitchStatementSyntax>
    {
        private static readonly ImmutableArray<SyntaxKind> s_kindsOfInterest = ImmutableArray.Create(SyntaxKind.SwitchStatement);
        protected override ImmutableArray<SyntaxKind> SyntaxKindsOfInterest { get; } = s_kindsOfInterest;

        protected override SyntaxNode GetExpression(SwitchStatementSyntax switchBlock)
        {
            return switchBlock.Expression;
        }

        protected override List<SyntaxNode> GetCaseLabels(SwitchStatementSyntax switchBlock, out bool hasDefaultCase)
        {
            hasDefaultCase = false;
            
            var caseLabels = new List<SyntaxNode>();

            foreach (var section in switchBlock.Sections)
            {
                foreach (var label in section.Labels)
                {
                    if (label.IsKind(SyntaxKind.CaseSwitchLabel))
                    {
                        caseLabels.Add(((CaseSwitchLabelSyntax)label).Value);
                    }

                    if (label.IsKind(SyntaxKind.DefaultSwitchLabel))
                    {
                        hasDefaultCase = true;
                    }
                }
            }

            return caseLabels;
        }
    }
}
