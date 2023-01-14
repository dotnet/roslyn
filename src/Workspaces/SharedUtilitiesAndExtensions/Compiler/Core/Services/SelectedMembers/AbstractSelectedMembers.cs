// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.LanguageService
{
    internal abstract class AbstractSelectedMembers<
        TMemberDeclarationSyntax,
        TFieldDeclarationSyntax,
        TPropertyDeclarationSyntax,
        TTypeDeclarationSyntax,
        TVariableSyntax>
        where TMemberDeclarationSyntax : SyntaxNode
        where TFieldDeclarationSyntax : TMemberDeclarationSyntax
        where TPropertyDeclarationSyntax : TMemberDeclarationSyntax
        where TTypeDeclarationSyntax : TMemberDeclarationSyntax
        where TVariableSyntax : SyntaxNode
    {
        protected abstract SyntaxList<TMemberDeclarationSyntax> GetMembers(TTypeDeclarationSyntax containingType);
        protected abstract ImmutableArray<(SyntaxNode declarator, SyntaxToken identifier)> GetDeclaratorsAndIdentifiers(TMemberDeclarationSyntax member);

        public Task<ImmutableArray<SyntaxNode>> GetSelectedFieldsAndPropertiesAsync(
            SyntaxTree tree, TextSpan textSpan, bool allowPartialSelection, CancellationToken cancellationToken)
                => GetSelectedMembersAsync(tree, textSpan, allowPartialSelection, IsFieldOrProperty, cancellationToken);

        public Task<ImmutableArray<SyntaxNode>> GetSelectedMembersAsync(
            SyntaxTree tree, TextSpan textSpan, bool allowPartialSelection, CancellationToken cancellationToken)
                => GetSelectedMembersAsync(tree, textSpan, allowPartialSelection, static _ => true, cancellationToken);

        private async Task<ImmutableArray<SyntaxNode>> GetSelectedMembersAsync(
            SyntaxTree tree, TextSpan textSpan, bool allowPartialSelection,
            Func<TMemberDeclarationSyntax, bool> membersToKeep, CancellationToken cancellationToken)
        {
            var text = await tree.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);

            // If there is a selection, look for the token to the right of the selection That helps
            // the user select like so:
            //
            //          int i;[|
            //          int j;|]
            //
            // In this case (which is common with a mouse), we want to consider 'j' selected, and
            // 'i' not involved in all.
            //
            // However, if there is no selection and the user has:
            //
            //          int i;$$
            //          int j;
            //
            // Then we want to consider 'i' selected instead.  So we do a normal FindToken.

            var token = textSpan.IsEmpty
                ? root.FindToken(textSpan.Start)
                : root.FindTokenOnRightOfPosition(textSpan.Start);
            var firstMember = token.GetAncestors<TMemberDeclarationSyntax>()
                                   .Where(m => m.Parent is TTypeDeclarationSyntax)
                                   .FirstOrDefault();
            if (firstMember == null)
                return ImmutableArray<SyntaxNode>.Empty;

            return GetMembersInSpan(root, text, textSpan, firstMember, allowPartialSelection, membersToKeep);
        }

        private ImmutableArray<SyntaxNode> GetMembersInSpan(
            SyntaxNode root, SourceText text, TextSpan textSpan,
            TMemberDeclarationSyntax firstMember, bool allowPartialSelection,
            Func<TMemberDeclarationSyntax, bool> membersToKeep)
        {
            var containingType = (TTypeDeclarationSyntax)firstMember.Parent;
            var members = GetMembers(containingType);
            var fieldIndex = members.IndexOf(firstMember);
            if (fieldIndex < 0)
                return ImmutableArray<SyntaxNode>.Empty;

            using var _ = ArrayBuilder<SyntaxNode>.GetInstance(out var selectedMembers);
            for (var i = fieldIndex; i < members.Count; i++)
            {
                var member = members[i];
                AddSelectedMemberDeclarations(member, membersToKeep);
            }

            return selectedMembers.ToImmutable();

            void AddAllMembers(TMemberDeclarationSyntax member)
            {
                selectedMembers.AddRange(GetDeclaratorsAndIdentifiers(member).Select(pair => pair.declarator));
            }

            // local functions
            void AddSelectedMemberDeclarations(TMemberDeclarationSyntax member, Func<TMemberDeclarationSyntax, bool> membersToKeep)
            {
                if (!membersToKeep(member))
                {
                    return;
                }

                // first, check if entire member is selected.  If so, we definitely include this member.
                if (textSpan.Contains(member.Span))
                {
                    AddAllMembers(member);
                    return;
                }

                if (textSpan.IsEmpty)
                {
                    // No selection.  We consider this member selected if a few cases are true:
                    //
                    //  1. Position precedes the first token of the member (on the same line).
                    //  2. Position touches the name of the member.
                    //  3. Position touches an immediate child token of the member (on the same line)
                    //  4. Position is after the last token of the member (on the same line).

                    var position = textSpan.Start;
                    if (IsBeforeOrAfterNodeOnSameLine(text, root, member, position))
                    {
                        AddAllMembers(member);
                        return;
                    }
                    else
                    {
                        foreach (var (decl, id) in GetDeclaratorsAndIdentifiers(member))
                        {
                            if (id.FullSpan.IntersectsWith(position))
                            {
                                selectedMembers.Add(decl);
                                return;
                            }
                        }
                    }
                }
                else
                {
                    // if the user has an actual selection, get the fields/props if the selection
                    // surrounds the names of in the case of allowPartialSelection. Selecting other keywords
                    // should not be considered member selection if the name is not also selected

                    if (!allowPartialSelection)
                        return;

                    foreach (var (decl, id) in GetDeclaratorsAndIdentifiers(member))
                    {
                        if (textSpan.OverlapsWith(id.Span))
                        {
                            selectedMembers.Add(decl);
                        }
                    }
                }
            }
        }

        private static bool IsBeforeOrAfterNodeOnSameLine(
            SourceText text, SyntaxNode root, SyntaxNode member, int position)
        {
            var token = root.FindToken(position);
            if (token == member.GetFirstToken() &&
                position <= token.SpanStart &&
                text.AreOnSameLine(position, token.SpanStart))
            {
                return true;
            }

            if (token == member.GetLastToken() &&
                position >= token.Span.End &&
                text.AreOnSameLine(position, token.Span.End))
            {
                return true;
            }

            return false;
        }

        private static bool IsFieldOrProperty(TMemberDeclarationSyntax member)
            => member is TFieldDeclarationSyntax or TPropertyDeclarationSyntax;
    }
}
