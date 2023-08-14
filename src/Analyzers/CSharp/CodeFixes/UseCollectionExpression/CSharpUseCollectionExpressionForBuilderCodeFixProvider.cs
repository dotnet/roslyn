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
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.UseCollectionInitializer;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.UseCollectionExpression;

using static SyntaxFactory;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseCollectionExpressionForBuilder), Shared]
internal partial class CSharpUseCollectionExpressionForBuilderCodeFixProvider
    : ForkingSyntaxEditorBasedCodeFixProvider<InvocationExpressionSyntax>
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpUseCollectionExpressionForBuilderCodeFixProvider()
        : base(CSharpCodeFixesResources.Use_collection_expression,
               IDEDiagnosticIds.UseCollectionExpressionForBuilderDiagnosticId)
    {
    }

    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(IDEDiagnosticIds.UseCollectionExpressionForBuilderDiagnosticId);

    protected override async Task FixAsync(
        Document document,
        SyntaxEditor editor,
        CodeActionOptionsProvider fallbackOptions,
        InvocationExpressionSyntax invocationExpression,
        ImmutableDictionary<string, string?> properties,
        CancellationToken cancellationToken)
    {
        var semanticDocument = await SemanticDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        var matchOpt = CSharpUseCollectionExpressionForBuilderDiagnosticAnalyzer.AnalyzeInvocation(
            semanticDocument.SemanticModel, invocationExpression, cancellationToken);
        if (matchOpt is not { } fullMatch)
            return;

        // Get the expressions that we're going to fill the new collection expression with.
        var arguments = GetArguments(fullMatch.Matches);

        var dummyObjectAnnotation = new SyntaxAnnotation();
        var dummyObjectCreation = ImplicitObjectCreationExpression(ArgumentList(arguments), initializer: null)
            .WithTriviaFrom(invocationExpression)
            .WithAdditionalAnnotations(dummyObjectAnnotation);

        var newSemanticDocument = await semanticDocument.WithSyntaxRootAsync(
            semanticDocument.Root.ReplaceNode(invocationExpression, dummyObjectCreation), cancellationToken).ConfigureAwait(false);
        dummyObjectCreation = (ImplicitObjectCreationExpressionSyntax)newSemanticDocument.Root.GetAnnotatedNodes(dummyObjectAnnotation).Single();

        // Grab the moved expressions and create the final match-list to pass to the rewriter. Importantly, add the
        // original information about if we're making spread elements or not based on the original analysis.
        var expressions = dummyObjectCreation.ArgumentList.Arguments.Select(a => a.Expression);
        var matches = expressions.Zip(fullMatch.Matches).SelectAsArray(static t => new CollectionExpressionMatch<ExpressionSyntax>(t.First, t.Second.UseSpread));

        var collectionExpression = await CSharpCollectionExpressionRewriter.CreateCollectionExpressionAsync(
            newSemanticDocument.Document,
            fallbackOptions,
            dummyObjectCreation,
            matches,
            static o => o.Initializer,
            static (o, i) => o.WithInitializer(i),
            cancellationToken).ConfigureAwait(false);

        // Remove the actual declaration of the builder.
        editor.RemoveNode(fullMatch.LocalDeclarationStatement);

        // Remove all the nodes mutating the builder.
        foreach (var (statement, _) in fullMatch.Matches)
            editor.RemoveNode(statement);

        // Finally, replace the invocation where we convert the builder to a collection with the new collection expression.
        editor.ReplaceNode(invocationExpression, collectionExpression);
    }

    private static SeparatedSyntaxList<ArgumentSyntax> GetArguments(ImmutableArray<Match<StatementSyntax>> matches)
    {
        using var _ = ArrayBuilder<SyntaxNodeOrToken>.GetInstance(out var nodesAndTokens);

        foreach (var match in matches)
        {
            var 
        }

        return SeparatedList<ArgumentSyntax>(NodeOrTokenList(nodesAndTokens));

        var arguments = invocationExpression.ArgumentList.Arguments;

        // If we're not unwrapping a singular argument expression, then just pass back all the explicit argument
        // expressions the user wrote out.
        if (!unwrapArgument)
            return arguments;

        Contract.ThrowIfTrue(arguments.Count != 1);
        var expression = arguments.Single().Expression;

        var initializer = expression switch
        {
            ImplicitArrayCreationExpressionSyntax implicitArray => implicitArray.Initializer,
            ImplicitStackAllocArrayCreationExpressionSyntax implicitStackAlloc => implicitStackAlloc.Initializer,
            ArrayCreationExpressionSyntax arrayCreation => arrayCreation.Initializer,
            StackAllocArrayCreationExpressionSyntax stackAllocCreation => stackAllocCreation.Initializer,
            ImplicitObjectCreationExpressionSyntax implicitObjectCreation => implicitObjectCreation.Initializer,
            ObjectCreationExpressionSyntax objectCreation => objectCreation.Initializer,
            _ => throw ExceptionUtilities.Unreachable(),
        };

        return initializer is null
            ? default
            : SeparatedList<ArgumentSyntax>(initializer.Expressions.GetWithSeparators().Select(
                nodeOrToken => nodeOrToken.IsToken ? nodeOrToken : Argument((ExpressionSyntax)nodeOrToken.AsNode()!)));
    }
}
