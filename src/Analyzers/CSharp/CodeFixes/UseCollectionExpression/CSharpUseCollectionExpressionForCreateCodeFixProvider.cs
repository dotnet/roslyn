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
using Microsoft.CodeAnalysis.UseCollectionExpression;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseCollectionExpression;

using static CSharpCollectionExpressionRewriter;
using static SyntaxFactory;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseCollectionExpressionForCreate), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal partial class CSharpUseCollectionExpressionForCreateCodeFixProvider()
    : AbstractUseCollectionExpressionCodeFixProvider<InvocationExpressionSyntax>(
        CSharpCodeFixesResources.Use_collection_expression,
        IDEDiagnosticIds.UseCollectionExpressionForCreateDiagnosticId)
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = [IDEDiagnosticIds.UseCollectionExpressionForCreateDiagnosticId];

    protected override async Task FixAsync(
        Document document,
        SyntaxEditor editor,
        InvocationExpressionSyntax invocationExpression,
        ImmutableDictionary<string, string?> properties,
        CancellationToken cancellationToken)
    {
        var unwrapArgument = properties.ContainsKey(CSharpUseCollectionExpressionForCreateDiagnosticAnalyzer.UnwrapArgument);

        // We want to replace `XXX.Create(...)` with the new collection expression.  To do this, we go through the
        // following steps.  First, we replace `XXX.Create(a, b, c)` with `new(a, b, c)` (a dummy object creation
        // expression). We then call into our helper which replaces expressions with collection expressions.  The reason
        // for the dummy object creation expression is that it serves as an actual node the rewriting code can attach an
        // initializer to, by which it can figure out appropriate wrapping and indentation for the collection expression
        // elements.

        var semanticDocument = await SemanticDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        // Get the expressions that we're going to fill the new collection expression with.
        var arguments = UseCollectionExpressionHelpers.GetArguments(invocationExpression, unwrapArgument);

        var dummyObjectAnnotation = new SyntaxAnnotation();
        var dummyObjectCreation = ImplicitObjectCreationExpression(ArgumentList(arguments), initializer: null)
            .WithTriviaFrom(invocationExpression)
            .WithAdditionalAnnotations(dummyObjectAnnotation);

        var newSemanticDocument = await semanticDocument.WithSyntaxRootAsync(
            semanticDocument.Root.ReplaceNode(invocationExpression, dummyObjectCreation), cancellationToken).ConfigureAwait(false);
        dummyObjectCreation = (ImplicitObjectCreationExpressionSyntax)newSemanticDocument.Root.GetAnnotatedNodes(dummyObjectAnnotation).Single();
        var expressions = dummyObjectCreation.ArgumentList.Arguments.Select(a => a.Expression);
        var matches = expressions.SelectAsArray(static e => new CollectionExpressionMatch<ExpressionSyntax>(e, UseSpread: false));

        var collectionExpression = await CreateCollectionExpressionAsync(
            newSemanticDocument.Document,
            dummyObjectCreation,
            matches,
            static o => o.Initializer,
            static (o, i) => o.WithInitializer(i),
            cancellationToken).ConfigureAwait(false);

        editor.ReplaceNode(invocationExpression, collectionExpression);
    }
}
