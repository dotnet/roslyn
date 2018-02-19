// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.Snippets;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Snippets.SnippetFunctions
{
    internal sealed class SnippetFunctionClassName : AbstractSnippetFunctionClassName
    {
        public SnippetFunctionClassName(SnippetExpansionClient snippetExpansionClient, ITextView textView, ITextBuffer subjectBuffer, string fieldName)
            : base(snippetExpansionClient, textView, subjectBuffer, fieldName)
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
