// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.CodeAnalysis.CSharp.MisplacedUsings
{
    /// <summary>
    /// Implements a code fix for all misplaced using statements.
    /// </summary>
    internal partial class MisplacedUsingsCodeFixProvider
    {
        /// <summary>
        /// Provides helper methods to work with indentation.
        /// </summary>
        internal static class IndentationHelper
        {
            /// <summary>
            /// Generate a new indentation string.
            /// </summary>
            /// <param name="indentationSteps">The number of indentation steps.</param>
            /// <param name="useTabs">Whether tabs should be used for indentation.</param>
            /// <param name="tabSize">The width of a tab stop.</param>
            /// <param name="indentationSize">The width of an indentation level.</param>
            /// <returns>A string containing the amount of whitespace needed for the given indentation steps.</returns>
            public static string GenerateIndentationString(int indentationSteps, bool useTabs, int tabSize, int indentationSize)
            {
                var indentationCount = indentationSteps * indentationSize;
                return indentationCount.CreateIndentationString(useTabs, tabSize);
            }

            /// <summary>
            /// Gets the number of steps that the given node is indented.
            /// </summary>
            /// <param name="node">The node to inspect.</param>
            /// <param name="tabSize">The width of a tab stop.</param>
            /// <param name="indentationSize">The width of an indentation level.</param>
            /// <returns>The number of steps that the node is indented.</returns>
            public static int GetIndentationSteps(SyntaxNode node, int tabSize, int indentationSize)
            {
                var syntaxTree = node.SyntaxTree;
                var leadingTrivia = node.GetLeadingTrivia();
                var triviaSpan = syntaxTree.GetLineSpan(leadingTrivia.FullSpan);

                // There is no indentation when the leading trivia doesn't begin at the start of the line.
                if ((triviaSpan.StartLinePosition == triviaSpan.EndLinePosition) && (triviaSpan.StartLinePosition.Character > 0))
                {
                    return 0;
                }

                var builder = StringBuilderPool.Allocate();

                foreach (var trivia in leadingTrivia.Reverse())
                {
                    if (!trivia.IsKind(SyntaxKind.WhitespaceTrivia))
                    {
                        break;
                    }

                    builder.Insert(0, trivia.ToFullString());
                }

                var indentationCount = 0;
                for (var i = 0; i < builder.Length; i++)
                {
                    indentationCount += builder[i] == '\t' ? tabSize - (indentationCount % tabSize) : 1;
                }

                StringBuilderPool.ReturnAndFree(builder);

                return (indentationCount + (indentationSize / 2)) / indentationSize;
            }
        }
    }
}
