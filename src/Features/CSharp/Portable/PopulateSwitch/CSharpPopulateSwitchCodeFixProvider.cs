using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.PopulateSwitch
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.PopulateSwitch), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.ImplementInterface)]
    internal class CSharpPopulateSwitchCodeFixProvider : AbstractPopulateSwitchCodeFixProvider<SwitchStatementSyntax>
    {
        protected override SyntaxNode GetSwitchStatementNode(SyntaxNode root, TextSpan span)
        {
            var switchExpression = (ExpressionSyntax)root.FindNode(span);
            return (SwitchStatementSyntax)switchExpression.Parent;
        }

        protected override SyntaxNode GetSwitchExpression(SwitchStatementSyntax switchStatement) => switchStatement.Expression;

        protected override int InsertPosition(List<SyntaxNode> sections) => sections.Count - 1;

        protected override List<SyntaxNode> GetSwitchSections(SwitchStatementSyntax switchStatement) => new List<SyntaxNode>(switchStatement.Sections);

        protected override SyntaxNode NewSwitchNode(SwitchStatementSyntax switchStatement, List<SyntaxNode> sections) => 
            switchStatement.WithSections(SyntaxFactory.List(sections))
                .WithAdditionalAnnotations(Formatter.Annotation, Simplifier.Annotation);

        protected override List<SyntaxNode> GetCaseLabels(SwitchStatementSyntax switchStatement, out bool containsDefaultLabel)
        {
            containsDefaultLabel = false;

            var caseLabels = new List<SyntaxNode>();
            foreach (var section in switchStatement.Sections)
            {
                foreach (var label in section.Labels)
                {
                    var caseLabel = label as CaseSwitchLabelSyntax;
                    if (caseLabel != null)
                    {
                        caseLabels.Add(caseLabel.Value);
                    }

                    if (label.IsKind(SyntaxKind.DefaultSwitchLabel))
                    {
                        containsDefaultLabel = true;
                    }
                }
            }

            return caseLabels;
        }
    }
}
