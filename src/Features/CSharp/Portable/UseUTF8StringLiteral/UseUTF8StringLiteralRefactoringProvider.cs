// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseUTF8StringLiteral
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.UseUTF8StringLiteral), Shared]
    internal sealed class UseUTF8StringLiteralRefactoringProvider : CodeRefactoringProvider
    {
        private const char QuoteCharacter = '"';
        private const string Suffix = "u8";

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public UseUTF8StringLiteralRefactoringProvider()
        {
        }

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, _, cancellationToken) = context;

            var node = (SyntaxNode?)await context.TryGetRelevantNodeAsync<ArrayCreationExpressionSyntax>().ConfigureAwait(false) ??
                await context.TryGetRelevantNodeAsync<ImplicitArrayCreationExpressionSyntax>().ConfigureAwait(false);
            if (node is null)
                return;

            var model = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (model.Compilation.GetTypeByMetadataName(typeof(ReadOnlySpan<>).FullName!) is null)
                return;

            if (!model.Compilation.LanguageVersion().IsCSharp11OrAbove())
                return;

            if (model.GetOperation(node, cancellationToken) is not IArrayCreationOperation arrayCreationOperation)
                return;

            // Only replace arrays with initializers
            if (arrayCreationOperation.Initializer is null)
                return;

            // Using UTF8 string literals as nested array initializers is invalid
            if (arrayCreationOperation.DimensionSizes.Length > 1)
                return;

            // Must be a byte array
            if (arrayCreationOperation.Type is not IArrayTypeSymbol { ElementType.SpecialType: SpecialType.System_Byte })
                return;

            // UTF8 strings are not valid to use in attributes
            if (arrayCreationOperation.Syntax.Ancestors().OfType<AttributeSyntax>().Any())
                return;

            // Can't use a UTF8 string inside an expression tree.
            var expressionType = model.Compilation.GetTypeByMetadataName(typeof(System.Linq.Expressions.Expression<>).FullName!);
            if (node.IsInExpressionTree(model, expressionType, context.CancellationToken))
                return;

            var elements = arrayCreationOperation.Initializer.ElementValues;

            // If the compiler has constructed this array creation, then we don't want to do anything
            // if there aren't any elements, as we could just end up inserting ""u8 somewhere.
            if (arrayCreationOperation.IsImplicit && elements.Length == 0)
                return;

            // We don't want to pass a builder here, because there is no point realizing a potentially large string
            // if the user doesn't click the refactoring
            if (!TryConvertToUTF8String(builder: null, elements))
                return;

            context.RegisterRefactoring(
               CodeAction.Create(
                   CSharpFeaturesResources.Use_UTF8_string_literal,
                   c => ConvertToUTF8StringLiteralAsync(document, node, elements, c),
                   CSharpFeaturesResources.Use_UTF8_string_literal),
               node.Span);
        }

        private static bool TryConvertToUTF8String(StringBuilder? builder, ImmutableArray<IOperation> arrayCreationElements)
        {
            for (var i = 0; i < arrayCreationElements.Length;)
            {
                // Need to call a method to do the actual rune decoding as it uses stackalloc, and stackalloc
                // in a loop is a bad idea
                if (!TryGetNextRune(arrayCreationElements, i, out var rune, out var bytesConsumed))
                    return false;

                i += bytesConsumed;

                if (builder is not null)
                {
                    if (rune.TryGetEscapeCharacter(out var escapeChar))
                    {
                        builder.Append('\\');
                        builder.Append(escapeChar);
                    }
                    else
                    {
                        builder.Append(rune.ToString());
                    }
                }
            }

            return true;
        }

        private static bool TryGetNextRune(ImmutableArray<IOperation> arrayCreationElements, int startIndex, out Rune rune, out int bytesConsumed)
        {
            rune = default;
            bytesConsumed = 0;

            // We only need max 4 elements for a single Rune
            var length = Math.Min(arrayCreationElements.Length - startIndex, 4);

            Span<byte> array = stackalloc byte[length];
            for (var i = 0; i < length; i++)
            {
                var element = arrayCreationElements[startIndex + i];

                // First basic check is that the array element is actually a byte
                if (element.ConstantValue.Value is not byte b)
                    return false;

                array[i] = b;
            }

            // If we can't decode a rune from the array then it can't be represented as a string
            return Rune.DecodeFromUtf8(array, out rune, out bytesConsumed) == System.Buffers.OperationStatus.Done;
        }

        private static async Task<Document> ConvertToUTF8StringLiteralAsync(Document document, SyntaxNode node, ImmutableArray<IOperation> arrayCreationElements, CancellationToken cancellationToken)
        {
            // Get our list of bytes from the array elements
            using var _ = PooledStringBuilder.GetInstance(out var builder);
            builder.Capacity = arrayCreationElements.Length;
            if (!TryConvertToUTF8String(builder, arrayCreationElements))
            {
                // We shouldn't get here, because the code fix shouldn't ask for a string value
                // if the analyzer couldn't convert it
                throw ExceptionUtilities.Unreachable;
            }

            var stringValue = builder.ToString();

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var editor = new SyntaxEditor(root, document.Project.Solution.Workspace.Services);

            editor.ReplaceNode(node, CreateUTF8StringLiteralToArrayInvocation(stringValue).WithTriviaFrom(node));

            var newRoot = editor.GetChangedRoot();
            return document.WithSyntaxRoot(newRoot);
        }

        private static InvocationExpressionSyntax CreateUTF8StringLiteralToArrayInvocation(string stringValue)
        {
            // Create the "goo"u8
            var literal = SyntaxFactory.Token(
                    leading: SyntaxTriviaList.Empty,
                    kind: SyntaxKind.UTF8StringLiteralToken,
                    text: QuoteCharacter + stringValue + QuoteCharacter + Suffix,
                    valueText: "",
                    trailing: SyntaxTriviaList.Empty);

            // Call .ToArray() on it
            return SyntaxFactory.InvocationExpression(
                     SyntaxFactory.MemberAccessExpression(
                         SyntaxKind.SimpleMemberAccessExpression,
                         SyntaxFactory.LiteralExpression(SyntaxKind.UTF8StringLiteralExpression, literal),
                         SyntaxFactory.IdentifierName(nameof(ReadOnlySpan<byte>.ToArray))));
        }
    }
}
