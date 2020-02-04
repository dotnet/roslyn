// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
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

        private static bool AtEndOfIncompleteStringOrCharLiteral(SyntaxToken token, int position, char lastChar)
        {
            if (!token.IsKind(SyntaxKind.StringLiteralToken, SyntaxKind.CharacterLiteralToken))
            {
                throw new ArgumentException("Expected string or char literal", nameof(token));
            }

            var startLength = 1;
            if (token.IsVerbatimStringLiteral())
            {
                startLength = 2;
            }

            return position == token.Span.End &&
                (token.Span.Length == startLength || (token.Span.Length > startLength && token.ToString().Cast<char>().LastOrDefault() != lastChar));
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

        public static bool IsEntirelyWithinCrefSyntax(this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
        {
            if (syntaxTree.IsCrefContext(position, cancellationToken))
            {
                return true;
            }

            var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken, includeDocumentationComments: true);
            return token.GetAncestor<CrefSyntax>() != null;
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
    }
}
