// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.Snippets;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Snippets.SnippetFunctions
{
    internal sealed class SnippetFunctionClassName : AbstractSnippetFunctionClassName
    {
        public SnippetFunctionClassName(SnippetExpansionClient snippetExpansionClient, ITextBuffer subjectBuffer, string fieldName)
            : base(snippetExpansionClient, subjectBuffer, fieldName)
        {
        }

        protected override int GetContainingClassName(Document document, SnapshotSpan fieldSpan, CancellationToken cancellationToken, ref string value, ref int hasDefaultValue)
        {
            // Find the nearest enclosing type declaration and use its name
            var syntaxTree = document.GetSyntaxTreeSynchronously(cancellationToken);
            var type = syntaxTree.FindTokenOnLeftOfPosition(fieldSpan.Start.Position, cancellationToken).GetAncestor<TypeDeclarationSyntax>();

            if (type != null)
            {
                value = type.Identifier.ToString();

                if (!string.IsNullOrWhiteSpace(value))
                {
                    hasDefaultValue = 1;
                }
            }

            return VSConstants.S_OK;
        }
    }
}
