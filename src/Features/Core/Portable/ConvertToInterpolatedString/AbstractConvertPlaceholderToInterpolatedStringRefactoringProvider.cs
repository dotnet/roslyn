// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ConvertToInterpolatedString;

internal abstract class AbstractConvertPlaceholderToInterpolatedStringRefactoringProvider<
    TExpressionSyntax,
    TLiteralExpressionSyntax,
    TInvocationExpressionSyntax,
    TInterpolatedStringExpressionSyntax,
    TArgumentSyntax,
    TArgumentListExpressionSyntax,
    TInterpolationSyntax> : SyntaxEditorBasedCodeRefactoringProvider
    where TExpressionSyntax : SyntaxNode
    where TLiteralExpressionSyntax : TExpressionSyntax
    where TInvocationExpressionSyntax : TExpressionSyntax
    where TInterpolatedStringExpressionSyntax : TExpressionSyntax
    where TArgumentSyntax : SyntaxNode
    where TArgumentListExpressionSyntax : SyntaxNode
    where TInterpolationSyntax : SyntaxNode
{
    private readonly record struct InvocationData(
        TInvocationExpressionSyntax Invocation,
        TArgumentSyntax PlaceholderArgument,
        IMethodSymbol InvocationSymbol,
        TInterpolatedStringExpressionSyntax InterpolatedString,
        bool ShouldReplaceInvocation);

    protected abstract TExpressionSyntax ParseExpression(string text);

    protected override ImmutableArray<RefactorAllScope> SupportedRefactorAllScopes { get; } = AllRefactorAllScopes;

    private InvocationData? AnalyzeInvocation(
        Document document,
        SemanticModel semanticModel,
        TInvocationExpressionSyntax invocation,
        TArgumentSyntax placeholderArgument,
        CancellationToken cancellationToken)
    {
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

        var placeholderExpression = syntaxFacts.GetExpressionOfArgument(placeholderArgument);
        var stringToken = placeholderExpression.GetFirstToken();

        // don't offer if the string argument has errors in it, or if converting it to an interpolated string creates errors.
        if (stringToken.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error))
            return null;

        // Not supported if there are any omitted arguments following the placeholder.
        var arguments = syntaxFacts.GetArgumentsOfInvocationExpression(invocation);
        var placeholderIndex = arguments.IndexOf(placeholderArgument);
        Contract.ThrowIfTrue(placeholderIndex < 0);
        for (var i = placeholderIndex + 1; i < arguments.Count; i++)
        {
            var argument = arguments[i];
            if (syntaxFacts.GetExpressionOfArgument(argument) is null)
                return null;

            if (syntaxFacts.GetRefKindOfArgument(argument) != RefKind.None)
                return null;
        }

        if (semanticModel.GetSymbolInfo(invocation, cancellationToken).GetAnySymbol() is not IMethodSymbol invocationSymbol)
            return null;

        // If the user is actually passing an array to a params argument, we can't change this to be an interpolated string.
        // This includes explicit array creation expressions (new object[] {...}), implicit array creation expressions (new[] {...}),
        // and collection expressions ([...]).
        if (invocationSymbol.IsParams())
        {
            var lastArgument = syntaxFacts.GetArgumentsOfInvocationExpression(invocation).Last();
            var lastArgumentExpression = syntaxFacts.GetExpressionOfArgument(lastArgument);

            // Check if it's syntactically an array or collection expression
            if (syntaxFacts.IsArrayCreationExpression(lastArgumentExpression) ||
                lastArgumentExpression.RawKind == syntaxFacts.SyntaxKinds.ImplicitArrayCreationExpression)
            {
                return null;
            }

            // Also check for collection expressions (C# 12+)
            // Collection expressions are target-typed and may not have a natural Type, so we check the ConvertedType
            var typeInfo = semanticModel.GetTypeInfo(lastArgumentExpression, cancellationToken);
            if (typeInfo.Type is IArrayTypeSymbol || typeInfo.ConvertedType is IArrayTypeSymbol)
                return null;
        }

        // if the user is explicitly passing in a CultureInfo, don't offer as it's likely they want specialized
        // formatting for the values.
        foreach (var argument in arguments)
        {
            var type = semanticModel.GetTypeInfo(syntaxFacts.GetExpressionOfArgument(argument), cancellationToken).Type;
            if (type is { Name: nameof(CultureInfo), ContainingNamespace.Name: nameof(System.Globalization), ContainingNamespace.ContainingNamespace.Name: nameof(System) })
                return null;
        }

        if (ParseExpression("$" + stringToken.Text) is not TInterpolatedStringExpressionSyntax interpolatedString)
            return null;

        if (interpolatedString.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error))
            return null;

        var shouldReplaceInvocation = invocationSymbol is { ContainingType.SpecialType: SpecialType.System_String, Name: nameof(string.Format) };

        return new(invocation, placeholderArgument, invocationSymbol, interpolatedString, shouldReplaceInvocation);
    }

    public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
    {
        var (document, span, cancellationToken) = context;
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        var stringType = semanticModel.Compilation.GetSpecialType(SpecialType.System_String);
        if (stringType.IsErrorType())
            return;

        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

        var (invocation, placeholderArgument) = await TryFindInvocationAsync().ConfigureAwait(false);
        if (invocation is null || placeholderArgument is null)
            return;

        var data = this.AnalyzeInvocation(document, semanticModel, invocation, placeholderArgument, cancellationToken);
        if (data is null)
            return;

        context.RegisterRefactoring(
            CodeAction.Create(
                FeaturesResources.Convert_to_interpolated_string,
                cancellationToken => CreateInterpolatedStringAsync(document, data.Value, cancellationToken),
                nameof(FeaturesResources.Convert_to_interpolated_string)),
            invocation.Span);

        return;

        async Task<(TInvocationExpressionSyntax? invocation, TArgumentSyntax? placeholderArgument)> TryFindInvocationAsync()
        {
            // If selection is empty there can be multiple matching invocations (we can be deep in), need to go through all of them
            var invocations = await document.GetRelevantNodesAsync<TInvocationExpressionSyntax>(span, cancellationToken).ConfigureAwait(false);
            foreach (var invocation in invocations)
            {
                var placeholderArgument = FindValidPlaceholderArgument(syntaxFacts, invocation, cancellationToken);
                if (placeholderArgument != null)
                    return (invocation, placeholderArgument);
            }

            {
                // User selected a single argument of the invocation (expression / format string) instead of the whole invocation.
                var selectedArgument = await document.TryGetRelevantNodeAsync<TArgumentSyntax>(span, cancellationToken).ConfigureAwait(false);
                var invocation = selectedArgument?.Parent?.Parent as TInvocationExpressionSyntax;
                var placeholderArgument = FindValidPlaceholderArgument(syntaxFacts, invocation, cancellationToken);
                if (placeholderArgument != null)
                    return (invocation, placeholderArgument);
            }

            {
                // User selected the whole argument list: string format with placeholders plus all expressions
                var argumentList = await document.TryGetRelevantNodeAsync<TArgumentListExpressionSyntax>(span, cancellationToken).ConfigureAwait(false);
                var invocation = argumentList?.Parent as TInvocationExpressionSyntax;
                var placeholderArgument = FindValidPlaceholderArgument(syntaxFacts, invocation, cancellationToken);
                if (placeholderArgument != null)
                    return (invocation, placeholderArgument);
            }

            return default;
        }
    }

    private static TArgumentSyntax? FindValidPlaceholderArgument(
        ISyntaxFacts syntaxFacts, TInvocationExpressionSyntax? invocation, CancellationToken cancellationToken)
    {
        if (invocation != null)
        {
            // look for a string argument containing `"...{0}..."`, followed by more arguments.
            var arguments = (SeparatedSyntaxList<TArgumentSyntax>)syntaxFacts.GetArgumentsOfInvocationExpression(invocation);
            for (int i = 0, n = arguments.Count - 1; i < n; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var argument = arguments[i];
                var expression = syntaxFacts.GetExpressionOfArgument(argument);
                if (syntaxFacts.IsStringLiteralExpression(expression))
                {
                    var remainingArgCount = arguments.Count - i - 1;
                    Debug.Assert(remainingArgCount > 0);
                    var stringLiteralText = expression.GetFirstToken().Text;
                    if (stringLiteralText.Contains('{') && stringLiteralText.Contains('}'))
                    {
                        if (IsValidPlaceholderArgument(stringLiteralText, remainingArgCount))
                            return argument;
                    }
                }
            }
        }

        return null;

        bool IsValidPlaceholderArgument(string stringLiteralText, int remainingArgCount)
        {
            // See how many arguments follow the `"...{0}..."`.  We have to have a {0}, {1}, ... {N} part in the
            // string for each of them.  Note, those could be in any order.
            for (var i = 0; i < remainingArgCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var indexString = i.ToString(CultureInfo.InvariantCulture);
                if (!ContainsIndex(stringLiteralText, indexString))
                    return false;
            }

            return true;
        }

        bool ContainsIndex(string stringLiteralText, string indexString)
        {
            var currentLocation = -1;
            while (true)
            {
                currentLocation = stringLiteralText.IndexOf(indexString, currentLocation + 1);
                if (currentLocation < 0)
                    return false;

                if (currentLocation + 1 >= stringLiteralText.Length)
                    return false;

                // while we found the number, it was followed by another number.  so this isn't a valid match.  Keep looking.
                if (stringLiteralText[currentLocation + 1] is >= '0' and <= '9')
                    continue;

                // now check if the number is preceeded by whitespace and a '{'

                var lookbackLocation = currentLocation - 1;
                while (lookbackLocation > 0 && char.IsWhiteSpace(stringLiteralText[lookbackLocation]))
                    lookbackLocation--;

                if (stringLiteralText[lookbackLocation] != '{')
                {
                    // not a match, keep looking.
                    continue;
                }

                // success.  we found `{ N`
                return true;
            }
        }
    }

    protected override async Task RefactorAllAsync(
        Document document,
        ImmutableArray<TextSpan> fixAllSpans,
        SyntaxEditor editor,
        string? equivalenceKey,
        CancellationToken cancellationToken)
    {
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        var normalizedSpans = new NormalizedTextSpanCollection(fixAllSpans);

        using var _ = ArrayBuilder<SyntaxNode>.GetInstance(out var stack);
        stack.Push(root);

        while (stack.TryPop(out var node))
        {
            if (node is TInvocationExpressionSyntax invocation)
            {
                var placeholderArgument = FindValidPlaceholderArgument(syntaxFacts, invocation, cancellationToken);
                if (placeholderArgument is not null &&
                    AnalyzeInvocation(document, semanticModel, invocation, placeholderArgument, cancellationToken) is { } invocationData)
                {
                    ReplaceInvocation(editor, semanticModel, syntaxFacts, invocationData, cancellationToken);
                    continue;
                }
            }

            foreach (var child in node.ChildNodesAndTokens())
            {
                if (child.IsNode)
                {
                    var childNode = child.AsNode();
                    if (normalizedSpans.IntersectsWith(childNode!.Span))
                        stack.Push(childNode);
                }
            }
        }
    }

    private static async Task<Document> CreateInterpolatedStringAsync(
        Document document,
        InvocationData invocationData,
        CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        var syntaxGenerator = document.GetRequiredLanguageService<SyntaxGenerator>();
        var editor = new SyntaxEditor(root, syntaxGenerator);

        ReplaceInvocation(editor, semanticModel, syntaxFacts, invocationData, cancellationToken);

        return document.WithSyntaxRoot(editor.GetChangedRoot());
    }

    private static void ReplaceInvocation(
        SyntaxEditor editor,
        SemanticModel semanticModel,
        ISyntaxFacts syntaxFacts,
        InvocationData invocationData,
        CancellationToken cancellationToken)
    {
        var (invocation, placeholderArgument, invocationSymbol, interpolatedString, shouldReplaceInvocation) = invocationData;

        var arguments = (SeparatedSyntaxList<TArgumentSyntax>)syntaxFacts.GetArgumentsOfInvocationExpression(invocation);
        var literalExpression = (TLiteralExpressionSyntax?)syntaxFacts.GetExpressionOfArgument(placeholderArgument);
        Contract.ThrowIfNull(literalExpression);

        var syntaxGenerator = editor.Generator;

        var newInterpolatedString =
            InsertArgumentsIntoInterpolatedString(
                ExpandArgumentExpressions(
                    GetReorderedArgumentsAfterPlaceholderArgument()));

        var replacementNode = shouldReplaceInvocation
            ? newInterpolatedString
            : syntaxGenerator.InvocationExpression(
                syntaxFacts.GetExpressionOfInvocationExpression(invocation),
                newInterpolatedString);

        editor.ReplaceNode(invocation, replacementNode.WithTriviaFrom(invocation));
        return;

        ImmutableArray<TArgumentSyntax> GetReorderedArgumentsAfterPlaceholderArgument()
        {
            var placeholderIndex = arguments.IndexOf(placeholderArgument);
            Contract.ThrowIfTrue(placeholderIndex < 0);

            var afterPlaceholderArguments = arguments.Skip(placeholderIndex + 1).ToImmutableArray();
            var unnamedArguments = afterPlaceholderArguments.TakeWhile(a => !syntaxFacts.IsNamedArgument(a)).ToImmutableArray();
            var namedAndUnnamedArguments = afterPlaceholderArguments.Skip(unnamedArguments.Length).ToImmutableArray();

            // if all the remaining arguments are named, then sort by name in the original member so that the index
            // finds the right one.  If not all the arguments are named, then they must be in the right order
            // already (the language requires this), so no need to sort.
            if (namedAndUnnamedArguments.All(syntaxFacts.IsNamedArgument))
            {
                namedAndUnnamedArguments = namedAndUnnamedArguments.Sort((arg1, arg2) =>
                {
                    var arg1Name = syntaxFacts.GetNameForArgument(arg1);
                    var arg2Name = syntaxFacts.GetNameForArgument(arg2);

                    var param1 = invocationSymbol.Parameters.FirstOrDefault(p => syntaxFacts.StringComparer.Equals(p.Name, arg1Name));
                    var param2 = invocationSymbol.Parameters.FirstOrDefault(p => syntaxFacts.StringComparer.Equals(p.Name, arg2Name));

                    // Couldn't find the corresponding parameter.  No way to know how to order these.  Keep in original order
                    if (param1 is null || param2 is null)
                        return namedAndUnnamedArguments.IndexOf(arg1) - namedAndUnnamedArguments.IndexOf(arg2);

                    return param1.Ordinal - param2.Ordinal;
                });
            }

            return [.. unnamedArguments, .. namedAndUnnamedArguments];
        }

        ImmutableArray<TExpressionSyntax> ExpandArgumentExpressions(ImmutableArray<TArgumentSyntax> argumentsAfterPlaceholder)
        {
            using var _ = ArrayBuilder<TExpressionSyntax>.GetInstance(out var builder);
            foreach (var argument in argumentsAfterPlaceholder)
            {
                var argumentExpression = syntaxFacts.GetExpressionOfArgument(argument);
                var convertedType = semanticModel.GetTypeInfo(argumentExpression, cancellationToken).ConvertedType;

                builder.Add(convertedType is null
                    ? (TExpressionSyntax)syntaxGenerator.AddParentheses(argumentExpression)
                    : (TExpressionSyntax)syntaxGenerator.CastExpression(convertedType, syntaxGenerator.AddParentheses(argumentExpression)));
            }

            return builder.ToImmutableAndClear();
        }

        TExpressionSyntax InsertArgumentsIntoInterpolatedString(ImmutableArray<TExpressionSyntax> expressions)
        {
            return interpolatedString.ReplaceNodes(syntaxFacts.GetContentsOfInterpolatedString(interpolatedString), (oldNode, newNode) =>
            {
                if (newNode is TInterpolationSyntax interpolation)
                {
                    if (syntaxFacts.GetExpressionOfInterpolation(interpolation) is TLiteralExpressionSyntax literalExpression &&
                        syntaxFacts.IsNumericLiteralExpression(literalExpression) &&
                        int.TryParse(literalExpression.GetFirstToken().ValueText, out var index) &&
                        index >= 0 && index < expressions.Length)
                    {
                        return interpolation.ReplaceNode(
                            literalExpression,
                            syntaxFacts.ConvertToSingleLine(expressions[index], useElasticTrivia: true).WithAdditionalAnnotations(Formatter.Annotation));
                    }
                }

                return newNode;
            });
        }
    }
}
