// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Implementation.Highlighting;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.Highlighting.KeywordHighlighters
{
    [ExportHighlighter(LanguageNames.CSharp)]
    internal class DiscardParameterHighlighter : AbstractKeywordHighlighter<ArgumentSyntax>
    {
        [ImportingConstructor]
        public DiscardParameterHighlighter()
        {
        }

        protected override IEnumerable<TextSpan> GetHighlights(ArgumentSyntax node, CancellationToken cancellationToken)
        {
            if (node.Expression.IsKind(SyntaxKind.IdentifierName))
            {
                var syntax = (IdentifierNameSyntax)node.Expression;

                if(syntax.Identifier.Text == "_")
                {
                    return ImmutableArray.Create(syntax.Identifier.Span);
                }
            }

            if (node.Expression.IsKind(SyntaxKind.DeclarationExpression))
            {
                var syntax = (DeclarationExpressionSyntax)node.Expression;

                if (syntax.Designation.IsKind(SyntaxKind.DiscardDesignation))
                {
                    return ImmutableArray.Create(syntax.Designation.Span);
                }
            }

            return SpecializedCollections.EmptyEnumerable<TextSpan>();
        }
    }
}
