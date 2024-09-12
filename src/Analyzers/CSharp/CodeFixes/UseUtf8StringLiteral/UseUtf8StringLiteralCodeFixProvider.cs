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

namespace Microsoft.CodeAnalysis.CSharp.UseUtf8StringLiteral;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseUtf8StringLiteral), Shared]
internal sealed class UseUtf8StringLiteralCodeFixProvider : SyntaxEditorBasedCodeFixProvider
{
    private const char QuoteCharacter = '"';
    private const string Suffix = "u8";

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public UseUtf8StringLiteralCodeFixProvider()
    {
    }

    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        [IDEDiagnosticIds.UseUtf8StringLiteralDiagnosticId];

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        RegisterCodeFix(context, CSharpAnalyzersResources.Use_Utf8_string_literal, nameof(CSharpAnalyzersResources.Use_Utf8_string_literal));
        return Task.CompletedTask;
    }

    protected override async Task FixAllAsync(
        Document document, ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor, CodeActionOptionsProvider options, CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        var readOnlySpanType = semanticModel.Compilation.GetBestTypeByMetadataName(typeof(ReadOnlySpan<>).FullName!);
        // The analyzer wouldn't raise a diagnostic if this were null
        Contract.ThrowIfNull(readOnlySpanType);

        foreach (var diagnostic in diagnostics)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var node = diagnostic.Location.FindNode(getInnermostNodeForTie: true, cancellationToken);
            var arrayOp = GetArrayCreationOperation(semanticModel, diagnostic, cancellationToken);
            Contract.ThrowIfNull(arrayOp.Initializer);

            var stringValue = GetUtf8StringValueFromArrayInitializer(arrayOp.Initializer);

            // If our array is parented by a conversion to ReadOnlySpan<byte> then we don't want to call
            // ToArray after the string literal, or we'll be regressing perf.
            var isConvertedToReadOnlySpan = arrayOp.Parent is IConversionOperation conversion &&
                conversion.Type is INamedTypeSymbol { IsGenericType: true } namedType &&
                namedType.OriginalDefinition.Equals(readOnlySpanType) &&
                namedType.TypeArguments[0].SpecialType == SpecialType.System_Byte;

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
                editor.ReplaceNode(node, CreateArgumentListWithUtf8String(argumentList, diagnostic.Location, stringValue, isConvertedToReadOnlySpan));
            }
            else
            {
                editor.ReplaceNode(node, CreateUtf8String(node, stringValue, isConvertedToReadOnlySpan));
            }
        }
    }

    private static IArrayCreationOperation GetArrayCreationOperation(SemanticModel semanticModel, Diagnostic diagnostic, CancellationToken cancellationToken)
    {
        // For computing the UTF-8 string we need the original location of the array creation
        // operation, which is stored in additional locations.
        var location = diagnostic.AdditionalLocations[0];
        var node = location.FindNode(getInnermostNodeForTie: true, cancellationToken);

        var operation = semanticModel.GetRequiredOperation(node, cancellationToken);

        var operationLocationString = diagnostic.Properties[nameof(UseUtf8StringLiteralDiagnosticAnalyzer.ArrayCreationOperationLocation)];
        if (!Enum.TryParse(operationLocationString, out UseUtf8StringLiteralDiagnosticAnalyzer.ArrayCreationOperationLocation operationLocation))
            throw ExceptionUtilities.Unreachable();

        // Because we get the location from an IOperation.Syntax, sometimes we have to look a
        // little harder to get back from syntax to the operation that triggered the diagnostic
        if (operationLocation == UseUtf8StringLiteralDiagnosticAnalyzer.ArrayCreationOperationLocation.Ancestors)
        {
            // For collection initializers where the Add method takes a param array, and the array creation
            // will be a parent of the operation
            return FindArrayCreationOperationAncestor(operation);
        }
        else if (operationLocation == UseUtf8StringLiteralDiagnosticAnalyzer.ArrayCreationOperationLocation.Descendants)
        {
            // Otherwise, we must have an implicit array creation for a parameter array, so the location
            // will be the invocation, or similar, that has the argument, and we need to descend child
            // nodes to find the one we are interested in. To make sure we're finding the right one,
            // we can use the diagnostic location for that, since the analyzer raises it on the first element.
            return operation.DescendantsAndSelf()
                .OfType<IArrayCreationOperation>()
                .Where(a => a.Initializer?.ElementValues.FirstOrDefault()?.Syntax.SpanStart == diagnostic.Location.SourceSpan.Start)
                .First();
        }

        return (IArrayCreationOperation)operation;

        static IArrayCreationOperation FindArrayCreationOperationAncestor(IOperation operation)
        {
            while (operation is not null)
            {
                if (operation is IArrayCreationOperation arrayOperation)
                    return arrayOperation;

                operation = operation.Parent!;
            }

            throw ExceptionUtilities.Unreachable();
        }
    }

    private static string GetUtf8StringValueFromArrayInitializer(IArrayInitializerOperation initializer)
    {
        // Get our list of bytes from the array elements
        using var _ = PooledStringBuilder.GetInstance(out var builder);
        builder.Capacity = initializer.ElementValues.Length;

        // Can never fail as the analyzer already validated this would work.
        Contract.ThrowIfFalse(UseUtf8StringLiteralDiagnosticAnalyzer.TryConvertToUtf8String(builder, initializer.ElementValues));

        return builder.ToString();
    }

    private static SyntaxNode CreateArgumentListWithUtf8String(BaseArgumentListSyntax argumentList, Location location, string stringValue, bool isConvertedToReadOnlySpan)
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
                var stringLiteral = CreateUtf8String(SyntaxTriviaList.Empty, stringValue, argumentList.Arguments.Last().GetTrailingTrivia(), isConvertedToReadOnlySpan);
                arguments.Add(SyntaxFactory.Argument(stringLiteral));
                break;
            }

            arguments.Add(argument);
        }

        return argumentList.WithArguments(SyntaxFactory.SeparatedList<ArgumentSyntax>(arguments));
    }

    private static ExpressionSyntax CreateUtf8String(SyntaxNode nodeToTakeTriviaFrom, string stringValue, bool isConvertedToReadOnlySpan)
    {
        return CreateUtf8String(nodeToTakeTriviaFrom.GetLeadingTrivia(), stringValue, nodeToTakeTriviaFrom.GetTrailingTrivia(), isConvertedToReadOnlySpan);
    }

    private static ExpressionSyntax CreateUtf8String(SyntaxTriviaList leadingTrivia, string stringValue, SyntaxTriviaList trailingTrivia, bool isConvertedToReadOnlySpan)
    {
        var stringLiteral = SyntaxFactory.LiteralExpression(SyntaxKind.Utf8StringLiteralExpression,
            SyntaxFactory.Token(
                leading: leadingTrivia,
                kind: SyntaxKind.Utf8StringLiteralToken,
                text: QuoteCharacter + stringValue + QuoteCharacter + Suffix,
                valueText: "",
                trailing: SyntaxTriviaList.Empty));

        if (isConvertedToReadOnlySpan)
        {
            return stringLiteral.WithTrailingTrivia(trailingTrivia);
        }

        // We're replacing a byte array with a ReadOnlySpan<byte>, so if that byte array wasn't originally being
        // converted to the same, then we need to call .ToArray() to get things back to a byte array.
        return SyntaxFactory.InvocationExpression(
                 SyntaxFactory.MemberAccessExpression(
                     SyntaxKind.SimpleMemberAccessExpression,
                     stringLiteral,
                     SyntaxFactory.IdentifierName(nameof(ReadOnlySpan<byte>.ToArray))))
               .WithTrailingTrivia(trailingTrivia);
    }
}
