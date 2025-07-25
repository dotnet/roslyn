// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static partial class SyntaxTreeExtensions
{
    extension([NotNullWhen(true)] SyntaxTree? tree)
    {
        public bool OverlapsHiddenPosition(TextSpan span, CancellationToken cancellationToken)
        {
            // Short-circuit if there are no line mappings to avoid potentially realizing the source text.
            // All lines are visible if there are no line mappings.
            if (tree == null || tree.GetLineMappings(cancellationToken).IsEmpty())
            {
                return false;
            }

            var text = tree.GetText(cancellationToken);

            return text.OverlapsHiddenPosition(span, (position, cancellationToken2) =>
                {
                    // implements the ASP.NET IsHidden rule
                    var lineVisibility = tree.GetLineVisibility(position, cancellationToken2);
                    return lineVisibility is LineVisibility.Hidden or LineVisibility.BeforeFirstLineDirective;
                },
                cancellationToken);
        }
    }

    extension(SyntaxTree syntaxTree)
    {
        public bool IsScript()
        => syntaxTree.Options.Kind != SourceCodeKind.Regular;

        /// <summary>
        /// Returns the identifier, keyword, contextual keyword or preprocessor keyword touching this
        /// position, or a token of Kind = None if the caret is not touching either.
        /// </summary>
        public Task<SyntaxToken> GetTouchingWordAsync(
            int position,
            ISyntaxFacts syntaxFacts,
            CancellationToken cancellationToken,
            bool findInsideTrivia = false)
        {
            return GetTouchingTokenAsync(syntaxTree, semanticModel: null, position, (_, t) => syntaxFacts.IsWord(t), cancellationToken, findInsideTrivia);
        }

        public Task<SyntaxToken> GetTouchingTokenAsync(
            int position,
            CancellationToken cancellationToken,
            bool findInsideTrivia = false)
        {
            return GetTouchingTokenAsync(syntaxTree, semanticModel: null, position, (_, _) => true, cancellationToken, findInsideTrivia);
        }

        public async Task<SyntaxToken> GetTouchingTokenAsync(
            SemanticModel? semanticModel,
            int position,
            Func<SemanticModel?, SyntaxToken, bool> predicate,
            CancellationToken cancellationToken,
            bool findInsideTrivia = false)
        {
            Contract.ThrowIfNull(syntaxTree);

            if (position > syntaxTree.Length)
            {
                return default;
            }

            var root = await syntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(position, findInsideTrivia);

            if ((token.Span.Contains(position) || token.Span.End == position) && predicate(semanticModel, token))
            {
                return token;
            }

            token = token.GetPreviousToken();

            if (token.Span.End == position && predicate(semanticModel, token))
            {
                return token;
            }

            // SyntaxKind = None
            return default;
        }

        public bool IsBeforeFirstToken(int position, CancellationToken cancellationToken)
        {
            var root = syntaxTree.GetRoot(cancellationToken);
            var firstToken = root.GetFirstToken(includeZeroWidth: true, includeSkipped: true);

            return position <= firstToken.SpanStart;
        }

        public SyntaxToken FindTokenOrEndToken(
    int position, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(syntaxTree);

            var root = syntaxTree.GetRoot(cancellationToken);
            var result = root.FindToken(position, findInsideTrivia: true);
            if (result.RawKind != 0)
            {
                return result;
            }

            // Special cases.  See if we're actually at the end of a:
            // a) doc comment
            // b) pp directive
            // c) file

            var compilationUnit = (ICompilationUnitSyntax)root;
            var triviaList = compilationUnit.EndOfFileToken.LeadingTrivia;
            foreach (var trivia in triviaList.Reverse())
            {
                if (trivia.HasStructure)
                {
                    var token = trivia.GetStructure()!.GetLastToken(includeZeroWidth: true);
                    if (token.Span.End == position)
                    {
                        return token;
                    }
                }
            }

            if (position == root.FullSpan.End)
            {
                return compilationUnit.EndOfFileToken;
            }

            return default;
        }

        internal SyntaxTrivia FindTriviaAndAdjustForEndOfFile(
    int position, CancellationToken cancellationToken, bool findInsideTrivia = false)
        {
            var root = syntaxTree.GetRoot(cancellationToken);
            var trivia = root.FindTrivia(position, findInsideTrivia);

            // If we ask right at the end of the file, we'll get back nothing.
            // We handle that case specially for now, though SyntaxTree.FindTrivia should
            // work at the end of a file.
            if (position == root.FullWidth())
            {
                var compilationUnit = (ICompilationUnitSyntax)root;
                var endOfFileToken = compilationUnit.EndOfFileToken;
                if (endOfFileToken.HasLeadingTrivia)
                {
                    trivia = endOfFileToken.LeadingTrivia.Last();
                }
                else
                {
                    var token = endOfFileToken.GetPreviousToken(includeSkipped: true);
                    if (token.HasTrailingTrivia)
                    {
                        trivia = token.TrailingTrivia.Last();
                    }
                }
            }

            return trivia;
        }

        /// <summary>
        /// If the position is inside of token, return that token; otherwise, return the token to the right.
        /// </summary>
        public SyntaxToken FindTokenOnRightOfPosition(
            int position,
            CancellationToken cancellationToken,
            bool includeSkipped = true,
            bool includeDirectives = false,
            bool includeDocumentationComments = false)
        {
            return syntaxTree.GetRoot(cancellationToken).FindTokenOnRightOfPosition(
                position, includeSkipped, includeDirectives, includeDocumentationComments);
        }

        /// <summary>
        /// If the position is inside of token, return that token; otherwise, return the token to the left.
        /// </summary>
        public SyntaxToken FindTokenOnLeftOfPosition(
            int position,
            CancellationToken cancellationToken,
            bool includeSkipped = true,
            bool includeDirectives = false,
            bool includeDocumentationComments = false)
        {
            return syntaxTree.GetRoot(cancellationToken).FindTokenOnLeftOfPosition(
                position, includeSkipped, includeDirectives, includeDocumentationComments);
        }

        public bool IsGeneratedCode(AnalyzerOptions? analyzerOptions, ISyntaxFacts syntaxFacts, CancellationToken cancellationToken)
        {
            // First check if user has configured "generated_code = true | false" in .editorconfig
            if (analyzerOptions != null)
            {
                var analyzerConfigOptions = analyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(syntaxTree);
                var isUserConfiguredGeneratedCode = GeneratedCodeUtilities.GetGeneratedCodeKindFromOptions(analyzerConfigOptions).ToNullable();
                if (isUserConfiguredGeneratedCode.HasValue)
                {
                    return isUserConfiguredGeneratedCode.Value;
                }
            }

            // Otherwise, fallback to generated code heuristic.
            return GeneratedCodeUtilities.IsGeneratedCode(
                syntaxTree, t => syntaxFacts.IsRegularComment(t) || syntaxFacts.IsDocumentationComment(t), cancellationToken);
        }

        /// <summary>
        /// Finds the node in the given <paramref name="syntaxTree"/> corresponding to the given <paramref name="span"/>.
        /// If the <paramref name="span"/> is <see langword="null"/>, then returns the root node of the tree.
        /// </summary>
        public SyntaxNode FindNode(TextSpan? span, bool findInTrivia, bool getInnermostNodeForTie, CancellationToken cancellationToken)
        {
            var root = syntaxTree.GetRoot(cancellationToken);
            return root.FindNode(span, findInTrivia, getInnermostNodeForTie);
        }
    }

    extension(SyntaxTree tree)
    {
        public bool IsEntirelyHidden(TextSpan span, CancellationToken cancellationToken)
        {
            if (!tree.HasHiddenRegions())
            {
                return false;
            }

            var text = tree.GetText(cancellationToken);
            var startLineNumber = text.Lines.IndexOf(span.Start);
            var endLineNumber = text.Lines.IndexOf(span.End);

            for (var lineNumber = startLineNumber; lineNumber <= endLineNumber; lineNumber++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var linePosition = text.Lines[lineNumber].Start;
                if (!tree.IsHiddenPosition(linePosition, cancellationToken))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
