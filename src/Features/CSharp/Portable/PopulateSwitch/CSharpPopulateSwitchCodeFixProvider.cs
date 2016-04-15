﻿using System.Collections.Generic;
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
        protected override ExpressionSyntax GetSwitchExpression(SwitchStatementSyntax switchStatement) => switchStatement.Expression;

        protected override int InsertPosition(SyntaxList<SwitchSectionSyntax> sections) => sections.Count - 1;

        protected override SyntaxList<SwitchSectionSyntax> GetSwitchSections(SwitchStatementSyntax switchStatement)
            => switchStatement.Sections;

        protected override SwitchStatementSyntax NewSwitchNode(SwitchStatementSyntax switchStatement, SyntaxList<SwitchSectionSyntax> sections) => 
            switchStatement.WithSections(SyntaxFactory.List(sections));

        protected override List<ExpressionSyntax> GetCaseLabels(SwitchStatementSyntax switchStatement, out bool containsDefaultLabel)
            => CSharpPopulateSwitchHelpers.GetCaseLabels(switchStatement, out containsDefaultLabel);
    }
}
