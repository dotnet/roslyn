// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.Snippets;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Snippets.SnippetFunctions
{
    internal sealed class SnippetFunctionClassName : AbstractSnippetFunctionClassName
    {
        public SnippetFunctionClassName(SnippetExpansionClient snippetExpansionClient, ITextView textView, ITextBuffer subjectBuffer, string fieldName)
            : base(snippetExpansionClient, textView, subjectBuffer, fieldName)
        {
        }

        protected override void GetContainingClassName(Document document, SnapshotSpan fieldSpan, CancellationToken cancellationToken, ref string value, ref bool hasDefaultValue)
        {
            // Find the nearest enclosing type declaration and use its name
            var syntaxTree = document.GetSyntaxTreeSynchronously(cancellationToken);
            var type = syntaxTree.FindTokenOnLeftOfPosition(fieldSpan.Start.Position, cancellationToken).GetAncestor<TypeDeclarationSyntax>();

            if (type != null)
            {
                value = type.Identifier.ToString();

                if (!string.IsNullOrWhiteSpace(value))
                {
                    hasDefaultValue = true;
                }
            }
        }
    }
}
