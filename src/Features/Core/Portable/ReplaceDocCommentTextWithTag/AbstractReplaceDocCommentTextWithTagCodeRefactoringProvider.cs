// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ReplaceDocCommentTextWithTag
{
    internal abstract class AbstractReplaceDocCommentTextWithTagCodeRefactoringProvider : CodeRefactoringProvider
    {
        protected abstract bool IsInXMLAttribute(SyntaxToken token);
        protected abstract bool IsKeyword(string text);
        protected abstract bool IsXmlTextToken(SyntaxToken token);
        protected abstract SyntaxNode ParseExpression(string text);

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, span, cancellationToken) = context;
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(span.Start, findInsideTrivia: true);

            if (!IsXmlTextToken(token))
                return;

            if (!token.FullSpan.Contains(span))
                return;

            if (IsInXMLAttribute(token))
                return;

            var sourceText = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

            var singleWordSpan = ExpandSpan(sourceText, span, fullyQualifiedName: false);
            var singleWordText = sourceText.ToString(singleWordSpan);
            if (singleWordText == "")
                return;

            // First see if they're on an appropriate keyword. 
            if (IsKeyword(singleWordText))
            {
                RegisterRefactoring(context, singleWordSpan, $@"<see langword=""{singleWordText}""/>");
                return;
            }

            // Not a keyword, see if it semantically means anything in the current context.
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var symbol = GetEnclosingSymbol(semanticModel, span.Start, cancellationToken);
            if (symbol == null)
                return;

            // See if we can expand the term out to a fully qualified name. Do this
            // first in case the user has something like X.memberName.  We don't want 
            // to try to bind "memberName" first as it might bind to something like 
            // a parameter, which is not what the user intends
            var fullyQualifiedSpan = ExpandSpan(sourceText, span, fullyQualifiedName: true);
            if (fullyQualifiedSpan != singleWordSpan)
            {
                var fullyQualifiedText = sourceText.ToString(fullyQualifiedSpan);
                if (TryRegisterSeeCrefTagIfSymbol(
                        context, semanticModel, token, fullyQualifiedSpan, cancellationToken))
                {
                    return;
                }
            }

            // Check if the single word could be binding to a type parameter or parameter
            // for the current symbol.
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var parameter = symbol.GetParameters().FirstOrDefault(p => syntaxFacts.StringComparer.Equals(p.Name, singleWordText));
            if (parameter != null)
            {
                RegisterRefactoring(context, singleWordSpan, $@"<paramref name=""{singleWordText}""/>");
                return;
            }

            var typeParameter = symbol.GetTypeParameters().FirstOrDefault(t => syntaxFacts.StringComparer.Equals(t.Name, singleWordText));
            if (typeParameter != null)
            {
                RegisterRefactoring(context, singleWordSpan, $@"<typeparamref name=""{singleWordText}""/>");
                return;
            }

            // Doc comments on a named type can see the members inside of it.  So check
            // inside the named type for a member that matches.
            if (symbol is INamedTypeSymbol namedType)
            {
                var childMember = namedType.GetMembers().FirstOrDefault(m => syntaxFacts.StringComparer.Equals(m.Name, singleWordText));
                if (childMember != null)
                {
                    RegisterRefactoring(context, singleWordSpan, $@"<see cref=""{singleWordText}""/>");
                    return;
                }
            }

            // Finally, try to speculatively bind the name and see if it binds to anything
            // in the surrounding context.
            TryRegisterSeeCrefTagIfSymbol(
                context, semanticModel, token, singleWordSpan, cancellationToken);
        }

        private bool TryRegisterSeeCrefTagIfSymbol(
            CodeRefactoringContext context, SemanticModel semanticModel, SyntaxToken token, TextSpan replacementSpan, CancellationToken cancellationToken)
        {
            var sourceText = semanticModel.SyntaxTree.GetText(cancellationToken);
            var text = sourceText.ToString(replacementSpan);

            var parsed = ParseExpression(text);
            var foundSymbol = semanticModel.GetSpeculativeSymbolInfo(token.SpanStart, parsed, SpeculativeBindingOption.BindAsExpression).GetAnySymbol();
            if (foundSymbol == null)
            {
                return false;
            }

            RegisterRefactoring(context, replacementSpan, $@"<see cref=""{text}""/>");
            return true;
        }

        private static ISymbol? GetEnclosingSymbol(SemanticModel semanticModel, int position, CancellationToken cancellationToken)
        {
            var root = semanticModel.SyntaxTree.GetRoot(cancellationToken);
            var token = root.FindToken(position);

            for (var node = token.Parent; node != null; node = node.Parent)
            {
                if (semanticModel.GetDeclaredSymbol(node, cancellationToken) is ISymbol declaration)
                    return declaration;
            }

            return null;
        }

        private static void RegisterRefactoring(
            CodeRefactoringContext context, TextSpan expandedSpan, string replacement)
        {
            context.RegisterRefactoring(
                CodeAction.Create(
                    string.Format(FeaturesResources.Use_0, replacement),
                    c => ReplaceTextAsync(context.Document, expandedSpan, replacement, c),
                    nameof(FeaturesResources.Use_0) + "_" + replacement),
                expandedSpan);
        }

        private static async Task<Document> ReplaceTextAsync(
            Document document, TextSpan span, string replacement, CancellationToken cancellationToken)
        {
            var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
            var newText = text.Replace(span, replacement);

            return document.WithText(newText);
        }

        private static TextSpan ExpandSpan(SourceText sourceText, TextSpan span, bool fullyQualifiedName)
        {
            if (span.Length != 0)
            {
                return span;
            }

            var startInclusive = span.Start;
            var endExclusive = span.Start;
            while (startInclusive > 0 &&
                   ShouldExpandSpanBackwardOneCharacter(sourceText, startInclusive, fullyQualifiedName))
            {
                startInclusive--;
            }

            while (endExclusive < sourceText.Length &&
                   ShouldExpandSpanForwardOneCharacter(sourceText, endExclusive, fullyQualifiedName))
            {
                endExclusive++;
            }

            return TextSpan.FromBounds(startInclusive, endExclusive);
        }

        private static bool ShouldExpandSpanForwardOneCharacter(
            SourceText sourceText, int endExclusive, bool fullyQualifiedName)
        {
            var currentChar = sourceText[endExclusive];

            if (char.IsLetterOrDigit(currentChar))
            {
                return true;
            }

            // Only consume a dot in front of the current word if it is part of a dotted
            // word chain, and isn't just the end of a sentence.
            if (fullyQualifiedName && currentChar == '.' &&
                endExclusive + 1 < sourceText.Length && char.IsLetterOrDigit(sourceText[endExclusive + 1]))
            {
                return true;
            }

            return false;
        }

        private static bool ShouldExpandSpanBackwardOneCharacter(
            SourceText sourceText, int startInclusive, bool fullyQualifiedName)
        {
            Debug.Assert(startInclusive > 0);

            var previousCharacter = sourceText[startInclusive - 1];
            if (char.IsLetterOrDigit(previousCharacter))
            {
                return true;
            }

            if (fullyQualifiedName && previousCharacter == '.')
            {
                return true;
            }

            return false;
        }
    }
}
