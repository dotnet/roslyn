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
        private sealed class AddParenthesisCodeAction : CodeAction
        {
            public AddParenthesisCodeAction(Document document, int conditionalExpressionSyntaxStartPosition)
            {
                this.Document = document;
                this.ConditionalExpressionSyntaxStartPosition = conditionalExpressionSyntaxStartPosition;
            }

            private Document Document { get; }
            private int ConditionalExpressionSyntaxStartPosition { get; }

            public override string Title => CSharpFeaturesResources.AddParenthesisAroundConditionalExpressionInInterpolatedString;

            protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                var text = await Document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var openParenthesisPosition = ConditionalExpressionSyntaxStartPosition;
                var textWithOpenParenthesis = text.Replace(openParenthesisPosition, 0, "(");
                var documentWithOpenParenthesis = Document.WithText(textWithOpenParenthesis);
                var syntaxTree = await documentWithOpenParenthesis.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                var syntaxRoot = await syntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
                var conditionalExpressionSyntaxNode = syntaxRoot.FindNode(new TextSpan(openParenthesisPosition, 0));
                var diagnostics = syntaxTree.GetDiagnostics(conditionalExpressionSyntaxNode);
                var cs1026 = diagnostics.FirstOrDefault(d => d.Id == "CS1026");
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
}
