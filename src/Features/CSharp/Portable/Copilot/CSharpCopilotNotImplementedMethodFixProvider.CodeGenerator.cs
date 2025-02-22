// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.Copilot;

internal sealed partial class CSharpCopilotNotImplementedMethodFixProvider
{
    private static class CodeGenerator
    {
        public static void GenerateCode(SyntaxEditor editor, StatementSyntax throwStatement, string copilotSuggestedCodeBlock)
        {
            // Use it to replace the throw statement
            if (!string.IsNullOrWhiteSpace(copilotSuggestedCodeBlock))
            {
                try
                {
                    // Get the base indentation from the throw statement
                    var baseIndentation = throwStatement.GetLeadingTrivia()
                        .Where(t => t.IsKind(SyntaxKind.WhitespaceTrivia))
                        .LastOrDefault()
                        .ToString() ?? "    ";

                    // Parse the text as a code block to preserve comments
                    var blockText = $"{{\n{copilotSuggestedCodeBlock}\n}}";
                    var parsedBlock = SyntaxFactory.ParseStatement(blockText) as BlockSyntax;
                    if (parsedBlock == null)
                    {
                        return;
                    }

                    // Get the statements with their original trivia
                    var statements = parsedBlock.Statements
                        .Select(s => s.WithLeadingTrivia(
                            s.GetLeadingTrivia()
                                .Select(t => t.IsKind(SyntaxKind.WhitespaceTrivia)
                                    ? SyntaxFactory.Whitespace(baseIndentation)
                                    : t))
                            .WithAdditionalAnnotations(Formatter.Annotation, Simplifier.Annotation));

                    // Replace the throw statement with the properly formatted statements
                    editor.ReplaceNode(throwStatement, (node, generator) => statements);
                }
                catch (Exception)
                {
                    // Handle any exceptions that occur during the replacement
                }
            }
        }
    }
}
