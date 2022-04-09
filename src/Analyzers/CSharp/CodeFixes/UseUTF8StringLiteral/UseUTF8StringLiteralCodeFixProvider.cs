// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.UseUTF8StringLiteral
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseUTF8StringLiteral), Shared]
    internal sealed class UseUTF8StringLiteralCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        private const char QuoteCharacter = '"';
        private const string Suffix = "u8";

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public UseUTF8StringLiteralCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(IDEDiagnosticIds.UseUTF8StringLiteralDiagnosticId);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(CodeAction.Create(
                CSharpAnalyzersResources.Use_UTF8_string_literal,
                c => FixAsync(context.Document, context.Diagnostics[0], c),
                nameof(CSharpAnalyzersResources.Use_UTF8_string_literal)),
                context.Diagnostics);

            return Task.CompletedTask;
        }

        protected override Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            foreach (var diagnostic in diagnostics)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var arrayNode = diagnostic.Location.FindNode(getInnermostNodeForTie: true, cancellationToken);
                var stringValue = diagnostic.Properties[UseUTF8StringLiteralDiagnosticAnalyzer.StringValuePropertyName]!;

                // If we're replacing a byte array that is passed to a parameter array, not and an explicit array creation
                // then we'll get then arrayNode will be the ArgumentListSyntax so we have to work a bit harder
                //
                // eg given a method:
                //     M(string x, params byte[] b)
                // our diagnositic would be reported on:
                //     M("hi", [|1, 2, 3, 4|]);
                //
                // but arrayNode will be the whole argument list syntax

                if (arrayNode is ArgumentListSyntax argumentList)
                {
                    editor.ReplaceNode(arrayNode, CreateArgumentListWithUTF8String(argumentList, diagnostic.Location, stringValue));
                }
                else
                {
                    editor.ReplaceNode(arrayNode, CreateUTF8String(arrayNode, stringValue));
                }
            }

            return Task.CompletedTask;
        }

        private static SyntaxNode CreateArgumentListWithUTF8String(ArgumentListSyntax argumentList, Location location, string stringValue)
        {
            // To construct our new argument list we add any existing arguments before the location
            // and then once we hit the location, we add our string literal
            var _ = ArrayBuilder<ArgumentSyntax>.GetInstance(out var arguments);
            foreach (var argument in argumentList.Arguments)
            {
                if (argument.Span.Start >= location.SourceSpan.Start)
                {
                    // If this is the first argument in the argument list, then trivia will be
                    // attached to the open parentheses, so we don't need to do anything.
                    var leadingTrivia = argument == argumentList.Arguments[0]
                        ? SyntaxTriviaList.Empty
                        : SyntaxFactory.TriviaList(argument.GetAllPrecedingTriviaToPreviousToken());

                    var stringLiteral = CreateUTF8String(leadingTrivia, stringValue, argumentList.Arguments.Last().GetTrailingTrivia());
                    arguments.Add(SyntaxFactory.Argument(stringLiteral));
                    break;
                }

                arguments.Add(argument);
            }

            return argumentList.WithArguments(SyntaxFactory.SeparatedList(arguments));
        }

        private static LiteralExpressionSyntax CreateUTF8String(SyntaxNode nodeToTakeTriviaFrom, string stringValue)
        {
            return CreateUTF8String(nodeToTakeTriviaFrom.GetLeadingTrivia(), stringValue, nodeToTakeTriviaFrom.GetTrailingTrivia());
        }

        private static LiteralExpressionSyntax CreateUTF8String(SyntaxTriviaList leadingTrivia, string stringValue, SyntaxTriviaList trailingTrivia)
        {
            var literal = SyntaxFactory.Token(
                    leading: leadingTrivia,
                    kind: SyntaxKind.UTF8StringLiteralToken,
                    text: QuoteCharacter + stringValue + QuoteCharacter + Suffix,
                    valueText: "",
                    trailing: trailingTrivia);

            return SyntaxFactory.LiteralExpression(SyntaxKind.UTF8StringLiteralExpression, literal);
        }
    }
}
