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
            if (node.Expression is IdentifierNameSyntax nameSyntax
                && nameSyntax.Identifier.Text == "_")
            {
                return ImmutableArray.Create(nameSyntax.Identifier.Span);
            }

            if (node.Expression is DeclarationExpressionSyntax declarationSyntax
                && declarationSyntax.Designation.IsKind(SyntaxKind.DiscardDesignation))
            {
                return ImmutableArray.Create(declarationSyntax.Designation.Span);
            }

            return SpecializedCollections.EmptyEnumerable<TextSpan>();
        }
    }
}
