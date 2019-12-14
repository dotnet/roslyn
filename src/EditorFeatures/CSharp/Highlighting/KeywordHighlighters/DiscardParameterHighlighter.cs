using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Implementation.Highlighting;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.Highlighting.KeywordHighlighters
{
    [ExportHighlighter(LanguageNames.CSharp)]
    internal class DiscardParameterHighlighter : AbstractKeywordHighlighter
    {
        [ImportingConstructor]
        public DiscardParameterHighlighter()
        {
        }

        protected override bool IsHighlightableNode(SyntaxNode node)
        {
            if (!node.IsKind(CodeAnalysis.CSharp.SyntaxKind.Argument) || !(node is ArgumentSyntax syntax))
            {
                return false;
            }

            if (!syntax.RefOrOutKeyword.IsKind(SyntaxKind.OutKeyword))
            {
                return false;
            }

            if (syntax.Expression.IsKind(SyntaxKind.IdentifierName) && syntax.Expression is IdentifierNameSyntax name)
            {
                return name.Identifier.Text == "_";
            }

            if (syntax.Expression.IsKind(SyntaxKind.DeclarationExpression) && syntax.Expression is DeclarationExpressionSyntax declaration)
            {
                return declaration.Designation.IsKind(SyntaxKind.DiscardDesignation);
            }

            return false;
        }

        protected override IEnumerable<TextSpan> GetHighlightsForNode(SyntaxNode node, CancellationToken cancellationToken)
        {
            var argument = (ArgumentSyntax)node;

            if (argument.Expression.IsKind(SyntaxKind.IdentifierName))
            {
                return ImmutableArray.Create(((IdentifierNameSyntax)argument.Expression).Identifier.Span);
            }

            if (argument.Expression.IsKind(SyntaxKind.DeclarationExpression))
            {
                return ImmutableArray.Create(((DeclarationExpressionSyntax)argument.Expression).Designation.Span);
            }


            return SpecializedCollections.EmptyEnumerable<TextSpan>();
        }
    }
}
