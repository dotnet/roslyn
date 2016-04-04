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
    internal class CSharpPopulateSwitchCodeFixProvider : AbstractPopulateSwitchCodeFixProvider
    {
        protected override SyntaxNode GetSwitchStatementNode(SyntaxNode root, TextSpan span)
        {
            var token = root.FindToken(span.Start);
            if (!token.Span.IntersectsWith(span))
            {
                return null;
            }
            
            var switchExpression = (ExpressionSyntax)root.FindNode(span);
            return (SwitchStatementSyntax) switchExpression.Parent;
        }

        protected override SyntaxNode GetSwitchExpression(SyntaxNode node)
        {
            var switchStatement = (SwitchStatementSyntax)node;
            return switchStatement.Expression;
        }

        protected override List<string> GetMissingLabels(SyntaxNode node, SemanticModel model, INamedTypeSymbol enumType, out bool containsDefaultLabel)
        {
            var caseLabels = GetCaseLabels((SwitchStatementSyntax)node, out containsDefaultLabel);

            var symbols = new List<ISymbol>();
            foreach (var label in caseLabels)
            {
                // these are the labels like `MyEnum.EnumMember`
                var memberAccessExpression = label as MemberAccessExpressionSyntax;
                if (memberAccessExpression != null)
                {
                    var symbol = model.GetSymbolInfo(memberAccessExpression).Symbol;
                    if (symbol != null)
                    {
                        symbols.Add(symbol);
                    }
                    continue;
                }

                // these are the labels like `EnumMember` (such as when using `using static Namespace.MyEnum;`)
                var identifierName = label as IdentifierNameSyntax;
                if (identifierName != null)
                {
                    var symbol = model.GetSymbolInfo(identifierName).Symbol;
                    if (symbol != null)
                    {
                        symbols.Add(symbol);
                    }
                }
            }

            var missingLabels = new List<string>();
            foreach (var member in enumType.GetMembers())
            {
                // don't create members like ".ctor"
                if (member.IsImplicitlyDeclared)
                {
                    continue;
                }

                var memberExists = false;
                foreach (var symbol in symbols)
                {
                    if (symbol == member)
                    {
                        memberExists = true;
                        break;
                    }
                }

                if (!memberExists)
                {
                    missingLabels.Add(member.Name);
                }
            }

            return missingLabels;
        }

        protected override int InsertPosition(List<SyntaxNode> sections)
        {
            return sections.Count - 1;
        }

        protected override List<SyntaxNode> GetSwitchSections(SyntaxNode node)
        {
            var switchBlock = (SwitchStatementSyntax)node;
            return new List<SyntaxNode>(switchBlock.Sections);
        }

        protected override SyntaxNode NewSwitchNode(SyntaxNode node, List<SyntaxNode> sections)
        {
            var switchBlock = (SwitchStatementSyntax)node;
            return switchBlock.WithSections(SyntaxFactory.List(sections))
                .WithAdditionalAnnotations(Formatter.Annotation, Simplifier.Annotation);
        }

        private List<ExpressionSyntax> GetCaseLabels(SwitchStatementSyntax switchBlock, out bool containsDefaultLabel)
        {
            containsDefaultLabel = false;

            var caseLabels = new List<ExpressionSyntax>();
            foreach (var section in switchBlock.Sections)
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
