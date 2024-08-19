// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal sealed class SyntaxNavigator
    {
        private const int None = 0;

        public static readonly SyntaxNavigator Instance = new SyntaxNavigator();

        private SyntaxNavigator()
        {
        }

        [Flags]
        private enum SyntaxKinds
        {
            DocComments = 1,
            Directives = 2,
            SkippedTokens = 4,
        }

        private static readonly Func<SyntaxTrivia, bool>?[] s_stepIntoFunctions = new Func<SyntaxTrivia, bool>?[]
        {
            /* 000 */ null,
            /* 001 */ t =>                                             t.IsDocumentationCommentTrivia,
            /* 010 */ t =>                            t.IsDirective,
            /* 011 */ t =>                            t.IsDirective || t.IsDocumentationCommentTrivia,
            /* 100 */ t => t.IsSkippedTokensTrivia,
            /* 101 */ t => t.IsSkippedTokensTrivia                  || t.IsDocumentationCommentTrivia,
            /* 110 */ t => t.IsSkippedTokensTrivia || t.IsDirective,
            /* 111 */ t => t.IsSkippedTokensTrivia || t.IsDirective || t.IsDocumentationCommentTrivia,
        };

        private static Func<SyntaxTrivia, bool>? GetStepIntoFunction(
            bool skipped, bool directives, bool docComments)
        {
            var index = (skipped ? SyntaxKinds.SkippedTokens : 0) |
                        (directives ? SyntaxKinds.Directives : 0) |
                        (docComments ? SyntaxKinds.DocComments : 0);
            return s_stepIntoFunctions[(int)index];
        }

        private static Func<SyntaxToken, bool> GetPredicateFunction(bool includeZeroWidth)
        {
            return includeZeroWidth ? SyntaxToken.Any : SyntaxToken.NonZeroWidth;
        }

        private static bool Matches(Func<SyntaxToken, bool>? predicate, SyntaxToken token)
        {
            return predicate == null || ReferenceEquals(predicate, SyntaxToken.Any) || predicate(token);
        }

        internal SyntaxToken GetFirstToken(in SyntaxNode current, bool includeZeroWidth, bool includeSkipped, bool includeDirectives, bool includeDocumentationComments)
        {
            return GetFirstToken(current, GetPredicateFunction(includeZeroWidth), GetStepIntoFunction(includeSkipped, includeDirectives, includeDocumentationComments));
        }

        internal SyntaxToken GetLastToken(in SyntaxNode current, bool includeZeroWidth, bool includeSkipped, bool includeDirectives, bool includeDocumentationComments)
        {
            return GetLastToken(current, GetPredicateFunction(includeZeroWidth), GetStepIntoFunction(includeSkipped, includeDirectives, includeDocumentationComments));
        }

        internal SyntaxToken GetPreviousToken(in SyntaxToken current, bool includeZeroWidth, bool includeSkipped, bool includeDirectives, bool includeDocumentationComments)
        {
            return GetPreviousToken(current, GetPredicateFunction(includeZeroWidth), GetStepIntoFunction(includeSkipped, includeDirectives, includeDocumentationComments));
        }

        internal SyntaxToken GetNextToken(in SyntaxToken current, bool includeZeroWidth, bool includeSkipped, bool includeDirectives, bool includeDocumentationComments)
        {
            return GetNextToken(current, GetPredicateFunction(includeZeroWidth), GetStepIntoFunction(includeSkipped, includeDirectives, includeDocumentationComments));
        }

        internal SyntaxToken GetPreviousToken(in SyntaxToken current, Func<SyntaxToken, bool> predicate, Func<SyntaxTrivia, bool>? stepInto)
        {
            return GetPreviousToken(current, predicate, stepInto != null, stepInto);
        }

        internal SyntaxToken GetNextToken(in SyntaxToken current, Func<SyntaxToken, bool> predicate, Func<SyntaxTrivia, bool>? stepInto)
        {
            return GetNextToken(current, predicate, stepInto != null, stepInto);
        }

        private static readonly ObjectPool<Stack<ChildSyntaxList.Enumerator>> s_childEnumeratorStackPool
            = new ObjectPool<Stack<ChildSyntaxList.Enumerator>>(() => new Stack<ChildSyntaxList.Enumerator>(), 10);

        internal SyntaxToken GetFirstToken(SyntaxNode current, Func<SyntaxToken, bool>? predicate, Func<SyntaxTrivia, bool>? stepInto)
        {
            var stack = s_childEnumeratorStackPool.Allocate();
            try
            {
                stack.Push(current.ChildNodesAndTokens().GetEnumerator());

                while (stack.Count > 0)
                {
                    // only pop enumerator off the stack if it is actually done
                    var en = stack.Peek();

                    if (en.MoveNext())
                    {
                        var child = en.Current;

                        if (child.IsToken)
                        {
                            var token = GetFirstToken(child.AsToken(), predicate, stepInto);
                            if (token.RawKind != None)
                            {
                                // done, pop enumerator off stack
                                stack.Pop();
                                return token;
                            }
                        }

                        if (child.IsNode)
                        {
                            Debug.Assert(child.IsNode);
                            stack.Push(child.AsNode()!.ChildNodesAndTokens().GetEnumerator());
                        }
                    }
                }

                return default;
            }
            finally
            {
                stack.Clear();
                s_childEnumeratorStackPool.Free(stack);
            }
        }

        private static readonly ObjectPool<Stack<ChildSyntaxList.Reversed.Enumerator>> s_childReversedEnumeratorStackPool
            = new ObjectPool<Stack<ChildSyntaxList.Reversed.Enumerator>>(() => new Stack<ChildSyntaxList.Reversed.Enumerator>(), 10);

        internal SyntaxToken GetLastToken(SyntaxNode current, Func<SyntaxToken, bool> predicate, Func<SyntaxTrivia, bool>? stepInto)
        {
            var stack = s_childReversedEnumeratorStackPool.Allocate();
            try
            {
                stack.Push(current.ChildNodesAndTokens().Reverse().GetEnumerator());

                while (stack.Count > 0)
                {
                    // only pop enumerator off the stack if it is actually done
                    var en = stack.Peek();

                    if (en.MoveNext())
                    {
                        var child = en.Current;

                        if (child.IsToken)
                        {
                            var token = GetLastToken(child.AsToken(), predicate, stepInto);
                            if (token.RawKind != None)
                            {
                                // done, pop enumerator off stack
                                stack.Pop();
                                return token;
                            }
                        }

                        if (child.IsNode)
                        {
                            Debug.Assert(child.IsNode);
                            stack.Push(child.AsNode()!.ChildNodesAndTokens().Reverse().GetEnumerator());
                        }
                    }
                }

                return default;
            }
            finally
            {
                stack.Clear();
                s_childReversedEnumeratorStackPool.Free(stack);
            }
        }

        private SyntaxToken GetFirstToken(
            SyntaxTriviaList triviaList,
            Func<SyntaxToken, bool>? predicate,
            Func<SyntaxTrivia, bool> stepInto)
        {
            Debug.Assert(stepInto != null);
            foreach (var trivia in triviaList)
            {
                if (trivia.TryGetStructure(out var structure) && stepInto(trivia))
                {
                    var token = GetFirstToken(structure, predicate, stepInto);
                    if (token.RawKind != None)
                    {
                        return token;
                    }
                }
            }

            return default;
        }

        private SyntaxToken GetLastToken(
            SyntaxTriviaList list,
            Func<SyntaxToken, bool> predicate,
            Func<SyntaxTrivia, bool> stepInto)
        {
            Debug.Assert(stepInto != null);

            foreach (var trivia in list.Reverse())
            {
                SyntaxToken token;
                if (TryGetLastTokenForStructuredTrivia(trivia, predicate, stepInto, out token))
                {
                    return token;
                }
            }

            return default;
        }

        private bool TryGetLastTokenForStructuredTrivia(
            SyntaxTrivia trivia,
            Func<SyntaxToken, bool> predicate,
            Func<SyntaxTrivia, bool>? stepInto,
            out SyntaxToken token)
        {
            token = default;

            if (!trivia.TryGetStructure(out var structure) || stepInto == null || !stepInto(trivia))
            {
                return false;
            }

            token = GetLastToken(structure, predicate, stepInto);

            return token.RawKind != None;
        }

        private SyntaxToken GetFirstToken(
            SyntaxToken token,
            Func<SyntaxToken, bool>? predicate,
            Func<SyntaxTrivia, bool>? stepInto)
        {
            // find first token that matches (either specified token or token inside related trivia)
            if (stepInto != null)
            {
                // search in leading trivia
                var firstToken = GetFirstToken(token.LeadingTrivia, predicate, stepInto);
                if (firstToken.RawKind != None)
                {
                    return firstToken;
                }
            }

            if (Matches(predicate, token))
            {
                return token;
            }

            if (stepInto != null)
            {
                // search in trailing trivia
                var firstToken = GetFirstToken(token.TrailingTrivia, predicate, stepInto);
                if (firstToken.RawKind != None)
                {
                    return firstToken;
                }
            }

            return default;
        }

        private SyntaxToken GetLastToken(
            SyntaxToken token,
            Func<SyntaxToken, bool> predicate,
            Func<SyntaxTrivia, bool>? stepInto)
        {
            // find first token that matches (either specified token or token inside related trivia)
            if (stepInto != null)
            {
                // search in leading trivia
                var lastToken = GetLastToken(token.TrailingTrivia, predicate, stepInto);
                if (lastToken.RawKind != None)
                {
                    return lastToken;
                }
            }

            if (Matches(predicate, token))
            {
                return token;
            }

            if (stepInto != null)
            {
                // search in trailing trivia
                var lastToken = GetLastToken(token.LeadingTrivia, predicate, stepInto);
                if (lastToken.RawKind != None)
                {
                    return lastToken;
                }
            }

            return default;
        }

        internal SyntaxToken GetNextToken(
            SyntaxTrivia current,
            Func<SyntaxToken, bool>? predicate,
            Func<SyntaxTrivia, bool>? stepInto)
        {
            bool returnNext = false;

            // look inside leading trivia for current & next
            var token = GetNextToken(current, current.Token.LeadingTrivia, predicate, stepInto, ref returnNext);
            if (token.RawKind != None)
            {
                return token;
            }

            // consider containing token if current trivia was in the leading trivia
            if (returnNext && (predicate == null || predicate == SyntaxToken.Any || predicate(current.Token)))
            {
                return current.Token;
            }

            // look inside trailing trivia for current & next (or just next)
            token = GetNextToken(current, current.Token.TrailingTrivia, predicate, stepInto, ref returnNext);
            if (token.RawKind != None)
            {
                return token;
            }

            // did not find next inside trivia, try next sibling token 
            // (don't look in trailing trivia of token since it was already searched above)
            return GetNextToken(current.Token, predicate, false, stepInto);
        }

        internal SyntaxToken GetPreviousToken(
            SyntaxTrivia current,
            Func<SyntaxToken, bool> predicate,
            Func<SyntaxTrivia, bool>? stepInto)
        {
            bool returnPrevious = false;

            // look inside leading trivia for current & next
            var token = GetPreviousToken(current, current.Token.TrailingTrivia, predicate, stepInto, ref returnPrevious);
            if (token.RawKind != None)
            {
                return token;
            }

            // consider containing token if current trivia was in the leading trivia
            if (returnPrevious && Matches(predicate, current.Token))
            {
                return current.Token;
            }

            // look inside trailing trivia for current & next (or just next)
            token = GetPreviousToken(current, current.Token.LeadingTrivia, predicate, stepInto, ref returnPrevious);
            if (token.RawKind != None)
            {
                return token;
            }

            // did not find next inside trivia, try next sibling token 
            // (don't look in trailing trivia of token since it was already searched above)
            return GetPreviousToken(current.Token, predicate, false, stepInto);
        }

        private SyntaxToken GetNextToken(
            SyntaxTrivia current,
            SyntaxTriviaList list,
            Func<SyntaxToken, bool>? predicate,
            Func<SyntaxTrivia, bool>? stepInto,
            ref bool returnNext)
        {
            foreach (var trivia in list)
            {
                if (returnNext)
                {
                    if (trivia.TryGetStructure(out var structure) && stepInto != null && stepInto(trivia))
                    {
                        var token = GetFirstToken(structure!, predicate, stepInto);
                        if (token.RawKind != None)
                        {
                            return token;
                        }
                    }
                }
                else if (trivia == current)
                {
                    returnNext = true;
                }
            }

            return default;
        }

        private SyntaxToken GetPreviousToken(
            SyntaxTrivia current,
            SyntaxTriviaList list,
            Func<SyntaxToken, bool> predicate,
            Func<SyntaxTrivia, bool>? stepInto,
            ref bool returnPrevious)
        {
            foreach (var trivia in list.Reverse())
            {
                if (returnPrevious)
                {
                    SyntaxToken token;
                    if (TryGetLastTokenForStructuredTrivia(trivia, predicate, stepInto, out token))
                    {
                        return token;
                    }
                }
                else if (trivia == current)
                {
                    returnPrevious = true;
                }
            }

            return default;
        }

        internal SyntaxToken GetNextToken(
            SyntaxNode node,
            Func<SyntaxToken, bool>? predicate,
            Func<SyntaxTrivia, bool>? stepInto)
        {
            while (node.Parent != null)
            {
                // walk forward in parent's child list until we find ourselves and then return the
                // next token
                bool returnNext = false;
                foreach (var child in node.Parent.ChildNodesAndTokens())
                {
                    if (returnNext)
                    {
                        if (child.IsToken)
                        {
                            var token = GetFirstToken(child.AsToken(), predicate, stepInto);
                            if (token.RawKind != None)
                            {
                                return token;
                            }
                        }
                        else
                        {
                            Debug.Assert(child.IsNode);
                            var token = GetFirstToken(child.AsNode()!, predicate, stepInto);
                            if (token.RawKind != None)
                            {
                                return token;
                            }
                        }
                    }
                    else if (child.IsNode && child.AsNode() == node)
                    {
                        returnNext = true;
                    }
                }

                // didn't find the next token in my parent's children, look up the tree
                node = node.Parent;
            }

            if (node.IsStructuredTrivia)
            {
                return GetNextToken(((IStructuredTriviaSyntax)node).ParentTrivia, predicate, stepInto);
            }

            return default;
        }

        internal SyntaxToken GetPreviousToken(
            SyntaxNode node,
            Func<SyntaxToken, bool> predicate,
            Func<SyntaxTrivia, bool>? stepInto)
        {
            while (node.Parent != null)
            {
                // walk forward in parent's child list until we find ourselves and then return the
                // previous token
                bool returnPrevious = false;
                foreach (var child in node.Parent.ChildNodesAndTokens().Reverse())
                {
                    if (returnPrevious)
                    {
                        if (child.IsToken)
                        {
                            var token = GetLastToken(child.AsToken(), predicate, stepInto);
                            if (token.RawKind != None)
                            {
                                return token;
                            }
                        }
                        else
                        {
                            Debug.Assert(child.IsNode);
                            var token = GetLastToken(child.AsNode()!, predicate, stepInto);
                            if (token.RawKind != None)
                            {
                                return token;
                            }
                        }
                    }
                    else if (child.IsNode && child.AsNode() == node)
                    {
                        returnPrevious = true;
                    }
                }

                // didn't find the previous token in my parent's children, look up the tree
                node = node.Parent;
            }

            if (node.IsStructuredTrivia)
            {
                return GetPreviousToken(((IStructuredTriviaSyntax)node).ParentTrivia, predicate, stepInto);
            }

            return default;
        }

        internal SyntaxToken GetNextToken(in SyntaxToken current, Func<SyntaxToken, bool>? predicate, bool searchInsideCurrentTokenTrailingTrivia, Func<SyntaxTrivia, bool>? stepInto)
        {
            Debug.Assert(searchInsideCurrentTokenTrailingTrivia == false || stepInto != null);
            if (current.Parent != null)
            {
                // look inside trailing trivia for structure
                if (searchInsideCurrentTokenTrailingTrivia)
                {
                    var firstToken = GetFirstToken(current.TrailingTrivia, predicate, stepInto!);
                    if (firstToken.RawKind != None)
                    {
                        return firstToken;
                    }
                }

                // walk forward in parent's child list until we find ourself 
                // and then return the next token
                bool returnNext = false;
                foreach (var child in current.Parent.ChildNodesAndTokens())
                {
                    if (returnNext)
                    {
                        if (child.IsToken)
                        {
                            var token = GetFirstToken(child.AsToken(), predicate, stepInto);
                            if (token.RawKind != None)
                            {
                                return token;
                            }
                        }
                        else
                        {
                            Debug.Assert(child.IsNode);
                            var token = GetFirstToken(child.AsNode()!, predicate, stepInto);
                            if (token.RawKind != None)
                            {
                                return token;
                            }
                        }
                    }
                    else if (child.IsToken && child.AsToken() == current)
                    {
                        returnNext = true;
                    }
                }

                // otherwise get next token from the parent's parent, and so on
                return GetNextToken(current.Parent, predicate, stepInto);
            }

            return default;
        }

        internal SyntaxToken GetPreviousToken(in SyntaxToken current, Func<SyntaxToken, bool> predicate, bool searchInsideCurrentTokenLeadingTrivia,
            Func<SyntaxTrivia, bool>? stepInto)
        {
            Debug.Assert(searchInsideCurrentTokenLeadingTrivia == false || stepInto != null);
            if (current.Parent != null)
            {
                // look inside trailing trivia for structure
                if (searchInsideCurrentTokenLeadingTrivia)
                {
                    var lastToken = GetLastToken(current.LeadingTrivia, predicate, stepInto!);
                    if (lastToken.RawKind != None)
                    {
                        return lastToken;
                    }
                }

                // walk forward in parent's child list until we find ourself 
                // and then return the next token
                bool returnPrevious = false;
                foreach (var child in current.Parent.ChildNodesAndTokens().Reverse())
                {
                    if (returnPrevious)
                    {
                        if (child.IsToken)
                        {
                            var token = GetLastToken(child.AsToken(), predicate, stepInto);
                            if (token.RawKind != None)
                            {
                                return token;
                            }
                        }
                        else
                        {
                            Debug.Assert(child.IsNode);
                            var token = GetLastToken(child.AsNode()!, predicate, stepInto);
                            if (token.RawKind != None)
                            {
                                return token;
                            }
                        }
                    }
                    else if (child.IsToken && child.AsToken() == current)
                    {
                        returnPrevious = true;
                    }
                }

                // otherwise get next token from the parent's parent, and so on
                return GetPreviousToken(current.Parent, predicate, stepInto);
            }

            return default;
        }
    }
}
