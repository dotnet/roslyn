// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    using Green = InternalSyntax;

    internal static class SyntaxEquivalence
    {
        internal static bool AreEquivalent(SyntaxTree? before, SyntaxTree? after, Func<SyntaxKind, bool>? ignoreChildNode, bool topLevel)
        {
            if (before == after)
            {
                return true;
            }

            if (before == null || after == null)
            {
                return false;
            }

            return AreEquivalent(before.GetRoot(), after.GetRoot(), ignoreChildNode, topLevel);
        }

        public static bool AreEquivalent(SyntaxNode? before, SyntaxNode? after, Func<SyntaxKind, bool>? ignoreChildNode, bool topLevel)
        {
            Debug.Assert(!topLevel || ignoreChildNode == null);

            if (before == null || after == null)
            {
                return before == after;
            }

            return AreEquivalentRecursive(before.Green, after.Green, ignoreChildNode, topLevel: topLevel);
        }

        public static bool AreEquivalent(SyntaxTokenList before, SyntaxTokenList after)
        {
            return AreEquivalentRecursive(before.Node, after.Node, ignoreChildNode: null, topLevel: false);
        }

        public static bool AreEquivalent(SyntaxToken before, SyntaxToken after)
        {
            return before.RawKind == after.RawKind && (before.Node == null || AreTokensEquivalent(before.Node, after.Node, ignoreChildNode: null));
        }

        private static bool AreTokensEquivalent(GreenNode? before, GreenNode? after, Func<SyntaxKind, bool>? ignoreChildNode)
        {
            if (before is null || after is null)
            {
                return (before is null && after is null);
            }

            // NOTE(cyrusn): Do we want to drill into trivia?  Can documentation ever affect the
            // global meaning of symbols?  This can happen in java with things like the "@obsolete"
            // clause in doc comment.  However, i don't know if anything like that exists in C#. 

            // NOTE(cyrusn): I don't believe we need to examine skipped text.  It isn't relevant from
            // the perspective of global symbolic information.
            Debug.Assert(before.RawKind == after.RawKind);

            if (before.IsMissing != after.IsMissing)
            {
                return false;
            }

            // These are the tokens that don't have fixed text.
            switch ((SyntaxKind)before.RawKind)
            {
                case SyntaxKind.IdentifierToken:
                    if (((Green.SyntaxToken)before).ValueText != ((Green.SyntaxToken)after).ValueText)
                    {
                        return false;
                    }
                    break;

                case SyntaxKind.NumericLiteralToken:
                case SyntaxKind.CharacterLiteralToken:
                case SyntaxKind.StringLiteralToken:
                case SyntaxKind.SingleLineRawStringLiteralToken:
                case SyntaxKind.MultiLineRawStringLiteralToken:
                case SyntaxKind.InterpolatedStringTextToken:
                    if (((Green.SyntaxToken)before).Text != ((Green.SyntaxToken)after).Text)
                    {
                        return false;
                    }
                    break;
            }

            return AreNullableDirectivesEquivalent(before, after, ignoreChildNode);
        }

        private static bool AreEquivalentRecursive(GreenNode? before, GreenNode? after, Func<SyntaxKind, bool>? ignoreChildNode, bool topLevel)
        {
            if (before == after)
            {
                return true;
            }

            if (before == null || after == null)
            {
                return false;
            }

            if (before.RawKind != after.RawKind)
            {
                return false;
            }

            if (before.IsToken)
            {
                Debug.Assert(after.IsToken);
                return AreTokensEquivalent(before, after, ignoreChildNode);
            }

            if (topLevel)
            {
                // Once we get down to the body level we don't need to go any further and we can
                // consider these trees equivalent.
                switch ((SyntaxKind)before.RawKind)
                {
                    case SyntaxKind.Block:
                    case SyntaxKind.ArrowExpressionClause:
                        return AreNullableDirectivesEquivalent(before, after, ignoreChildNode);
                }

                // If we're only checking top level equivalence, then we don't have to go down into
                // the initializer for a field. However, we can't put that optimization for all
                // fields. For example, fields that are 'const' do need their initializers checked as
                // changing them can affect binding results.
                if ((SyntaxKind)before.RawKind == SyntaxKind.FieldDeclaration)
                {
                    var fieldBefore = (Green.FieldDeclarationSyntax)before;
                    var fieldAfter = (Green.FieldDeclarationSyntax)after;

                    var isConstBefore = fieldBefore.Modifiers.Any((int)SyntaxKind.ConstKeyword);
                    var isConstAfter = fieldAfter.Modifiers.Any((int)SyntaxKind.ConstKeyword);

                    if (!isConstBefore && !isConstAfter)
                    {
                        ignoreChildNode = childKind => childKind == SyntaxKind.EqualsValueClause;
                    }
                }

                // NOTE(cyrusn): Do we want to avoid going down into attribute expressions?  I don't
                // think we can avoid it as there are likely places in the compiler that use these
                // expressions.  For example, if the user changes [InternalsVisibleTo("goo")] to
                // [InternalsVisibleTo("bar")] then that must count as a top level change as it
                // affects symbol visibility.  Perhaps we could enumerate the places in the compiler
                // that use the values inside source attributes and we can check if we're in an
                // attribute with that name.  It wouldn't be 100% correct (because of annoying things
                // like using aliases), but would likely be good enough for the incremental cases in
                // the IDE.
            }

            if (ignoreChildNode != null)
            {
                var e1 = before.ChildNodesAndTokens().GetEnumerator();
                var e2 = after.ChildNodesAndTokens().GetEnumerator();
                while (true)
                {
                    GreenNode? child1 = null;
                    GreenNode? child2 = null;

                    // skip ignored children:
                    while (e1.MoveNext())
                    {
                        var c = e1.Current;
                        if (c != null && (c.IsToken || !ignoreChildNode((SyntaxKind)c.RawKind)))
                        {
                            child1 = c;
                            break;
                        }
                    }

                    while (e2.MoveNext())
                    {
                        var c = e2.Current;
                        if (c != null && (c.IsToken || !ignoreChildNode((SyntaxKind)c.RawKind)))
                        {
                            child2 = c;
                            break;
                        }
                    }

                    if (child1 == null || child2 == null)
                    {
                        // false if some children remained
                        return child1 == child2;
                    }

                    if (!AreEquivalentRecursive(child1, child2, ignoreChildNode, topLevel))
                    {
                        return false;
                    }
                }
            }
            else
            {
                // simple comparison - not ignoring children

                int slotCount = before.SlotCount;
                if (slotCount != after.SlotCount)
                {
                    return false;
                }

                for (int i = 0; i < slotCount; i++)
                {
                    var child1 = before.GetSlot(i);
                    var child2 = after.GetSlot(i);

                    if (!AreEquivalentRecursive(child1, child2, ignoreChildNode, topLevel))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        private static bool AreNullableDirectivesEquivalent(GreenNode before, GreenNode after, Func<SyntaxKind, bool>? ignoreChildNode)
        {
            // Fast path for when the caller does not care about nullable directives. This can happen in some IDE refactorings.
            if (ignoreChildNode is object && ignoreChildNode(SyntaxKind.NullableDirectiveTrivia))
            {
                return true;
            }

            using var beforeDirectivesEnumerator = ((Green.CSharpSyntaxNode)before).GetDirectives().GetEnumerator();
            using var afterDirectivesEnumerator = ((Green.CSharpSyntaxNode)after).GetDirectives().GetEnumerator();
            while (true)
            {
                Green.DirectiveTriviaSyntax? beforeAnnotation = getNextNullableDirective(beforeDirectivesEnumerator, ignoreChildNode);
                Green.DirectiveTriviaSyntax? afterAnnotation = getNextNullableDirective(afterDirectivesEnumerator, ignoreChildNode);

                if (beforeAnnotation == null || afterAnnotation == null)
                {
                    return beforeAnnotation == afterAnnotation;
                }

                if (!AreEquivalentRecursive(beforeAnnotation, afterAnnotation, ignoreChildNode, topLevel: false))
                {
                    return false;
                }

                static Green.DirectiveTriviaSyntax? getNextNullableDirective(IEnumerator<Green.DirectiveTriviaSyntax> enumerator, Func<SyntaxKind, bool>? ignoreChildNode)
                {
                    while (enumerator.MoveNext())
                    {
                        var current = enumerator.Current;
                        if (current.Kind == SyntaxKind.NullableDirectiveTrivia)
                        {
                            return current;
                        }
                    }

                    return null;
                }
            }
        }
    }
}
