// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

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
            RegisterCodeFix(context, CSharpAnalyzersResources.Use_UTF8_string_literal, nameof(CSharpAnalyzersResources.Use_UTF8_string_literal));
            return Task.CompletedTask;
        }

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CodeActionOptionsProvider options, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            foreach (var diagnostic in diagnostics)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var node = diagnostic.Location.FindNode(getInnermostNodeForTie: true, cancellationToken);
                var stringValue = GetUTF8StringValueForDiagnostic(semanticModel, diagnostic, cancellationToken);

                // If we're replacing a byte array that is passed to a parameter array, not and an explicit array creation
                // then node will be the ArgumentListSyntax that the implicit array creation is just a part of, so we have
                // to handle that separately, as we can't just replace node with a string literal
                //
                // eg given a method:
                //     M(string x, params byte[] b)
                // our diagnostic would be reported on:
                //     M("hi", [|1, 2, 3, 4|]);
                // but node will point to:
                //     M([|"hi", 1, 2, 3, 4|]);

                if (node is BaseArgumentListSyntax argumentList)
                {
                    editor.ReplaceNode(node, CreateArgumentListWithUTF8String(argumentList, diagnostic.Location, stringValue));
                }
                else
                {
                    editor.ReplaceNode(node, CreateUTF8String(node, stringValue));
                }
            }
        }

        private static string GetUTF8StringValueForDiagnostic(SemanticModel semanticModel, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            // For computing the UTF8 string we need the original location of the array creation
            // operation, which is stored in additional locations.
            var location = diagnostic.AdditionalLocations[0];
            var node = location.FindNode(getInnermostNodeForTie: true, cancellationToken);

            var operation = semanticModel.GetRequiredOperation(node, cancellationToken);

            var operationLocationString = diagnostic.Properties[nameof(UseUTF8StringLiteralDiagnosticAnalyzer.ArrayCreationOperationLocation)];
            if (!Enum.TryParse(operationLocationString, out UseUTF8StringLiteralDiagnosticAnalyzer.ArrayCreationOperationLocation operationLocation))
                throw ExceptionUtilities.Unreachable;

            IArrayCreationOperation arrayOp;

            // Because we get the location from an IOperation.Syntax, sometimes we have to look a
            // little harder to get back from syntax to the operation that triggered the diagnostic
            if (operationLocation == UseUTF8StringLiteralDiagnosticAnalyzer.ArrayCreationOperationLocation.Ancestors)
            {
                // For collection initializers where the Add method takes a param array, and the array creation
                // will be a parent of the operation
                arrayOp = FindArrayCreationOperationAncestor(operation);
            }
            else if (operationLocation == UseUTF8StringLiteralDiagnosticAnalyzer.ArrayCreationOperationLocation.Descendants)
            {
                // Otherwise, we must have an implicit array creation for a parameter array, so the location
                // will be the invocation, or similar, that has the argument, and we need to descend child
                // nodes to find the one we are interested in. To make sure we're finding the right one,
                // we can use the diagnostic location for that, since the analyzer raises it on the first element.
                arrayOp = operation.DescendantsAndSelf()
                    .OfType<IArrayCreationOperation>()
                    .Where(a => a.Initializer?.ElementValues.FirstOrDefault()?.Syntax.SpanStart == diagnostic.Location.SourceSpan.Start)
                    .First();
            }
            else
            {
                arrayOp = (IArrayCreationOperation)operation;
            }

            Contract.ThrowIfNull(arrayOp.Initializer);

            // Get our list of bytes from the array elements
            using var _ = PooledStringBuilder.GetInstance(out var builder);
            builder.Capacity = arrayOp.Initializer.ElementValues.Length;
            if (!UseUTF8StringLiteralDiagnosticAnalyzer.TryConvertToUTF8String(builder, arrayOp.Initializer.ElementValues))
            {
                // We shouldn't get here, because the code fix shouldn't ask for a string value
                // if the analyzer couldn't convert it
                throw ExceptionUtilities.Unreachable;
            }

            return builder.ToString();

            static IArrayCreationOperation FindArrayCreationOperationAncestor(IOperation operation)
            {
                while (operation is not null)
                {
                    if (operation is IArrayCreationOperation arrayOperation)
                        return arrayOperation;

                    operation = operation.Parent!;
                }

                throw ExceptionUtilities.Unreachable;
            }
        }

        private static SyntaxNode CreateArgumentListWithUTF8String(BaseArgumentListSyntax argumentList, Location location, string stringValue)
        {
            // To construct our new argument list we add any existing tokens before the location
            // and then once we hit the location, we add our string literal
            // We can't just loop through the arguments, as we want to preserve trivia on the
            // comma tokens, if any.
            using var _ = ArrayBuilder<SyntaxNodeOrToken>.GetInstance(out var arguments);
            foreach (var argument in argumentList.ChildNodesAndTokens())
            {
                // Skip the open paren, its a child token but not an argument
                if (argument.Kind() is SyntaxKind.OpenParenToken or SyntaxKind.OpenBracketToken)
                {
                    continue;
                }

                // See if we found our first argument
                if (argument.Span.Start == location.SourceSpan.Start)
                {
                    // We don't need to worry about leading trivia here, because anything before the current
                    // argument will have been trailing trivia on the previous comma.
                    var stringLiteral = CreateUTF8String(SyntaxTriviaList.Empty, stringValue, argumentList.Arguments.Last().GetTrailingTrivia());
                    arguments.Add(SyntaxFactory.Argument(stringLiteral));
                    break;
                }

                arguments.Add(argument);
            }

            return argumentList.WithArguments(SyntaxFactory.SeparatedList<ArgumentSyntax>(arguments));
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
