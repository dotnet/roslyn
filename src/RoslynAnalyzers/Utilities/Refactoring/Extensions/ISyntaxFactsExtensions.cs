// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Analyzer.Utilities.Extensions
{
    internal static class ISyntaxFactsExtensions
    {
        public static TextSpan GetSpanWithoutAttributes(this ISyntaxFacts syntaxFacts, SyntaxNode root, SyntaxNode node)
        {
            // Span without AttributeLists
            // - No AttributeLists -> original .Span
            // - Some AttributeLists -> (first non-trivia/comment Token.Span.Begin, original.Span.End)
            //   - We need to be mindful about comments due to:
            //      // [Test1]
            //      //Comment1
            //      [||]object Property1 { get; set; }
            //     the comment node being part of the next token's (`object`) leading trivia and not the AttributeList's node.
            // - In case only attribute is written we need to be careful to not to use next (unrelated) token as beginning current the node.
            var attributeList = syntaxFacts.GetAttributeLists(node);
            if (attributeList.Any())
            {
                var endOfAttributeLists = attributeList.Last().Span.End;
                var afterAttributesToken = root.FindTokenOnRightOfPosition(endOfAttributeLists);

                var endOfNode = node.Span.End;
                var startOfNodeWithoutAttributes = Math.Min(afterAttributesToken.Span.Start, endOfNode);

                return TextSpan.FromBounds(startOfNodeWithoutAttributes, endOfNode);
            }

            return node.Span;
        }

        /// <summary>
        /// Checks if the position is on the header of a type (from the start of the type up through it's name).
        /// </summary>
        public static bool IsOnTypeHeader(this ISyntaxFacts syntaxFacts, SyntaxNode root, int position, [NotNullWhen(true)] out SyntaxNode? typeDeclaration)
            => syntaxFacts.IsOnTypeHeader(root, position, fullHeader: false, out typeDeclaration);

        public static bool IsExpressionStatement(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
            => node?.RawKind == syntaxFacts.SyntaxKinds.ExpressionStatement;

        public static bool IsLocalDeclarationStatement(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
            => node?.RawKind == syntaxFacts.SyntaxKinds.LocalDeclarationStatement;

        public static bool IsVariableDeclarator(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
            => node?.RawKind == syntaxFacts.SyntaxKinds.VariableDeclarator;
    }
}
