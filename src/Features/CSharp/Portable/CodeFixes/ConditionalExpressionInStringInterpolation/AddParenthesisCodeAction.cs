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
        private const string CS1026 = nameof(CS1026); // ) expected

        private static async Task<Document> GetChangedDocumentAsync(Document document, int conditionalExpressionSyntaxStartPosition, CancellationToken cancellationToken)
        {
            // The usual SyntaxTree transformations are complicated if string literals are present in the false part as in
            // $"{ condition ? "Success": "Failure" }"
            // The colon starts a FormatClause and the double quote left to 'F' therefore ends the interpolated string.
            // The text starting with 'F' is parsed as code and the resulting syntax tree is impractical.
            // The same problem arises if a } is present in the false part.
            // To circumvent these problems this solution
            // 1. Inserts an opening parenthesis
            // 2. Re-parses the resulting document (now the colon isn't treated as starting a FormatClause anymore)
            // 3. Looks for CS1026: ) expected
            // 4. Inserts a closing parenthesis at CS1026
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var openParenthesisPosition = conditionalExpressionSyntaxStartPosition;
            var textWithOpenParenthesis = text.Replace(openParenthesisPosition, 0, "(");
            var documentWithOpenParenthesis = document.WithText(textWithOpenParenthesis);
            var syntaxTree = await documentWithOpenParenthesis.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var syntaxRoot = await syntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
            var conditionalExpression = syntaxRoot.FindNode(new TextSpan(openParenthesisPosition, 0));
            var diagnostics = syntaxTree.GetDiagnostics(conditionalExpression);
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
