// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

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
#pragma warning disable IDE0060 // Remove unused parameter - Unused this parameter for consistency with other extension methods.
            this SyntaxTree syntaxTree,
#pragma warning restore IDE0060 // Remove unused parameter
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
                    case SyntaxKind.OutKeyword:
                    case SyntaxKind.InKeyword:
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

                        if (token.HasMatchingText(SyntaxKind.DataKeyword))
                        {
                            result.Add(SyntaxKind.DataKeyword);
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
            => syntaxTree.IsRightOf(position, s_isDotOrArrow, cancellationToken);

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

        public static bool IsAfterKeyword(this SyntaxTree syntaxTree, int position, SyntaxKind kind, CancellationToken cancellationToken)
        {
            var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
            token = token.GetPreviousTokenIfTouchingWord(position);

            return token.Kind() == kind;
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
                throw new ArgumentException(CSharpCompilerExtensionsResources.Expected_string_or_char_literal, nameof(token));
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
    }
}
