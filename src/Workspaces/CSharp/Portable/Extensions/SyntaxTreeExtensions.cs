// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
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
        public static ISet<SyntaxKind> GetPrecedingModifiers(
            this SyntaxTree syntaxTree,
            int position,
            SyntaxToken tokenOnLeftOfPosition)
            => syntaxTree.GetPrecedingModifiers(position, tokenOnLeftOfPosition, out var _);

        public static ISet<SyntaxKind> GetPrecedingModifiers(
            this SyntaxTree syntaxTree,
            int position,
            SyntaxToken tokenOnLeftOfPosition,
            out int positionBeforeModifiers)
        {
            var token = tokenOnLeftOfPosition;
            token = token.GetPreviousTokenIfTouchingWord(position);

            var result = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer);
            while (true)
            {
                switch (token.Kind())
                {
                    case SyntaxKind.PublicKeyword:
                    case SyntaxKind.InternalKeyword:
                    case SyntaxKind.ProtectedKeyword:
                    case SyntaxKind.PrivateKeyword:
                    case SyntaxKind.SealedKeyword:
                    case SyntaxKind.AbstractKeyword:
                    case SyntaxKind.StaticKeyword:
                    case SyntaxKind.VirtualKeyword:
                    case SyntaxKind.ExternKeyword:
                    case SyntaxKind.NewKeyword:
                    case SyntaxKind.OverrideKeyword:
                    case SyntaxKind.ReadOnlyKeyword:
                    case SyntaxKind.VolatileKeyword:
                    case SyntaxKind.UnsafeKeyword:
                    case SyntaxKind.AsyncKeyword:
                    case SyntaxKind.RefKeyword:
                        result.Add(token.Kind());
                        token = token.GetPreviousToken(includeSkipped: true);
                        continue;
                    case SyntaxKind.IdentifierToken:
                        if (token.HasMatchingText(SyntaxKind.AsyncKeyword))
                        {
                            result.Add(SyntaxKind.AsyncKeyword);
                            token = token.GetPreviousToken(includeSkipped: true);
                            continue;
                        }

                        break;
                }

                break;
            }

            positionBeforeModifiers = token.FullSpan.End;
            return result;
        }

        public static TypeDeclarationSyntax GetContainingTypeDeclaration(
            this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
        {
            return syntaxTree.GetContainingTypeDeclarations(position, cancellationToken).FirstOrDefault();
        }

        public static BaseTypeDeclarationSyntax GetContainingTypeOrEnumDeclaration(
            this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
        {
            return syntaxTree.GetContainingTypeOrEnumDeclarations(position, cancellationToken).FirstOrDefault();
        }

        public static IEnumerable<TypeDeclarationSyntax> GetContainingTypeDeclarations(
            this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
        {
            var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);

            return token.GetAncestors<TypeDeclarationSyntax>().Where(t =>
            {
                return BaseTypeDeclarationContainsPosition(t, position);
            });
        }

        private static bool BaseTypeDeclarationContainsPosition(BaseTypeDeclarationSyntax declaration, int position)
        {
            if (position <= declaration.OpenBraceToken.SpanStart)
            {
                return false;
            }

            if (declaration.CloseBraceToken.IsMissing)
            {
                return true;
            }

            return position <= declaration.CloseBraceToken.SpanStart;
        }

        public static IEnumerable<BaseTypeDeclarationSyntax> GetContainingTypeOrEnumDeclarations(
            this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
        {
            var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);

            return token.GetAncestors<BaseTypeDeclarationSyntax>().Where(t => BaseTypeDeclarationContainsPosition(t, position));
        }

        private static readonly Func<SyntaxKind, bool> s_isDotOrArrow = k => k == SyntaxKind.DotToken || k == SyntaxKind.MinusGreaterThanToken;
        private static readonly Func<SyntaxKind, bool> s_isDotOrArrowOrColonColon =
            k => k == SyntaxKind.DotToken || k == SyntaxKind.MinusGreaterThanToken || k == SyntaxKind.ColonColonToken;

        public static bool IsRightOfDotOrArrowOrColonColon(this SyntaxTree syntaxTree, int position, SyntaxToken targetToken, CancellationToken cancellationToken)
        {
            return
                (targetToken.IsKind(SyntaxKind.DotDotToken) && position == targetToken.SpanStart + 1) ||
                syntaxTree.IsRightOf(position, s_isDotOrArrowOrColonColon, cancellationToken);
        }

        public static bool IsRightOfDotOrArrow(this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
        {
            return syntaxTree.IsRightOf(position, s_isDotOrArrow, cancellationToken);
        }

        private static bool IsRightOf(
            this SyntaxTree syntaxTree, int position, Func<SyntaxKind, bool> predicate, CancellationToken cancellationToken)
        {
            var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
            token = token.GetPreviousTokenIfTouchingWord(position);

            if (token.Kind() == SyntaxKind.None)
            {
                return false;
            }

            return predicate(token.Kind());
        }

        public static bool IsRightOfNumericLiteral(this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
        {
            var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
            return token.Kind() == SyntaxKind.NumericLiteralToken;
        }

        public static bool IsPrimaryFunctionExpressionContext(this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition)
        {
            return
                syntaxTree.IsTypeOfExpressionContext(position, tokenOnLeftOfPosition) ||
                syntaxTree.IsDefaultExpressionContext(position, tokenOnLeftOfPosition) ||
                syntaxTree.IsSizeOfExpressionContext(position, tokenOnLeftOfPosition);
        }

        public static bool IsAfterKeyword(this SyntaxTree syntaxTree, int position, SyntaxKind kind, CancellationToken cancellationToken)
        {
            var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
            token = token.GetPreviousTokenIfTouchingWord(position);

            return token.Kind() == kind;
        }

        public static bool IsEntirelyWithinComment(this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
        {
            return
                syntaxTree.IsEntirelyWithinTopLevelSingleLineComment(position, cancellationToken) ||
                syntaxTree.IsEntirelyWithinPreProcessorSingleLineComment(position, cancellationToken) ||
                syntaxTree.IsEntirelyWithinMultiLineComment(position, cancellationToken) ||
                syntaxTree.IsEntirelyWithinMultiLineDocComment(position, cancellationToken) ||
                syntaxTree.IsEntirelyWithinSingleLineDocComment(position, cancellationToken);
        }

        public static ImmutableArray<MemberDeclarationSyntax> GetFieldsAndPropertiesInSpan(
            this SyntaxNode root, TextSpan textSpan, bool allowPartialSelection)
        {
            var token = root.FindTokenOnRightOfPosition(textSpan.Start);
            var firstMember = token.GetAncestors<MemberDeclarationSyntax>().FirstOrDefault();
            if (firstMember != null)
            {
                if (firstMember.Parent is TypeDeclarationSyntax containingType)
                {
                    return GetFieldsAndPropertiesInSpan(textSpan, containingType, firstMember, allowPartialSelection);
                }
            }

            return ImmutableArray<MemberDeclarationSyntax>.Empty;
        }

        private static ImmutableArray<MemberDeclarationSyntax> GetFieldsAndPropertiesInSpan(
            TextSpan textSpan,
            TypeDeclarationSyntax containingType,
            MemberDeclarationSyntax firstMember,
            bool allowPartialSelection)
        {
            var members = containingType.Members;
            var fieldIndex = members.IndexOf(firstMember);
            if (fieldIndex < 0)
            {
                return ImmutableArray<MemberDeclarationSyntax>.Empty;
            }

            var selectedMembers = ArrayBuilder<MemberDeclarationSyntax>.GetInstance();
            for (var i = fieldIndex; i < members.Count; i++)
            {
                var member = members[i];
                if (IsSelectedFieldOrProperty(textSpan, member, allowPartialSelection))
                {
                    selectedMembers.Add(member);
                }
            }

            return selectedMembers.ToImmutableAndFree();

            // local functions
            static bool IsSelectedFieldOrProperty(TextSpan textSpan, MemberDeclarationSyntax member, bool allowPartialSelection)
            {
                if (!member.IsKind(SyntaxKind.FieldDeclaration, SyntaxKind.PropertyDeclaration))
                {
                    return false;
                }

                // first, check if entire member is selected
                if (textSpan.Contains(member.Span))
                {
                    return true;
                }

                if (!allowPartialSelection)
                {
                    return false;
                }

                // next, check if identifier is at least partially selected
                switch (member)
                {
                    case FieldDeclarationSyntax field:
                        var variables = field.Declaration.Variables;
                        foreach (var variable in variables)
                        {
                            if (textSpan.OverlapsWith(variable.Identifier.Span))
                            {
                                return true;
                            }
                        }
                        return false;
                    case PropertyDeclarationSyntax property:
                        return textSpan.OverlapsWith(property.Identifier.Span);
                    default:
                        return false;
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
