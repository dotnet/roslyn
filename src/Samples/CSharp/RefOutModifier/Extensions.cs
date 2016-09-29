// *********************************************************
//
// Copyright © Microsoft Corporation
//
// Licensed under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in
// compliance with the License. You may obtain a copy of
// the License at
//
// http://www.apache.org/licenses/LICENSE-2.0 
//
// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES
// OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED,
// INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES
// OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY OR NON-INFRINGEMENT.
//
// See the Apache 2 License for the specific language
// governing permissions and limitations under the License.
//
// *********************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Roslyn.Samples.CodeAction.AddOrRemoveRefOutModifier
{
    // Miscellaneous helper extension methods over many different classes
    internal static class Extensions
    {
        public static bool IsEmpty<T>(this IEnumerable<T> list)
        {
            // if the thing in the list is null, it still means empty
            var first = list.FirstOrDefault();
            return first == null;
        }

        public static async Task<SyntaxTree> GetCSharpSyntaxTreeAsync(this Document document, CancellationToken token)
        {
            return (SyntaxTree)await document.GetSyntaxTreeAsync(token).ConfigureAwait(false);
        }

        public static bool OnArgumentOrParameter(this SyntaxTree tree, int position)
        {
            var token = tree.GetRoot().FindToken(position);
            if (token.Kind() == SyntaxKind.None)
            {
                return false;
            }

            var argument = token.AncestorAndSelf<ArgumentSyntax>();
            if (argument != null && argument.Span.IntersectsWith(position))
            {
                return true;
            }

            var parameter = token.AncestorAndSelf<ParameterSyntax>();
            if (parameter != null && parameter.Span.IntersectsWith(position))
            {
                return true;
            }

            return false;
        }

        public static bool OnArgumentOrParameterWithoutRefOut(this SyntaxTree tree, int position)
        {
            var token = tree.GetRoot().FindToken(position);
            if (token.Kind() == SyntaxKind.None)
            {
                return false;
            }

            var argument = token.AncestorAndSelf<ArgumentSyntax>();
            if (argument != null && argument.Span.IntersectsWith(position))
            {
                if (argument.RefOrOutKeyword.Kind() != SyntaxKind.None)
                {
                    return false;
                }

                return true;
            }

            var parameter = token.AncestorAndSelf<ParameterSyntax>();
            if (parameter != null && parameter.Span.IntersectsWith(position))
            {
                if (parameter.Modifiers.Any(m => m.Kind() == SyntaxKind.OutKeyword || m.Kind() == SyntaxKind.RefKeyword))
                {
                    return false;
                }

                return true;
            }

            return false;
        }

        public static bool OnSpecificToken(this SyntaxTree tree, SyntaxKind tokenKind, int position)
        {
            var token = tree.GetRoot().FindToken(position);
            if (!token.IsKind(tokenKind))
            {
                return false;
            }

            if (!token.Span.IntersectsWith(position))
            {
                return false;
            }

            // token must belong to either argument or parameter
            if (!token.Parent.IsKind(SyntaxKind.Argument) &&
                !token.Parent.IsKind(SyntaxKind.Parameter))
            {
                return false;
            }

            return true;
        }

        public static T AncestorAndSelf<T>(this SyntaxToken token) where T : SyntaxNode
        {
            return token.Parent.AncestorAndSelf<T>();
        }

        public static T AncestorAndSelf<T>(this SyntaxNode node) where T : SyntaxNode
        {
            return node.AncestorsAndSelf().FirstOrDefault(n => n is T) as T;
        }

        public static SyntaxToken FindToken(this Location location)
        {
            return ((SyntaxTree)location.SourceTree).GetRoot().FindToken(location.SourceSpan.Start);
        }

        public static IEnumerable<Document> GetContainingDocuments(this Project project, IEnumerable<SyntaxNode> nodes, CancellationToken cancellationToken)
        {
            return nodes.Where(n => n.SyntaxTree != null).Select(n => project.GetDocument(n.SyntaxTree)).Distinct();
        }

        public static SyntaxTokenList Add(this SyntaxTokenList list, SyntaxToken token)
        {
            var tokens = new List<SyntaxToken>(list);
            tokens.Add(token);

            return SyntaxFactory.TokenList(tokens);
        }

        public static SyntaxToken MergeTrailingTrivia(this SyntaxToken token, SyntaxToken tokenToRemove)
        {
            // this has a bug where if tokenToRemove is the first token on line, trivia should be
            // attached to leading trivia of next token, not trailing trivia of previous token
            var trivia = new List<SyntaxTrivia>(token.TrailingTrivia);
            trivia.AddRange(tokenToRemove.LeadingTrivia);
            trivia.AddRange(tokenToRemove.TrailingTrivia);

            return token.WithTrailingTrivia(SyntaxFactory.TriviaList(trivia));
        }
    }
}
