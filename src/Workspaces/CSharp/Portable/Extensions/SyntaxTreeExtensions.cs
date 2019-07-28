// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            SyntaxToken tokenOnLeftOfPosition,
            CancellationToken cancellationToken)
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

        public static bool IsPrimaryFunctionExpressionContext(this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition, CancellationToken cancellationToken)
        {
            return
                syntaxTree.IsTypeOfExpressionContext(position, tokenOnLeftOfPosition, cancellationToken) ||
                syntaxTree.IsDefaultExpressionContext(position, tokenOnLeftOfPosition, cancellationToken) ||
                syntaxTree.IsSizeOfExpressionContext(position, tokenOnLeftOfPosition, cancellationToken);
        }

        public static bool IsAfterKeyword(this SyntaxTree syntaxTree, int position, SyntaxKind kind, CancellationToken cancellationToken)
        {
            var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
            token = token.GetPreviousTokenIfTouchingWord(position);

            return token.Kind() == kind;
        }

        public static bool IsInNonUserCode(this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
        {
            return
                syntaxTree.IsEntirelyWithinNonUserCodeComment(position, cancellationToken) ||
                syntaxTree.IsEntirelyWithinConflictMarker(position, cancellationToken) ||
                syntaxTree.IsEntirelyWithinStringOrCharLiteral(position, cancellationToken) ||
                syntaxTree.IsInInactiveRegion(position, cancellationToken);
        }

        public static bool IsEntirelyWithinNonUserCodeComment(this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
        {
            var inNonUserSingleLineDocComment =
                syntaxTree.IsEntirelyWithinSingleLineDocComment(position, cancellationToken) && !syntaxTree.IsEntirelyWithinCrefSyntax(position, cancellationToken);
            return
                syntaxTree.IsEntirelyWithinTopLevelSingleLineComment(position, cancellationToken) ||
                syntaxTree.IsEntirelyWithinPreProcessorSingleLineComment(position, cancellationToken) ||
                syntaxTree.IsEntirelyWithinMultiLineComment(position, cancellationToken) ||
                syntaxTree.IsEntirelyWithinMultiLineDocComment(position, cancellationToken) ||
                inNonUserSingleLineDocComment;
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

        public static bool IsCrefContext(this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
        {
            var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken, includeDocumentationComments: true);
            token = token.GetPreviousTokenIfTouchingWord(position);

            if (token.Parent is XmlCrefAttributeSyntax attribute)
            {
                return token == attribute.StartQuoteToken;
            }

            return false;
        }

        public static bool IsEntirelyWithinCrefSyntax(this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
        {
            if (syntaxTree.IsCrefContext(position, cancellationToken))
            {
                return true;
            }

            var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken, includeDocumentationComments: true);
            return token.GetAncestor<CrefSyntax>() != null;
        }

        public static bool IsEntirelyWithinSingleLineDocComment(
            this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
        {
            var root = syntaxTree.GetRoot(cancellationToken) as CompilationUnitSyntax;
            var trivia = root.FindTrivia(position);

            // If we ask right at the end of the file, we'll get back nothing.
            // So move back in that case and ask again.
            var eofPosition = root.FullWidth();
            if (position == eofPosition)
            {
                var eof = root.EndOfFileToken;
                if (eof.HasLeadingTrivia)
                {
                    trivia = eof.LeadingTrivia.Last();
                }
            }

            if (trivia.IsSingleLineDocComment())
            {
                var span = trivia.Span;
                var fullSpan = trivia.FullSpan;
                var endsWithNewLine = trivia.GetStructure().GetLastToken(includeSkipped: true).Kind() == SyntaxKind.XmlTextLiteralNewLineToken;

                if (endsWithNewLine)
                {
                    if (position > fullSpan.Start && position < fullSpan.End)
                    {
                        return true;
                    }
                }
                else
                {
                    if (position > fullSpan.Start && position <= fullSpan.End)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool IsEntirelyWithinMultiLineDocComment(
            this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
        {
            var trivia = syntaxTree.GetRoot(cancellationToken).FindTrivia(position);

            if (trivia.IsMultiLineDocComment())
            {
                var span = trivia.FullSpan;

                if (position > span.Start && position < span.End)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsEntirelyWithinMultiLineComment(
            this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
        {
            var trivia = syntaxTree.FindTriviaAndAdjustForEndOfFile(position, cancellationToken);

            if (trivia.IsMultiLineComment())
            {
                var span = trivia.FullSpan;

                return trivia.IsCompleteMultiLineComment()
                    ? position > span.Start && position < span.End
                    : position > span.Start && position <= span.End;
            }

            return false;
        }

        public static bool IsEntirelyWithinConflictMarker(
            this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
        {
            var trivia = syntaxTree.FindTriviaAndAdjustForEndOfFile(position, cancellationToken);

            if (trivia.Kind() == SyntaxKind.EndOfLineTrivia)
            {
                // Check if we're on the newline right at the end of a comment
                trivia = trivia.GetPreviousTrivia(syntaxTree, cancellationToken);
            }

            return trivia.Kind() == SyntaxKind.ConflictMarkerTrivia;
        }

        public static bool IsEntirelyWithinTopLevelSingleLineComment(
            this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
        {
            var trivia = syntaxTree.FindTriviaAndAdjustForEndOfFile(position, cancellationToken);

            if (trivia.Kind() == SyntaxKind.EndOfLineTrivia)
            {
                // Check if we're on the newline right at the end of a comment
                trivia = trivia.GetPreviousTrivia(syntaxTree, cancellationToken);
            }

            if (trivia.IsSingleLineComment() || trivia.IsShebangDirective())
            {
                var span = trivia.FullSpan;

                if (position > span.Start && position <= span.End)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsEntirelyWithinPreProcessorSingleLineComment(
            this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
        {
            // Search inside trivia for directives to ensure that we recognize
            // single-line comments at the end of preprocessor directives.
            var trivia = syntaxTree.FindTriviaAndAdjustForEndOfFile(position, cancellationToken, findInsideTrivia: true);

            if (trivia.Kind() == SyntaxKind.EndOfLineTrivia)
            {
                // Check if we're on the newline right at the end of a comment
                trivia = trivia.GetPreviousTrivia(syntaxTree, cancellationToken, findInsideTrivia: true);
            }

            if (trivia.IsSingleLineComment())
            {
                var span = trivia.FullSpan;

                if (position > span.Start && position <= span.End)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool AtEndOfIncompleteStringOrCharLiteral(SyntaxToken token, int position, char lastChar)
        {
            if (!token.IsKind(SyntaxKind.StringLiteralToken, SyntaxKind.CharacterLiteralToken))
            {
                throw new ArgumentException(CSharpWorkspaceResources.Expected_string_or_char_literal, nameof(token));
            }

            var startLength = 1;
            if (token.IsVerbatimStringLiteral())
            {
                startLength = 2;
            }

            return position == token.Span.End &&
                (token.Span.Length == startLength || (token.Span.Length > startLength && token.ToString().Cast<char>().LastOrDefault() != lastChar));
        }

        public static bool IsEntirelyWithinStringOrCharLiteral(
            this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
        {
            return
                syntaxTree.IsEntirelyWithinStringLiteral(position, cancellationToken) ||
                syntaxTree.IsEntirelyWithinCharLiteral(position, cancellationToken);
        }

        public static bool IsEntirelyWithinStringLiteral(
            this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
        {
            var token = syntaxTree.GetRoot(cancellationToken).FindToken(position, findInsideTrivia: true);

            // If we ask right at the end of the file, we'll get back nothing. We handle that case
            // specially for now, though SyntaxTree.FindToken should work at the end of a file.
            if (token.IsKind(SyntaxKind.EndOfDirectiveToken, SyntaxKind.EndOfFileToken))
            {
                token = token.GetPreviousToken(includeSkipped: true, includeDirectives: true);
            }

            if (token.IsKind(SyntaxKind.StringLiteralToken))
            {
                var span = token.Span;

                // cases:
                // "|"
                // "|  (e.g. incomplete string literal)
                return (position > span.Start && position < span.End)
                    || AtEndOfIncompleteStringOrCharLiteral(token, position, '"');
            }

            if (token.IsKind(SyntaxKind.InterpolatedStringStartToken, SyntaxKind.InterpolatedStringTextToken, SyntaxKind.InterpolatedStringEndToken))
            {
                return token.SpanStart < position && token.Span.End > position;
            }

            return false;
        }

        public static bool IsEntirelyWithinCharLiteral(
            this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
        {
            var root = syntaxTree.GetRoot(cancellationToken) as CompilationUnitSyntax;
            var token = root.FindToken(position, findInsideTrivia: true);

            // If we ask right at the end of the file, we'll get back nothing.
            // We handle that case specially for now, though SyntaxTree.FindToken should
            // work at the end of a file.
            if (position == root.FullWidth())
            {
                token = root.EndOfFileToken.GetPreviousToken(includeSkipped: true, includeDirectives: true);
            }

            if (token.Kind() == SyntaxKind.CharacterLiteralToken)
            {
                var span = token.Span;

                // cases:
                // '|'
                // '|  (e.g. incomplete char literal)
                return (position > span.Start && position < span.End)
                    || AtEndOfIncompleteStringOrCharLiteral(token, position, '\'');
            }

            return false;
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
            var genericName = token.Parent as GenericNameSyntax;

            return genericName != null && genericName.TypeArgumentList != null &&
                   genericName.TypeArgumentList.LessThanToken == lessThanToken && !genericName.TypeArgumentList.GreaterThanToken.IsMissing;
        }
    }
}
