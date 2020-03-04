// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static partial class SyntaxTreeExtensions
    {
        public static bool IsPrimaryFunctionExpressionContext(this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition)
        {
            return
                syntaxTree.IsTypeOfExpressionContext(position, tokenOnLeftOfPosition) ||
                syntaxTree.IsDefaultExpressionContext(position, tokenOnLeftOfPosition) ||
                syntaxTree.IsSizeOfExpressionContext(position, tokenOnLeftOfPosition);
        }

        public static bool IsInNonUserCode(this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
        {
            return
                syntaxTree.IsEntirelyWithinNonUserCodeComment(position, cancellationToken) ||
                syntaxTree.IsEntirelyWithinConflictMarker(position, cancellationToken) ||
                syntaxTree.IsEntirelyWithinStringOrCharLiteral(position, cancellationToken) ||
                syntaxTree.IsInInactiveRegion(position, cancellationToken);
        }

        public static bool IsInInactiveRegion(
            this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(syntaxTree);

            // cases:
            // $ is EOF

            // #if false
            //    |

            // #if false
            //    |$

            // #if false
            // |

            // #if false
            // |$

            if (syntaxTree.IsPreProcessorKeywordContext(position, cancellationToken))
            {
                return false;
            }

            // The latter two are the hard cases we don't actually have an 
            // DisabledTextTrivia yet. 
            var trivia = syntaxTree.GetRoot(cancellationToken).FindTrivia(position, findInsideTrivia: false);
            if (trivia.Kind() == SyntaxKind.DisabledTextTrivia)
            {
                return true;
            }

            var token = syntaxTree.FindTokenOrEndToken(position, cancellationToken);
            if (token.Kind() == SyntaxKind.EndOfFileToken)
            {
                var triviaList = token.LeadingTrivia;
                foreach (var triviaTok in triviaList.Reverse())
                {
                    if (triviaTok.Span.Contains(position))
                    {
                        return false;
                    }

                    if (triviaTok.Span.End < position)
                    {
                        if (!triviaTok.HasStructure)
                        {
                            return false;
                        }

                        var structure = triviaTok.GetStructure();
                        if (structure is BranchingDirectiveTriviaSyntax branch)
                        {
                            return !branch.IsActive || !branch.BranchTaken;
                        }
                    }
                }
            }

            return false;
        }

        public static async Task<ImmutableArray<SyntaxNode>> GetSelectedFieldsAndPropertiesAsync(
            this SyntaxTree tree, TextSpan textSpan, bool allowPartialSelection, CancellationToken cancellationToken)
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
            var firstMember = token.GetAncestors<MemberDeclarationSyntax>().FirstOrDefault();
            if (firstMember != null)
            {
                if (firstMember.Parent is TypeDeclarationSyntax containingType)
                    return GetFieldsAndPropertiesInSpan(root, text, textSpan, containingType, firstMember, allowPartialSelection);
            }

            return ImmutableArray<SyntaxNode>.Empty;
        }

        private static ImmutableArray<SyntaxNode> GetFieldsAndPropertiesInSpan(
            SyntaxNode root,
            SourceText text,
            TextSpan textSpan,
            TypeDeclarationSyntax containingType,
            MemberDeclarationSyntax firstMember,
            bool allowPartialSelection)
        {
            var members = containingType.Members;
            var fieldIndex = members.IndexOf(firstMember);
            if (fieldIndex < 0)
                return ImmutableArray<SyntaxNode>.Empty;

            var selectedMembers = ArrayBuilder<SyntaxNode>.GetInstance();
            for (var i = fieldIndex; i < members.Count; i++)
            {
                var member = members[i];
                AddSelectedFieldOrPropertyDeclarations(member);
            }

            return selectedMembers.ToImmutableAndFree();

            void AddAllMembers(MemberDeclarationSyntax member)
            {
                switch (member)
                {
                    case FieldDeclarationSyntax field:
                        selectedMembers.AddRange(field.Declaration.Variables);
                        return;
                    case PropertyDeclarationSyntax property:
                        selectedMembers.Add(property);
                        return;
                }
            }

            // local functions
            void AddSelectedFieldOrPropertyDeclarations(MemberDeclarationSyntax member)
            {
                if (!member.IsKind(SyntaxKind.FieldDeclaration, SyntaxKind.PropertyDeclaration))
                    return;

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
                    //  2. Position touches the name of the field/prop.
                    //  3. Position is after the last token of the member (on the same line).

                    var position = textSpan.Start;
                    if (text.IsBeforeOrAfterNodeOnSameLine(root, member, position))
                    {
                        AddAllMembers(member);
                        return;
                    }
                    else
                    {
                        switch (member)
                        {
                            case FieldDeclarationSyntax field:
                                foreach (var varDecl in field.Declaration.Variables)
                                {
                                    if (varDecl.Identifier.FullSpan.IntersectsWith(position))
                                        selectedMembers.Add(varDecl);
                                }

                                return;
                            case PropertyDeclarationSyntax property:
                                if (property.Identifier.FullSpan.IntersectsWith(position))
                                    selectedMembers.Add(property);

                                return;
                        }
                    }
                }
                else
                {
                    // if the user has an actual selection, get the fields/props if the selection
                    // surrounds the names of in the case of allowPartialSelection.

                    if (!allowPartialSelection)
                        return;

                    switch (member)
                    {
                        case FieldDeclarationSyntax field:
                            var variables = field.Declaration.Variables;
                            foreach (var variable in variables)
                            {
                                if (textSpan.OverlapsWith(variable.Identifier.Span))
                                    selectedMembers.Add(variable);
                            }

                            return;
                        case PropertyDeclarationSyntax property:
                            selectedMembers.Add(property);
                            return;
                    }
                }
            }
        }

        public static bool IsInPartiallyWrittenGeneric(
            this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
        {
            return syntaxTree.IsInPartiallyWrittenGeneric(position, cancellationToken, out var genericIdentifier, out var lessThanToken);
        }

        public static bool IsInPartiallyWrittenGeneric(
            this SyntaxTree syntaxTree,
            int position,
            CancellationToken cancellationToken,
            out SyntaxToken genericIdentifier)
        {
            return syntaxTree.IsInPartiallyWrittenGeneric(position, cancellationToken, out genericIdentifier, out var lessThanToken);
        }

        public static bool IsInPartiallyWrittenGeneric(
            this SyntaxTree syntaxTree,
            int position,
            CancellationToken cancellationToken,
            out SyntaxToken genericIdentifier,
            out SyntaxToken lessThanToken)
        {
            genericIdentifier = default;
            lessThanToken = default;
            var index = 0;

            var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
            if (token.Kind() == SyntaxKind.None)
            {
                return false;
            }

            // check whether we are under type or member decl
            if (token.GetAncestor<TypeParameterListSyntax>() != null)
            {
                return false;
            }

            var stack = 0;
            while (true)
            {
                switch (token.Kind())
                {
                    case SyntaxKind.LessThanToken:
                        if (stack == 0)
                        {
                            // got here so we read successfully up to a < now we have to read the
                            // name before that and we're done!
                            lessThanToken = token;
                            token = token.GetPreviousToken(includeSkipped: true);
                            if (token.Kind() == SyntaxKind.None)
                            {
                                return false;
                            }

                            // ok
                            // so we've read something like:
                            // ~~~~~~~~~<a,b,...
                            // but we need to know the simple name that precedes the <
                            // it could be
                            // ~~~~~~goo<a,b,...
                            if (token.Kind() == SyntaxKind.IdentifierToken)
                            {
                                // okay now check whether it is actually partially written
                                if (IsFullyWrittenGeneric(token, lessThanToken))
                                {
                                    return false;
                                }

                                genericIdentifier = token;
                                return true;
                            }

                            return false;
                        }
                        else
                        {
                            stack--;
                            break;
                        }

                    case SyntaxKind.GreaterThanGreaterThanToken:
                        stack++;
                        goto case SyntaxKind.GreaterThanToken;

                    // fall through
                    case SyntaxKind.GreaterThanToken:
                        stack++;
                        break;

                    case SyntaxKind.AsteriskToken:      // for int*
                    case SyntaxKind.QuestionToken:      // for int?
                    case SyntaxKind.ColonToken:         // for global::  (so we don't dismiss help as you type the first :)
                    case SyntaxKind.ColonColonToken:    // for global::
                    case SyntaxKind.CloseBracketToken:
                    case SyntaxKind.OpenBracketToken:
                    case SyntaxKind.DotToken:
                    case SyntaxKind.IdentifierToken:
                        break;

                    case SyntaxKind.CommaToken:
                        if (stack == 0)
                        {
                            index++;
                        }

                        break;

                    default:
                        // user might have typed "in" on the way to typing "int"
                        // don't want to disregard this genericname because of that
                        if (SyntaxFacts.IsKeywordKind(token.Kind()))
                        {
                            break;
                        }

                        // anything else and we're sunk.
                        return false;
                }

                // look backward one token, include skipped tokens, because the parser frequently
                // does skip them in cases like: "Func<A, B", which get parsed as: expression
                // statement "Func<A" with missing semicolon, expression statement "B" with missing
                // semicolon, and the "," is skipped.
                token = token.GetPreviousToken(includeSkipped: true);
                if (token.Kind() == SyntaxKind.None)
                {
                    return false;
                }
            }
        }

        private static bool IsFullyWrittenGeneric(SyntaxToken token, SyntaxToken lessThanToken)
        {
            return token.Parent is GenericNameSyntax genericName && genericName.TypeArgumentList != null &&
                   genericName.TypeArgumentList.LessThanToken == lessThanToken && !genericName.TypeArgumentList.GreaterThanToken.IsMissing;
        }
    }
}
