// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;

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
            /// <param name="options">The settings to use.</param>
            /// <param name="indentationSteps">The number of indentation steps.</param>
            /// <returns>A string containing the amount of whitespace needed for the given indentation steps.</returns>
            public static string GenerateIndentationString(OptionSet options, int indentationSteps)
            {
                var tabSize = options.GetOption(FormattingOptions.TabSize, LanguageNames.CSharp);
                var indentationSize = options.GetOption(FormattingOptions.IndentationSize, LanguageNames.CSharp);
                var useTabs = options.GetOption(FormattingOptions.UseTabs, LanguageNames.CSharp);

                string result;

                var indentationCount = indentationSteps * indentationSize;
                if (useTabs)
                {
                    var tabCount = indentationCount / tabSize;
                    var spaceCount = indentationCount % tabSize;
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
            /// <param name="options">The settings to use.</param>
            /// <param name="node">The node to inspect.</param>
            /// <returns>The number of steps that the node is indented.</returns>
            public static int GetIndentationSteps(OptionSet options, SyntaxNode node)
            {
                var tabSize = options.GetOption(FormattingOptions.TabSize, LanguageNames.CSharp);
                var indentationSize = options.GetOption(FormattingOptions.IndentationSize, LanguageNames.CSharp);

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
