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
            /// <param name="indentationSettings">The indentation settings to use.</param>
            /// <param name="indentationSteps">The number of indentation steps.</param>
            /// <returns>A string containing the amount of whitespace needed for the given indentation steps.</returns>
            public static string GenerateIndentationString(IndentationSettings indentationSettings, int indentationSteps)
            {
                string result;
                var indentationCount = indentationSteps * indentationSettings.IndentationSize;
                if (indentationSettings.UseTabs)
                {
                    var tabCount = indentationCount / indentationSettings.TabSize;
                    var spaceCount = indentationCount % indentationSettings.TabSize;
                    result = new string('\t', tabCount) + new string(' ', spaceCount);
                }
                else
                {
                    result = new string(' ', indentationCount);
                }

                return result;
            }

            /// <summary>
            /// Gets the number of steps that the given node is indented.
            /// </summary>
            /// <param name="indentationSettings">The indentation settings to use.</param>
            /// <param name="node">The node to inspect.</param>
            /// <returns>The number of steps that the node is indented.</returns>
            public static int GetIndentationSteps(IndentationSettings indentationSettings, SyntaxNode node)
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

                foreach (SyntaxTrivia trivia in leadingTrivia.Reverse())
                {
                    if (!trivia.IsKind(SyntaxKind.WhitespaceTrivia))
                    {
                        break;
                    }

                    builder.Insert(0, trivia.ToFullString());
                }

                var tabSize = indentationSettings.TabSize;
                var indentationCount = 0;
                for (var i = 0; i < builder.Length; i++)
                {
                    indentationCount += builder[i] == '\t' ? tabSize - (indentationCount % tabSize) : 1;
                }

                StringBuilderPool.ReturnAndFree(builder);

                return (indentationCount + (indentationSettings.IndentationSize / 2)) / indentationSettings.IndentationSize;
            }
        }
    }
}
