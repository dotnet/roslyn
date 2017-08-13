// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.ReplaceDocCommentTextWithTag
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp), Shared]
    internal class CSharpReplaceDocCommentTextWithTagCodeRefactoringProvider : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var span = context.Span;
            var cancellationToken = context.CancellationToken;

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(span.Start, findInsideTrivia: true);

            if (token.Kind() != SyntaxKind.XmlTextLiteralToken &&
                token.Kind() != SyntaxKind.XmlTextLiteralNewLineToken)
            {
                return;
            }

            if (!token.FullSpan.Contains(span))
            {
                return;
            }

            var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            var expandedSpan = ExpandSpan(sourceText, span, fullyQualifiedName: false);
            var text = sourceText.ToString(expandedSpan);

            if (SyntaxFacts.GetKeywordKind(text) != SyntaxKind.None ||
                SyntaxFacts.GetContextualKeywordKind(text) != SyntaxKind.None)
            {
                RegisterRefactoring(context, expandedSpan, $@"<see langword=""{text}""/>");
                return;
            }

            var memberDecl = token.Parent.GetAncestorOrThis<MemberDeclarationSyntax>();
            if (memberDecl == null)
            {
                return;
            }

            expandedSpan = ExpandSpan(sourceText, span, fullyQualifiedName: true);
            text = sourceText.ToString(expandedSpan);

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var speculativePosition = memberDecl.GetLastToken().FullSpan.Start - 1;
            if (!TryMatchTextAsSymbol(context, expandedSpan, text, semanticModel, speculativePosition, SpeculativeBindingOption.BindAsTypeOrNamespace) &&
                !TryMatchTextAsSymbol(context, expandedSpan, text, semanticModel, speculativePosition, SpeculativeBindingOption.BindAsExpression))
            {
                return;
            }
        }

        private void RegisterRefactoring(
            CodeRefactoringContext context, TextSpan expandedSpan, string replacement)
        {
            context.RegisterRefactoring(new MyCodeAction(
                string.Format(FeaturesResources.Use_0, replacement),
                c => ReplaceTextAsync(context.Document, expandedSpan, replacement, c)));
        }

        private bool TryMatchTextAsSymbol(
            CodeRefactoringContext context, TextSpan expandedSpan, 
            string text, SemanticModel semanticModel, 
            int speculativePosition, SpeculativeBindingOption binding)
        {
            var parsed = SyntaxFactory.ParseExpression(text);

            var symbolInfo = semanticModel.GetSpeculativeSymbolInfo(speculativePosition, parsed, binding);
            switch (symbolInfo.GetAnySymbol())
            {
                case IParameterSymbol parameter:
                    RegisterRefactoring(context, expandedSpan, $@"<paramref name=""{text}""/>");
                    return true;
                case ITypeParameterSymbol typeParameter:
                    RegisterRefactoring(context, expandedSpan, $@"<typeparamref name=""{text}""/>");
                    return true;
                case ISymbol symbol:
                    RegisterRefactoring(context, expandedSpan, $@"<see cref=""{text}""/>");
                    return true;
                default:
                    return false;
            }
        }

        private async Task<Document> ReplaceTextAsync(
            Document document, TextSpan span, string replacement, CancellationToken cancellationToken)
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var newText = text.Replace(span, replacement);

            return document.WithText(newText);
        }

        private TextSpan ExpandSpan(SourceText sourceText, TextSpan span, bool fullyQualifiedName)
        {
            if (span.Length != 0)
            {
                return span;
            }

            var start = span.Start;
            var end = span.Start;
            while (start > 0 && ExpandBackward(sourceText, start, fullyQualifiedName))
            {
                start--;
            }

            while (end < sourceText.Length && ExpandForward(sourceText, end, fullyQualifiedName))
            {
                end++;
            }

            return TextSpan.FromBounds(start, end);
        }

        private bool ExpandForward(SourceText sourceText, int end, bool fullyQualifiedName)
        {
            var currentChar = sourceText[end];

            if (char.IsLetterOrDigit(currentChar))
            {
                return true;
            }

            // Only consume a dot in front of the current word if it is part of a dotted
            // word chain, and isn't just the end of a sentence.
            if (fullyQualifiedName && currentChar == '.' &&
                end + 1 < sourceText.Length && char.IsLetterOrDigit(sourceText[end + 1]))
            {
                return true;
            }

            return false;
        }

        private bool ExpandBackward(
            SourceText sourceText, int start, bool fullyQualifiedName)
        {
            var previousCharacter = sourceText[start - 1];
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

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument) 
                : base(title, createChangedDocument)
            {
            }
        }
    }
}
