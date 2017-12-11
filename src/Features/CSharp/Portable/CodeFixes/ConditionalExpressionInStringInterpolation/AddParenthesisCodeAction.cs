// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.ConditionalExpressionInStringInterpolation
{
    internal partial class CSharpAddParenthesisAroundConditionalExpressionInInterpolatedStringCodeFixProvider
    {
        private const string CS1026 = "CS1026"; // ) expected

        private static async Task<Document> GetChangedDocumentAsync(Document document, int conditionalExpressionSyntaxStartPosition, CancellationToken cancellationToken)
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var openParenthesisPosition = conditionalExpressionSyntaxStartPosition;
            var textWithOpenParenthesis = text.Replace(openParenthesisPosition, 0, "(");
            var documentWithOpenParenthesis = document.WithText(textWithOpenParenthesis);
            var syntaxTree = await documentWithOpenParenthesis.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var syntaxRoot = await syntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
            var conditionalExpressionSyntaxNode = syntaxRoot.FindNode(new TextSpan(openParenthesisPosition, 0));
            var diagnostics = syntaxTree.GetDiagnostics(conditionalExpressionSyntaxNode);
            var cs1026 = diagnostics.FirstOrDefault(d => d.Id == CS1026);
            if (cs1026 != null)
            {
                var closeParenthesisPosition = cs1026.Location.SourceSpan.Start;
                var textWithBothParenthesis = textWithOpenParenthesis.Replace(closeParenthesisPosition, 0, ")");
                return documentWithOpenParenthesis.WithText(textWithBothParenthesis);
            }

            return documentWithOpenParenthesis;
        }
    }
}
