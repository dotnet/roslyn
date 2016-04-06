using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.PopulateSwitch;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.PopulateSwitch
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.PopulateSwitch), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.ImplementInterface)]
    internal class CSharpPopulateSwitchCodeFixProvider : AbstractPopulateSwitchCodeFixProvider<SwitchStatementSyntax, ExpressionSyntax, SwitchSectionSyntax>
    {
        protected override SwitchStatementSyntax GetSwitchStatementNode(SyntaxNode root, TextSpan span)
        {
            var switchExpression = (ExpressionSyntax)root.FindNode(span);
            return (SwitchStatementSyntax)switchExpression.Parent;
        }

        protected override ExpressionSyntax GetSwitchExpression(SwitchStatementSyntax switchStatement) => switchStatement.Expression;

        protected override int InsertPosition(List<SwitchSectionSyntax> sections) => sections.Count - 1;

        protected override List<SwitchSectionSyntax> GetSwitchSections(SwitchStatementSyntax switchStatement) => new List<SwitchSectionSyntax>(switchStatement.Sections);

        protected override SyntaxNode NewSwitchNode(SwitchStatementSyntax switchStatement, List<SwitchSectionSyntax> sections) => 
            switchStatement.WithSections(SyntaxFactory.List(sections));

        protected override List<ExpressionSyntax> GetCaseLabels(SwitchStatementSyntax switchStatement, out bool containsDefaultLabel)
            => CSharpPopulateSwitchHelperClass.GetCaseLabels(switchStatement, out containsDefaultLabel);
    }
}
